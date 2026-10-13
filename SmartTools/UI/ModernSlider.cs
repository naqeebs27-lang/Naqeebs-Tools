using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools.UI;

/// <summary>
/// Smooth horizontal slider with a round thumb (replacement for the HTML range input).
/// Custom drawn, same behaviour and public surface as the WinForms version.
/// </summary>
public class ModernSlider : Control
{
    private double _min;
    private double _max = 100;
    private double _step = 1;
    private double _value;
    private bool _drag;
    private Color _accent = ThemePalette.Accent;

    public event EventHandler ValueChanged;

    public Color AccentColor
    {
        get => _accent;
        set { _accent = value; InvalidateVisual(); }
    }

    public ModernSlider()
    {
        Focusable = true;
        Height = 30;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public double Minimum
    {
        get => _min;
        set
        {
            _min = value;
            if (_max < _min) _max = _min;
            Value = _value;
            InvalidateVisual();
        }
    }

    public double Maximum
    {
        get => _max;
        set
        {
            _max = value;
            if (_max < _min) _min = _max;
            Value = _value;
            InvalidateVisual();
        }
    }

    public double Step
    {
        get => _step;
        set { _step = value; InvalidateVisual(); }
    }

    public double Value
    {
        get => _value;
        set
        {
            double v = Clamp(Snap(value));
            if (Math.Abs(v - _value) < 1e-12)
            {
                InvalidateVisual();
                return;
            }
            _value = v;
            InvalidateVisual();
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

    private const double Pad = 11;

    private void SetFromX(double x)
    {
        double span = Math.Max(1, Bounds.Width - 2 * Pad);
        double t = (x - Pad) / span;
        if (t < 0) t = 0;
        if (t > 1) t = 1;
        Value = _min + t * (_max - _min);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Focus();
            _drag = true;
            e.Pointer.Capture(this);
            SetFromX(e.GetPosition(this).X);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_drag) SetFromX(e.GetPosition(this).X);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_drag)
        {
            _drag = false;
            e.Pointer.Capture(null);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        double inc = _step > 0 ? _step : (_max - _min) / 100.0;
        if (e.Key == Key.Left || e.Key == Key.Down)
        {
            Value = _value - inc;
            e.Handled = true;
        }
        else if (e.Key == Key.Right || e.Key == Key.Up)
        {
            Value = _value + inc;
            e.Handled = true;
        }
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;

        // transparent fill makes the whole strip hit-testable
        context.FillRectangle(Brushes.Transparent, new Rect(0, 0, w, h));

        double trackH = 6;
        double cy = h / 2;
        double trackW = Math.Max(1, w - 2 * Pad);

        var trackRect = new Rect(Pad, cy - trackH / 2, trackW, trackH);
        context.DrawRectangle(new SolidColorBrush(ThemePalette.Border), null, trackRect, trackH / 2, trackH / 2);

        double t = _max > _min ? (_value - _min) / (_max - _min) : 0;
        double x = Pad + Math.Round(t * trackW);

        double fillW = x - Pad;
        if (fillW >= trackH)
        {
            var fillRect = new Rect(Pad, cy - trackH / 2, fillW, trackH);
            context.DrawRectangle(new SolidColorBrush(_accent), null, fillRect, trackH / 2, trackH / 2);
        }

        double r = 10; // thumb radius (20 px diameter)
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), null, new Point(x, cy + 2), r, r);
        context.DrawEllipse(Brushes.White, new Pen(new SolidColorBrush(_accent), 3), new Point(x, cy), r - 1.5, r - 1.5);
    }
}
