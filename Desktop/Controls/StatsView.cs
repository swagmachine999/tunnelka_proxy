using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Tunnelka.Services;
using Tunnelka.UI;

namespace Tunnelka.Desktop;

public sealed class StatsView : Control
{
    private const double TotalHeight = 486;
    private const double Boost = 1.1;
    private static readonly FontFamily Family = new("Segoe UI");

    private List<(double Down, double Up)> _speeds = new();
    private TrafficCounters _sum = new();
    private string _period = "";
    private string _graphPeriod = "";
    private double _down;
    private double _up;
    private bool _connected;

    public StatsView()
    {
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public void SetData(string period, string graphPeriod, TrafficCounters sum, List<(double Down, double Up)> speeds, double down, double up, bool connected)
    {
        _period = period;
        _graphPeriod = graphPeriod;
        _sum = sum;
        _speeds = speeds;
        _down = down;
        _up = up;
        _connected = connected;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width, TotalHeight);

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var tileWidth = (w - 12) / 2;
        DrawTile(context, new Rect(0, 0, tileWidth, 92), L.T("Загрузка"), _down, Ui.Color(this, "AccentColor"), true);
        DrawTile(context, new Rect(tileWidth + 12, 0, tileWidth, 92), L.T("Отдача"), _up, Ui.Color(this, "PinkColor"), false);
        DrawGraph(context, new Rect(0, 106, w, 190));
        DrawTotals(context, new Rect(0, 310, w, 176));
    }

    private void DrawTile(DrawingContext context, Rect r, string title, double speed, Color color, bool down)
    {
        Card(context, r);

        var icon = new Rect(r.X + 16, r.Y + 16, 28, 28);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B)), null, icon, 9, 9);
        var pen = new Pen(new SolidColorBrush(color), 1.8, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var cx = icon.X + 14;
        var top = icon.Y + 8;
        var bottom = icon.Y + 20;
        context.DrawLine(pen, new Point(cx, top), new Point(cx, bottom));
        if (down)
        {
            context.DrawLine(pen, new Point(cx, bottom), new Point(cx - 5, bottom - 5));
            context.DrawLine(pen, new Point(cx, bottom), new Point(cx + 5, bottom - 5));
        }
        else
        {
            context.DrawLine(pen, new Point(cx, top), new Point(cx - 5, top + 5));
            context.DrawLine(pen, new Point(cx, top), new Point(cx + 5, top + 5));
        }

        Write(context, title, 13, FontWeight.SemiBold, Ui.Brush(this, "TextMutedBrush"), new Rect(icon.Right + 10, icon.Y, Math.Max(1, r.Width - 70), icon.Height));
        Write(context, ServerText.Bytes(speed) + L.T("/с"), 23, FontWeight.SemiBold, Ui.Brush(this, "TextBrush"), new Rect(r.X + 16, r.Y + 50, Math.Max(1, r.Width - 24), 32));
    }

    private void DrawGraph(DrawingContext context, Rect r)
    {
        Card(context, r);
        var muted = Ui.Brush(this, "TextMutedBrush");
        Write(context, L.F("Скорость за {0}", _graphPeriod), 13, FontWeight.SemiBold, muted, new Rect(r.X + 16, r.Y + 10, Math.Max(1, r.Width - 32), 20));

        var plot = new Rect(r.X + 16, r.Y + 40, Math.Max(1, r.Width - 32), r.Height - 56);
        var grid = new Pen(Ui.Brush(this, "BorderBrush2"), 1, new DashStyle(new double[] { 4, 4 }, 0));
        var down = _speeds.Select(s => s.Down).ToList();
        var up = _speeds.Select(s => s.Up).ToList();
        var empty = !_connected && down.All(v => v == 0) && up.All(v => v == 0);
        var top = AxisTop(Math.Max(down.DefaultIfEmpty(0).Max(), up.DefaultIfEmpty(0).Max()));

        for (var i = 0; i <= 2; i++)
        {
            var y = plot.Y + plot.Height * i / 2;
            context.DrawLine(grid, new Point(plot.X, y), new Point(plot.Right, y));
            if (empty || i == 2)
                continue;

            var label = ServerText.Bytes(top * (2 - i) / 2) + L.T("/с");
            Write(context, label, 11, FontWeight.Normal, muted, new Rect(plot.X, y - 15, plot.Width, 14), TextAlignment.Right);
        }

        if (empty)
        {
            Write(context, L.T("Подключись, чтобы увидеть трафик"), 14, FontWeight.Normal, muted, plot, TextAlignment.Center);
            return;
        }

        DrawSeries(context, plot, up, top, Ui.Color(this, "PinkColor"));
        DrawSeries(context, plot, down, top, Ui.Color(this, "AccentColor"));
    }

    private static double AxisTop(double peak)
    {
        const double Kb = 1024;
        const double Mb = 1024 * 1024;
        double[] steps = { 64 * Kb, 128 * Kb, 256 * Kb, 512 * Kb, Mb, 2 * Mb, 3 * Mb, 5 * Mb, 10 * Mb, 15 * Mb, 20 * Mb, 30 * Mb, 50 * Mb, 100 * Mb, 200 * Mb, 500 * Mb, 1000 * Mb };
        foreach (var step in steps)
        {
            if (peak * 1.05 <= step)
                return step;
        }

        return peak * 1.15;
    }

    private static void DrawSeries(DrawingContext context, Rect plot, List<double> values, double max, Color color)
    {
        if (values.Count < 2)
            return;

        var step = plot.Width / (values.Count - 1);
        var points = values.Select((v, i) => new Point(plot.X + step * i, plot.Bottom - v / max * plot.Height)).ToList();

        var area = new StreamGeometry();
        using (var ctx = area.Open())
        {
            ctx.BeginFigure(new Point(points[0].X, plot.Bottom), true);
            foreach (var point in points)
                ctx.LineTo(point);
            ctx.LineTo(new Point(points[^1].X, plot.Bottom));
            ctx.EndFigure(true);
        }

        var fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(plot.X, plot.Y, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(plot.X, plot.Bottom, RelativeUnit.Absolute),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(90, color.R, color.G, color.B), 0),
                new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1)
            }
        };
        context.DrawGeometry(fill, null, area);

        var line = new StreamGeometry();
        using (var ctx = line.Open())
        {
            ctx.BeginFigure(points[0], false);
            for (var i = 1; i < points.Count; i++)
                ctx.LineTo(points[i]);
            ctx.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(new SolidColorBrush(color), 2.2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), line);
    }

    private void DrawTotals(DrawingContext context, Rect r)
    {
        Card(context, r);
        var rows = new (string Title, long Down, long Up)[]
        {
            (L.F("Через VPN за {0}", _period), _sum.ProxyDown, _sum.ProxyUp),
            (L.F("Напрямую за {0}", _period), _sum.DirectDown, _sum.DirectUp),
            (L.F("Всего за {0}", _period), _sum.ProxyDown + _sum.DirectDown, _sum.ProxyUp + _sum.DirectUp)
        };

        var muted = Ui.Brush(this, "TextMutedBrush");
        var text = Ui.Brush(this, "TextBrush");
        var downColor = Ui.Color(this, "AccentStrongColor");
        var upColor = Ui.Color(this, "PingBadColor");
        var y = r.Y + 12;
        for (var i = 0; i < rows.Length; i++)
        {
            var (title, down, up) = rows[i];
            var row = new Rect(r.X + 16, y, Math.Max(1, r.Width - 32), 50);
            Write(context, title, 13, FontWeight.Normal, muted, new Rect(row.X, row.Y, row.Width, 20));
            var cy = row.Y + 32;
            DrawArrow(context, row.X, cy, true, downColor);
            Write(context, ServerText.Bytes(down), 14, FontWeight.SemiBold, text, new Rect(row.X + 16, row.Y + 20, Math.Max(1, row.Width / 2 - 16), 24));
            DrawArrow(context, row.X + row.Width / 2, cy, false, upColor);
            Write(context, ServerText.Bytes(up), 14, FontWeight.SemiBold, text, new Rect(row.X + row.Width / 2 + 16, row.Y + 20, Math.Max(1, row.Width / 2 - 16), 24));

            if (i < rows.Length - 1)
            {
                var divider = new Pen(Ui.Brush(this, "BorderBrush2"), 1);
                context.DrawLine(divider, new Point(row.X, row.Bottom + 2.5), new Point(row.Right, row.Bottom + 2.5));
            }

            y += 54;
        }
    }

    private static void DrawArrow(DrawingContext context, double x, double cy, bool down, Color color)
    {
        var pen = new Pen(new SolidColorBrush(color), 1.8, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var top = cy - 6;
        var bottom = cy + 6;
        context.DrawLine(pen, new Point(x + 5, top), new Point(x + 5, bottom));
        var tip = down ? bottom : top;
        var back = down ? -4.5 : 4.5;
        context.DrawLine(pen, new Point(x + 5, tip), new Point(x + 1, tip + back));
        context.DrawLine(pen, new Point(x + 5, tip), new Point(x + 9, tip + back));
    }

    private void Card(DrawingContext context, Rect r)
    {
        var rect = new Rect(r.X + 0.5, r.Y + 0.5, Math.Max(1, r.Width - 1), Math.Max(1, r.Height - 4));
        context.DrawRectangle(Ui.Brush(this, "CardBrush"), new Pen(Ui.Brush(this, "BorderBrush2"), 1), rect, 14, 14);
    }

    private static void Write(DrawingContext context, string text, double size, FontWeight weight, IBrush brush, Rect r, TextAlignment align = TextAlignment.Left)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(Family, FontStyle.Normal, weight), size * Boost, brush)
        {
            MaxTextWidth = r.Width,
            TextAlignment = align
        };
        context.DrawText(formatted, new Point(r.X, r.Y + (r.Height - formatted.Height) / 2));
    }
}
