# Pdf2Cbz

> **Convert PDF files to CBZ** — optimized for comics, manga and illustrated books.

A lightweight Windows desktop app that converts PDF files into CBZ archives (Comic Book ZIP), preserving original image quality whenever possible.

Created by **Metasharp**.

## How to support?

https://ko-fi.com/metasharp

## Features

- **Drag & drop** — Drop PDF files or entire folders (recursive scan of subfolders)
- **Raw JPEG extraction** — When a page is a single embedded JPEG, the original bytes are extracted as-is, with zero quality loss and no re-encoding
- **Smart composite rendering** — Pages with tiles, text overlays or mixed content are rendered at the optimal resolution, computed from an area-weighted average of tile DPIs (clamped 72–300 DPI)
- **Per-page resolution** — Each page is analyzed and rendered individually, correctly handling mixed portrait/landscape and varying page sizes (A4, A3, Italian format, double-page spreads…)
- **JPEG 97% output** — Non-raw pages are saved as JPEG at 97% quality for an excellent size/quality ratio (0 visual difference with 100%)
- **Auto page numbering** — Pages are named with zero-padded numbers adapted to the total page count (`01.jpg`, `001.jpg`, `0001.jpg`…)
- **CBZ output** — Images are packaged into a max compression ZIP with `.cbz` extension, placed next to the source PDF

## Screenshot

The application is a single window with a drop zone, a real-time log and a progress bar.

## Requirements

- Windows 10/11
- .NET 10 Runtime

## Build

```bash
dotnet build
```

## Usage

1. Launch `Pdf2Cbz.exe`
2. Drag & drop one or more `.pdf` files — or a folder containing PDFs
3. CBZ files are created alongside the source PDFs

## How it works

```
PDF page
  │
  ├─ Single image covering the page?
  │   ├─ JPEG encoded? → extract raw bytes (lossless, zero re-encoding)
  │   └─ Other format? → decode → JPEG 97%
  │
  └─ Composite (tiles, text…)?
      → Compute area-weighted DPI from image tiles
      → Render full page via PDFium at that DPI
      → Save as JPEG 97%
```

## Dependencies

| Package | Role |
|---------|------|
| [PdfPig](https://github.com/UglyToad/PdfPig) | PDF parsing & image extraction |
| [Docnet.Core](https://github.com/GowenGit/docnet) | PDF page rendering (PDFium wrapper) |

## License

MIT

## Statistics

GitHub Downloads stats : https://hanadigital.github.io/grev/?user=MetasharpNet&repo=Pdf2Cbz
