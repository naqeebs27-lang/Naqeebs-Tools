using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ThemePalette = SmartTools.UI.Theme;

namespace SmartTools.UI;

/// <summary>Doughnut chart that shows principal vs interest split.</summary>
public class DonutChart : Control
{
    private double _first;
    private double _second;

    public Color FirstColor { get; set; } = ThemePalette.FromHex("#0284C7");
    public Color SecondColor { get; set; } = ThemePalette.Warning;

    public DonutChart()
    {
        Width = 190;
        Height = 190;
    }

    public void SetValues(double first, double second)
    {
        _first = Math.Max(0, first);
        _second = Math.Max(0, second);
        InvalidateVisual();
    }

    private static Geometry Arc(Point center, double radius, double startDeg, double sweepDeg)
    {
        double a0 = startDeg * Math.PI / 180.0;
        double a1 = (startDeg + sweepDeg) * Math.PI / 180.0;
        var p0 = new Point(center.X + radius * Math.Cos(a0), center.Y + radius * Math.Sin(a0));
        var p1 = new Point(center.X + radius * Math.Cos(a1), center.Y + radius * Math.Sin(a1));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(p0, false);
            ctx.ArcTo(p1, new Size(radius, radius), 0, sweepDeg > 180, SweepDirection.Clockwise);
            ctx.EndFigure(false);
        }
        return geometry;
    }

    public override void Render(DrawingContext context)
    {
        double thickness = 24;
        double size = Math.Min(Bounds.Width, Bounds.Height) - thickness - 4;
        if (size <= 10) return;

        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);
        double radius = size / 2.0;

        double total = _first + _second;
        if (total <= 0)
        {
            context.DrawEllipse(null, new Pen(new SolidColorBrush(ThemePalette.Border), thickness), center, radius, radius);
            return;
        }

        if (_second <= 0)
        {
            context.DrawEllipse(null, new Pen(new SolidColorBrush(FirstColor), thickness), center, radius, radius);
            return;
        }

        double sweepFirst = 360.0 * _first / total;
        context.DrawGeometry(null, new Pen(new SolidColorBrush(FirstColor), thickness), Arc(center, radius, -90, sweepFirst));
        context.DrawGeometry(null, new Pen(new SolidColorBrush(SecondColor), thickness), Arc(center, radius, -90 + sweepFirst, 360 - sweepFirst));
    }
}
