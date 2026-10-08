using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SmartTools.UI;

/// <summary>Sidebar navigation entry: coloured badge + title. Painted by hand for a modern look.</summary>
public class NavButton : Control
{
    private bool _hover;
    private bool _selected;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Badge { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BadgeColor { get; set; } = Theme.Accent;

    public NavButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = Theme.S(48);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
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
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Sidebar);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (_selected || _hover)
        {
            using var path = Theme.RoundedRect(rect, Theme.S(10));
            using var brush = new SolidBrush(_selected ? Theme.SidebarActive : Theme.SidebarHover);
            g.FillPath(brush, path);
        }

        if (_selected)
        {
            using var bar = new SolidBrush(Theme.Accent);
            g.FillRectangle(bar, 0, Theme.S(12), Theme.S(3), Height - Theme.S(24));
        }

        int badge = Theme.S(32);
        var badgeRect = new Rectangle(Theme.S(14), (Height - badge) / 2, badge, badge);
        using (var bp = Theme.RoundedRect(badgeRect, Theme.S(8)))
        using (var bb = new SolidBrush(BadgeColor))
        {
            g.FillPath(bb, bp);
        }

        using (var badgeFont = new Font("Segoe UI", Badge.Length > 2 ? 7f : 9f, FontStyle.Bold))
        {
            TextRenderer.DrawText(g, Badge, badgeFont, badgeRect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        var textRect = new Rectangle(badgeRect.Right + Theme.S(12), 0, Width - badgeRect.Right - Theme.S(16), Height);
        TextRenderer.DrawText(g, Text, Font, textRect, _selected ? Color.White : (_hover ? Color.White : Theme.SidebarText),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>Base class for every tool screen: title, subtitle and a vertical stack that scrolls.</summary>
public class ToolPage : UserControl
{
    /// <summary>Add cards / rows here.</summary>
    protected readonly TableLayoutPanel Stack;

    public ToolPage(string title, string subtitle)
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        AutoScroll = true;
        DoubleBuffered = true;
        Padding = Theme.P(34, 28, 34, 28);

        Stack = Ui.Stack();
        Stack.Dock = DockStyle.Top;
        Controls.Add(Stack);

        Stack.AddRow(Ui.Text(title, Theme.Title), 0, 2);
        Stack.AddRow(Ui.Text(subtitle, Theme.Body, Theme.TextMuted), 0, 20);
    }

    protected static void Warn(string message)
    {
        MessageBox.Show(message, "Smart Tools Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
