using System.Drawing.Drawing2D;
using VpnClient.Services;

namespace VpnClient.UI.Pages;

public class StatsView : Control
{
    private const int HistorySize = 60;

    private readonly List<double> _down = new();
    private readonly List<double> _up = new();
    private TrafficCounters _session = new();
    private long _totalDown;
    private long _totalUp;
    private bool _connected;

    public StatsView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void SetConnected(bool connected)
    {
        _connected = connected;
        if (connected)
        {
            _down.Clear();
            _up.Clear();
            _session = new TrafficCounters();
        }
        Invalidate();
    }

    public void Push(TrafficCounters session, double downSpeed, double upSpeed)
    {
        _session = session;
        _down.Add(downSpeed);
        _up.Add(upSpeed);
        if (_down.Count > HistorySize)
        {
            _down.RemoveAt(0);
            _up.RemoveAt(0);
        }
        Invalidate();
    }

    public void SetTotals(long down, long up)
    {
        _totalDown = down;
        _totalUp = up;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Surface);

        float w = Width - 4;
        var tileWidth = (w - 12) / 2;

        DrawTile(g, new RectangleF(0, 0, tileWidth, 92), "Загрузка", Last(_down), Theme.Accent, true);
        DrawTile(g, new RectangleF(tileWidth + 12, 0, tileWidth, 92), "Отдача", Last(_up), Theme.Pink, false);
        DrawGraph(g, new RectangleF(0, 106, w, 190));
        DrawTotals(g, new RectangleF(0, 310, w, 176));
    }

    private static double Last(List<double> values) => values.Count == 0 ? 0 : values[values.Count - 1];

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
        Theme.DrawText(g, ServerText.Bytes(speed) + "/с", Theme.Big, Theme.Text, new RectangleF(r.X + 16, r.Y + 50, r.Width - 24, 32));
    }

    private void DrawGraph(Graphics g, RectangleF r)
    {
        Card(g, r);
        Theme.DrawText(g, "Скорость за минуту", Theme.CaptionBold, Theme.TextMuted, new RectangleF(r.X + 16, r.Y + 10, r.Width - 32, 20));

        var plot = new RectangleF(r.X + 16, r.Y + 40, r.Width - 32, r.Height - 56);
        using (var grid = new Pen(Theme.Border, 1f) { DashStyle = DashStyle.Dash })
        {
            for (var i = 0; i <= 3; i++)
            {
                var y = plot.Y + plot.Height * i / 3;
                g.DrawLine(grid, plot.X, y, plot.Right, y);
            }
        }

        if (!_connected && _down.Count == 0)
        {
            Theme.DrawText(g, "Подключись, чтобы увидеть трафик", Theme.Body, Theme.TextMuted, plot, StringAlignment.Center);
            return;
        }

        var max = Math.Max(1024, Math.Max(_down.DefaultIfEmpty(0).Max(), _up.DefaultIfEmpty(0).Max())) * 1.15;
        Theme.DrawText(g, ServerText.Bytes(max) + "/с", Theme.Caption, Theme.TextMuted,
            new RectangleF(r.X + 16, r.Y + 10, r.Width - 32, 20), StringAlignment.Far);

        DrawLine(g, plot, _up, max, Theme.Pink);
        DrawLine(g, plot, _down, max, Theme.Accent);
    }

    private static void DrawLine(Graphics g, RectangleF plot, List<double> values, double max, Color color)
    {
        if (values.Count < 2)
            return;

        var step = plot.Width / (HistorySize - 1);
        var start = plot.Right - step * (values.Count - 1);
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
            ("Через VPN за сессию", _session.ProxyDown, _session.ProxyUp),
            ("Напрямую за сессию", _session.DirectDown, _session.DirectUp),
            ("Через VPN за всё время", _totalDown, _totalUp)
        };

        var y = r.Y + 12;
        for (var i = 0; i < rows.Length; i++)
        {
            var (title, down, up) = rows[i];
            var row = new RectangleF(r.X + 16, y, r.Width - 32, 50);
            Theme.DrawText(g, title, Theme.Caption, Theme.TextMuted, new RectangleF(row.X, row.Y, row.Width, 20));
            Theme.DrawText(g, $"↓ {ServerText.Bytes(down)}", Theme.BodyBold, Theme.Text, new RectangleF(row.X, row.Y + 20, row.Width / 2, 24));
            Theme.DrawText(g, $"↑ {ServerText.Bytes(up)}", Theme.BodyBold, Theme.Text, new RectangleF(row.X + row.Width / 2, row.Y + 20, row.Width / 2, 24));

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
