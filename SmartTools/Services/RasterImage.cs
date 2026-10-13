using System.Text;
using SkiaSharp;

namespace SmartTools.Services;

/// <summary>
/// A generated barcode / QR image plus its DPI. Replaces System.Drawing.Bitmap, which is Windows-only on .NET 8.
/// </summary>
public sealed class RasterImage : IDisposable
{
    public SKBitmap Bitmap { get; }
    public int Dpi { get; set; }

    public int Width => Bitmap.Width;
    public int Height => Bitmap.Height;

    public RasterImage(SKBitmap bitmap, int dpi = 96)
    {
        Bitmap = bitmap;
        Dpi = dpi;
    }

    public void Dispose()
    {
        Bitmap.Dispose();
    }

    /// <summary>Returns a new image rotated clockwise by 90 degrees x quarterTurns (the original is not changed).</summary>
    public RasterImage Rotated(int quarterTurns)
    {
        int q = ((quarterTurns % 4) + 4) % 4;
        int w = Bitmap.Width;
        int h = Bitmap.Height;
        bool swap = q % 2 == 1;

        var dst = new SKBitmap(swap ? h : w, swap ? w : h);
        using (var canvas = new SKCanvas(dst))
        {
            canvas.Clear(SKColors.White);
            switch (q)
            {
                case 1: canvas.Translate(h, 0); canvas.RotateDegrees(90); break;
                case 2: canvas.Translate(w, h); canvas.RotateDegrees(180); break;
                case 3: canvas.Translate(0, w); canvas.RotateDegrees(270); break;
            }
            canvas.DrawBitmap(Bitmap, 0, 0);
        }
        return new RasterImage(dst, Dpi);
    }

    /// <summary>Encodes as "PNG", "JPEG" or "GIF" (DPI is written into PNG and JPEG files).</summary>
    public byte[] Encode(string format)
    {
        switch (ImageExport.Normalize(format))
        {
            case "JPEG":
                return ImageMeta.JpegWithDpi(EncodeSkia(SKEncodedImageFormat.Jpeg, 95), Dpi);
            case "GIF":
                return GifEncoder.Encode(Bitmap);
            default:
                return ImageMeta.PngWithDpi(EncodeSkia(SKEncodedImageFormat.Png, 100), Dpi);
        }
    }

    /// <summary>Converts to an Avalonia bitmap for the on-screen preview.</summary>
    public Avalonia.Media.Imaging.Bitmap ToAvalonia()
    {
        using var ms = new MemoryStream(EncodeSkia(SKEncodedImageFormat.Png, 100));
        return new Avalonia.Media.Imaging.Bitmap(ms);
    }

    private byte[] EncodeSkia(SKEncodedImageFormat format, int quality)
    {
        using var image = SKImage.FromBitmap(Bitmap);
        using var data = image.Encode(format, quality);
        return data.ToArray();
    }
}

/// <summary>Writes the DPI into PNG (pHYs chunk) and JPEG (JFIF header) bytes.</summary>
internal static class ImageMeta
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }
            table[n] = c;
        }
        return table;
    }

    private static uint Crc32(byte[] buffer, int offset, int count)
    {
        uint c = 0xFFFFFFFFu;
        for (int i = 0; i < count; i++)
        {
            c = CrcTable[(c ^ buffer[offset + i]) & 0xFF] ^ (c >> 8);
        }
        return c ^ 0xFFFFFFFFu;
    }

    public static byte[] PngWithDpi(byte[] png, int dpi)
    {
        // PNG signature (8) + IHDR chunk (4 length + 4 type + 13 data + 4 crc = 25) = 33
        if (png.Length < 33 || png[0] != 0x89 || png[1] != 0x50 || png[12] != (byte)'I' || png[13] != (byte)'H')
        {
            return png;
        }

        uint ppm = (uint)Math.Round(dpi / 0.0254);

        var chunk = new byte[4 + 4 + 9 + 4];
        WriteBe(chunk, 0, 9);
        chunk[4] = (byte)'p'; chunk[5] = (byte)'H'; chunk[6] = (byte)'Y'; chunk[7] = (byte)'s';
        WriteBe(chunk, 8, ppm);
        WriteBe(chunk, 12, ppm);
        chunk[16] = 1; // unit: metre
        WriteBe(chunk, 17, Crc32(chunk, 4, 4 + 9));

        var result = new byte[png.Length + chunk.Length];
        Buffer.BlockCopy(png, 0, result, 0, 33);
        Buffer.BlockCopy(chunk, 0, result, 33, chunk.Length);
        Buffer.BlockCopy(png, 33, result, 33 + chunk.Length, png.Length - 33);
        return result;
    }

    public static byte[] JpegWithDpi(byte[] jpg, int dpi)
    {
        // SOI(2) APP0 marker(2) length(2) "JFIF\0"(5) version(2) units(1) Xdensity(2) Ydensity(2)
        if (jpg.Length > 18 && jpg[0] == 0xFF && jpg[1] == 0xD8 && jpg[2] == 0xFF && jpg[3] == 0xE0 &&
            jpg[6] == (byte)'J' && jpg[7] == (byte)'F' && jpg[8] == (byte)'I' && jpg[9] == (byte)'F')
        {
            jpg[13] = 1; // dots per inch
            jpg[14] = (byte)((dpi >> 8) & 0xFF);
            jpg[15] = (byte)(dpi & 0xFF);
            jpg[16] = (byte)((dpi >> 8) & 0xFF);
            jpg[17] = (byte)(dpi & 0xFF);
        }
        return jpg;
    }

    private static void WriteBe(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}

/// <summary>Minimal GIF89a writer for black/white images (Skia cannot encode GIF).</summary>
internal static class GifEncoder
{
    public static byte[] Encode(SKBitmap bmp)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        if (w <= 0 || h <= 0 || w > 65535 || h > 65535)
        {
            throw new InvalidOperationException("The image is too large for the GIF format.");
        }

        SKColor[] px = bmp.Pixels;
        var index = new byte[w * h];
        for (int i = 0; i < index.Length; i++)
        {
            SKColor c = px[i];
            index[i] = (byte)(((c.Red + c.Green + c.Blue) / 3) < 128 ? 0 : 1);
        }

        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("GIF89a"), 0, 6);
        WriteU16(ms, w);
        WriteU16(ms, h);
        ms.WriteByte(0x80);          // global colour table, 2 entries
        ms.WriteByte(0);             // background colour index
        ms.WriteByte(0);             // pixel aspect ratio
        ms.Write(new byte[] { 0, 0, 0, 255, 255, 255 }, 0, 6); // black, white

        ms.WriteByte(0x2C);          // image descriptor
        WriteU16(ms, 0);
        WriteU16(ms, 0);
        WriteU16(ms, w);
        WriteU16(ms, h);
        ms.WriteByte(0);

        const int minCodeSize = 2;
        ms.WriteByte(minCodeSize);

        byte[] lzw = Lzw(index, minCodeSize);
        for (int pos = 0; pos < lzw.Length; pos += 255)
        {
            int n = Math.Min(255, lzw.Length - pos);
            ms.WriteByte((byte)n);
            ms.Write(lzw, pos, n);
        }
        ms.WriteByte(0);             // end of image data
        ms.WriteByte(0x3B);          // trailer
        return ms.ToArray();
    }

    private static byte[] Lzw(byte[] pixels, int minCodeSize)
    {
        int clearCode = 1 << minCodeSize;
        int eoiCode = clearCode + 1;

        var output = new MemoryStream();
        int cur = 0;
        int nbits = 0;

        void Emit(int code, int size)
        {
            cur |= code << nbits;
            nbits += size;
            while (nbits >= 8)
            {
                output.WriteByte((byte)(cur & 0xFF));
                cur >>= 8;
                nbits -= 8;
            }
        }

        int codeSize = minCodeSize + 1;
        int nextCode = eoiCode + 1;
        var dict = new Dictionary<int, int>();

        Emit(clearCode, codeSize);
        int prefix = pixels[0];
        for (int i = 1; i < pixels.Length; i++)
        {
            int c = pixels[i];
            int key = (prefix << 8) | c;
            if (dict.TryGetValue(key, out int found))
            {
                prefix = found;
            }
            else
            {
                Emit(prefix, codeSize);
                dict[key] = nextCode;
                nextCode++;
                if (nextCode - 1 == (1 << codeSize) && codeSize < 12)
                {
                    codeSize++;
                }
                if (nextCode == 4097)
                {
                    Emit(clearCode, codeSize);
                    dict.Clear();
                    codeSize = minCodeSize + 1;
                    nextCode = eoiCode + 1;
                }
                prefix = c;
            }
        }
        Emit(prefix, codeSize);
        Emit(eoiCode, codeSize);
        if (nbits > 0)
        {
            output.WriteByte((byte)(cur & 0xFF));
        }
        return output.ToArray();
    }

    private static void WriteU16(Stream s, int value)
    {
        s.WriteByte((byte)(value & 0xFF));
        s.WriteByte((byte)((value >> 8) & 0xFF));
    }
}
