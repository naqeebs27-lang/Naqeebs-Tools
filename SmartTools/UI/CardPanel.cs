using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SmartTools.UI;

/// <summary>
/// A rounded "card". It is also a one-column TableLayoutPanel so rows can be added with AddRow().
/// </summary>
public class CardPanel : TableLayoutPanel
{
    private Color _fill = Color.White;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor
    {
        get => _fill;
        set
        {
            _fill = value;
            BackColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Theme.Border;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = Theme.S(14);

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        ColumnCount = 1;
        RowCount = 0;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.White;
        Padding = Theme.P(22);
        Margin = Padding.Empty;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // everything is drawn in OnPaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentBack(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(rect, Radius);
        using (var fill = new SolidBrush(_fill))
        {
            g.FillPath(fill, path);
        }
        using (var pen = new Pen(BorderColor))
        {
            g.DrawPath(pen, path);
        }
    }
}

/// <summary>Flat, rounded button with hover / pressed states.</summary>
public class ModernButton : Button
{
    private bool _hover;
    private bool _down;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color NormalColor { get; set; } = Theme.Accent;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Theme.AccentDark;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TextColor { get; set; } = Color.White;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.Transparent;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = Theme.S(10);

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Font = Theme.BodyBold;
        Height = Theme.S(42);
    }

    public static ModernButton Primary(string text)
    {
        return new ModernButton { Text = text };
    }

    public static ModernButton Success(string text)
    {
        return new ModernButton { Text = text, NormalColor = Theme.Success, HoverColor = Theme.SuccessDark };
    }

    public static ModernButton Secondary(string text)
    {
        return new ModernButton
        {
            Text = text,
            NormalColor = Theme.Neutral,
            HoverColor = Theme.NeutralHover,
            TextColor = Theme.TextPrimary,
            BorderColor = Theme.Border
        };
    }

    public static ModernButton Dark(string text)
    {
        return new ModernButton
        {
            Text = text,
            NormalColor = Theme.FromHex("#1E293B"),
            HoverColor = Theme.FromHex("#334155"),
            TextColor = Color.White
        };
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _down = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _down = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _down = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentBack(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color fillColor;
        if (!Enabled) fillColor = Theme.Disabled;
        else if (_down) fillColor = Theme.Darken(HoverColor, 0.08f);
        else if (_hover) fillColor = HoverColor;
        else fillColor = NormalColor;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(rect, Radius);
        using (var brush = new SolidBrush(fillColor))
        {
            g.FillPath(brush, path);
        }

        if (BorderColor.A > 0 && Enabled)
        {
            using var pen = new Pen(BorderColor);
            g.DrawPath(pen, path);
        }

        Color tc = Enabled ? TextColor : Theme.DisabledText;
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, tc,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
