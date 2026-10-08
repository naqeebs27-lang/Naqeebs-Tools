using System.Drawing.Printing;

namespace SmartTools.Services;

public static class ReportPrintHelper
{
    public static void ShowPreview(IWin32Window owner, string title, IReadOnlyList<string> lines)
    {
        using var document = CreateDocument(title, lines);
        using var preview = new PrintPreviewDialog
        {
            Document = document,
            UseAntiAlias = true,
            Width = 1000,
            Height = 700,
            StartPosition = FormStartPosition.CenterParent
        };
        preview.ShowDialog(owner);
    }

    private static PrintDocument CreateDocument(string title, IReadOnlyList<string> lines)
    {
        var document = new PrintDocument
        {
            DocumentName = title
        };
        document.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        document.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
        document.PrintPage += (s, e) => PrintPage(e, title, lines);
        return document;
    }

    private static void PrintPage(PrintPageEventArgs e, string title, IReadOnlyList<string> lines)
    {
        var g = e.Graphics;
        float x = e.MarginBounds.Left;
        float y = e.MarginBounds.Top;

        using var titleFont = new Font("Segoe UI", 18f, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 11f);
        using var linePen = new Pen(Color.LightGray, 1f);
        using var titleBrush = new SolidBrush(Color.FromArgb(30, 41, 59));
        using var bodyBrush = new SolidBrush(Color.FromArgb(51, 65, 85));
        using var accentBrush = new SolidBrush(Color.FromArgb(37, 99, 235));

        g.DrawString(title.ToUpperInvariant(), titleFont, titleBrush, x, y);
        y += titleFont.GetHeight(g) + 12;
        g.DrawLine(linePen, x, y, e.MarginBounds.Right, y);
        y += 18;

        foreach (string line in lines)
        {
            if (line.Length == 0)
            {
                y += bodyFont.GetHeight(g);
                continue;
            }

            bool heading = line.EndsWith(":", StringComparison.Ordinal);
            g.DrawString(heading ? "" : "•", bodyFont, accentBrush, x, y);
            g.DrawString(line, heading ? titleFont : bodyFont, heading ? titleBrush : bodyBrush,
                heading ? x : x + 18, y);
            y += (heading ? titleFont : bodyFont).GetHeight(g) + (heading ? 10 : 6);
        }

        e.HasMorePages = false;
    }
}
