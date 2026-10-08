using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using SmartTools.UI;

namespace SmartTools.Tools;

/// <summary>Everything the report window / printer / CSV export needs, already formatted as text.</summary>
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

public static class LoanReportPrinter
{
    public static PrintDocument CreateDocument(ReportData d)
    {
        var doc = new PrintDocument { DocumentName = "Loan Estimate Report" };
        doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        doc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);

        int rowIndex = 0;
        bool firstPage = true;

        doc.BeginPrint += (s, e) => { rowIndex = 0; firstPage = true; };
        doc.PrintPage += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var m = e.MarginBounds;
            float x = m.Left;
            float y = m.Top;
            float w = m.Width;

            using var titleFont = new Font("Segoe UI", 17f, FontStyle.Bold);
            using var smallFont = new Font("Segoe UI", 8.5f);
            using var labelFont = new Font("Segoe UI", 8f);
            using var valueFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var headFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var cellFont = new Font("Segoe UI", 9f);
            using var gray = new SolidBrush(Theme.TextMuted);
            using var dark = new SolidBrush(Theme.TextPrimary);
            using var blue = new SolidBrush(Theme.Accent);

            if (firstPage)
            {
                var logo = Branding.Logo;
                if (logo != null)
                {
                    float lh = 60f;
                    float lw = lh * logo.Width / logo.Height;
                    g.DrawImage(logo, x + w - lw, y - 6, lw, lh);
                }
                g.DrawString("LOAN ESTIMATE & AMORTIZATION REPORT", titleFont, dark, x, y);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                y += titleFont.GetHeight(g) + 2;
                g.DrawString(d.Generated, smallFont, gray, x, y);
                y += smallFont.GetHeight(g) + 10;
                using (var pen = new Pen(Theme.TextPrimary, 2f)) g.DrawLine(pen, x, y, x + w, y);
                y += 16;

                string[] labels = { "Loan Amount", "Interest Rate", "Tenure", "Monthly Payable EMI" };
                string[] values = { d.Amount, d.Rate, d.Tenure, d.Emi };
                float colW = w / 4f;
                for (int i = 0; i < 4; i++)
                {
                    g.DrawString(labels[i], labelFont, gray, x + i * colW, y);
                    g.DrawString(values[i], valueFont, i == 3 ? blue : dark, x + i * colW, y + labelFont.GetHeight(g));
                }
                y += labelFont.GetHeight(g) + valueFont.GetHeight(g) + 14;

                var band = new RectangleF(x, y, w, 56);
                using (var bandBrush = new SolidBrush(Theme.Dark)) g.FillRectangle(bandBrush, band);
                using (var light = new SolidBrush(Theme.TextLight))
                using (var amber = new SolidBrush(Theme.Warning))
                using (var green = new SolidBrush(Theme.FromHex("#34D399")))
                {
                    g.DrawString("Total Interest Payable", labelFont, light, x + 12, y + 8);
                    g.DrawString(d.Interest, valueFont, amber, x + 12, y + 24);
                    g.DrawString("Total Amount Payable", labelFont, light, x + w / 2f + 12, y + 8);
                    g.DrawString(d.Total, valueFont, green, x + w / 2f + 12, y + 24);
                }
                y += 56 + 20;
                g.DrawString("Amortization Schedule Breakdown", headFont, dark, x, y);
                y += headFont.GetHeight(g) + 8;
                firstPage = false;
            }

            float[] frac = { 0.12f, 0.22f, 0.22f, 0.22f, 0.22f };
            string[] heads = { "Year", "Principal Paid", "Interest Paid", "Total Payment", "Remaining Balance" };
            float headerH = headFont.GetHeight(g) + 10;
            float rowH = cellFont.GetHeight(g) + 8;

            using (var headBack = new SolidBrush(Theme.Neutral)) g.FillRectangle(headBack, x, y, w, headerH);
            DrawRow(g, heads, frac, headFont, dark, x, y + 5, w);
            y += headerH;

            using var line = new Pen(Theme.Border);
            while (rowIndex < d.Rows.Count && y + rowH <= m.Bottom)
            {
                DrawRow(g, d.Rows[rowIndex], frac, cellFont, dark, x, y + 4, w);
                g.DrawLine(line, x, y + rowH, x + w, y + rowH);
                y += rowH;
                rowIndex++;
            }

            e.HasMorePages = rowIndex < d.Rows.Count;
        };
        return doc;
    }

    private static void DrawRow(Graphics g, string[] cells, float[] frac, Font font, Brush brush, float x, float y, float w)
    {
        float cx = x;
        for (int i = 0; i < cells.Length; i++)
        {
            float cw = w * frac[i];
            var rect = new RectangleF(cx + 6, y, cw - 12, font.GetHeight(g) + 2);
            var fmt = new StringFormat
            {
                Alignment = i == 0 ? StringAlignment.Near : StringAlignment.Far,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            g.DrawString(cells[i], font, brush, rect, fmt);
            fmt.Dispose();
            cx += cw;
        }
    }
}

/// <summary>Modal window that previews the loan report and exports it (PDF / print / copy / CSV).</summary>
public class LoanReportForm : Form
{
    private const string PdfPrinterName = "Microsoft Print to PDF";

    private readonly ReportData _data;
    private readonly Label _toast = Ui.Text("", Theme.BodyBold, Theme.Success);
    private readonly System.Windows.Forms.Timer _toastTimer = new System.Windows.Forms.Timer { Interval = 3500 };

    public LoanReportForm(ReportData data)
    {
        _data = data;

        Text = "Loan Estimate & Amortization Report";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(Theme.S(960), Theme.S(760));
        MinimumSize = new Size(Theme.S(760), Theme.S(560));
        BackColor = Theme.Background;
        Font = Theme.Body;
        ShowInTaskbar = false;
        Padding = Theme.P(24);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // header
        var head = Ui.Stack();
        head.AddRow(Ui.Text("LOAN ESTIMATE & AMORTIZATION REPORT", Theme.Large), 0, 2);
        head.AddRow(Ui.Text(data.Generated, Theme.Small, Theme.TextMuted), 0, 14);
        head.Dock = DockStyle.Fill;
        root.Controls.Add(head, 0, 0);

        // summary
        var leftCard = new CardPanel();
        leftCard.AddRow(Ui.KeyValue("Loan amount", Ui.Text(data.Amount, Theme.BodyBold)), 0, 8);
        leftCard.AddRow(Ui.KeyValue("Interest rate", Ui.Text(data.Rate, Theme.BodyBold)), 0, 8);
        leftCard.AddRow(Ui.KeyValue("Tenure", Ui.Text(data.Tenure, Theme.BodyBold)), 0, 0);

        var rightCard = new CardPanel { FillColor = Theme.Dark, BorderColor = Theme.Dark };
        rightCard.AddRow(Ui.KeyValue("Monthly payable EMI", Ui.Text(data.Emi, Theme.BodyBold, Color.White), Theme.SidebarText), 0, 8);
        rightCard.AddRow(Ui.KeyValue("Total interest", Ui.Text(data.Interest, Theme.BodyBold, Theme.Warning), Theme.SidebarText), 0, 8);
        rightCard.AddRow(Ui.KeyValue("Total payable", Ui.Text(data.Total, Theme.BodyBold, Theme.FromHex("#34D399")), Theme.SidebarText), 0, 0);

        var summary = Ui.TwoCards(leftCard, rightCard, 50f, 16);
        summary.Dock = DockStyle.Fill;
        summary.Margin = new Padding(0, 0, 0, Theme.S(14));
        root.Controls.Add(summary, 0, 1);

        // table
        var grid = ScheduleGrid.Create();
        grid.Dock = DockStyle.Fill;
        foreach (var r in data.Rows) grid.Rows.Add(r[0], r[1], r[2], r[3], r[4]);
        grid.ClearSelection();
        root.Controls.Add(grid, 0, 2);

        // buttons
        var bar = Ui.Columns(Ui.Pct(100f), Ui.Px(130), Ui.Px(110), Ui.Px(130), Ui.Px(130), Ui.Px(100));
        bar.Margin = new Padding(0, Theme.S(14), 0, 0);
        bar.Dock = DockStyle.Fill;

        _toast.AutoSize = false;
        _toast.TextAlign = ContentAlignment.MiddleLeft;
        _toast.Height = Theme.S(42);

        var pdf = ModernButton.Primary("Save PDF");
        var print = ModernButton.Secondary("Print");
        var copy = ModernButton.Secondary("Copy Text");
        var csv = ModernButton.Secondary("Save CSV");
        var close = ModernButton.Dark("Close");

        pdf.Click += (s, e) => SavePdf();
        print.Click += (s, e) => ShowPreview();
        copy.Click += (s, e) => CopyText();
        csv.Click += (s, e) => SaveCsv();
        close.Click += (s, e) => Close();

        bar.AddCell(_toast, 0, 0, 10);
        bar.AddCell(pdf, 1, 0, 8);
        bar.AddCell(print, 2, 0, 8);
        bar.AddCell(copy, 3, 0, 8);
        bar.AddCell(csv, 4, 0, 8);
        bar.AddCell(close, 5, 0, 0);
        root.Controls.Add(bar, 0, 3);

        Controls.Add(root);

        _toastTimer.Tick += (s, e) => { _toast.Text = ""; _toastTimer.Stop(); };
    }

    private void Toast(string message)
    {
        _toast.Text = message;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ShowPreview()
    {
        using var preview = new PrintPreviewDialog
        {
            Document = LoanReportPrinter.CreateDocument(_data),
            Width = Theme.S(1000),
            Height = Theme.S(800)
        };
        preview.ShowDialog(this);
    }

    private void SavePdf()
    {
        bool hasPdfPrinter = PrinterSettings.InstalledPrinters.Cast<string>()
            .Any(p => string.Equals(p, PdfPrinterName, StringComparison.OrdinalIgnoreCase));

        if (!hasPdfPrinter)
        {
            MessageBox.Show(this,
                "The 'Microsoft Print to PDF' printer is not installed on this PC.\n\n" +
                "The print preview will open instead - you can print from there with any PDF printer.",
                "Smart Tools Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowPreview();
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Save report as PDF",
            FileName = "Loan_Estimate_Report.pdf",
            Filter = "PDF document (*.pdf)|*.pdf",
            DefaultExt = "pdf"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var doc = LoanReportPrinter.CreateDocument(_data);
            doc.PrinterSettings.PrinterName = PdfPrinterName;
            doc.PrinterSettings.PrintToFile = true;
            doc.PrinterSettings.PrintFileName = dlg.FileName;
            doc.Print();
            Toast("PDF saved successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "PDF could not be created:\n" + ex.Message, "Smart Tools Suite",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("LOAN ESTIMATE & AMORTIZATION REPORT");
        sb.AppendLine("====================================");
        sb.AppendLine("Loan Amount: " + _data.Amount);
        sb.AppendLine("Interest Rate: " + _data.Rate);
        sb.AppendLine("Tenure: " + _data.Tenure);
        sb.AppendLine("Monthly Payable EMI: " + _data.Emi);
        sb.AppendLine("Total Interest: " + _data.Interest);
        sb.AppendLine("Total Payable Amount: " + _data.Total);
        sb.AppendLine();
        sb.AppendLine("YEAR-WISE AMORTIZATION SCHEDULE:");
        sb.AppendLine("Year | Principal Paid | Interest Paid | Total Payment | Remaining Balance");
        sb.AppendLine("-----------------------------------------------------------------");
        foreach (var r in _data.Rows)
        {
            sb.AppendLine(r[0] + " | " + r[1] + " | " + r[2] + " | " + r[3] + " | " + r[4]);
        }

        try
        {
            Clipboard.SetText(sb.ToString());
            Toast("Report summary copied to clipboard!");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not copy to the clipboard:\n" + ex.Message, "Smart Tools Suite",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveCsv()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Save schedule as CSV",
            FileName = "Loan_Amortization_Report.csv",
            Filter = "CSV file (*.csv)|*.csv",
            DefaultExt = "csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("Loan Amount," + _data.Amount.Replace(",", ""));
        sb.AppendLine("Interest Rate," + _data.Rate);
        sb.AppendLine("Tenure," + _data.Tenure);
        sb.AppendLine();
        sb.AppendLine("Year,Principal Paid,Interest Paid,Total Yearly Payment,Remaining Balance");
        foreach (var r in _data.Rows)
        {
            sb.AppendLine(string.Join(",", r.Select(c => "\"" + c + "\"")));
        }

        try
        {
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            Toast("CSV saved successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not save the file:\n" + ex.Message, "Smart Tools Suite",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _toastTimer.Dispose();
        base.Dispose(disposing);
    }
}
