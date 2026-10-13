using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public partial class BusinessTaxTool : ToolPage
{
    private const int NewestTaxYear = 2027;
    private const int YearCount = 5;

    private double _lastIncome;
    private TaxMath.BusinessTaxResult _lastResult;
    private int _lastYear;

    public BusinessTaxTool()
    {
        InitializeComponent();

        var labels = new List<string>();
        for (int i = 0; i < YearCount; i++)
        {
            int y = NewestTaxYear - i;
            labels.Add("Tax Year " + y + " (Finance Act " + (y - 1) + ")");
        }
        YearBox.SetItems(labels.ToArray());

        CalcButton.Click += async (s, e) => await CalculateAsync();
        IncomeBox.EnterPressed += async (s, e) => await CalculateAsync();
        PrintButton.Click += async (s, e) => await PrintReportAsync();
    }

    private void SetResultVisible(bool visible)
    {
        HintText.IsVisible = !visible;
        CaptionText.IsVisible = visible;
        TaxText.IsVisible = visible;
        NoteText.IsVisible = visible;
    }

    private async Task CalculateAsync()
    {
        if (!Fmt.TryParse(IncomeBox.Text, out double income) || income <= 0)
        {
            await Warn("Please enter a valid income amount.");
            return;
        }

        int year = NewestTaxYear - Math.Max(0, YearBox.SelectedIndex);
        TaxMath.BusinessTaxResult r = TaxMath.BusinessAnnualTax(income, year);
        _lastIncome = income;
        _lastResult = r;
        _lastYear = year;
        PrintButton.IsEnabled = true;

        TaxText.Text = "Rs. " + Fmt.Int(r.Tax);
        NoteText.Text = r.Note;
        SetResultVisible(true);
    }

    private async Task PrintReportAsync()
    {
        await ReportPrintHelper.ShowPreviewAsync(this, "Business Tax Report", new[]
        {
            "Business income:",
            "Annual taxable income: Rs. " + Fmt.Int(_lastIncome),
            "Tax year: " + _lastYear,
            "",
            "Estimated tax:",
            "Tax payable: Rs. " + Fmt.Int(_lastResult.Tax),
            _lastResult.Note
        });
    }
}
