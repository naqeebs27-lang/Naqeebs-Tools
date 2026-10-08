using System.Drawing.Imaging;
using ZXing.Common;

namespace SmartTools.Services;

/// <summary>Turns a ZXing BitMatrix into a bitmap.</summary>
public static class MatrixRenderer
{
    public static Bitmap Render(BitMatrix matrix, int cellWidth, int cellHeight)
    {
        int w = matrix.Width * cellWidth;
        int h = matrix.Height * cellHeight;
        if (w <= 0 || h <= 0 || w > 20000 || h > 20000)
        {
            throw new InvalidOperationException("The generated barcode has an invalid size.");
        }

        var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            using var black = new SolidBrush(Color.Black);

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
                    g.FillRectangle(black, start * cellWidth, y * cellHeight, (x - start) * cellWidth, cellHeight);
                }
            }
        }
        return bmp;
    }
}

/// <summary>Save helpers shared by all image-producing tools.</summary>
public static class ImageExport
{
    public static ImageFormat FormatFromName(string name)
    {
        switch ((name ?? "").ToUpperInvariant())
        {
            case "JPEG":
            case "JPG":
                return ImageFormat.Jpeg;
            case "GIF":
                return ImageFormat.Gif;
            default:
                return ImageFormat.Png;
        }
    }

    public static string ExtensionFor(string name)
    {
        switch ((name ?? "").ToUpperInvariant())
        {
            case "JPEG":
            case "JPG":
                return "jpg";
            case "GIF":
                return "gif";
            default:
                return "png";
        }
    }

    public static void Save(Bitmap bmp, string path, string formatName)
    {
        var format = FormatFromName(formatName);
        if (format.Equals(ImageFormat.Jpeg))
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
            bmp.Save(path, codec, parameters);
        }
        else
        {
            bmp.Save(path, format);
        }
    }

    /// <summary>Shows a Save dialog and writes the image. Returns true when a file was written.</summary>
    public static bool SaveWithDialog(IWin32Window owner, Bitmap bmp, string baseName, string formatName)
    {
        if (bmp == null)
        {
            MessageBox.Show(owner, "Generate a code first.", "Smart Tools Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        string ext = ExtensionFor(formatName);
        using var dlg = new SaveFileDialog
        {
            Title = "Save image",
            FileName = baseName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "." + ext,
            Filter = ext.ToUpperInvariant() + " image (*." + ext + ")|*." + ext,
            DefaultExt = ext,
            OverwritePrompt = true
        };
        if (dlg.ShowDialog(owner) != DialogResult.OK) return false;

        try
        {
            Save(bmp, dlg.FileName, formatName);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, "Could not save the file:\n" + ex.Message, "Smart Tools Suite",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
