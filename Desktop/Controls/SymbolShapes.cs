using Avalonia;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public static class SymbolShapes
{
    private static readonly IBrush StarBrush = new SolidColorBrush(Color.FromRgb(247, 196, 72));
    private static readonly IBrush InfinityBrush = new SolidColorBrush(Color.FromRgb(110, 164, 244));

    public static void DrawStar(DrawingContext context, Rect rect)
    {
        var cx = rect.X + rect.Width / 2;
        var cy = rect.Y + rect.Height / 2 + rect.Height * 0.04;
        var outer = rect.Width / 2;
        var inner = outer * 0.48;
        var points = new Point[10];
        for (var i = 0; i < 10; i++)
        {
            var radius = i % 2 == 0 ? outer : inner;
            var angle = Math.PI / 5 * i - Math.PI / 2;
            points[i] = new Point(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
        }

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(points[0], true);
            for (var i = 1; i < points.Length; i++)
                stream.LineTo(points[i]);
            stream.EndFigure(true);
        }

        var pen = new Pen(StarBrush, 1.4, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(StarBrush, pen, geometry);
    }

    public static void DrawInfinity(DrawingContext context, Rect rect)
    {
        var cy = rect.Y + rect.Height / 2;
        var w = rect.Width * 1.1;
        var x = rect.X - (w - rect.Width) / 2;
        var h = rect.Height * 0.42;

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(new Point(x + w / 2, cy), false);
            stream.CubicBezierTo(new Point(x + w * 0.75, cy - h), new Point(x + w, cy - h), new Point(x + w, cy));
            stream.CubicBezierTo(new Point(x + w, cy + h), new Point(x + w * 0.75, cy + h), new Point(x + w / 2, cy));
            stream.CubicBezierTo(new Point(x + w * 0.25, cy - h), new Point(x, cy - h), new Point(x, cy));
            stream.CubicBezierTo(new Point(x, cy + h), new Point(x + w * 0.25, cy + h), new Point(x + w / 2, cy));
            stream.EndFigure(false);
        }

        var pen = new Pen(InfinityBrush, Math.Max(1.6, rect.Height / 8), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(null, pen, geometry);
    }
}
