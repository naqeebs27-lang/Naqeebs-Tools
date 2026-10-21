using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using SmartTools.Services;
using SmartTools.UI;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools.Tools;

public partial class CurrencyCalculatorTool : ToolPage
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly DateTime EarliestDate = new DateTime(1999, 1, 4);

    private List<string> _reportLines = new List<string>();
    private int _version;
    private bool _suppress;
    private bool _started;
    private string _pendingStatus;

    public CurrencyCalculatorTool()
    {
        InitializeComponent();

        var items = new List<string>();
        foreach (var c in CurrencyCatalog.All)
        {
            items.Add(c.Key + " - " + c.Value);
        }

        FromBox.SetItems(items.ToArray());
        ToBox.SetItems(items.ToArray());
        FromBox.SelectedIndex = IndexOf("USD");
        ToBox.SelectedIndex = IndexOf("PKR");

        SourceBox.SetItems("SBP interbank (PKR) + world cross rates", "World market rates (open data)");

        DatePick.MinYear = new DateTimeOffset(EarliestDate);
        DatePick.MaxYear = new DateTimeOffset(DateTime.Today);
        DatePick.SelectedDate = new DateTimeOffset(DateTime.Today);
        ComparePick.MinYear = new DateTimeOffset(EarliestDate);
        ComparePick.MaxYear = new DateTimeOffset(DateTime.Today);
        ComparePick.SelectedDate = new DateTimeOffset(DateTime.Today.AddDays(-7));

        SwapButton.Click += (s, e) => SwapCurrencies();
        RefreshButton.Click += (s, e) => RecalculateAsync(true);
        UpdateRatesButton.Click += (s, e) => RecalculateAsync(true);
        SaveManualRatesButton.Click += async (s, e) => await SaveManualRatesAsync();
        PrintButton.Click += async (s, e) => await PrintReportAsync();

        AmountBox.TextChanged += (s, e) => Changed();
        FromBox.SelectedIndexChanged += (s, e) => Changed();
        ToBox.SelectedIndexChanged += (s, e) => Changed();
        SourceBox.SelectedIndexChanged += (s, e) => Changed();
        DatePick.SelectedDateChanged += (s, e) => { ClampDate(DatePick); Changed(); };
        ComparePick.SelectedDateChanged += (s, e) => { ClampDate(ComparePick); Changed(); };
        CompareCheck.IsCheckedChanged += (s, e) =>
        {
            ComparePick.IsEnabled = CompareCheck.IsChecked == true;
            Changed();
        };

        // Retry current and missing rates whenever the page is opened.
        Loaded += (s, e) =>
        {
            if (!_started)
            {
                _started = true;
                RecalculateAsync(true);
            }
        };
    }

    // ------------------------------------------------------------------ helpers

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

    private static DateTime DateOf(DatePicker picker, DateTime fallback)
    {
        return picker.SelectedDate.HasValue ? picker.SelectedDate.Value.Date : fallback;
    }

    /// <summary>Keeps a picker inside 1999-01-04 .. today (WinForms MinDate / MaxDate).</summary>
    private void ClampDate(DatePicker picker)
    {
        if (_suppress || !picker.SelectedDate.HasValue) return;

        DateTime value = picker.SelectedDate.Value.Date;
        DateTime fixedValue = value > DateTime.Today ? DateTime.Today : (value < EarliestDate ? EarliestDate : value);
        if (fixedValue != value)
        {
            _suppress = true;
            picker.SelectedDate = new DateTimeOffset(fixedValue);
            _suppress = false;
        }
    }

    private void SetCompareVisible(bool visible)
    {
        CmpDivider.IsVisible = visible;
        CmpTitle.IsVisible = visible;
        CmpMain.IsVisible = visible;
        CmpRate.IsVisible = visible;
        CmpChange.IsVisible = visible;
    }

    private void SetStatus(string text, IBrush brush)
    {
        StatusText.Text = text ?? "";
        StatusText.Foreground = brush;
        StatusText.IsVisible = !string.IsNullOrEmpty(text);
    }

    private void SwapCurrencies()
    {
        int a = FromBox.SelectedIndex;
        int b = ToBox.SelectedIndex;

        _suppress = true;
        FromBox.SelectedIndex = b;
        ToBox.SelectedIndex = a;
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

    private async Task SaveManualRatesAsync()
    {
        SaveManualRatesButton.IsEnabled = false;
        SetStatus("Saving manual SBP rates and creating XML...", ThemePalette.TextMutedBrush);

        try
        {
            DateTime date = DateOf(DatePick, DateTime.Today);
            var saved = await RateService.SaveManualSbpRatesAsync(
                date,
                Fmt.ParseOr(ManualUsdBox.Text),
                Fmt.ParseOr(ManualJpyBox.Text),
                Fmt.ParseOr(ManualGbpBox.Text),
                Fmt.ParseOr(ManualEurBox.Text));
            _pendingStatus = "Saved manual SBP rates and created " + Path.GetFileName(saved.XmlPath) + " in the application data folder.";
            RecalculateAsync(false);
        }
        catch (Exception ex)
        {
            SetStatus("Manual SBP rate save failed: " + ex.Message, ThemePalette.DangerBrush);
        }
        finally
        {
            SaveManualRatesButton.IsEnabled = true;
        }
    }

    // ------------------------------------------------------------------ calculation

    private async void RecalculateAsync(bool force)
    {
        int version = ++_version;

        RateSource source = SourceBox.SelectedIndex == 1 ? RateSource.WorldMarket : RateSource.SbpInterbank;
        string from = CodeOf(FromBox.SelectedText);
        string to = CodeOf(ToBox.SelectedText);
        double amount = Fmt.ParseOr(AmountBox.Text);
        DateTime date1 = DateOf(DatePick, DateTime.Today);
        bool compare = CompareCheck.IsChecked == true;
        DateTime date2 = DateOf(ComparePick, DateTime.Today.AddDays(-7));

        RefreshButton.IsEnabled = false;
        UpdateRatesButton.IsEnabled = false;
        SetStatus("Loading rates...", ThemePalette.TextMutedBrush);

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
            if (!string.IsNullOrEmpty(_pendingStatus))
            {
                string status = _pendingStatus;
                _pendingStatus = null;
                SetStatus(status, ThemePalette.TextMutedBrush);
            }
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
                RefreshButton.IsEnabled = true;
                UpdateRatesButton.IsEnabled = true;
            }
        }
    }

    private void ShowError(string message)
    {
        AmountLine.Text = "";
        MainText.Text = "-";
        MainText.FontSize = 35;
        RateLine.Text = "";
        InverseLine.Text = "";
        DateLine.Text = "";
        BasisLine.Text = "";
        SetCompareVisible(false);
            SetStatus(message, ThemePalette.DangerBrush);
        PrintButton.IsEnabled = false;
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
        AmountLine.Text = FormatAmount(amount) + " " + from + " =";
        MainText.FontSize = mainText.Length > 15 ? 21 : 35;
        MainText.Text = mainText;
        RateLine.Text = "1 " + from + " = " + FormatRate(rate1) + " " + to;
        InverseLine.Text = "1 " + to + " = " + FormatRate(1.0 / rate1) + " " + from;
        DateLine.Text = "Rate date: " + D(t1.RateDate) + "  \u2022  " + t1.SourceLabel;

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
        BasisLine.Text = basis;

        if (!string.IsNullOrEmpty(t1.Note))
        {
            SetStatus(t1.Note, t1.NoteIsWarning ? ThemePalette.WarningTextBrush : ThemePalette.TextMutedBrush);
        }
        else
        {
            SetStatus("", ThemePalette.TextMutedBrush);
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
            CmpTitle.Text = "Comparison date";
            CmpMain.Text = "Not available";
            CmpRate.Text = compareError;
            CmpChange.Text = "";
        }
        else
        {
            SetCompareVisible(false);
        }

        lines.Add("");
        lines.Add("Note:");
        lines.Add("Indicative rates for reference only - actual dealing rates differ.");
        _reportLines = lines;
        PrintButton.IsEnabled = true;
    }

    private void ShowComparison(RateTable t1, RateTable t2, string from, string to, double amount,
                                double rate1, double result1, List<string> lines)
    {
        SetCompareVisible(true);
        CmpTitle.Text = "On " + D(t2.RateDate) + (t2.RateDate.Date != t2.RequestedDate.Date ? " (closest available)" : "");

        if (!t2.TryGetRate(from, to, out double rate2, out string error))
        {
            CmpMain.Text = "Not available";
            CmpRate.Text = error;
            CmpChange.Text = "";
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

        CmpMain.Text = FormatAmount(result2) + " " + to;
        CmpRate.Text = "1 " + from + " = " + FormatRate(rate2) + " " + to;
        CmpChange.Text = "Change from " + D(t2.RateDate) + " to " + D(t1.RateDate) + ": " + changeText;

        lines.Add("");
        lines.Add("Comparison:");
        lines.Add("On " + D(t2.RateDate) + ": " + FormatAmount(result2) + " " + to + " (1 " + from + " = " + FormatRate(rate2) + " " + to + ")");
        lines.Add("Change from " + D(t2.RateDate) + " to " + D(t1.RateDate) + ": " + changeText);
    }

    private async Task PrintReportAsync()
    {
        await ReportPrintHelper.ShowPreviewAsync(this, "Currency Conversion Report", _reportLines);
    }
}
