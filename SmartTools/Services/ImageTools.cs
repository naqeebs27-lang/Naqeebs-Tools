using Avalonia;
using SkiaSharp;
using SmartTools.UI;
using ZXing.Common;

namespace SmartTools.Services;

/// <summary>Turns a ZXing BitMatrix into a bitmap.</summary>
public static class MatrixRenderer
{
    public static RasterImage Render(BitMatrix matrix, int cellWidth, int cellHeight)
    {
        int w = matrix.Width * cellWidth;
        int h = matrix.Height * cellHeight;
        if (w <= 0 || h <= 0 || w > 20000 || h > 20000)
        {
            throw new InvalidOperationException("The generated barcode has an invalid size.");
        }

        var bmp = new SKBitmap(w, h);
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(SKColors.White);
            using var black = new SKPaint { Color = SKColors.Black, IsAntialias = false, Style = SKPaintStyle.Fill };

            for (int y = 0; y < matrix.Height; y++)
            {
                int x = 0;
                while (x < matrix.Width)
                {
                    if (!matrix[x, y])
                    {
                        x++;
                        continue;
                    }
                    int start = x;
                    while (x < matrix.Width && matrix[x, y]) x++;
                    canvas.DrawRect(start * cellWidth, y * cellHeight, (x - start) * cellWidth, cellHeight, black);
                }
            }
        }
        return new RasterImage(bmp);
    }
}

/// <summary>Save helpers shared by all image-producing tools.</summary>
public static class ImageExport
{
    /// <summary>Maps "JPG"/"JPEG"/"GIF"/anything else to JPEG / GIF / PNG.</summary>
    public static string Normalize(string name)
    {
        switch ((name ?? "").ToUpperInvariant())
        {
            case "JPEG":
            case "JPG":
                return "JPEG";
            case "GIF":
                return "GIF";
            default:
                return "PNG";
        }
    }

    public static string ExtensionFor(string name)
    {
        switch (Normalize(name))
        {
            case "JPEG":
                return "jpg";
            case "GIF":
                return "gif";
            default:
                return "png";
        }
    }

    /// <summary>Shows a Save dialog and writes the image. Returns true when a file was written.</summary>
    public static async Task<bool> SaveWithDialogAsync(Visual owner, RasterImage image, string baseName, string formatName)
    {
        if (image == null)
        {
            await Dialogs.ShowMessageAsync(owner, "Generate a code first.");
            return false;
        }

        string ext = ExtensionFor(formatName);
        try
        {
            byte[] data = image.Encode(formatName);
            return await Files.SaveBytesAsync(owner, "Save image",
                baseName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "." + ext,
                ext, ext.ToUpperInvariant() + " image", data);
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(owner, "Could not save the file:\n" + ex.Message);
            return false;
        }
    }
}
