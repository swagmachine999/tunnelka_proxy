using System.Drawing.Drawing2D;
using Tunnelka.Services;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class StatsView : ThemedControl
{
    private List<(double Down, double Up)> _speeds = new();
    private TrafficCounters _sum = new();
    private string _period = "";
    private string _graphPeriod = "";
    private double _down;
    private double _up;
    private bool _connected;

    public void SetData(string period, string graphPeriod, TrafficCounters sum, List<(double Down, double Up)> speeds, double down, double up, bool connected)
    {
        _period = period;
        _graphPeriod = graphPeriod;
        _sum = sum;
        _speeds = speeds;
        _down = down;
        _up = up;
        _connected = connected;
        Invalidate();
    }

    protected override void Draw(Graphics g)
    {
        float w = W - 4;
        var tileWidth = (w - 12) / 2;

        DrawTile(g, new RectangleF(0, 0, tileWidth, 92), L.T("Загрузка"), _down, Theme.Accent, true);
        DrawTile(g, new RectangleF(tileWidth + 12, 0, tileWidth, 92), L.T("Отдача"), _up, Theme.Pink, false);
        DrawGraph(g, new RectangleF(0, 106, w, 190));
        DrawTotals(g, new RectangleF(0, 310, w, 176));
    }

    private void DrawTile(Graphics g, RectangleF r, string title, double speed, Color color, bool down)
    {
        Card(g, r);

        var icon = new RectangleF(r.X + 16, r.Y + 16, 28, 28);
        Theme.FillRounded(g, Color.FromArgb(50, color), icon, 9);
        using (var pen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            var cx = icon.X + 14;
            var top = icon.Y + 8;
            var bottom = icon.Y + 20;
            g.DrawLine(pen, cx, top, cx, bottom);
            if (down)
            {
                g.DrawLine(pen, cx, bottom, cx - 5, bottom - 5);
                g.DrawLine(pen, cx, bottom, cx + 5, bottom - 5);
            }
            else
            {
                g.DrawLine(pen, cx, top, cx - 5, top + 5);
                g.DrawLine(pen, cx, top, cx + 5, top + 5);
            }
        }

        Theme.DrawText(g, title, Theme.CaptionBold, Theme.TextMuted, new RectangleF(icon.Right + 10, icon.Y, r.Width - 70, icon.Height));
        Theme.DrawText(g, ServerText.Bytes(speed) + L.T("/с"), Theme.Big, Theme.Text, new RectangleF(r.X + 16, r.Y + 50, r.Width - 24, 32));
    }

    private void DrawGraph(Graphics g, RectangleF r)
    {
        Card(g, r);
        Theme.DrawText(g, L.F("Скорость за {0}", _graphPeriod), Theme.CaptionBold, Theme.TextMuted, new RectangleF(r.X + 16, r.Y + 10, r.Width - 32, 20));

        var plot = new RectangleF(r.X + 16, r.Y + 40, r.Width - 32, r.Height - 56);
        using (var grid = new Pen(Theme.Border, 1f) { DashStyle = DashStyle.Dash })
        {
            for (var i = 0; i <= 3; i++)
            {
                var y = plot.Y + plot.Height * i / 3;
                g.DrawLine(grid, plot.X, y, plot.Right, y);
            }
        }

        var down = _speeds.Select(s => s.Down).ToList();
        var up = _speeds.Select(s => s.Up).ToList();
        if (!_connected && down.All(v => v == 0) && up.All(v => v == 0))
        {
            Theme.DrawText(g, L.T("Подключись, чтобы увидеть трафик"), Theme.Body, Theme.TextMuted, plot, StringAlignment.Center);
            return;
        }

        var max = Math.Max(1024, Math.Max(down.DefaultIfEmpty(0).Max(), up.DefaultIfEmpty(0).Max())) * 1.15;
        Theme.DrawText(g, ServerText.Bytes(max) + L.T("/с"), Theme.Caption, Theme.TextMuted,
            new RectangleF(r.X + 16, r.Y + 10, r.Width - 32, 20), StringAlignment.Far);

        DrawLine(g, plot, up, max, Theme.Pink);
        DrawLine(g, plot, down, max, Theme.Accent);
    }

    private static void DrawLine(Graphics g, RectangleF plot, List<double> values, double max, Color color)
    {
        if (values.Count < 2)
            return;

        var step = plot.Width / (values.Count - 1);
        var start = plot.X;
        var points = values.Select((v, i) => new PointF(start + step * i, plot.Bottom - (float)(v / max) * plot.Height)).ToArray();

        using (var area = new GraphicsPath())
        {
            area.AddLines(points);
            area.AddLine(points[points.Length - 1], new PointF(points[points.Length - 1].X, plot.Bottom));
            area.AddLine(new PointF(points[points.Length - 1].X, plot.Bottom), new PointF(points[0].X, plot.Bottom));
            area.CloseFigure();
            using var brush = new LinearGradientBrush(plot, Color.FromArgb(90, color), Color.FromArgb(0, color), 90f);
            g.FillPath(brush, area);
        }

        using var pen = new Pen(color, 2.2f) { LineJoin = LineJoin.Round };
        g.DrawLines(pen, points);
    }

    private void DrawTotals(Graphics g, RectangleF r)
    {
        Card(g, r);
        var rows = new (string Title, long Down, long Up)[]
        {
            (L.F("Через VPN за {0}", _period), _sum.ProxyDown, _sum.ProxyUp),
            (L.F("Напрямую за {0}", _period), _sum.DirectDown, _sum.DirectUp),
            (L.F("Всего за {0}", _period), _sum.ProxyDown + _sum.DirectDown, _sum.ProxyUp + _sum.DirectUp)
        };

        var y = r.Y + 12;
        for (var i = 0; i < rows.Length; i++)
        {
            var (title, down, up) = rows[i];
            var row = new RectangleF(r.X + 16, y, r.Width - 32, 50);
            Theme.DrawText(g, title, Theme.Caption, Theme.TextMuted, new RectangleF(row.X, row.Y, row.Width, 20));
            var cy = row.Y + 32;
            Theme.DrawArrow(g, row.X, cy, true, Theme.AccentStrong);
            Theme.DrawText(g, ServerText.Bytes(down), Theme.BodyBold, Theme.Text, new RectangleF(row.X + 16, row.Y + 20, row.Width / 2 - 16, 24));
            Theme.DrawArrow(g, row.X + row.Width / 2, cy, false, Theme.PingBad);
            Theme.DrawText(g, ServerText.Bytes(up), Theme.BodyBold, Theme.Text, new RectangleF(row.X + row.Width / 2 + 16, row.Y + 20, row.Width / 2 - 16, 24));

            if (i < rows.Length - 1)
            {
                using var pen = new Pen(Theme.Border);
                g.DrawLine(pen, row.X, row.Bottom + 2, row.Right, row.Bottom + 2);
            }

            y += 54;
        }
    }

    private static void Card(Graphics g, RectangleF r)
    {
        var rect = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1);
        Theme.FillRounded(g, Theme.Card, rect, 14);
        Theme.DrawRounded(g, Theme.Border, rect, 14);
    }
}
