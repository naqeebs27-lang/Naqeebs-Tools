using SkiaSharp;
using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

/// <summary>Everything the report window / PDF / CSV export needs, already formatted as text.</summary>
public class ReportData
{
    public string Generated = "";
    public string Amount = "";
    public string Rate = "";
    public string Tenure = "";
    public string Emi = "";
    public string Interest = "";
    public string Total = "";
    public List<string[]> Rows = new List<string[]>();   // Year, Principal, Interest, Payment, Balance
}

/// <summary>Loan estimate &amp; amortisation report (was LoanReportPrinter / PrintDocument). Flows over several pages.</summary>
public sealed class LoanReportDocument : ReportDocument
{
    private readonly ReportData _d;

    public LoanReportDocument(ReportData data)
    {
        Title = "Loan Estimate Report";
        FileName = "Loan_Estimate_Report";
        _d = data;
    }

    public override void Render(IPageSink sink)
    {
        using var titleFont = new RFont(17f, true);
        using var smallFont = new RFont(8.5f);
        using var labelFont = new RFont(8f);
        using var valueFont = new RFont(11f, true);
        using var headFont = new RFont(9f, true);
        using var cellFont = new RFont(9f);

        var gray = new SKColor(0x64, 0x74, 0x8B);
        var dark = new SKColor(0x0F, 0x17, 0x2A);
        var blue = new SKColor(0x25, 0x63, 0xEB);
        var neutral = new SKColor(0xEE, 0xF2, 0xF7);
        var border = new SKColor(0xE2, 0xE8, 0xF0);
        var light = new SKColor(0x94, 0xA3, 0xB8);
        var amber = new SKColor(0xF5, 0x9E, 0x0B);
        var green = new SKColor(0x34, 0xD3, 0x99);

        float x = ReportPage.Margin;
        float w = ReportPage.Width - 2 * ReportPage.Margin;
        float bottom = ReportPage.Height - ReportPage.Margin;

        float[] frac = { 0.12f, 0.22f, 0.22f, 0.22f, 0.22f };
        string[] heads = { "Year", "Principal Paid", "Interest Paid", "Total Payment", "Remaining Balance" };

        int rowIndex = 0;
        bool firstPage = true;

        do
        {
            var g = new ReportGraphics(sink.BeginPage());
            float y = ReportPage.Margin;

            if (firstPage)
            {
                SKBitmap logo = Branding.LogoSk;
                if (logo != null)
                {
                    float lh = 60f;
                    float lw = lh * logo.Width / logo.Height;
                    g.DrawImage(logo, x + w - lw, y - 6, lw, lh);
                }

                g.DrawString("LOAN ESTIMATE & AMORTIZATION REPORT", titleFont, dark, x, y);
                y += titleFont.Height + 2;
                g.DrawString(_d.Generated, smallFont, gray, x, y);
                y += smallFont.Height + 10;
                g.DrawLine(dark, 2f, x, y, x + w, y);
                y += 16;

                string[] labels = { "Loan Amount", "Interest Rate", "Tenure", "Monthly Payable EMI" };
                string[] values = { _d.Amount, _d.Rate, _d.Tenure, _d.Emi };
                float colW = w / 4f;
                for (int i = 0; i < 4; i++)
                {
                    g.DrawString(labels[i], labelFont, gray, x + i * colW, y);
                    g.DrawString(values[i], valueFont, i == 3 ? blue : dark, x + i * colW, y + labelFont.Height);
                }
                y += labelFont.Height + valueFont.Height + 14;

                g.FillRect(dark, x, y, w, 56);
                g.DrawString("Total Interest Payable", labelFont, light, x + 12, y + 8);
                g.DrawString(_d.Interest, valueFont, amber, x + 12, y + 24);
                g.DrawString("Total Amount Payable", labelFont, light, x + w / 2f + 12, y + 8);
                g.DrawString(_d.Total, valueFont, green, x + w / 2f + 12, y + 24);
                y += 56 + 20;

                g.DrawString("Amortization Schedule Breakdown", headFont, dark, x, y);
                y += headFont.Height + 8;
                firstPage = false;
            }

            float headerH = headFont.Height + 10;
            float rowH = cellFont.Height + 8;

            g.FillRect(neutral, x, y, w, headerH);
            DrawRow(g, heads, frac, headFont, dark, x, y + 5, w);
            y += headerH;

            while (rowIndex < _d.Rows.Count && y + rowH <= bottom)
            {
                DrawRow(g, _d.Rows[rowIndex], frac, cellFont, dark, x, y + 4, w);
                g.DrawLine(border, 1f, x, y + rowH, x + w, y + rowH);
                y += rowH;
                rowIndex++;
            }

            sink.EndPage();
        }
        while (rowIndex < _d.Rows.Count);
    }

    private static void DrawRow(ReportGraphics g, string[] cells, float[] frac, RFont font, SKColor color, float x, float y, float w)
    {
        float cx = x;
        for (int i = 0; i < cells.Length; i++)
        {
            float cw = w * frac[i];
            g.DrawStringFit(cells[i], font, color, cx + 6, y, cw - 12, i != 0);
            cx += cw;
        }
    }
}
