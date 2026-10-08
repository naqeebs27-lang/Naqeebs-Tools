using System.Drawing.Imaging;

namespace SmartTools.Services;

/// <summary>Code 39 encoder + renderer (no external library needed).</summary>
public static class Code39
{
    // Each pattern is 9 elements: bar, space, bar, space, bar, space, bar, space, bar.  n = narrow, w = wide.
    private static readonly Dictionary<char, string> Patterns = new Dictionary<char, string>
    {
        ['0'] = "nnnwwnwnn", ['1'] = "wnnwnnnnw", ['2'] = "nnwwnnnnw", ['3'] = "wnwwnnnnn",
        ['4'] = "nnnwwnnnw", ['5'] = "wnnwwnnnn", ['6'] = "nnwwwnnnn", ['7'] = "nnnwnnwnw",
        ['8'] = "wnnwnnwnn", ['9'] = "nnwwnnwnn",
        ['A'] = "wnnnnwnnw", ['B'] = "nnwnnwnnw", ['C'] = "wnwnnwnnn", ['D'] = "nnnnwwnnw",
        ['E'] = "wnnnwwnnn", ['F'] = "nnwnwwnnn", ['G'] = "nnnnnwwnw", ['H'] = "wnnnnwwnn",
        ['I'] = "nnwnnwwnn", ['J'] = "nnnnwwwnn", ['K'] = "wnnnnnnww", ['L'] = "nnwnnnnww",
        ['M'] = "wnwnnnnwn", ['N'] = "nnnnwnnww", ['O'] = "wnnnwnnwn", ['P'] = "nnwnwnnwn",
        ['Q'] = "nnnnnnwww", ['R'] = "wnnnnnwwn", ['S'] = "nnwnnnwwn", ['T'] = "nnnnwnwwn",
        ['U'] = "wwnnnnnnw", ['V'] = "nwwnnnnnw", ['W'] = "wwwnnnnnn", ['X'] = "nwnnwnnnw",
        ['Y'] = "wwnnwnnnn", ['Z'] = "nwwnwnnnn",
        ['-'] = "nwnnnnwnw", ['.'] = "wwnnnnwnn", [' '] = "nwwnnnwnn", ['*'] = "nwnnwnwnn",
        ['$'] = "nwnwnwnnn", ['/'] = "nwnwnnnwn", ['+'] = "nwnnnwnwn", ['%'] = "nnnwnwnwn"
    };

    public const string SupportedCharacters = "0-9  A-Z  - . space $ / + %";

    /// <summary>Returns the first character that cannot be encoded, or null when all are valid.</summary>
    public static char? FindInvalid(string upperData)
    {
        foreach (char c in upperData)
        {
            if (c == '*' || !Patterns.ContainsKey(c)) return c;
        }
        return null;
    }

    /// <summary>Renders the barcode. moduleWidth is in CSS-style pixels at 96 DPI and is scaled by dpi/96.</summary>
    public static Bitmap Render(string data, double moduleWidth, int dpi)
    {
        string text = string.IsNullOrEmpty(data) ? " " : data.ToUpperInvariant();
        char? bad = FindInvalid(text);
        if (bad.HasValue)
        {
            throw new InvalidOperationException("Character '" + bad.Value + "' is not supported by Code 39.");
        }

        float scale = dpi / 96f;
        int narrow = Math.Max(1, (int)Math.Round(moduleWidth * scale));
        int wide = narrow * 2;
        int barHeight = (int)Math.Round(80 * scale);
        int margin = 10;
        float fontPx = 16 * scale;

        string full = "*" + text + "*";
        int barsWidth = 0;
        foreach (char c in full)
        {
            foreach (char el in Patterns[c])
            {
                barsWidth += el == 'w' ? wide : narrow;
            }
        }
        barsWidth += (full.Length - 1) * narrow; // inter-character gaps

        using var font = new Font("Consolas", fontPx, FontStyle.Regular, GraphicsUnit.Pixel);
        Size textSize = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        int textHeight = textSize.Height + 4;

        int totalWidth = Math.Max(barsWidth, textSize.Width) + 2 * margin;
        int totalHeight = margin * 2 + barHeight + textHeight;
        if (totalWidth > 16000 || totalHeight > 16000)
        {
            throw new InvalidOperationException("The barcode is too large. Use less data or a smaller module width.");
        }

        var bmp = new Bitmap(totalWidth, totalHeight, PixelFormat.Format24bppRgb);
        bmp.SetResolution(dpi, dpi);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            using var black = new SolidBrush(Color.Black);

            int x = (totalWidth - barsWidth) / 2;
            for (int ci = 0; ci < full.Length; ci++)
            {
                string pattern = Patterns[full[ci]];
                for (int i = 0; i < pattern.Length; i++)
                {
                    int w = pattern[i] == 'w' ? wide : narrow;
                    bool isBar = i % 2 == 0;
                    if (isBar)
                    {
                        g.FillRectangle(black, x, margin, w, barHeight);
                    }
                    x += w;
                }
                x += narrow; // gap between characters
            }

            TextRenderer.DrawText(g, text, font, new Rectangle(0, margin + barHeight + 2, totalWidth, textHeight),
                Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
        }
        return bmp;
    }
}
