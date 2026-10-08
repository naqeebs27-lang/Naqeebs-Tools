using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace SmartTools.Services;

/// <summary>Renders PDF pages to PNG bytes using the Windows built-in PDF engine (Windows.Data.Pdf).</summary>
public static class PdfPageRenderer
{
    public static async Task<PdfDocument> OpenAsync(string path)
    {
        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
        return await PdfDocument.LoadFromFileAsync(file);
    }

    /// <summary>Renders one page (zero based index). scale 2.0 = twice the PDF's native 96-DPI size.</summary>
    public static async Task<byte[]> RenderPageAsync(PdfDocument doc, uint pageIndex, double scale)
    {
        using PdfPage page = doc.GetPage(pageIndex);

        var options = new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Max(1, Math.Round(page.Size.Width * scale)),
            DestinationHeight = (uint)Math.Max(1, Math.Round(page.Size.Height * scale))
        };

        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, options);

        uint size = (uint)stream.Size;
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync(size);
        var bytes = new byte[size];
        reader.ReadBytes(bytes);
        return bytes;
    }
}
