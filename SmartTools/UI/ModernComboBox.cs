using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SmartTools.UI;

/// <summary>Rounded drop-down selector (read-only list) with a styled popup.</summary>
public class ModernComboBox : Control
{
    private readonly List<string> _items = new List<string>();
    private int _selected = -1;
    private bool _hover;
    private ToolStripDropDown _drop;
    private long _closedAt;

    public event EventHandler SelectedIndexChanged;

    public ModernComboBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = Theme.S(42);
        Font = Theme.Body;
        Cursor = Cursors.Hand;
    }

    public IReadOnlyList<string> Items => _items;

    public void SetItems(params string[] items)
    {
        _items.Clear();
        _items.AddRange(items);
        _selected = _items.Count > 0 ? 0 : -1;
        Invalidate();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selected;
        set
        {
            int v = value;
            if (v < -1 || v >= _items.Count) v = -1;
            if (v == _selected) return;
            _selected = v;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string SelectedItem => _selected >= 0 && _selected < _items.Count ? _items[_selected] : "";

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

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (Environment.TickCount64 - _closedAt < 250) return;
        ShowList();
    }

    private void ShowList()
    {
        if (_items.Count == 0) return;

        var list = new ListBox
        {
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = Theme.S(34),
            IntegralHeight = false,
            Font = Font,
            BackColor = Color.White
        };
        foreach (var s in _items) list.Items.Add(s);
        if (_selected >= 0) list.SelectedIndex = _selected;

        int visible = Math.Min(_items.Count, 8);
        list.Width = Math.Max(Width, Theme.S(120));
        list.Height = visible * list.ItemHeight + Theme.S(4);

        list.DrawItem += (s, ev) =>
        {
            if (ev.Index < 0) return;
            bool sel = (ev.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (var bg = new SolidBrush(sel ? Theme.AccentSoft : Color.White))
            {
                ev.Graphics.FillRectangle(bg, ev.Bounds);
            }
            var textRect = new Rectangle(ev.Bounds.X + Theme.S(12), ev.Bounds.Y,
                ev.Bounds.Width - Theme.S(12), ev.Bounds.Height);
            TextRenderer.DrawText(ev.Graphics, list.Items[ev.Index].ToString(), Font, textRect,
                sel ? Theme.Accent : Theme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        };

        list.MouseMove += (s, ev) =>
        {
            int i = list.IndexFromPoint(ev.Location);
            if (i >= 0 && i != list.SelectedIndex) list.SelectedIndex = i;
        };

        list.Click += (s, ev) =>
        {
            if (list.SelectedIndex >= 0)
            {
                SelectedIndex = list.SelectedIndex;
            }
            _drop?.Close();
        };

        var host = new ToolStripControlHost(list)
        {
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false,
            Size = list.Size
        };

        _drop = new ToolStripDropDown
        {
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        _drop.Items.Add(host);
        _drop.Closed += (s, ev) => _closedAt = Environment.TickCount64;
        _drop.Show(this, new Point(0, Height + Theme.S(2)));
        list.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentBack(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(rect, Theme.S(9));
        using (var brush = new SolidBrush(Color.White))
        {
            g.FillPath(brush, path);
        }
        using (var pen = new Pen(_hover ? Theme.Accent : Theme.BorderStrong, _hover ? 1.6f : 1f))
        {
            g.DrawPath(pen, path);
        }

        var textRect = new Rectangle(Theme.S(12), 0, Width - Theme.S(44), Height);
        TextRenderer.DrawText(g, SelectedItem, Font, textRect, Theme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        // chevron
        int cx = Width - Theme.S(22);
        int cy = Height / 2;
        int w = Theme.S(5);
        using var chevron = new Pen(Theme.TextMuted, 2f);
        chevron.StartCap = LineCap.Round;
        chevron.EndCap = LineCap.Round;
        g.DrawLine(chevron, cx - w, cy - w / 2, cx, cy + w / 2);
        g.DrawLine(chevron, cx, cy + w / 2, cx + w, cy - w / 2);
    }
}
