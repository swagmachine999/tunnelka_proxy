using System.Drawing.Drawing2D;

namespace VpnClient.UI.Controls;

public class HeroView : Control
{
    private readonly System.Windows.Forms.Timer _animation = new() { Interval = 40 };
    private float _time;
    private int _tick;

    private RectangleF _powerRect;
    private RectangleF _kittenRect;
    private RectangleF _nameRect;
    private RectangleF _pingRect;
    private RectangleF _pingResultRect;
    private RectangleF _toggleRect;

    private bool _hoverPower;
    private bool _hoverPing;
    private bool _hoverToggle;

    private bool _connected;
    private bool _proxyEnabled = true;
    private string _elapsed = "00:00:00";
    private string _serverName = "";
    private string? _serverCode;
    private string _pingText = "";
    private Color _pingColor = Theme.TextMuted;

    public event EventHandler? PowerClicked;
    public event EventHandler? PingClicked;
    public event EventHandler? ProxyToggled;

    public HeroView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.HeroBottom;

        _animation.Tick += (_, _) => Animate();
        _animation.Start();
    }

    public bool Connected
    {
        get => _connected;
        set { _connected = value; Invalidate(); }
    }

    public bool ProxyEnabled
    {
        get => _proxyEnabled;
        set { _proxyEnabled = value; Invalidate(); }
    }

    public string ElapsedText
    {
        get => _elapsed;
        set { _elapsed = value; Invalidate(Rectangle.Ceiling(_powerRect)); }
    }

    public void SetServer(string name, string? code)
    {
        _serverName = name;
        _serverCode = code;
        Invalidate();
    }

    public void SetPing(string text, Color color)
    {
        _pingText = text;
        _pingColor = color;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _animation.Dispose();
        base.Dispose(disposing);
    }

    private void Animate()
    {
        _time += 0.04f;
        _tick++;

        var area = RectangleF.Union(Inflate(_powerRect, 70), _kittenRect);
        Invalidate(Rectangle.Ceiling(area));
    }

    private void ComputeLayout()
    {
        float w = ClientSize.Width;
        float h = ClientSize.Height;
        var scale = Math.Max(0.55f, Math.Min(1f, h / 660f));

        var diameter = 200 * scale;
        var kittenW = 200 * scale;
        var kittenH = 160 * scale;
        var gap = 46 * scale;
        var total = diameter + gap + kittenH + 12 + 34 + 18 + 44 + 26;
        var top = Math.Max(76, (h - total) / 2 + 24);
        var cx = w / 2;

        _powerRect = new RectangleF(cx - diameter / 2, top, diameter, diameter);
        _kittenRect = new RectangleF(cx - kittenW / 2, _powerRect.Bottom + gap, kittenW, kittenH);
        _nameRect = new RectangleF(16, _kittenRect.Bottom + 12, w - 32, 34);
        _pingRect = new RectangleF(cx - 100, _nameRect.Bottom + 18, 200, 44);
        _pingResultRect = new RectangleF(16, _pingRect.Bottom + 4, w - 32, 22);
        _toggleRect = new RectangleF(w - 24 - 208, 24, 208, 40);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width < 10 || ClientSize.Height < 10)
            return;

        ComputeLayout();
        var g = e.Graphics;
        Theme.Smooth(g);

        DrawBackground(g);
        DrawToggle(g);
        DrawPower(g);
        KittenPainter.Draw(g, _kittenRect, _connected, _time, _connected && _tick % 110 < 4);
        DrawServer(g);
        DrawPingButton(g);
    }

    private void DrawBackground(Graphics g)
    {
        var rect = new Rectangle(Point.Empty, ClientSize);
        using (var brush = new LinearGradientBrush(rect, Theme.HeroTop, Theme.HeroBottom, 90f))
            g.FillRectangle(brush, rect);

        float w = rect.Width;
        float h = rect.Height;
        DrawGlow(g, new RectangleF(w * 0.55f, -h * 0.25f, w * 0.8f, w * 0.8f), Color.FromArgb(90, Theme.Pink));
        DrawGlow(g, new RectangleF(-w * 0.35f, h * 0.45f, w * 0.9f, w * 0.9f), Color.FromArgb(80, Theme.Accent));
    }

    private static void DrawGlow(Graphics g, RectangleF r, Color center)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(r);
        using var brush = new PathGradientBrush(path)
        {
            CenterColor = center,
            SurroundColors = new[] { Color.FromArgb(0, center) }
        };
        g.FillPath(brush, path);
    }

    private void DrawToggle(Graphics g)
    {
        var r = _toggleRect;
        Theme.FillRounded(g, Color.FromArgb(_hoverToggle ? 245 : 210, Color.White), r, r.Height / 2);
        Theme.DrawRounded(g, Theme.Border, r, r.Height / 2);

        Theme.DrawText(g, "Системный прокси", Theme.Body, Theme.Text, new RectangleF(r.X + 16, r.Y, r.Width - 70, r.Height));

        var track = new RectangleF(r.Right - 54, r.Y + (r.Height - 22) / 2, 40, 22);
        Theme.FillRounded(g, _proxyEnabled ? Theme.Accent : Color.FromArgb(221, 212, 234), track, 11);

        var knobX = _proxyEnabled ? track.Right - 19 : track.X + 3;
        using var knob = new SolidBrush(Color.White);
        g.FillEllipse(knob, knobX, track.Y + 3, 16, 16);
    }

    private void DrawPower(Graphics g)
    {
        var r = _powerRect;
        var pulse = _connected ? (float)(Math.Sin(_time * 2.2) + 1) / 2 : 0f;
        var ringColor = _connected ? Theme.Pink : Theme.Accent;

        for (var i = 0; i < 3; i++)
        {
            var alpha = _connected ? 80 - i * 24 : 40 - i * 12;
            using var pen = new Pen(Color.FromArgb(alpha, ringColor), 2f);
            g.DrawEllipse(pen, Inflate(r, 16 + i * 18 + pulse * 7));
        }

        DrawGlow(g, Inflate(r, 30), Color.FromArgb(_connected ? 110 : 60, ringColor));

        if (_connected)
        {
            using var brush = new LinearGradientBrush(r, Theme.Pink, Theme.Accent, 45f);
            g.FillEllipse(brush, r);
        }
        else
        {
            g.FillEllipse(Brushes.White, r);
            using var border = new Pen(Theme.Border, 2f);
            g.DrawEllipse(border, r);
        }

        if (_hoverPower)
        {
            using var hover = new SolidBrush(_connected ? Color.FromArgb(35, Color.White) : Color.FromArgb(18, Theme.Accent));
            g.FillEllipse(hover, r);
        }

        var iconColor = _connected ? Color.White : Theme.Accent;
        var size = r.Width * 0.24f;
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height * 0.4f;
        using (var pen = new Pen(iconColor, Math.Max(3f, r.Width * 0.028f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawArc(pen, cx - size / 2, cy - size / 2, size, size, -60, 300);
            g.DrawLine(pen, cx, cy - size * 0.62f, cx, cy - size * 0.08f);
        }

        var textTop = r.Y + r.Height * 0.6f;
        if (_connected)
        {
            Theme.DrawText(g, "ПОДКЛЮЧЕНО", Theme.Status, Color.FromArgb(235, Color.White), new RectangleF(r.X, textTop, r.Width, 18), StringAlignment.Center);
            Theme.DrawText(g, _elapsed, Theme.Timer, Color.White, new RectangleF(r.X, textTop + 18, r.Width, 28), StringAlignment.Center);
        }
        else
        {
            Theme.DrawText(g, "ОТКЛЮЧЕНО", Theme.Status, Theme.TextMuted, new RectangleF(r.X, textTop, r.Width, 18), StringAlignment.Center);
        }
    }

    private void DrawServer(Graphics g)
    {
        if (_serverName.Length == 0)
        {
            Theme.DrawText(g, "Выбери сервер", Theme.ServerName, Theme.TextMuted, _nameRect, StringAlignment.Center);
            return;
        }

        const float badge = 28;
        const float spacing = 10;
        var maxText = _nameRect.Width - badge - spacing;
        var textWidth = Math.Min(maxText, g.MeasureString(_serverName, Theme.ServerName).Width);
        var x = _nameRect.X + (_nameRect.Width - badge - spacing - textWidth) / 2;

        Theme.DrawBadge(g, new RectangleF(x, _nameRect.Y + (_nameRect.Height - badge) / 2, badge, badge), _serverCode);
        Theme.DrawText(g, _serverName, Theme.ServerName, Theme.Text, new RectangleF(x + badge + spacing, _nameRect.Y, textWidth + 4, _nameRect.Height));
    }

    private void DrawPingButton(Graphics g)
    {
        var r = _pingRect;
        using (var brush = new LinearGradientBrush(r, _hoverPing ? Theme.AccentDark : Theme.Accent, _hoverPing ? Theme.Accent : Theme.Pink, 0f))
        using (var path = Theme.RoundedRect(r, r.Height / 2))
            g.FillPath(brush, path);

        Theme.DrawText(g, "Тест пинга", Theme.BodyBold, Color.White, r, StringAlignment.Center);

        if (_pingText.Length > 0)
            Theme.DrawText(g, _pingText, Theme.CaptionBold, _pingColor, _pingResultRect, StringAlignment.Center);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var power = InCircle(_powerRect, e.Location);
        var ping = _pingRect.Contains(e.Location);
        var toggle = _toggleRect.Contains(e.Location);

        if (power != _hoverPower || ping != _hoverPing || toggle != _hoverToggle)
        {
            _hoverPower = power;
            _hoverPing = ping;
            _hoverToggle = toggle;
            Cursor = power || ping || toggle ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverPower = _hoverPing = _hoverToggle = false;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left)
            return;

        if (InCircle(_powerRect, e.Location))
            PowerClicked?.Invoke(this, EventArgs.Empty);
        else if (_pingRect.Contains(e.Location))
            PingClicked?.Invoke(this, EventArgs.Empty);
        else if (_toggleRect.Contains(e.Location))
            ProxyToggled?.Invoke(this, EventArgs.Empty);
    }

    private static bool InCircle(RectangleF r, Point p)
    {
        var dx = p.X - (r.X + r.Width / 2);
        var dy = p.Y - (r.Y + r.Height / 2);
        return dx * dx + dy * dy <= r.Width * r.Width / 4;
    }

    private static RectangleF Inflate(RectangleF r, float by) =>
        new(r.X - by, r.Y - by, r.Width + by * 2, r.Height + by * 2);
}
