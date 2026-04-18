namespace Pdf2Cbz
{
    public partial class FormMain : Form
    {
        private CancellationTokenSource? _cts;

        public FormMain()
        {
            InitializeComponent();
        }

        private void LblDropZone_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private async void LblDropZone_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
                return;

            // Collect all PDF files
            var pdfFiles = new List<string>();
            foreach (var path in paths)
            {
                if (File.Exists(path) && path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    pdfFiles.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    pdfFiles.AddRange(Directory.EnumerateFiles(path, "*.pdf", SearchOption.AllDirectories));
                }
            }

            if (pdfFiles.Count == 0)
            {
                Log("No PDF files found.");
                return;
            }

            lblDropZone.Enabled = false;
            lblDropZone.Text = "Converting...";
            progressBar.Maximum = pdfFiles.Count;
            progressBar.Value = 0;

            _cts = new CancellationTokenSource();

            int done = 0;
            foreach (var pdf in pdfFiles)
            {
                Log($"--- {Path.GetFileName(pdf)} ---");
                try
                {
                    await PdfConverter.ConvertAsync(pdf, Log, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log("Cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    Log($"ERROR: {ex.Message}");
                }

                done++;
                progressBar.Value = done;
            }

            lblDropZone.Enabled = true;
            lblDropZone.Text = "📁 Drop PDF files or folders here";
            Log($"=== Done: {done}/{pdfFiles.Count} files ===");
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => Log(message));
                return;
            }
            listBoxLog.Items.Add(message);
            listBoxLog.TopIndex = listBoxLog.Items.Count - 1;
        }
    }
}
