namespace SmartTools.UI;

/// <summary>Small layout helpers built on TableLayoutPanel (keeps the code-only UI tidy and DPI safe).</summary>
public static class Ui
{
    /// <summary>A vertical stack: one column, rows grow automatically.</summary>
    public static TableLayoutPanel Stack()
    {
        var t = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 0,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        return t;
    }

    /// <summary>A single row with several columns. Cards placed inside stretch to equal height.</summary>
    public static TableLayoutPanel Columns(params ColumnStyle[] styles)
    {
        var t = new TableLayoutPanel
        {
            ColumnCount = styles.Length,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        foreach (var s in styles) t.ColumnStyles.Add(s);
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return t;
    }

    public static ColumnStyle Pct(float percent) => new ColumnStyle(SizeType.Percent, percent);
    public static ColumnStyle Px(int px) => new ColumnStyle(SizeType.Absolute, Theme.S(px));

    /// <summary>Adds a control as a new auto-height row (full width). Margins are in design pixels.</summary>
    public static void AddRow(this TableLayoutPanel t, Control c, int top = 0, int bottom = 0, bool center = false)
    {
        if (center)
        {
            c.Anchor = AnchorStyles.None;
        }
        else if (c is Label lb && lb.AutoSize)
        {
            c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        }
        else
        {
            c.Dock = DockStyle.Fill;
        }

        c.Margin = new Padding(0, Theme.S(top), 0, Theme.S(bottom));
        int row = t.RowStyles.Count;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.RowCount = row + 1;
        t.Controls.Add(c, 0, row);
    }

    /// <summary>Places a control in a given column of a Columns() layout.</summary>
    public static void AddCell(this TableLayoutPanel t, Control c, int column, int left = 0, int right = 0)
    {
        c.Dock = DockStyle.Fill;
        c.Margin = new Padding(Theme.S(left), 0, Theme.S(right), 0);
        t.Controls.Add(c, column, 0);
    }

    public static Label Caption(string text)
    {
        return new Label
        {
            Text = text,
            Font = Theme.Caption,
            ForeColor = Theme.TextMuted,
            AutoSize = true
        };
    }

    public static Label Text(string text, Font font = null, Color? color = null, bool autoSize = true)
    {
        return new Label
        {
            Text = text,
            Font = font ?? Theme.Body,
            ForeColor = color ?? Theme.TextPrimary,
            AutoSize = autoSize
        };
    }

    /// <summary>Caption label followed by an input control.</summary>
    public static void AddField(this TableLayoutPanel t, string caption, Control input, int top = 14)
    {
        t.AddRow(Caption(caption), top, 6);
        t.AddRow(input);
    }

    public static void AddHeading(this TableLayoutPanel t, string text, string subtitle = null)
    {
        t.AddRow(Text(text, Theme.Heading), 0, subtitle == null ? 4 : 0);
        if (subtitle != null)
        {
            t.AddRow(Text(subtitle, Theme.Small, Theme.TextMuted), 2, 6);
        }
    }

    /// <summary>A thin horizontal divider line.</summary>
    public static Panel Divider(Color? color = null)
    {
        return new Panel { Height = 1, BackColor = color ?? Theme.Border };
    }

    /// <summary>Caption on the left, value on the right (one row).</summary>
    public static TableLayoutPanel KeyValue(string caption, Label value, Color? captionColor = null)
    {
        var t = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var cap = Text(caption, Theme.Body, captionColor ?? Theme.TextMuted);
        cap.Anchor = AnchorStyles.Left;
        value.AutoSize = true;
        value.Anchor = AnchorStyles.Right;
        t.Controls.Add(cap, 0, 0);
        t.Controls.Add(value, 1, 0);
        return t;
    }

    /// <summary>Two cards side by side (left / right) added to a page stack.</summary>
    public static TableLayoutPanel TwoCards(Control left, Control right, float leftPercent = 50f, int gap = 20)
    {
        var cols = Columns(Pct(leftPercent), Pct(100f - leftPercent));
        cols.AddCell(left, 0, 0, gap / 2);
        cols.AddCell(right, 1, gap / 2, 0);
        return cols;
    }
}
