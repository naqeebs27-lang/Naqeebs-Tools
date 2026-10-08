using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SmartTools.UI;

/// <summary>Smooth horizontal slider with a round thumb (replacement for the HTML range input).</summary>
public class ModernSlider : Control
{
    private double _min;
    private double _max = 100;
    private double _step = 1;
    private double _value;
    private bool _drag;

    public event EventHandler ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor { get; set; } = Theme.Accent;

    public ModernSlider()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        TabStop = true;
        Height = Theme.S(30);
        Cursor = Cursors.Hand;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Minimum
    {
        get => _min;
        set
        {
            _min = value;
            if (_max < _min) _max = _min;
            Value = _value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Maximum
    {
        get => _max;
        set
        {
            _max = value;
            if (_max < _min) _min = _max;
            Value = _value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Step
    {
        get => _step;
        set { _step = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Value
    {
        get => _value;
        set
        {
            double v = Clamp(Snap(value));
            if (Math.Abs(v - _value) < 1e-12)
            {
                Invalidate();
                return;
            }
            _value = v;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private double Snap(double v)
    {
        if (_step > 0)
        {
            v = Math.Round((v - _min) / _step) * _step + _min;
        }
        return Math.Round(v, 6);
    }

    private double Clamp(double v)
    {
        if (v < _min) return _min;
        if (v > _max) return _max;
        return v;
    }

    private int Pad => Theme.S(11);

    private void SetFromX(int x)
    {
        double span = Math.Max(1, Width - 2 * Pad);
        double t = (x - Pad) / span;
        if (t < 0) t = 0;
        if (t > 1) t = 1;
        Value = _min + t * (_max - _min);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            Focus();
            _drag = true;
            Capture = true;
            SetFromX(e.X);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_drag) SetFromX(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _drag = false;
        Capture = false;
    }

    protected override bool IsInputKey(Keys keyData)
    {
        if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down)
        {
            return true;
        }
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        double inc = _step > 0 ? _step : (_max - _min) / 100.0;
        if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) Value = _value - inc;
        else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) Value = _value + inc;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentBack(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int pad = Pad;
        int trackH = Theme.S(6);
        int cy = Height / 2;
        int trackW = Math.Max(1, Width - 2 * pad);

        var trackRect = new Rectangle(pad, cy - trackH / 2, trackW, trackH);
        using (var trackPath = Theme.RoundedRect(trackRect, trackH / 2))
        using (var trackBrush = new SolidBrush(Theme.Border))
        {
            g.FillPath(trackBrush, trackPath);
        }

        double t = _max > _min ? (_value - _min) / (_max - _min) : 0;
        int x = pad + (int)Math.Round(t * trackW);

        int fillW = x - pad;
        if (fillW >= trackH)
        {
            var fillRect = new Rectangle(pad, cy - trackH / 2, fillW, trackH);
            using var fillPath = Theme.RoundedRect(fillRect, trackH / 2);
            using var fillBrush = new SolidBrush(AccentColor);
            g.FillPath(fillBrush, fillPath);
        }

        int d = Theme.S(20);
        var thumb = new Rectangle(x - d / 2, cy - d / 2, d, d);
        using (var shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
        {
            g.FillEllipse(shadow, thumb.X, thumb.Y + 2, thumb.Width, thumb.Height);
        }
        using (var white = new SolidBrush(Color.White))
        {
            g.FillEllipse(white, thumb);
        }
        using (var ring = new Pen(AccentColor, 3f))
        {
            g.DrawEllipse(ring, thumb.X + 1, thumb.Y + 1, thumb.Width - 2, thumb.Height - 2);
        }
    }
}

/// <summary>Doughnut chart that shows principal vs interest split.</summary>
public class DonutChart : Control
{
    private double _first;
    private double _second;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FirstColor { get; set; } = Theme.FromHex("#0284C7");

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SecondColor { get; set; } = Theme.FromHex("#F59E0B");

    public DonutChart()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(Theme.S(190), Theme.S(190));
    }

    public void SetValues(double first, double second)
    {
        _first = Math.Max(0, first);
        _second = Math.Max(0, second);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentBack(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float thickness = Theme.S(24);
        float size = Math.Min(Width, Height) - thickness - 4;
        if (size <= 10) return;
        float x = (Width - size) / 2f;
        float y = (Height - size) / 2f;
        var rect = new RectangleF(x, y, size, size);

        double total = _first + _second;
        if (total <= 0)
        {
            using var empty = new Pen(Theme.Border, thickness);
            g.DrawEllipse(empty, rect);
            return;
        }

        if (_second <= 0)
        {
            using var only = new Pen(FirstColor, thickness);
            g.DrawEllipse(only, rect);
            return;
        }

        float sweepFirst = (float)(360.0 * _first / total);
        using (var p1 = new Pen(FirstColor, thickness))
        {
            p1.StartCap = LineCap.Flat;
            p1.EndCap = LineCap.Flat;
            g.DrawArc(p1, rect, -90f, sweepFirst);
        }
        using (var p2 = new Pen(SecondColor, thickness))
        {
            p2.StartCap = LineCap.Flat;
            p2.EndCap = LineCap.Flat;
            g.DrawArc(p2, rect, -90f + sweepFirst, 360f - sweepFirst);
        }
    }
}
