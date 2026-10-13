using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using SkiaSharp;

namespace SmartTools.Services;

/// <summary>An opened PDF (kept in memory) that pages can be rendered from.</summary>
public sealed class PdfRenderDocument : IDisposable
{
    internal byte[] Bytes;
    internal IDocReader Reader;
    internal double ReaderScale;

    public uint PageCount { get; internal set; }

    public void Dispose()
    {
        Reader?.Dispose();
        Reader = null;
    }
}

/// <summary>
/// Renders PDF pages to PNG bytes with PDFium (Docnet.Core), which works on Windows and Linux.
/// Replaces the Windows-only Windows.Data.Pdf engine of the WinForms version.
/// </summary>
public static class PdfPageRenderer
{
    // PDFium is not thread safe - all calls go through this lock.
    private static readonly object Gate = new object();

    public static Task<PdfRenderDocument> OpenAsync(byte[] pdfBytes)
    {
        return Task.Run(() =>
        {
            lock (Gate)
            {
                using var reader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(1.0));
                return new PdfRenderDocument
                {
                    Bytes = pdfBytes,
                    PageCount = (uint)reader.GetPageCount()
                };
            }
        });
    }

    /// <summary>Renders one page (zero based index). scale 2.0 = twice the PDF's native 96-DPI size.</summary>
    public static Task<byte[]> RenderPageAsync(PdfRenderDocument doc, uint pageIndex, double scale)
    {
        return Task.Run(() =>
        {
            lock (Gate)
            {
                // Windows.Data.Pdf measured pages in 1/96 inch, PDFium in 1/72 inch: keep the same output size.
                double pdfScale = scale * 96.0 / 72.0;

                if (doc.Reader == null || Math.Abs(doc.ReaderScale - pdfScale) > 1e-9)
                {
                    doc.Reader?.Dispose();
                    doc.Reader = DocLib.Instance.GetDocReader(doc.Bytes, new PageDimensions(pdfScale));
                    doc.ReaderScale = pdfScale;
                }

                using var page = doc.Reader.GetPageReader((int)pageIndex);
                int width = page.GetPageWidth();
                int height = page.GetPageHeight();
                byte[] bgra = page.GetImage();
                return BgraToPng(bgra, width, height);
            }
        });
    }

    private static byte[] BgraToPng(byte[] bgra, int width, int height)
    {
        // flatten transparency onto white so every page has an opaque white background
        var pixels = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            int o = i * 4;
            int a = bgra[o + 3];
            if (a == 255)
            {
                pixels[o] = bgra[o];
                pixels[o + 1] = bgra[o + 1];
                pixels[o + 2] = bgra[o + 2];
            }
            else
            {
                pixels[o] = (byte)((bgra[o] * a + 255 * (255 - a) + 127) / 255);
                pixels[o + 1] = (byte)((bgra[o + 1] * a + 255 * (255 - a) + 127) / 255);
                pixels[o + 2] = (byte)((bgra[o + 2] * a + 255 * (255 - a) + 127) / 255);
            }
            pixels[o + 3] = 255;
        }

        using var bmp = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque));
        IntPtr dest = bmp.GetPixels();
        int rowBytes = bmp.RowBytes;
        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(pixels, y * width * 4, IntPtr.Add(dest, y * rowBytes), width * 4);
        }

        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
