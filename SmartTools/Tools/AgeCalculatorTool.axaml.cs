using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public partial class AgeCalculatorTool : ToolPage
{
    private AgeResult _lastResult;
    private DateTime _lastBirth;
    private DateTime _lastEnd;

    public AgeCalculatorTool()
    {
        InitializeComponent();

        // empty = "not selected" (the WinForms picker had a check box for this)
        DobPicker.SelectedDate = null;
        TargetPicker.SelectedDate = new DateTimeOffset(DateTime.Today);

        CalcButton.Click += (s, e) => Calculate();
        PrintButton.Click += async (s, e) => await ShowPrintPreviewAsync();
    }

    private void SetResultVisible(bool visible)
    {
        HintText.IsVisible = !visible;
        AgeCaption.IsVisible = visible;
        PrimaryText.IsVisible = visible;
        ResultDivider.IsVisible = visible;
        StatsText.IsVisible = visible;
    }

    private void Calculate()
    {
        if (DobPicker.SelectedDate == null)
        {
            _ = Warn("Please select your Date of Birth first.");
            return;
        }

        DateTime birth = DobPicker.SelectedDate.Value.Date;
        DateTime end = (TargetPicker.SelectedDate ?? new DateTimeOffset(DateTime.Today)).Date;
        if (end < birth)
        {
            _ = Warn("The target date cannot be earlier than your birth date.");
            return;
        }

        AgeResult r = AgeMath.Compute(birth, end);
        _lastBirth = birth;
        _lastEnd = end;
        _lastResult = r;
        PrintButton.IsEnabled = true;

        PrimaryText.Text = r.Years + " years " + r.Months + " months " + r.Days + " days";
        StatsText.Text =
            "\u2022  " + r.TotalMonths + " months " + r.Days + " days\n" +
            "\u2022  " + r.Weeks.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " weeks " + r.RemainingDays + " days\n" +
            "\u2022  " + r.TotalDays.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " days\n" +
            "\u2022  " + r.Hours.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " hours\n" +
            "\u2022  " + r.Minutes.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " minutes\n" +
            "\u2022  " + r.Seconds.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " seconds";

        SetResultVisible(true);
    }

    private async Task ShowPrintPreviewAsync()
    {
        if (_lastResult == null) return;
        await ReportPreviewWindow.ShowAsync(this, new AgeReportDocument(_lastBirth, _lastEnd, _lastResult));
    }
}
