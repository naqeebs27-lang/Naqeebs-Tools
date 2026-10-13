using System.Diagnostics;
using System.Globalization;
using SkiaSharp;

namespace SmartTools.Services;

/// <summary>Receives the pages a report draws (a PDF file or on-screen bitmaps).</summary>
public interface IPageSink
{
    SKCanvas BeginPage();
    void EndPage();
}

/// <summary>A4 page expressed in 1/100 inch - the same units the old PrintDocument layouts used.</summary>
public static class ReportPage
{
    public const float Width = 827f;
    public const float Height = 1169f;
    public const float Margin = 50f;
}

/// <summary>Font with GDI-like metrics. Size is given in points; layout units are 1/100 inch.</summary>
public sealed class RFont : IDisposable
{
    public SKPaint Paint { get; }
    public float Ascent { get; }
    /// <summary>Line height (like Font.GetHeight in GDI+).</summary>
    public float Height { get; }

    public RFont(float points, bool bold = false)
    {
        Paint = new SKPaint
        {
            Typeface = SkFonts.Resolve(SkFonts.Sans, bold),
            TextSize = points * 100f / 72f,
            IsAntialias = true
        };
        SKFontMetrics m = Paint.FontMetrics;
        Ascent = -m.Ascent;
        Height = m.Descent - m.Ascent + m.Leading;
    }

    public void Dispose()
    {
        Paint.Dispose();
    }
}

/// <summary>Tiny drawing helper that mimics the few GDI+ calls the reports used.</summary>
public sealed class ReportGraphics
{
    public SKCanvas Canvas { get; }

    public ReportGraphics(SKCanvas canvas)
    {
        Canvas = canvas;
    }

    /// <summary>Draws text with its top-left corner at (x, y).</summary>
    public void DrawString(string text, RFont font, SKColor color, float x, float y)
    {
        if (string.IsNullOrEmpty(text)) return;
        font.Paint.Color = color;
        Canvas.DrawText(text, x, y + font.Ascent, font.Paint);
    }

    /// <summary>Single line inside a column: trimmed with an ellipsis, optionally right aligned.</summary>
    public void DrawStringFit(string text, RFont font, SKColor color, float x, float y, float width, bool alignRight)
    {
        if (string.IsNullOrEmpty(text)) return;
        string t = text;
        float w = font.Paint.MeasureText(t);
        if (w > width)
        {
            while (t.Length > 1 && font.Paint.MeasureText(t + "\u2026") > width)
            {
                t = t.Substring(0, t.Length - 1);
            }
            t += "\u2026";
            w = font.Paint.MeasureText(t);
        }
        DrawString(t, font, color, alignRight ? x + width - w : x, y);
    }

    public void DrawLine(SKColor color, float thickness, float x1, float y1, float x2, float y2)
    {
        using var p = new SKPaint { Color = color, StrokeWidth = thickness, IsAntialias = true, Style = SKPaintStyle.Stroke };
        Canvas.DrawLine(x1, y1, x2, y2, p);
    }

    public void FillRect(SKColor color, float x, float y, float w, float h)
    {
        using var p = new SKPaint { Color = color, Style = SKPaintStyle.Fill };
        Canvas.DrawRect(x, y, w, h, p);
    }

    public void DrawImage(SKBitmap bmp, float x, float y, float w, float h)
    {
        using var p = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        Canvas.DrawBitmap(bmp, new SKRect(x, y, x + w, y + h), p);
    }
}

/// <summary>A printable report. Render() calls sink.BeginPage()/EndPage() once per page.</summary>
public abstract class ReportDocument
{
    public string Title { get; protected set; } = "Report";
    public string FileName { get; protected set; } = "Report";

    public abstract void Render(IPageSink sink);
}

/// <summary>Exports a report to PDF or to page bitmaps (for the preview window).</summary>
public static class ReportExport
{
    public static void SavePdf(ReportDocument doc, string path)
    {
        using var document = SKDocument.CreatePdf(path);
        doc.Render(new PdfSink(document));
        document.Close();
    }

    /// <summary>Renders the report to PDF and returns the file bytes.</summary>
    public static byte[] ToPdfBytes(ReportDocument doc)
    {
        string tmp = Path.Combine(Path.GetTempPath(), "smarttools-" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            SavePdf(doc, tmp);
            return File.ReadAllBytes(tmp);
        }
        finally
        {
            try { File.Delete(tmp); } catch { /* temp file, ignore */ }
        }
    }

    /// <summary>Saves a temporary PDF and opens it in the system PDF viewer (print from there).</summary>
    public static void OpenInViewer(ReportDocument doc)
    {
        string path = Path.Combine(Path.GetTempPath(),
            doc.FileName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".pdf");
        SavePdf(doc, path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    /// <summary>Renders every page to a bitmap. pixelsPerUnit 1.5 = 150 DPI.</summary>
    public static List<SKBitmap> RenderPages(ReportDocument doc, float pixelsPerUnit)
    {
        var sink = new BitmapSink(pixelsPerUnit);
        doc.Render(sink);
        return sink.Pages;
    }

    private sealed class PdfSink : IPageSink
    {
        private readonly SKDocument _doc;
        public PdfSink(SKDocument doc) { _doc = doc; }

        public SKCanvas BeginPage()
        {
            SKCanvas canvas = _doc.BeginPage(595.28f, 841.89f); // A4 in points
            canvas.Scale(0.72f);                                  // 1/100 inch -> points
            return canvas;
        }

        public void EndPage()
        {
            _doc.EndPage();
        }
    }

    private sealed class BitmapSink : IPageSink
    {
        private readonly float _scale;
        private SKBitmap _bmp;
        private SKCanvas _canvas;
        public List<SKBitmap> Pages { get; } = new List<SKBitmap>();

        public BitmapSink(float scale) { _scale = scale; }

        public SKCanvas BeginPage()
        {
            _bmp = new SKBitmap((int)Math.Round(ReportPage.Width * _scale), (int)Math.Round(ReportPage.Height * _scale));
            _canvas = new SKCanvas(_bmp);
            _canvas.Clear(SKColors.White);
            _canvas.Scale(_scale);
            return _canvas;
        }

        public void EndPage()
        {
            _canvas.Dispose();
            _canvas = null;
            Pages.Add(_bmp);
            _bmp = null;
        }
    }
}

/// <summary>Simple "heading: / bullet" report used by Discount, tax and currency tools.</summary>
public sealed class TextReportDocument : ReportDocument
{
    private readonly IReadOnlyList<string> _lines;

    public TextReportDocument(string title, IReadOnlyList<string> lines)
    {
        Title = title;
        FileName = title.Replace(' ', '_');
        _lines = lines;
    }

    public override void Render(IPageSink sink)
    {
        var g = new ReportGraphics(sink.BeginPage());
        float x = ReportPage.Margin;
        float y = ReportPage.Margin;
        float right = ReportPage.Width - ReportPage.Margin;

        using var titleFont = new RFont(18f, true);
        using var bodyFont = new RFont(11f);
        var titleColor = new SKColor(30, 41, 59);
        var bodyColor = new SKColor(51, 65, 85);
        var accent = new SKColor(37, 99, 235);

        g.DrawString(Title.ToUpperInvariant(), titleFont, titleColor, x, y);
        y += titleFont.Height + 12;
        g.DrawLine(SKColors.LightGray, 1f, x, y, right, y);
        y += 18;

        foreach (string line in _lines)
        {
            if (line.Length == 0)
            {
                y += bodyFont.Height;
                continue;
            }

            bool heading = line.EndsWith(":", StringComparison.Ordinal);
            g.DrawString(heading ? "" : "\u2022", bodyFont, accent, x, y);
            g.DrawString(line, heading ? titleFont : bodyFont, heading ? titleColor : bodyColor,
                heading ? x : x + 18, y);
            y += (heading ? titleFont : bodyFont).Height + (heading ? 10 : 6);
        }

        sink.EndPage();
    }
}

/// <summary>Age calculator report (same layout as the old PrintPage handler).</summary>
public sealed class AgeReportDocument : ReportDocument
{
    private readonly DateTime _birth;
    private readonly DateTime _end;
    private readonly AgeResult _r;

    public AgeReportDocument(DateTime birth, DateTime end, AgeResult result)
    {
        Title = "Age Calculator Report";
        FileName = "Age_Calculator_Report";
        _birth = birth;
        _end = end;
        _r = result;
    }

    public override void Render(IPageSink sink)
    {
        var g = new ReportGraphics(sink.BeginPage());
        float x = ReportPage.Margin;
        float y = ReportPage.Margin;
        float right = ReportPage.Width - ReportPage.Margin;

        using var titleFont = new RFont(18f, true);
        using var headingFont = new RFont(12f, true);
        using var bodyFont = new RFont(11f);
        using var valueFont = new RFont(11f, true);
        var muted = new SKColor(0x64, 0x74, 0x8B);
        var text = new SKColor(0x0F, 0x17, 0x2A);
        var accent = new SKColor(0x25, 0x63, 0xEB);
        var line = new SKColor(0xE2, 0xE8, 0xF0);

        g.DrawString("AGE CALCULATOR REPORT", titleFont, text, x, y);
        y += titleFont.Height + 4;
        g.DrawString("Generated for the selected dates", bodyFont, muted, x, y);
        y += bodyFont.Height + 12;
        g.DrawLine(line, 1f, x, y, right, y);
        y += 18;

        g.DrawString("Date of birth", bodyFont, muted, x, y);
        g.DrawString(_birth.ToLongDateString(), valueFont, text, x + 180, y);
        y += valueFont.Height + 10;
        g.DrawString("Age at the date of", bodyFont, muted, x, y);
        g.DrawString(_end.ToLongDateString(), valueFont, text, x + 180, y);
        y += valueFont.Height + 24;

        g.DrawString("Exact age", headingFont, muted, x, y);
        y += headingFont.Height + 4;
        g.DrawString($"{_r.Years} years {_r.Months} months {_r.Days} days", titleFont, accent, x, y);
        y += titleFont.Height + 22;
        g.DrawLine(line, 1f, x, y, right, y);
        y += 18;

        g.DrawString("Detailed calculation", headingFont, text, x, y);
        y += headingFont.Height + 12;

        string[] rows =
        {
            $"{_r.TotalMonths:N0} months {_r.Days} days",
            $"{_r.Weeks:N0} weeks {_r.RemainingDays} days",
            $"{_r.TotalDays:N0} days",
            $"{_r.Hours:N0} hours",
            $"{_r.Minutes:N0} minutes",
            $"{_r.Seconds:N0} seconds"
        };
        foreach (string row in rows)
        {
            g.DrawString("\u2022", bodyFont, accent, x, y);
            g.DrawString(row, bodyFont, text, x + 18, y);
            y += bodyFont.Height + 6;
        }

        sink.EndPage();
    }
}
