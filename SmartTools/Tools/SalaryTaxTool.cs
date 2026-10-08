using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public class SalaryTaxTool : ToolPage
{
    private const int NewestTaxYear = 2027;
    private const int OldestTaxYear = 2015;

    private readonly ModernTextBox _monthly = new ModernTextBox { NumericOnly = true, Prefix = "PKR", Placeholder = "Enter monthly salary" };
    private readonly ModernTextBox _annual = new ModernTextBox { NumericOnly = true, Prefix = "PKR", Placeholder = "Total annual salary" };
    private readonly ModernComboBox _year = new ModernComboBox();

    private readonly Label _hint = Ui.Text("Enter a salary and press Calculate.", Theme.Body, Theme.TextMuted);
    private readonly Label _monthlyTax = Ui.Text("", Theme.Big, Theme.Accent);
    private readonly Label _annualTax = Ui.Text("", Theme.Large, Theme.TextPrimary);
    private readonly ModernButton _print = ModernButton.Secondary("Print / Preview");
    private readonly Control[] _resultControls;
    private double _lastIncome;
    private double _lastTax;
    private int _lastYear;

    public SalaryTaxTool() : base("Salaried Tax Calculator", "Estimate income tax for a salaried person in Pakistan, tax years 2015 - 2027.")
    {
        var labels = new List<string>();
        for (int y = NewestTaxYear; y >= OldestTaxYear; y--)
        {
            labels.Add((y - 1) + " - " + y);
        }
        _year.SetItems(labels.ToArray());

        var calc = ModernButton.Primary("Calculate");
        calc.Click += (s, e) => Calculate();

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
        inputs.AddHeading("Salary details", "Type the monthly amount and the annual figure fills in automatically");
        inputs.AddField("Monthly salary (PKR)", _monthly, 8);
        inputs.AddField("Annual salary (PKR)", _annual);
        inputs.AddField("Tax year", _year);
        inputs.AddRow(actions, 22, 0);

        var monthlyCaption = Ui.Text("Monthly tax", Theme.Caption, Theme.TextMuted);
        var divider = Ui.Divider();
        var annualCaption = Ui.Text("Annual tax", Theme.Caption, Theme.TextMuted);
        _resultControls = new Control[] { monthlyCaption, _monthlyTax, divider, annualCaption, _annualTax };

        var results = new CardPanel();
        results.AddHeading("Estimated tax");
        results.AddRow(_hint, 8, 0);
        results.AddRow(monthlyCaption, 8, 0);
        results.AddRow(_monthlyTax, 2, 0);
        results.AddRow(divider, 14, 14);
        results.AddRow(annualCaption, 0, 0);
        results.AddRow(_annualTax, 2, 0);

        SetResultVisible(false);

        _monthly.TextChanged += (s, e) => SyncAnnual();
        _monthly.EnterPressed += (s, e) => Calculate();
        _annual.EnterPressed += (s, e) => Calculate();

        Stack.AddRow(Ui.TwoCards(inputs, results, 50f));
    }

    private void SetResultVisible(bool visible)
    {
        _hint.Visible = !visible;
        foreach (var c in _resultControls) c.Visible = visible;
    }

    private void SyncAnnual()
    {
        if (Fmt.TryParse(_monthly.Text, out double m))
        {
            double annual = Math.Round(m * 12, MidpointRounding.AwayFromZero);
            _annual.Text = annual.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            _annual.Text = "";
        }
    }

    private void Calculate()
    {
        if (!Fmt.TryParse(_annual.Text, out double income) || income <= 0)
        {
            Warn("Please enter a valid salary amount.");
            return;
        }

        int year = NewestTaxYear - Math.Max(0, _year.SelectedIndex);
        double tax = TaxMath.SalariedAnnualTax(income, year);
        _lastIncome = income;
        _lastTax = tax;
        _lastYear = year;
        _print.Enabled = true;

        _annualTax.Text = "Rs. " + Fmt.Int(tax);
        _monthlyTax.Text = "Rs. " + Fmt.Int(tax / 12);
        SetResultVisible(true);
    }

    private void PrintReport()
    {
        ReportPrintHelper.ShowPreview(FindForm(), "Salaried Tax Report", new[]
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
