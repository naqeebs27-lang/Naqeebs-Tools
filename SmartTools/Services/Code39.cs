using SkiaSharp;

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
    public static RasterImage Render(string data, double moduleWidth, int dpi)
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

        using var textPaint = new SKPaint
        {
            Typeface = SkFonts.Resolve(SkFonts.Mono, false),
            TextSize = fontPx,
            IsAntialias = true,
            Color = SKColors.Black
        };
        float textWidth = textPaint.MeasureText(text);
        SKFontMetrics metrics = textPaint.FontMetrics;
        int textHeight = (int)Math.Ceiling(metrics.Descent - metrics.Ascent) + 4;

        int totalWidth = Math.Max(barsWidth, (int)Math.Ceiling(textWidth)) + 2 * margin;
        int totalHeight = margin * 2 + barHeight + textHeight;
        if (totalWidth > 16000 || totalHeight > 16000)
        {
            throw new InvalidOperationException("The barcode is too large. Use less data or a smaller module width.");
        }

        var bmp = new SKBitmap(totalWidth, totalHeight);
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(SKColors.White);
            using var black = new SKPaint { Color = SKColors.Black, IsAntialias = false, Style = SKPaintStyle.Fill };

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
                        canvas.DrawRect(x, margin, w, barHeight, black);
                    }
                    x += w;
                }
                x += narrow; // gap between characters
            }

            float textX = (totalWidth - textWidth) / 2f;
            float baseline = margin + barHeight + 2 - metrics.Ascent;
            canvas.DrawText(text, textX, baseline, textPaint);
        }
        return new RasterImage(bmp, dpi);
    }
}
