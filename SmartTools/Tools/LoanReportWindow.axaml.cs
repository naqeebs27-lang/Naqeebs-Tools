using System.Text;
using Avalonia.Controls;
using Avalonia.Threading;
using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

/// <summary>Modal window that previews the loan report and exports it (PDF / print / copy / CSV).</summary>
public partial class LoanReportWindow : Window
{
    private readonly ReportData _data;
    private readonly DispatcherTimer _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3500) };

    public LoanReportWindow()
    {
        InitializeComponent();
    }

    public LoanReportWindow(ReportData data) : this()
    {
        _data = data;

        GeneratedText.Text = data.Generated;
        AmountText.Text = data.Amount;
        RateText.Text = data.Rate;
        TenureText.Text = data.Tenure;
        EmiText.Text = data.Emi;
        InterestText.Text = data.Interest;
        TotalText.Text = data.Total;

        var grid = ScheduleGrid.Create();
        grid.ItemsSource = data.Rows.Select(ScheduleLine.FromCells).ToList();
        GridHost.Child = grid;

        _toastTimer.Tick += (s, e) => { ToastText.Text = ""; _toastTimer.Stop(); };
        PdfButton.Click += async (s, e) => await SavePdfAsync();
        PrintButton.Click += async (s, e) => await ShowPreviewAsync();
        CopyButton.Click += async (s, e) => await CopyTextAsync();
        CsvButton.Click += async (s, e) => await SaveCsvAsync();
        CloseButton.Click += (s, e) => Close();
    }

    private void Toast(string message)
    {
        ToastText.Text = message;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private async Task ShowPreviewAsync()
    {
        await ReportPreviewWindow.ShowAsync(this, new LoanReportDocument(_data));
    }

    private async Task SavePdfAsync()
    {
        try
        {
            byte[] pdf = ReportExport.ToPdfBytes(new LoanReportDocument(_data));
            bool saved = await Files.SaveBytesAsync(this, "Save report as PDF", "Loan_Estimate_Report.pdf",
                "pdf", "PDF document", pdf);
            if (saved) Toast("PDF saved successfully!");
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(this, "PDF could not be created:\n" + ex.Message);
        }
    }

    private async Task CopyTextAsync()
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
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null) throw new InvalidOperationException("Clipboard is not available.");
            await clipboard.SetTextAsync(sb.ToString());
            Toast("Report summary copied to clipboard!");
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(this, "Could not copy to the clipboard:\n" + ex.Message);
        }
    }

    private async Task SaveCsvAsync()
    {
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
            var utf8 = new UTF8Encoding(true);
            byte[] preamble = utf8.GetPreamble();
            byte[] body = utf8.GetBytes(sb.ToString());
            var all = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, all, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, all, preamble.Length, body.Length);

            bool saved = await Files.SaveBytesAsync(this, "Save schedule as CSV", "Loan_Amortization_Report.csv",
                "csv", "CSV file", all);
            if (saved) Toast("CSV saved successfully!");
        }
        catch (Exception ex)
        {
            await Dialogs.ShowMessageAsync(this, "Could not save the file:\n" + ex.Message);
        }
    }
}
