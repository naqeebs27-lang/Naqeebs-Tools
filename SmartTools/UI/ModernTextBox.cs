using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SmartTools.UI;

/// <summary>Rounded text input with optional prefix / suffix text (e.g. "PKR" or "%").</summary>
public class ModernTextBox : Control
{
    private readonly TextBox _tb = new TextBox();
    private bool _focused;
    private string _prefix = "";
    private string _suffix = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool NumericOnly { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AllowDecimal { get; set; } = true;

    public event EventHandler EnterPressed;

    public ModernTextBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;

        Height = Theme.S(42);
        Font = Theme.Body;

        _tb.BorderStyle = BorderStyle.None;
        _tb.Font = Font;
        _tb.BackColor = Color.White;
        _tb.ForeColor = Theme.TextPrimary;
        Controls.Add(_tb);

        _tb.GotFocus += (s, e) => { _focused = true; Invalidate(); };
        _tb.LostFocus += (s, e) => { _focused = false; Invalidate(); };
        _tb.TextChanged += (s, e) => OnTextChanged(EventArgs.Empty);
        _tb.KeyPress += OnInnerKeyPress;
        _tb.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && !_tb.Multiline)
            {
                e.SuppressKeyPress = true;
                EnterPressed?.Invoke(this, EventArgs.Empty);
            }
        };

        DoLayoutInner();
    }

    public override string Text
    {
        get => _tb.Text;
        set => _tb.Text = value ?? "";
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Prefix
    {
        get => _prefix;
        set { _prefix = value ?? ""; DoLayoutInner(); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Suffix
    {
        get => _suffix;
        set { _suffix = value ?? ""; DoLayoutInner(); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Placeholder
    {
        get => _tb.PlaceholderText;
        set => _tb.PlaceholderText = value ?? "";
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Multiline
    {
        get => _tb.Multiline;
        set
        {
            _tb.Multiline = value;
            if (value)
            {
                _tb.AcceptsReturn = true;
                _tb.ScrollBars = ScrollBars.Vertical;
            }
            DoLayoutInner();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UsePasswordChar
    {
        get => _tb.UseSystemPasswordChar;
        set => _tb.UseSystemPasswordChar = value;
    }

    public void SelectEnd()
    {
        _tb.SelectionStart = _tb.TextLength;
        _tb.SelectionLength = 0;
    }

    public void FocusInput()
    {
        _tb.Focus();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _tb.Font = Font;
        DoLayoutInner();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        DoLayoutInner();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _tb.Focus();
    }

    private void OnInnerKeyPress(object sender, KeyPressEventArgs e)
    {
        if (!NumericOnly) return;
        if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
        if (AllowDecimal && e.KeyChar == '.' && !_tb.Text.Contains('.')) return;
        e.Handled = true;
    }

    private void DoLayoutInner()
    {
        int pad = Theme.S(12);
        int left = pad;
        int right = pad;

        if (_prefix.Length > 0)
        {
            left += TextRenderer.MeasureText(_prefix, Font, Size.Empty, TextFormatFlags.NoPadding).Width + Theme.S(8);
        }
        if (_suffix.Length > 0)
        {
            right += TextRenderer.MeasureText(_suffix, Font, Size.Empty, TextFormatFlags.NoPadding).Width + Theme.S(8);
        }

        int w = Math.Max(10, Width - left - right);
        if (_tb.Multiline)
        {
            _tb.SetBounds(left, Theme.S(10), w, Math.Max(10, Height - Theme.S(20)));
        }
        else
        {
            _tb.Width = w;
            _tb.Location = new Point(left, Math.Max(0, (Height - _tb.Height) / 2));
        }
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
        using (var pen = new Pen(_focused ? Theme.Accent : Theme.BorderStrong, _focused ? 1.8f : 1f))
        {
            g.DrawPath(pen, path);
        }

        if (_prefix.Length > 0)
        {
            TextRenderer.DrawText(g, _prefix, Font, new Rectangle(Theme.S(12), 0, Width, Height),
                Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        if (_suffix.Length > 0)
        {
            TextRenderer.DrawText(g, _suffix, Font, new Rectangle(0, 0, Width - Theme.S(12), Height),
                Theme.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tb.Dispose();
        }
        base.Dispose(disposing);
    }
}
