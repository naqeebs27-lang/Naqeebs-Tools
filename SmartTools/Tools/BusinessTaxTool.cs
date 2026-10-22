using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public class BusinessTaxTool : ToolPage
{
    private const int NewestTaxYear = 2027;
    private const int YearCount = 5;

    private readonly ModernTextBox _income = new ModernTextBox { NumericOnly = true, Prefix = "PKR", Placeholder = "e.g. 1200000" };
    private readonly ModernComboBox _year = new ModernComboBox();

    private readonly Label _hint = Ui.Text("Enter your annual taxable income and press Calculate.", Theme.Body, Theme.TextMuted);
    private readonly Label _caption = Ui.Text("Total estimated income tax payable", Theme.Caption, Theme.TextMuted);
    private readonly Label _tax = Ui.Text("Rs. 0", Theme.Big, Theme.Success);
    private readonly Label _note = Ui.Text("", Theme.Small, Theme.TextMuted);
    private readonly ModernButton _print = ModernButton.Secondary("Print / Preview");
    private double _lastIncome;
    private TaxMath.BusinessTaxResult _lastResult;
    private int _lastYear;

    public BusinessTaxTool() : base("Business Tax Calculator", "Estimate FBR income tax for business / non-salaried individuals and AOPs.")
    {
        var labels = new List<string>();
        for (int i = 0; i < YearCount; i++)
        {
            int y = NewestTaxYear - i;
            labels.Add("Tax Year " + y + " (Finance Act " + (y - 1) + ")");
        }
        _year.SetItems(labels.ToArray());

        var calc = ModernButton.Success("Calculate Tax");
        calc.Click += (s, e) => Calculate();
        _income.EnterPressed += (s, e) => Calculate();

        _print.Enabled = false;
        _print.Click += (s, e) => PrintReport();
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actions.Controls.Add(calc);
        actions.Controls.Add(_print);

        var inputs = new CardPanel();
        inputs.AddHeading("Business income");
        inputs.AddField("Annual taxable income (PKR)", _income, 8);
        inputs.AddField("Tax year", _year);
        inputs.AddRow(actions, 22, 0);

        _note.AutoSize = false;
        _note.Height = Theme.S(60);

        var results = new CardPanel();
        results.AddHeading("Estimated tax");
        results.AddRow(_hint, 8, 0);
        results.AddRow(_caption, 8, 0);
        results.AddRow(_tax, 2, 0);
        results.AddRow(_note, 10, 0);

        SetResultVisible(false);

        Stack.AddRow(Ui.TwoCards(inputs, results, 50f));
    }

    private void SetResultVisible(bool visible)
    {
        _hint.Visible = !visible;
        _caption.Visible = visible;
        _tax.Visible = visible;
        _note.Visible = visible;
    }

    private void Calculate()
    {
        if (!Fmt.TryParse(_income.Text, out double income) || income <= 0)
        {
            Warn("Please enter a valid income amount.");
            return;
        }

        int year = NewestTaxYear - Math.Max(0, _year.SelectedIndex);
        TaxMath.BusinessTaxResult r = TaxMath.BusinessAnnualTax(income, year);
        _lastIncome = income;
        _lastResult = r;
        _lastYear = year;
        _print.Enabled = true;

        _tax.Text = "Rs. " + Fmt.Int(r.Tax);
        _note.Text = r.Note;
        SetResultVisible(true);
    }

    private void PrintReport()
    {
        ReportPrintHelper.ShowPreview(FindForm(), "Business Tax Report", new[]
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
