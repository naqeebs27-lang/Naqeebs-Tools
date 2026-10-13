using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SmartTools.Services;
using SmartTools.UI;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools.Tools;

public partial class LoanCalculatorTool : ToolPage
{
    private static readonly string[] CurrencySymbols = { "PKR", "$", "\u20AC", "\u00A3", "AED", "\u20B9" };
    private static readonly string[] CurrencyNames =
    {
        "PKR (Rs.)", "$ (USD)", "\u20AC (EUR)", "\u00A3 (GBP)", "AED", "\u20B9 (INR)"
    };

    private const double DefaultSliderMax = 50000000;

    private readonly TextBlock[] _amountMarkers;
    private readonly TextBlock[] _tenureMarkers;
    private readonly Avalonia.Controls.DataGrid _grid = ScheduleGrid.Create();

    private bool _busy;
    private bool _years = true;
    private LoanMath.Result _result = new LoanMath.Result();

    public LoanCalculatorTool()
    {
        InitializeComponent();

        _amountMarkers = new[] { AmtM0, AmtM1, AmtM2, AmtM3 };
        _tenureMarkers = new[] { TenM0, TenM1, TenM2, TenM3 };

        // slider setup (order matters: widen the range before assigning the value)
        AmountSlider.Minimum = 50000;
        AmountSlider.Maximum = DefaultSliderMax;
        AmountSlider.Step = 50000;
        AmountSlider.Value = 5000000;

        RateSlider.Minimum = 0;
        RateSlider.Maximum = 30;
        RateSlider.Step = 0.1;
        RateSlider.Value = 16.5;
        RateSlider.AccentColor = ThemePalette.Success;

        TenureSlider.Minimum = 1;
        TenureSlider.Maximum = 30;
        TenureSlider.Step = 1;
        TenureSlider.Value = 20;
        TenureSlider.AccentColor = ThemePalette.Warning;

        CurrencyBox.SetItems(CurrencyNames);
        CurrencyBox.SelectedIndexChanged += (s, e) => RefreshCurrency();

        BuildPresets();
        BuildAmountChips();

        GridHost.Child = _grid;
        ExportButton.Click += async (s, e) => await OpenReportAsync();
        ToggleButton.Click += (s, e) => ToggleSchedule();
        YearsButton.Click += (s, e) => SetTenureType(true);
        MonthsButton.Click += (s, e) => SetTenureType(false);

        AmountBox.TextChanged += (s, e) => OnAmountTyped();
        AmountSlider.ValueChanged += (s, e) =>
        {
            if (_busy) return;
            _busy = true;
            AmountBox.Text = Fmt.Int(AmountSlider.Value);
            _busy = false;
            UpdateWords();
            Calculate();
        };

        RateBox.TextChanged += (s, e) => OnRateTyped();
        RateSlider.ValueChanged += (s, e) =>
        {
            if (_busy) return;
            _busy = true;
            RateBox.Text = RateSlider.Value.ToString("0.##", CultureInfo.InvariantCulture);
            _busy = false;
            Calculate();
        };

        TenureBox.TextChanged += (s, e) => OnTenureTyped();
        TenureSlider.ValueChanged += (s, e) =>
        {
            if (_busy) return;
            _busy = true;
            TenureBox.Text = TenureSlider.Value.ToString("0.##", CultureInfo.InvariantCulture);
            _busy = false;
            Calculate();
        };

        SetAmount(5000000);
    }

    private string Symbol => CurrencySymbols[Math.Max(0, Math.Min(CurrencySymbols.Length - 1, CurrencyBox.SelectedIndex))];

    // ------------------------------------------------------------------ layout helpers

    private void BuildPresets()
    {
        var presets = new (string Text, double Amount, double Rate, double Years)[]
        {
            ("Home Loan (1 Crore)", 10000000, 16.5, 20),
            ("Personal Loan (10 Lakhs)", 1000000, 22.0, 3),
            ("Car Loan (35 Lakhs)", 3500000, 18.0, 5),
            ("Business Loan (2 Crores)", 20000000, 19.5, 10)
        };

        for (int i = 0; i < presets.Length; i++)
        {
            var p = presets[i];
            var b = new Button
            {
                Content = p.Text,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 8, 0),
                Classes = { "btn", "secondary" }
            };
            b.Click += (s, e) => ApplyPreset(p.Amount, p.Rate, p.Years);
            Grid.SetColumn(b, i);
            PresetGrid.Children.Add(b);
        }
    }

    private void BuildAmountChips()
    {
        var items = new (string Text, double Value)[]
        {
            ("50 K", 50000), ("25 Lakhs", 2500000), ("50 Lakhs", 5000000), ("1 Crore", 10000000),
            ("2.5 Crore", 25000000), ("5 Crore", 50000000), ("10 Crore", 100000000)
        };

        foreach (var item in items)
        {
            var b = new Button
            {
                Content = item.Text,
                Height = 34,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 6, 6),
                Classes = { "btn", "secondary" }
            };
            b.Click += (s, e) => SetAmount(item.Value);
            AmountChips.Children.Add(b);
        }
    }

    // ------------------------------------------------------------------ input handling

    private static double Digits(string text)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in text ?? "")
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.Length == 0 ? 0 : Fmt.ParseOr(sb.ToString());
    }

    private void AdjustSliderMax(double amount)
    {
        string sym = Symbol;
        if (amount > DefaultSliderMax)
        {
            AmountSlider.Maximum = amount;
            _amountMarkers[3].Text = Fmt.CroresLakhs(amount, sym);
        }
        else
        {
            AmountSlider.Maximum = DefaultSliderMax;
            _amountMarkers[3].Text = sym + " 5 Crore";
        }
    }

    private void UpdateWords()
    {
        AmountWords.Text = Fmt.CroresLakhs(Digits(AmountBox.Text), Symbol);
    }

    private void SetAmount(double value)
    {
        _busy = true;
        AdjustSliderMax(value);
        AmountSlider.Value = value;
        AmountBox.Text = Fmt.Int(value);
        _busy = false;
        UpdateWords();
        Calculate();
    }

    private void OnAmountTyped()
    {
        if (_busy) return;
        _busy = true;
        double value = Digits(AmountBox.Text);
        string formatted = value > 0 ? Fmt.Int(value) : "";
        if ((AmountBox.Text ?? "") != formatted)
        {
            AmountBox.Text = formatted;
            AmountBox.SelectEnd();
        }
        AdjustSliderMax(value);
        AmountSlider.Value = Math.Min(value, AmountSlider.Maximum);
        _busy = false;
        UpdateWords();
        Calculate();
    }

    private void SetRate(double value)
    {
        _busy = true;
        if (value > RateSlider.Maximum) RateSlider.Maximum = value;
        RateBox.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
        RateSlider.Value = value;
        _busy = false;
        Calculate();
    }

    private void OnRateTyped()
    {
        if (_busy) return;
        _busy = true;
        double value = Fmt.ParseOr(RateBox.Text);
        if (value > RateSlider.Maximum) RateSlider.Maximum = value;
        RateSlider.Value = value;
        _busy = false;
        Calculate();
    }

    private void SetTenure(double value, bool years)
    {
        if (years != _years) SetTenureType(years, false);
        _busy = true;
        TenureBox.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
        TenureSlider.Value = value;
        _busy = false;
        Calculate();
    }

    private void OnTenureTyped()
    {
        if (_busy) return;
        _busy = true;
        TenureSlider.Value = Fmt.ParseOr(TenureBox.Text);
        _busy = false;
        Calculate();
    }

    private void SetTenureType(bool years, bool recalc = true)
    {
        _years = years;
        _busy = true;

        YearsButton.SetClass("on", years);
        MonthsButton.SetClass("on", !years);

        TenureBox.Suffix = years ? "Years" : "Months";

        if (years)
        {
            TenureSlider.Step = 1;
            TenureSlider.Minimum = 1;
            TenureSlider.Maximum = 30;
            SetMarkers(_tenureMarkers, "1 Year", "10 Years", "20 Years", "30 Years");
        }
        else
        {
            TenureSlider.Step = 6;
            TenureSlider.Minimum = 6;
            TenureSlider.Maximum = 360;
            SetMarkers(_tenureMarkers, "6 Mos", "120 Mos", "240 Mos", "360 Mos");
        }

        _busy = false;
        if (recalc) Calculate();
    }

    private static void SetMarkers(TextBlock[] labels, params string[] texts)
    {
        for (int i = 0; i < labels.Length && i < texts.Length; i++) labels[i].Text = texts[i];
    }

    private void ApplyPreset(double amount, double rate, double years)
    {
        SetAmount(amount);
        SetRate(rate);
        SetTenure(years, true);
    }

    private void RefreshCurrency()
    {
        AmountBox.Prefix = Symbol;
        _busy = true;
        AdjustSliderMax(Digits(AmountBox.Text));
        _busy = false;
        UpdateWords();
        Calculate();
    }

    // ------------------------------------------------------------------ calculation + display

    private void Calculate()
    {
        double principal = Digits(AmountBox.Text);
        double rate = Fmt.ParseOr(RateBox.Text);
        double tenure = Fmt.ParseOr(TenureBox.Text);
        double months = _years ? tenure * 12 : tenure;

        _result = LoanMath.Calculate(principal, rate, months);
        UpdateResults();
        FillGrid();
    }

    private void UpdateResults()
    {
        string sym = Symbol;
        AmountBox.Prefix = sym;

        var r = _result;
        EmiText.Text = Fmt.Money(r.Emi, sym);
        PrincipalText.Text = Fmt.Money(r.Principal, sym);
        InterestText.Text = Fmt.Money(r.Interest, sym);
        TotalText.Text = Fmt.Money(r.Total, sym);

        double principalPct = r.Total > 0 ? Math.Round(r.Principal / r.Total * 100, MidpointRounding.AwayFromZero) : 100;
        double interestPct = r.Total > 0 ? 100 - principalPct : 0;
        LegendPrincipal.Text = "\u25CF Principal (" + principalPct.ToString("0", CultureInfo.InvariantCulture) + "%)";
        LegendInterest.Text = "\u25CF Interest (" + interestPct.ToString("0", CultureInfo.InvariantCulture) + "%)";

        Donut.SetValues(r.Principal > 0 ? r.Principal : 1, r.Interest > 0 ? r.Interest : 0);
    }

    private void FillGrid()
    {
        string sym = Symbol;
        _grid.ItemsSource = _result.Schedule.Select(row => new ScheduleLine
        {
            Year = "Year " + row.Year,
            Principal = Fmt.Money(row.Principal, sym),
            Interest = Fmt.Money(row.Interest, sym),
            Payment = Fmt.Money(row.Payment, sym),
            Balance = Fmt.Money(row.Balance, sym)
        }).ToList();
    }

    private void ToggleSchedule()
    {
        GridHost.IsVisible = !GridHost.IsVisible;
        ToggleButton.Content = GridHost.IsVisible ? "Hide year-wise schedule" : "View year-wise schedule";
    }

    private async Task OpenReportAsync()
    {
        string sym = Symbol;
        var data = new ReportData
        {
            Generated = "Generated: " + DateTime.Now.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture),
            Amount = sym + " " + AmountBox.Text,
            Rate = RateBox.Text + "%",
            Tenure = TenureBox.Text + " " + (_years ? "Years" : "Months"),
            Emi = Fmt.Money(_result.Emi, sym),
            Interest = Fmt.Money(_result.Interest, sym),
            Total = Fmt.Money(_result.Total, sym)
        };
        foreach (var row in _result.Schedule)
        {
            data.Rows.Add(new[]
            {
                "Year " + row.Year,
                Fmt.Money(row.Principal, sym),
                Fmt.Money(row.Interest, sym),
                Fmt.Money(row.Payment, sym),
                Fmt.Money(row.Balance, sym)
            });
        }

        var window = new LoanReportWindow(data);
        Window owner = Dialogs.WindowOf(this);
        if (owner != null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }
    }
}
