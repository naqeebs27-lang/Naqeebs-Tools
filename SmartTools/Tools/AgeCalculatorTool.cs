using System.Drawing.Printing;
using SmartTools.Services;
using SmartTools.UI;

namespace SmartTools.Tools;

public class AgeCalculatorTool : ToolPage
{
    private readonly DateTimePicker _dob = new DateTimePicker
    {
        Format = DateTimePickerFormat.Long,
        ShowCheckBox = true,
        Checked = false,
        Value = new DateTime(2000, 1, 1),
        Font = new Font("Segoe UI", 11f)
    };

    private readonly DateTimePicker _target = new DateTimePicker
    {
        Format = DateTimePickerFormat.Long,
        Value = DateTime.Today,
        Font = new Font("Segoe UI", 11f)
    };

    private readonly Label _hint = Ui.Text("Select a date of birth and press Calculate Age.", Theme.Body, Theme.TextMuted);
    private readonly Label _primary = Ui.Text("", Theme.Large, Theme.TextPrimary);
    private readonly Label _stats = Ui.Text("", Theme.Body, Theme.TextMuted);
    private readonly ModernButton _print = ModernButton.Secondary("Print / Preview");
    private readonly Control[] _resultControls;
    private AgeResult _lastResult;
    private DateTime _lastBirth;
    private DateTime _lastEnd;

    public AgeCalculatorTool() : base("Age Calculator", "Find an exact age in years, months, days - and every other unit.")
    {
        var calc = ModernButton.Primary("Calculate Age");
        calc.Click += (s, e) => Calculate();

        _print.Enabled = false;
        _print.Click += (s, e) => ShowPrintPreview();

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
        inputs.AddHeading("Dates");
        inputs.AddField("Date of birth", _dob, 8);
        inputs.AddField("Age at the date of", _target);
        inputs.AddRow(actions, 22, 0);

        var ageCaption = Ui.Text("Age", Theme.Caption, Theme.TextMuted);
        var divider = Ui.Divider();

        _resultControls = new Control[] { ageCaption, _primary, divider, _stats };

        var results = new CardPanel();
        results.AddHeading("Result");
        results.AddRow(_hint, 8, 0);
        results.AddRow(ageCaption, 8, 0);
        results.AddRow(_primary, 2, 0);
        results.AddRow(divider, 14, 14);
        results.AddRow(_stats, 0, 0);

        SetResultVisible(false);

        Stack.AddRow(Ui.TwoCards(inputs, results, 46f));
    }

    private void SetResultVisible(bool visible)
    {
        _hint.Visible = !visible;
        foreach (var c in _resultControls)
        {
            c.Visible = visible;
        }
    }

    private void Calculate()
    {
        if (!_dob.Checked)
        {
            Warn("Please select your Date of Birth first.");
            return;
        }

        DateTime birth = _dob.Value.Date;
        DateTime end = _target.Value.Date;
        if (end < birth)
        {
            Warn("The target date cannot be earlier than your birth date.");
            return;
        }

        AgeResult r = AgeMath.Compute(birth, end);
        _lastBirth = birth;
        _lastEnd = end;
        _lastResult = r;
        _print.Enabled = true;

        _primary.Text = r.Years + " years " + r.Months + " months " + r.Days + " days";
        _stats.Text =
            "\u2022  " + r.TotalMonths + " months " + r.Days + " days\r\n" +
            "\u2022  " + r.Weeks.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " weeks " + r.RemainingDays + " days\r\n" +
            "\u2022  " + r.TotalDays.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " days\r\n" +
            "\u2022  " + r.Hours.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " hours\r\n" +
            "\u2022  " + r.Minutes.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " minutes\r\n" +
            "\u2022  " + r.Seconds.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " seconds";

        SetResultVisible(true);
    }

    private void ShowPrintPreview()
    {
        if (_lastResult == null) return;

        using var document = CreatePrintDocument();
        using var preview = new PrintPreviewDialog
        {
            Document = document,
            UseAntiAlias = true,
            Width = 1000,
            Height = 700,
            StartPosition = FormStartPosition.CenterParent
        };
        preview.ShowDialog(FindForm());
    }

    private PrintDocument CreatePrintDocument()
    {
        var document = new PrintDocument
        {
            DocumentName = "Age Calculator Report"
        };
        document.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        document.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
        document.PrintPage += PrintPage;
        return document;
    }

    private void PrintPage(object sender, PrintPageEventArgs e)
    {
        var g = e.Graphics;
        var bounds = e.MarginBounds;
        float x = bounds.Left;
        float y = bounds.Top;

        using var titleFont = new Font("Segoe UI", 18f, FontStyle.Bold);
        using var headingFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 11f);
        using var valueFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var muted = new SolidBrush(Theme.TextMuted);
        using var text = new SolidBrush(Theme.TextPrimary);
        using var accent = new SolidBrush(Theme.Accent);
        using var line = new Pen(Theme.Border, 1f);

        g.DrawString("AGE CALCULATOR REPORT", titleFont, text, x, y);
        y += titleFont.GetHeight(g) + 4;
        g.DrawString("Generated for the selected dates", bodyFont, muted, x, y);
        y += bodyFont.GetHeight(g) + 12;
        g.DrawLine(line, x, y, bounds.Right, y);
        y += 18;

        g.DrawString("Date of birth", bodyFont, muted, x, y);
        g.DrawString(_lastBirth.ToLongDateString(), valueFont, text, x + 180, y);
        y += valueFont.GetHeight(g) + 10;
        g.DrawString("Age at the date of", bodyFont, muted, x, y);
        g.DrawString(_lastEnd.ToLongDateString(), valueFont, text, x + 180, y);
        y += valueFont.GetHeight(g) + 24;

        g.DrawString("Exact age", headingFont, muted, x, y);
        y += headingFont.GetHeight(g) + 4;
        g.DrawString($"{_lastResult.Years} years {_lastResult.Months} months {_lastResult.Days} days", titleFont, accent, x, y);
        y += titleFont.GetHeight(g) + 22;
        g.DrawLine(line, x, y, bounds.Right, y);
        y += 18;

        g.DrawString("Detailed calculation", headingFont, text, x, y);
        y += headingFont.GetHeight(g) + 12;

        string[] rows =
        {
            $"{_lastResult.TotalMonths:N0} months {_lastResult.Days} days",
            $"{_lastResult.Weeks:N0} weeks {_lastResult.RemainingDays} days",
            $"{_lastResult.TotalDays:N0} days",
            $"{_lastResult.Hours:N0} hours",
            $"{_lastResult.Minutes:N0} minutes",
            $"{_lastResult.Seconds:N0} seconds"
        };
        foreach (string row in rows)
        {
            g.DrawString("•", bodyFont, accent, x, y);
            g.DrawString(row, bodyFont, text, x + 18, y);
            y += bodyFont.GetHeight(g) + 6;
        }

        e.HasMorePages = false;
    }
}
