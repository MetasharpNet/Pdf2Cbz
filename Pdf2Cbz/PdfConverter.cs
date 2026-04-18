using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pdf2Cbz;

public static class PdfConverter
{
    private static readonly ImageCodecInfo JpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
    private static readonly EncoderParameters JpegParams = new(1) { Param = { [0] = new EncoderParameter(Encoder.Quality, 97L) } };

    public static async Task ConvertAsync(string pdfPath, Action<string> log, CancellationToken ct)
    {
        await Task.Run(() => Convert(pdfPath, log, ct), ct);
    }

    private static void Convert(string pdfPath, Action<string> log, CancellationToken ct)
    {
        string cbzPath = Path.ChangeExtension(pdfPath, ".cbz");
        string tempDir = Path.Combine(Path.GetTempPath(), "Pdf2Cbz_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            using var pdf = PdfDocument.Open(pdfPath);
            int totalPages = pdf.NumberOfPages;
            int digits = totalPages switch
            {
                < 10 => 1,
                < 100 => 2,
                < 1000 => 3,
                _ => 4
            };

            // Analyze each page
            var pageData = new List<PageInfo>();

            for (int i = 1; i <= totalPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                var page = pdf.GetPage(i);
                var images = page.GetImages().ToList();

                bool hasText = !string.IsNullOrWhiteSpace(page.Text);

                if (!hasText && images.Count == 1)
                {
                    var img = images[0];
                    var bounds = img.BoundingBox;
                    double coverage = (bounds.Width * bounds.Height) / (page.Width * page.Height);
                    if (coverage > 0.85)
                    {
                        pageData.Add(new PageInfo(i, true, 0, page.Width, page.Height));
                        continue;
                    }
                }

                // Composite page: compute effective DPI as area-weighted average of tile DPIs
                double weightedDpiSum = 0;
                double totalArea = 0;
                foreach (var img in images)
                {
                    if (img.BoundingBox.Width > 0 && img.BoundingBox.Height > 0)
                    {
                        double dpiX = img.WidthInSamples / (img.BoundingBox.Width / 72.0);
                        double dpiY = img.HeightInSamples / (img.BoundingBox.Height / 72.0);
                        double tileDpi = Math.Max(dpiX, dpiY);
                        double area = img.BoundingBox.Width * img.BoundingBox.Height;
                        weightedDpiSum += tileDpi * area;
                        totalArea += area;
                    }
                }
                double effectiveDpi = totalArea > 0 ? weightedDpiSum / totalArea : 150;
                effectiveDpi = Math.Clamp(effectiveDpi, 72, 300);

                pageData.Add(new PageInfo(i, false, effectiveDpi, page.Width, page.Height));
            }

            // Pass 1: extract single-image pages directly
            for (int idx = 0; idx < pageData.Count; idx++)
            {
                ct.ThrowIfCancellationRequested();
                var info = pageData[idx];
                if (!info.SingleImage) continue;

                string fileName = info.PageNum.ToString().PadLeft(digits, '0') + ".jpg";
                string filePath = Path.Combine(tempDir, fileName);

                var page = pdf.GetPage(info.PageNum);
                var img = page.GetImages().First();

                // If the image is already JPEG-encoded in the PDF, save raw bytes directly
                var rawBytes = img.RawBytes.ToArray();
                if (rawBytes.Length >= 2 && rawBytes[0] == 0xFF && rawBytes[1] == 0xD8)
                {
                    SaveWithCropIfNeeded(rawBytes, img, page, filePath);
                    log($"Page {info.PageNum}/{totalPages}: extracted raw JPEG");
                    continue;
                }

                // Otherwise decode and re-encode as JPEG
                if (img.TryGetPng(out var pngBytes))
                {
                    SaveWithCropIfNeeded(pngBytes, img, page, filePath);
                    log($"Page {info.PageNum}/{totalPages}: extracted image");
                    continue;
                }

                // PNG extraction failed, fall back to rendering
                double dpi = 150;
                if (img.BoundingBox.Width > 0 && img.BoundingBox.Height > 0)
                {
                    dpi = Math.Clamp(Math.Max(
                        img.WidthInSamples / (img.BoundingBox.Width / 72.0),
                        img.HeightInSamples / (img.BoundingBox.Height / 72.0)), 72, 300);
                }
                pageData[idx] = new PageInfo(info.PageNum, false, dpi, info.PageWidth, info.PageHeight);
            }

            // Pass 2: render composite pages (and failed extractions)
            var compositePages = pageData.Where(p => !p.SingleImage).ToList();
            if (compositePages.Count > 0)
            {
                using var docLib = DocLib.Instance;

                foreach (var info in compositePages)
                {
                    ct.ThrowIfCancellationRequested();
                    string fileName = info.PageNum.ToString().PadLeft(digits, '0') + ".jpg";
                    string filePath = Path.Combine(tempDir, fileName);

                    // Compute per-page pixel dimensions from its own size and DPI
                    int pixW = (int)Math.Ceiling(info.PageWidth * info.Dpi / 72.0);
                    int pixH = (int)Math.Ceiling(info.PageHeight * info.Dpi / 72.0);
                    int dimSmall = Math.Min(pixW, pixH);
                    int dimLarge = Math.Max(pixW, pixH);

                    using var reader = docLib.GetDocReader(pdfPath, new PageDimensions(dimSmall, dimLarge));
                    using var pageReader = reader.GetPageReader(info.PageNum - 1);
                    int w = pageReader.GetPageWidth();
                    int h = pageReader.GetPageHeight();
                    var rawBytes = pageReader.GetImage();

                    using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                    var bmpData = bmp.LockBits(
                        new Rectangle(0, 0, w, h),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format32bppArgb);

                    Marshal.Copy(rawBytes, 0, bmpData.Scan0, Math.Min(rawBytes.Length, bmpData.Stride * h));
                    bmp.UnlockBits(bmpData);

                    // Draw onto a white background so transparent areas don't turn black in JPEG
                    using var final = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(final))
                    {
                        g.Clear(Color.White);
                        g.DrawImage(bmp, 0, 0, w, h);
                    }
                    final.Save(filePath, JpegCodec, JpegParams);

                    log($"Page {info.PageNum}/{totalPages}: rendered at {info.Dpi:F0} DPI ({w}x{h})");
                }
            }

            // Create CBZ
            if (File.Exists(cbzPath))
                File.Delete(cbzPath);

            ZipFile.CreateFromDirectory(tempDir, cbzPath, CompressionLevel.SmallestSize, false);
            log($"✓ Created: {Path.GetFileName(cbzPath)}");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    private static void SaveWithCropIfNeeded(byte[] imageBytes, IPdfImage img, Page page, string filePath)
    {
        using var ms = new MemoryStream(imageBytes);
        using var bmp = new Bitmap(ms);

        // Check if the image needs cropping: the raw image may be larger than the
        // visible area defined by the bounding box relative to the page.
        var bounds = img.BoundingBox;
        bool needsCrop = false;
        int cropX = 0, cropY = 0, cropW = bmp.Width, cropH = bmp.Height;

        if (bmp.Width > 0 && bmp.Height > 0 && bounds.Width > 0 && bounds.Height > 0)
        {
            // Compute the scale: how many pixels per PDF point
            double scaleX = bmp.Width / bounds.Width;
            double scaleY = bmp.Height / bounds.Height;

            // The bounding box bottom-left in page coordinates tells us the offset.
            // If the image extends beyond the page, we need to crop.
            // PDF origin is bottom-left; image origin is top-left.
            double visibleLeft = Math.Max(0, -bounds.Left) * scaleX;
            double visibleBottom = Math.Max(0, -bounds.Bottom) * scaleY;
            double visibleRight = Math.Max(0, bounds.Right - page.Width) * scaleX;
            double visibleTop = Math.Max(0, bounds.Top - page.Height) * scaleY;

            if (visibleLeft > 0.5 || visibleTop > 0.5 || visibleRight > 0.5 || visibleBottom > 0.5)
            {
                cropX = (int)Math.Round(visibleLeft);
                cropY = (int)Math.Round(visibleTop);
                cropW = bmp.Width - cropX - (int)Math.Round(visibleRight);
                cropH = bmp.Height - cropY - (int)Math.Round(visibleBottom);
                cropW = Math.Max(1, cropW);
                cropH = Math.Max(1, cropH);
                needsCrop = true;
            }
        }

        if (needsCrop)
        {
            using var cropped = bmp.Clone(new Rectangle(cropX, cropY, cropW, cropH), bmp.PixelFormat);
            cropped.Save(filePath, JpegCodec, JpegParams);
        }
        else
        {
            bmp.Save(filePath, JpegCodec, JpegParams);
        }
    }

    private record PageInfo(int PageNum, bool SingleImage, double Dpi, double PageWidth, double PageHeight);
}
