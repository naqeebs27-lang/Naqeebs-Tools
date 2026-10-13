using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public partial class SalaryTaxTool : ToolPage
{
    private const int NewestTaxYear = 2027;
    private const int OldestTaxYear = 2015;

    private double _lastIncome;
    private double _lastTax;
    private int _lastYear;

    public SalaryTaxTool()
    {
        InitializeComponent();

        var labels = new List<string>();
        for (int y = NewestTaxYear; y >= OldestTaxYear; y--)
        {
            labels.Add((y - 1) + " - " + y);
        }
        YearBox.SetItems(labels.ToArray());

        CalcButton.Click += async (s, e) => await CalculateAsync();
        PrintButton.Click += async (s, e) => await PrintReportAsync();

        MonthlyBox.TextChanged += (s, e) => SyncAnnual();
        MonthlyBox.EnterPressed += async (s, e) => await CalculateAsync();
        AnnualBox.EnterPressed += async (s, e) => await CalculateAsync();
    }

    private void SetResultVisible(bool visible)
    {
        HintText.IsVisible = !visible;
        MonthlyCaption.IsVisible = visible;
        MonthlyTaxText.IsVisible = visible;
        ResultDivider.IsVisible = visible;
        AnnualCaption.IsVisible = visible;
        AnnualTaxText.IsVisible = visible;
    }

    private void SyncAnnual()
    {
        if (Fmt.TryParse(MonthlyBox.Text, out double m))
        {
            double annual = Math.Round(m * 12, MidpointRounding.AwayFromZero);
            AnnualBox.Text = annual.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            AnnualBox.Text = "";
        }
    }

    private async Task CalculateAsync()
    {
        if (!Fmt.TryParse(AnnualBox.Text, out double income) || income <= 0)
        {
            await Warn("Please enter a valid salary amount.");
            return;
        }

        int year = NewestTaxYear - Math.Max(0, YearBox.SelectedIndex);
        double tax = TaxMath.SalariedAnnualTax(income, year);
        _lastIncome = income;
        _lastTax = tax;
        _lastYear = year;
        PrintButton.IsEnabled = true;

        AnnualTaxText.Text = "Rs. " + Fmt.Int(tax);
        MonthlyTaxText.Text = "Rs. " + Fmt.Int(tax / 12);
        SetResultVisible(true);
    }

    private async Task PrintReportAsync()
    {
        await ReportPrintHelper.ShowPreviewAsync(this, "Salaried Tax Report", new[]
        {
            "Salary details:",
            "Annual salary: Rs. " + Fmt.Int(_lastIncome),
            "Tax year: " + _lastYear,
            "",
            "Estimated tax:",
            "Annual tax: Rs. " + Fmt.Int(_lastTax),
            "Monthly tax: Rs. " + Fmt.Int(_lastTax / 12)
        });
    }
}
