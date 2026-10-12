using System.Globalization;
using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public class CurrencyCalculatorTool : ToolPage
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly DateTime EarliestDate = new DateTime(1999, 1, 4);

    private readonly ModernTextBox _amount = new ModernTextBox { NumericOnly = true, Text = "1" };
    private readonly ModernComboBox _from = new ModernComboBox();
    private readonly ModernComboBox _to = new ModernComboBox();
    private readonly ModernComboBox _source = new ModernComboBox();

    private readonly DateTimePicker _date = new DateTimePicker
    {
        Format = DateTimePickerFormat.Long,
        MinDate = EarliestDate,
        MaxDate = DateTime.Today,
        Value = DateTime.Today,
        Font = new Font("Segoe UI", 11f)
    };

    private readonly DateTimePicker _compare = new DateTimePicker
    {
        Format = DateTimePickerFormat.Long,
        MinDate = EarliestDate,
        MaxDate = DateTime.Today,
        Value = DateTime.Today.AddDays(-7),
        ShowCheckBox = true,
        Checked = false,
        Font = new Font("Segoe UI", 11f)
    };

    private readonly ModernButton _refresh = ModernButton.Primary("Refresh rates");
    private readonly ModernButton _swap = ModernButton.Secondary("Swap currencies");
    private readonly ModernButton _print = ModernButton.Secondary("Print / Preview");

    private readonly Label _amountLine = Ui.Text("", Theme.Body, Theme.TextMuted);
    private readonly Label _main = Ui.Text("-", Theme.Big, Theme.Success);
    private readonly Label _rateLine = Ui.Text("", Theme.BodyBold);
    private readonly Label _inverseLine = Ui.Text("", Theme.Small, Theme.TextMuted);
    private readonly Label _dateLine = Ui.Text("", Theme.Body, Theme.TextMuted);
    private readonly Label _basisLine = Ui.Text("", Theme.Small, Theme.TextMuted);
    private readonly Label _status = Ui.Text("", Theme.Small, Theme.TextMuted);

    private readonly Panel _cmpDivider = Ui.Divider();
    private readonly Label _cmpTitle = Ui.Text("", Theme.Caption, Theme.TextMuted);
    private readonly Label _cmpMain = Ui.Text("", Theme.Large, Theme.TextPrimary);
    private readonly Label _cmpRate = Ui.Text("", Theme.Body, Theme.TextMuted);
    private readonly Label _cmpChange = Ui.Text("", Theme.BodyBold, Theme.TextPrimary);

    private List<string> _reportLines = new List<string>();
    private int _version;
    private bool _suppress;
    private bool _started;

    public CurrencyCalculatorTool() : base("Currency Calculator",
        "Convert world currencies with SBP interbank or world market rates - for today or a past date.")
    {
        var items = new List<string>();
        foreach (var c in CurrencyCatalog.All)
        {
            items.Add(c.Key + " - " + c.Value);
        }

        _from.SetItems(items.ToArray());
        _to.SetItems(items.ToArray());
        _from.SelectedIndex = IndexOf("USD");
        _to.SelectedIndex = IndexOf("PKR");

        _source.SetItems("SBP interbank (PKR) + world cross rates", "World market rates (open data)");

        Wrap(_amountLine);
        Wrap(_dateLine);
        Wrap(_basisLine);
        Wrap(_status);
        Wrap(_cmpTitle);
        Wrap(_cmpRate);
        Wrap(_cmpChange);

        _swap.Click += (s, e) => SwapCurrencies();
        _refresh.Click += (s, e) => RecalculateAsync(true);
        _print.Enabled = false;
        _print.Click += (s, e) => PrintReport();

        var inputs = new CardPanel();
        inputs.AddHeading("Convert", "Rates load automatically for the chosen date");
        inputs.AddField("Amount", _amount, 8);
        inputs.AddField("From", _from);
        inputs.AddRow(_swap, 10, 0);
        inputs.AddField("To", _to, 10);
        inputs.AddField("Rate source", _source);
        inputs.AddField("Rate date", _date);
        inputs.AddField("Compare with an earlier date (tick to enable)", _compare);
        inputs.AddRow(_refresh, 22, 0);

        var results = new CardPanel();
        results.AddHeading("Result", "Indicative rates for reference");
        results.AddRow(_amountLine, 10, 0);
        results.AddRow(_main, 2, 0);
        results.AddRow(Ui.Divider(), 14, 12);
        results.AddRow(_rateLine, 0, 0);
        results.AddRow(_inverseLine, 2, 0);
        results.AddRow(Ui.Divider(), 12, 12);
        results.AddRow(_dateLine, 0, 0);
        results.AddRow(_basisLine, 2, 0);
        results.AddRow(_status, 8, 0);
        results.AddRow(_cmpDivider, 12, 12);
        results.AddRow(_cmpTitle, 0, 0);
        results.AddRow(_cmpMain, 2, 0);
        results.AddRow(_cmpRate, 2, 0);
        results.AddRow(_cmpChange, 6, 0);
        results.AddRow(_print, 18, 0);

        SetCompareVisible(false);
        _status.Visible = false;

        Stack.AddRow(Ui.TwoCards(inputs, results, 46f));

        _amount.TextChanged += (s, e) => Changed();
        _from.SelectedIndexChanged += (s, e) => Changed();
        _to.SelectedIndexChanged += (s, e) => Changed();
        _source.SelectedIndexChanged += (s, e) => Changed();
        _date.ValueChanged += (s, e) => Changed();
        _compare.ValueChanged += (s, e) => Changed();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Nothing is downloaded until the page is opened for the first time.
        if (!_started)
        {
            _started = true;
            RecalculateAsync(false);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static void Wrap(Label label)
    {
        label.MaximumSize = new Size(Theme.S(360), 0);
    }

    private static int IndexOf(string code)
    {
        for (int i = 0; i < CurrencyCatalog.All.Count; i++)
        {
            if (CurrencyCatalog.All[i].Key == code)
            {
                return i;
            }
        }
        return 0;
    }

    private static string CodeOf(string item)
    {
        return item != null && item.Length >= 3 ? item.Substring(0, 3) : "";
    }

    private static string FormatAmount(double v)
    {
        double a = Math.Abs(v);
        if (a >= 1) return v.ToString("N2", Inv);
        if (a >= 0.01) return v.ToString("N4", Inv);
        if (a >= 0.0001) return v.ToString("N6", Inv);
        return v.ToString("N8", Inv);
    }

    private static string FormatRate(double v)
    {
        double a = Math.Abs(v);
        if (a >= 1) return v.ToString("N4", Inv);
        if (a >= 0.0001) return v.ToString("N6", Inv);
        return v.ToString("N8", Inv);
    }

    private static string D(DateTime d)
    {
        return d.ToString("dd MMM yyyy", Inv);
    }

    private void SetCompareVisible(bool visible)
    {
        _cmpDivider.Visible = visible;
        _cmpTitle.Visible = visible;
        _cmpMain.Visible = visible;
        _cmpRate.Visible = visible;
        _cmpChange.Visible = visible;
    }

    private void SetStatus(string text, Color color)
    {
        _status.Text = text ?? "";
        _status.ForeColor = color;
        _status.Visible = !string.IsNullOrEmpty(text);
    }

    private void SwapCurrencies()
    {
        int a = _from.SelectedIndex;
        int b = _to.SelectedIndex;

        _suppress = true;
        _from.SelectedIndex = b;
        _to.SelectedIndex = a;
        _suppress = false;

        Changed();
    }

    private void Changed()
    {
        if (_suppress || !_started)
        {
            return;
        }
        RecalculateAsync(false);
    }

    // ------------------------------------------------------------------ calculation

    private async void RecalculateAsync(bool force)
    {
        int version = ++_version;

        RateSource source = _source.SelectedIndex == 1 ? RateSource.WorldMarket : RateSource.SbpInterbank;
        string from = CodeOf(_from.SelectedItem);
        string to = CodeOf(_to.SelectedItem);
        double amount = Fmt.ParseOr(_amount.Text);
        DateTime date1 = _date.Value.Date;
        bool compare = _compare.Checked;
        DateTime date2 = _compare.Value.Date;

        _refresh.Enabled = false;
        SetStatus("Loading rates...", Theme.TextMuted);

        try
        {
            if (force)
            {
                RateService.Invalidate(source, date1);
                if (compare)
                {
                    RateService.Invalidate(source, date2);
                }
            }

            RateTable table1 = await RateService.GetAsync(source, date1);

            // A problem with the comparison date must not hide the main result.
            RateTable table2 = null;
            string compareError = null;
            if (compare)
            {
                try
                {
                    table2 = await RateService.GetAsync(source, date2);
                }
                catch (Exception compareEx)
                {
                    compareError = compareEx.Message;
                }
            }

            if (version != _version)
            {
                return; // a newer request replaced this one
            }

            ShowResult(table1, table2, compareError, from, to, amount);
        }
        catch (Exception ex)
        {
            if (version == _version)
            {
                ShowError(ex.Message);
            }
        }
        finally
        {
            if (version == _version)
            {
                _refresh.Enabled = true;
            }
        }
    }

    private void ShowError(string message)
    {
        _amountLine.Text = "";
        _main.Text = "-";
        _main.Font = Theme.Big;
        _rateLine.Text = "";
        _inverseLine.Text = "";
        _dateLine.Text = "";
        _basisLine.Text = "";
        SetCompareVisible(false);
        SetStatus(message, Theme.Danger);
        _print.Enabled = false;
    }

    private void ShowResult(RateTable t1, RateTable t2, string compareError, string from, string to, double amount)
    {
        if (!t1.TryGetRate(from, to, out double rate1, out string error))
        {
            ShowError(error);
            return;
        }

        double result1 = amount * rate1;

        string mainText = FormatAmount(result1) + " " + to;
        _amountLine.Text = FormatAmount(amount) + " " + from + " =";
        _main.Font = mainText.Length > 15 ? Theme.Large : Theme.Big;
        _main.Text = mainText;
        _rateLine.Text = "1 " + from + " = " + FormatRate(rate1) + " " + to;
        _inverseLine.Text = "1 " + to + " = " + FormatRate(1.0 / rate1) + " " + from;
        _dateLine.Text = "Rate date: " + D(t1.RateDate) + "  \u2022  " + t1.SourceLabel;

        string basis;
        if (t1.Source == RateSource.SbpInterbank)
        {
            string usdPkr = t1.PerUsd.TryGetValue("PKR", out double pkr) ? "SBP USD/PKR: " + FormatRate(pkr) + ". " : "";
            bool direct = t1.SbpPublished.Contains(from) && t1.SbpPublished.Contains(to);
            basis = usdPkr + (direct
                ? "Both currencies are published by SBP."
                : "Currencies SBP does not publish are crossed through USD.");
        }
        else
        {
            basis = "Cross rate through USD from the world market feed.";
        }
        _basisLine.Text = basis;

        if (!string.IsNullOrEmpty(t1.Note))
        {
            SetStatus(t1.Note, t1.NoteIsWarning ? Theme.FromHex("#B45309") : Theme.TextMuted);
        }
        else
        {
            SetStatus("", Theme.TextMuted);
        }

        var lines = new List<string>
        {
            "Conversion:",
            "Amount: " + FormatAmount(amount) + " " + from,
            "Converted: " + FormatAmount(result1) + " " + to,
            "Rate: 1 " + from + " = " + FormatRate(rate1) + " " + to,
            "Rate: 1 " + to + " = " + FormatRate(1.0 / rate1) + " " + from,
            "",
            "Rate details:",
            "Source: " + t1.SourceLabel,
            "Rate date: " + D(t1.RateDate) + (t1.RateDate.Date != t1.RequestedDate.Date ? " (requested " + D(t1.RequestedDate) + ")" : ""),
            basis
        };

        if (t2 != null)
        {
            ShowComparison(t1, t2, from, to, amount, rate1, result1, lines);
        }
        else if (compareError != null)
        {
            SetCompareVisible(true);
            _cmpTitle.Text = "Comparison date";
            _cmpMain.Text = "Not available";
            _cmpRate.Text = compareError;
            _cmpChange.Text = "";
        }
        else
        {
            SetCompareVisible(false);
        }

        lines.Add("");
        lines.Add("Note:");
        lines.Add("Indicative rates for reference only - actual dealing rates differ.");
        _reportLines = lines;
        _print.Enabled = true;
    }

    private void ShowComparison(RateTable t1, RateTable t2, string from, string to, double amount,
                                double rate1, double result1, List<string> lines)
    {
        SetCompareVisible(true);
        _cmpTitle.Text = "On " + D(t2.RateDate) + (t2.RateDate.Date != t2.RequestedDate.Date ? " (closest available)" : "");

        if (!t2.TryGetRate(from, to, out double rate2, out string error))
        {
            _cmpMain.Text = "Not available";
            _cmpRate.Text = error;
            _cmpChange.Text = "";
            lines.Add("");
            lines.Add("Comparison:");
            lines.Add("Rate on " + D(t2.RequestedDate) + ": not available");
            return;
        }

        double result2 = amount * rate2;
        double change = result1 - result2;
        double percent = rate2 > 0 ? (rate1 - rate2) / rate2 * 100.0 : 0;

        string sign = change >= 0 ? "+" : "-";
        string pctSign = percent >= 0 ? "+" : "-";
        string changeText = sign + FormatAmount(Math.Abs(change)) + " " + to + "  (" + pctSign + Math.Abs(percent).ToString("0.00", Inv) + "%)";

        _cmpMain.Text = FormatAmount(result2) + " " + to;
        _cmpRate.Text = "1 " + from + " = " + FormatRate(rate2) + " " + to;
        _cmpChange.Text = "Change from " + D(t2.RateDate) + " to " + D(t1.RateDate) + ": " + changeText;

        lines.Add("");
        lines.Add("Comparison:");
        lines.Add("On " + D(t2.RateDate) + ": " + FormatAmount(result2) + " " + to + " (1 " + from + " = " + FormatRate(rate2) + " " + to + ")");
        lines.Add("Change from " + D(t2.RateDate) + " to " + D(t1.RateDate) + ": " + changeText);
    }

    private void PrintReport()
    {
        ReportPrintHelper.ShowPreview(FindForm(), "Currency Conversion Report", _reportLines);
    }
}
