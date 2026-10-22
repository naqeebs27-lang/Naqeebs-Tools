using System.Globalization;
using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public class LoanCalculatorTool : ToolPage
{
    private static readonly string[] CurrencySymbols = { "PKR", "$", "\u20AC", "\u00A3", "AED", "\u20B9" };
    private static readonly string[] CurrencyNames =
    {
        "PKR (Rs.)", "$ (USD)", "\u20AC (EUR)", "\u00A3 (GBP)", "AED", "\u20B9 (INR)"
    };

    private const double DefaultSliderMax = 50000000;

    // inputs
    private readonly ModernComboBox _currency = new ModernComboBox();
    private readonly ModernTextBox _amount = new ModernTextBox { NumericOnly = true, AllowDecimal = false };
    private readonly ModernSlider _amountSlider = new ModernSlider { Minimum = 50000, Maximum = DefaultSliderMax, Step = 50000, Value = 5000000 };
    private readonly Label _amountWords = Ui.Text("", Theme.BodyBold, Theme.Accent);
    private readonly ModernTextBox _rate = new ModernTextBox { NumericOnly = true, Suffix = "%", Text = "16.5" };
    private readonly ModernSlider _rateSlider = new ModernSlider { Minimum = 0, Maximum = 30, Step = 0.1, Value = 16.5, AccentColor = Theme.Success };
    private readonly ModernTextBox _tenure = new ModernTextBox { NumericOnly = true, Suffix = "Years", Text = "20" };
    private readonly ModernSlider _tenureSlider = new ModernSlider { Minimum = 1, Maximum = 30, Step = 1, Value = 20, AccentColor = Theme.Warning };
    private readonly ModernButton _yearsBtn = ModernButton.Primary("Years");
    private readonly ModernButton _monthsBtn = ModernButton.Secondary("Months");

    private Label[] _amountMarkers;
    private Label[] _tenureMarkers;

    // results
    private readonly Label _emi = Ui.Text("PKR 0", Theme.Big, Color.White);
    private readonly Label _principalLbl = Ui.Text("PKR 0", Theme.BodyBold, Color.White);
    private readonly Label _interestLbl = Ui.Text("PKR 0", Theme.BodyBold, Theme.Warning);
    private readonly Label _totalLbl = Ui.Text("PKR 0", Theme.Large, Theme.FromHex("#34D399"));
    private readonly DonutChart _donut = new DonutChart();
    private readonly Label _legendPrincipal;
    private readonly Label _legendInterest;

    // schedule
    private readonly DataGridView _grid = ScheduleGrid.Create();
    private readonly ModernButton _toggleBtn = ModernButton.Secondary("View year-wise schedule");

    private bool _busy;
    private bool _years = true;
    private LoanMath.Result _result = new LoanMath.Result();

    public LoanCalculatorTool() : base("Loan EMI Calculator", "Monthly instalments, interest breakdown and a printable amortisation report.")
    {
        _legendPrincipal = LegendLabel(Theme.FromHex("#0284C7"));
        _legendInterest = LegendLabel(Theme.Warning);

        _currency.SetItems(CurrencyNames);
        _currency.SelectedIndexChanged += (s, e) => RefreshCurrency();

        BuildTopCard();
        BuildMainArea();
        BuildScheduleCard();

        _amount.TextChanged += (s, e) => OnAmountTyped();
        _amountSlider.ValueChanged += (s, e) =>
        {
            if (_busy) return;
            _busy = true;
            _amount.Text = Fmt.Int(_amountSlider.Value);
            _busy = false;
            UpdateWords();
            Calculate();
        };

        _rate.TextChanged += (s, e) => OnRateTyped();
        _rateSlider.ValueChanged += (s, e) =>
        {
            if (_busy) return;
            _busy = true;
            _rate.Text = _rateSlider.Value.ToString("0.##", CultureInfo.InvariantCulture);
            _busy = false;
            Calculate();
        };

        _tenure.TextChanged += (s, e) => OnTenureTyped();
        _tenureSlider.ValueChanged += (s, e) =>
        {
            if (_busy) return;
            _busy = true;
            _tenure.Text = _tenureSlider.Value.ToString("0.##", CultureInfo.InvariantCulture);
            _busy = false;
            Calculate();
        };

        SetAmount(5000000);
    }

    private string Symbol => CurrencySymbols[Math.Max(0, Math.Min(CurrencySymbols.Length - 1, _currency.SelectedIndex))];

    private static Label LegendLabel(Color color)
    {
        return new Label
        {
            AutoSize = false,
            Height = Theme.S(24),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Caption,
            ForeColor = color
        };
    }

    // ------------------------------------------------------------------ layout

    private void BuildTopCard()
    {
        var presets = new (string Text, double Amount, double Rate, double Years)[]
        {
            ("Home Loan (1 Crore)", 10000000, 16.5, 20),
            ("Personal Loan (10 Lakhs)", 1000000, 22.0, 3),
            ("Car Loan (35 Lakhs)", 3500000, 18.0, 5),
            ("Business Loan (2 Crores)", 20000000, 19.5, 10)
        };

        var row = Ui.Columns(Ui.Pct(22), Ui.Pct(22), Ui.Pct(22), Ui.Pct(22), Ui.Pct(12));
        for (int i = 0; i < presets.Length; i++)
        {
            var p = presets[i];
            var b = ModernButton.Secondary(p.Text);
            b.Font = Theme.Caption;
            b.Click += (s, e) => ApplyPreset(p.Amount, p.Rate, p.Years);
            row.AddCell(b, i, 0, 8);
        }
        row.AddCell(_currency, 4, 4, 0);

        var card = new CardPanel();
        card.AddRow(Ui.Caption("QUICK PRESETS  -  CURRENCY"), 0, 8);
        card.AddRow(row);
        Stack.AddRow(card, 0, 18);
    }

    private void BuildMainArea()
    {
        // ---- left: inputs
        var left = new CardPanel();

        // loan amount
        left.AddRow(Ui.KeyValue("Loan amount", _amountWords), 0, 6);
        left.AddRow(_amount, 0, 6);
        left.AddRow(_amountSlider);
        left.AddRow(MarkerRow(new[] { "50 K", "1 Crore", "2.5 Crore", "5 Crore" }, out _amountMarkers), 0, 8);
        left.AddRow(BuildAmountChips(), 0, 0);

        left.AddRow(Ui.Divider(), 14, 14);

        // interest
        left.AddRow(Ui.Text("Annual interest rate", Theme.Caption, Theme.TextMuted), 0, 6);
        left.AddRow(_rate, 0, 6);
        left.AddRow(_rateSlider);
        left.AddRow(MarkerRow(new[] { "0%", "8%", "15%", "30%" }, out _), 0, 0);

        left.AddRow(Ui.Divider(), 14, 14);

        // tenure
        var tenureHead = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        tenureHead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tenureHead.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(84)));
        tenureHead.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(92)));
        tenureHead.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var cap = Ui.Text("Loan tenure", Theme.Caption, Theme.TextMuted);
        cap.Anchor = AnchorStyles.Left;
        tenureHead.Controls.Add(cap, 0, 0);

        _yearsBtn.Height = Theme.S(32);
        _monthsBtn.Height = Theme.S(32);
        _yearsBtn.Font = Theme.Caption;
        _monthsBtn.Font = Theme.Caption;
        _yearsBtn.Dock = DockStyle.Fill;
        _monthsBtn.Dock = DockStyle.Fill;
        _yearsBtn.Margin = new Padding(0, 0, Theme.S(6), 0);
        _monthsBtn.Margin = Padding.Empty;
        _yearsBtn.Click += (s, e) => SetTenureType(true);
        _monthsBtn.Click += (s, e) => SetTenureType(false);
        tenureHead.Controls.Add(_yearsBtn, 1, 0);
        tenureHead.Controls.Add(_monthsBtn, 2, 0);

        left.AddRow(tenureHead, 0, 6);
        left.AddRow(_tenure, 0, 6);
        left.AddRow(_tenureSlider);
        left.AddRow(MarkerRow(new[] { "1 Year", "10 Years", "20 Years", "30 Years" }, out _tenureMarkers), 0, 0);

        // ---- right: results
        var emiCard = new CardPanel { FillColor = Theme.Dark, BorderColor = Theme.Dark };
        emiCard.AddRow(Ui.Text("MONTHLY PAYABLE EMI", Theme.Caption, Theme.FromHex("#38BDF8")), 0, 2);
        emiCard.AddRow(_emi, 0, 4);
        emiCard.AddRow(Ui.Text("Fixed monthly principal + interest, compounded monthly.", Theme.Small, Theme.SidebarText), 0, 0);
        emiCard.AddRow(Ui.Divider(Theme.DarkBorder), 14, 14);
        emiCard.AddRow(Ui.KeyValue("Principal amount", _principalLbl, Theme.SidebarText), 0, 8);
        emiCard.AddRow(Ui.KeyValue("Total interest", _interestLbl, Theme.SidebarText), 0, 12);
        emiCard.AddRow(Ui.Text("Total amount payable", Theme.Small, Theme.SidebarText), 0, 0);
        emiCard.AddRow(_totalLbl, 0, 0);

        var chartCard = new CardPanel();
        chartCard.AddRow(Ui.Text("Loan payment split", Theme.BodyBold), 0, 10);
        chartCard.AddRow(_donut, 0, 10, true);
        var legend = Ui.Columns(Ui.Pct(50), Ui.Pct(50));
        legend.AddCell(_legendPrincipal, 0);
        legend.AddCell(_legendInterest, 1);
        chartCard.AddRow(legend);

        var rightStack = Ui.Stack();
        rightStack.AddRow(emiCard, 0, 16);
        rightStack.AddRow(chartCard);

        var cols = Ui.Columns(Ui.Pct(57), Ui.Pct(43));
        cols.AddCell(left, 0, 0, 10);
        cols.AddCell(rightStack, 1, 10, 0);
        cols.Dock = DockStyle.Fill;
        Stack.AddRow(cols, 0, 18);
    }

    private void BuildScheduleCard()
    {
        var head = Ui.Columns(Ui.Pct(100f), Ui.Px(170), Ui.Px(210));
        var title = Ui.Text("Amortization schedule", Theme.Heading);
        title.Anchor = AnchorStyles.Left;
        head.Controls.Add(title, 0, 0);

        var export = ModernButton.Primary("Print / Preview");
        export.Click += (s, e) => OpenReport();
        _toggleBtn.Click += (s, e) => ToggleSchedule();
        head.AddCell(export, 1, 0, 8);
        head.AddCell(_toggleBtn, 2, 0, 0);

        _grid.Height = Theme.S(340);
        _grid.Visible = false;

        var card = new CardPanel();
        card.AddRow(head, 0, 4);
        card.AddRow(Ui.Text("Year-by-year repayment showing principal, interest and balance", Theme.Small, Theme.TextMuted), 0, 8);
        card.AddRow(_grid, 4, 0);
        Stack.AddRow(card);
    }

    private static TableLayoutPanel MarkerRow(string[] texts, out Label[] labels)
    {
        var t = new TableLayoutPanel
        {
            ColumnCount = texts.Length,
            RowCount = 1,
            Height = Theme.S(22),
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        labels = new Label[texts.Length];
        for (int i = 0; i < texts.Length; i++)
        {
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / texts.Length));
            var l = new Label
            {
                Text = texts[i],
                Font = Theme.Small,
                ForeColor = Theme.TextLight,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = i == 0 ? ContentAlignment.MiddleLeft
                          : (i == texts.Length - 1 ? ContentAlignment.MiddleRight : ContentAlignment.MiddleCenter)
            };
            labels[i] = l;
            t.Controls.Add(l, i, 0);
        }
        return t;
    }

    private TableLayoutPanel BuildAmountChips()
    {
        var items = new (string Text, double Value)[]
        {
            ("50 K", 50000), ("25 Lakhs", 2500000), ("50 Lakhs", 5000000), ("1 Crore", 10000000),
            ("2.5 Crore", 25000000), ("5 Crore", 50000000), ("10 Crore", 100000000)
        };

        var t = new TableLayoutPanel
        {
            ColumnCount = 4,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        for (int c = 0; c < 4; c++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var b = ModernButton.Secondary(item.Text);
            b.Height = Theme.S(34);
            b.Font = Theme.Caption;
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(0, 0, Theme.S(6), Theme.S(6));
            b.Click += (s, e) => SetAmount(item.Value);
            t.Controls.Add(b, i % 4, i / 4);
        }
        return t;
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
            _amountSlider.Maximum = amount;
            _amountMarkers[3].Text = Fmt.CroresLakhs(amount, sym);
        }
        else
        {
            _amountSlider.Maximum = DefaultSliderMax;
            _amountMarkers[3].Text = sym + " 5 Crore";
        }
    }

    private void UpdateWords()
    {
        _amountWords.Text = Fmt.CroresLakhs(Digits(_amount.Text), Symbol);
    }

    private void SetAmount(double value)
    {
        _busy = true;
        AdjustSliderMax(value);
        _amountSlider.Value = value;
        _amount.Text = Fmt.Int(value);
        _busy = false;
        UpdateWords();
        Calculate();
    }

    private void OnAmountTyped()
    {
        if (_busy) return;
        _busy = true;
        double value = Digits(_amount.Text);
        string formatted = value > 0 ? Fmt.Int(value) : "";
        if (_amount.Text != formatted)
        {
            _amount.Text = formatted;
            _amount.SelectEnd();
        }
        AdjustSliderMax(value);
        _amountSlider.Value = Math.Min(value, _amountSlider.Maximum);
        _busy = false;
        UpdateWords();
        Calculate();
    }

    private void SetRate(double value)
    {
        _busy = true;
        if (value > _rateSlider.Maximum) _rateSlider.Maximum = value;
        _rate.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
        _rateSlider.Value = value;
        _busy = false;
        Calculate();
    }

    private void OnRateTyped()
    {
        if (_busy) return;
        _busy = true;
        double value = Fmt.ParseOr(_rate.Text);
        if (value > _rateSlider.Maximum) _rateSlider.Maximum = value;
        _rateSlider.Value = value;
        _busy = false;
        Calculate();
    }

    private void SetTenure(double value, bool years)
    {
        if (years != _years) SetTenureType(years, false);
        _busy = true;
        _tenure.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
        _tenureSlider.Value = value;
        _busy = false;
        Calculate();
    }

    private void OnTenureTyped()
    {
        if (_busy) return;
        _busy = true;
        _tenureSlider.Value = Fmt.ParseOr(_tenure.Text);
        _busy = false;
        Calculate();
    }

    private void SetTenureType(bool years, bool recalc = true)
    {
        _years = years;
        _busy = true;

        _yearsBtn.NormalColor = years ? Theme.Accent : Theme.Neutral;
        _yearsBtn.HoverColor = years ? Theme.AccentDark : Theme.NeutralHover;
        _yearsBtn.TextColor = years ? Color.White : Theme.TextPrimary;
        _monthsBtn.NormalColor = years ? Theme.Neutral : Theme.Accent;
        _monthsBtn.HoverColor = years ? Theme.NeutralHover : Theme.AccentDark;
        _monthsBtn.TextColor = years ? Theme.TextPrimary : Color.White;
        _yearsBtn.Invalidate();
        _monthsBtn.Invalidate();

        _tenure.Suffix = years ? "Years" : "Months";

        if (years)
        {
            _tenureSlider.Step = 1;
            _tenureSlider.Minimum = 1;
            _tenureSlider.Maximum = 30;
            SetMarkers(_tenureMarkers, "1 Year", "10 Years", "20 Years", "30 Years");
        }
        else
        {
            _tenureSlider.Step = 6;
            _tenureSlider.Minimum = 6;
            _tenureSlider.Maximum = 360;
            SetMarkers(_tenureMarkers, "6 Mos", "120 Mos", "240 Mos", "360 Mos");
        }

        _busy = false;
        if (recalc) Calculate();
    }

    private static void SetMarkers(Label[] labels, params string[] texts)
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
        _amount.Prefix = Symbol;
        _busy = true;
        AdjustSliderMax(Digits(_amount.Text));
        _busy = false;
        UpdateWords();
        Calculate();
    }

    // ------------------------------------------------------------------ calculation + display

    private void Calculate()
    {
        double principal = Digits(_amount.Text);
        double rate = Fmt.ParseOr(_rate.Text);
        double tenure = Fmt.ParseOr(_tenure.Text);
        double months = _years ? tenure * 12 : tenure;

        _result = LoanMath.Calculate(principal, rate, months);
        UpdateResults();
        FillGrid();
    }

    private void UpdateResults()
    {
        string sym = Symbol;
        _amount.Prefix = sym;

        var r = _result;
        _emi.Text = Fmt.Money(r.Emi, sym);
        _principalLbl.Text = Fmt.Money(r.Principal, sym);
        _interestLbl.Text = Fmt.Money(r.Interest, sym);
        _totalLbl.Text = Fmt.Money(r.Total, sym);

        double principalPct = r.Total > 0 ? Math.Round(r.Principal / r.Total * 100, MidpointRounding.AwayFromZero) : 100;
        double interestPct = r.Total > 0 ? 100 - principalPct : 0;
        _legendPrincipal.Text = "\u25CF Principal (" + principalPct.ToString("0", CultureInfo.InvariantCulture) + "%)";
        _legendInterest.Text = "\u25CF Interest (" + interestPct.ToString("0", CultureInfo.InvariantCulture) + "%)";

        _donut.SetValues(r.Principal > 0 ? r.Principal : 1, r.Interest > 0 ? r.Interest : 0);
    }

    private void FillGrid()
    {
        string sym = Symbol;
        _grid.SuspendLayout();
        _grid.Rows.Clear();
        foreach (var row in _result.Schedule)
        {
            _grid.Rows.Add(
                "Year " + row.Year,
                Fmt.Money(row.Principal, sym),
                Fmt.Money(row.Interest, sym),
                Fmt.Money(row.Payment, sym),
                Fmt.Money(row.Balance, sym));
        }
        _grid.ClearSelection();
        _grid.ResumeLayout();
    }

    private void ToggleSchedule()
    {
        _grid.Visible = !_grid.Visible;
        _toggleBtn.Text = _grid.Visible ? "Hide year-wise schedule" : "View year-wise schedule";
    }

    private void OpenReport()
    {
        string sym = Symbol;
        var data = new ReportData
        {
            Generated = "Generated: " + DateTime.Now.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture),
            Amount = sym + " " + _amount.Text,
            Rate = _rate.Text + "%",
            Tenure = _tenure.Text + " " + (_years ? "Years" : "Months"),
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

        using var form = new LoanReportForm(data);
        form.ShowDialog(FindForm());
    }
}
