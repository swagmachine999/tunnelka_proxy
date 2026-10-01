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
    private RectangleF _refreshRect;
    private RectangleF _pingResultRect;
    private RectangleF _toggleRect;

    private bool _hoverPower;
    private bool _hoverPing;
    private bool _hoverRefresh;
    private bool _hoverToggle;

    private bool _connected;
    private bool _tun;
    private string _elapsed = "00:00:00";
    private IReadOnlyList<NamePart> _serverParts = Array.Empty<NamePart>();
    private string? _serverCode;
    private string _pingText = "";
    private bool _busy;
    private string? _speedDown;
    private string? _speedUp;
    private Color _pingColor = Theme.TextMuted;

    public event EventHandler? PowerClicked;
    public event EventHandler? PingClicked;
    public event EventHandler? RefreshClicked;
    public event Action<bool>? ModeSelected;

    public HeroView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        _animation.Tick += (_, _) => Animate();
        _animation.Start();
    }

    public bool Connected
    {
        get => _connected;
        set { _connected = value; Invalidate(); }
    }

    public bool Tun
    {
        get => _tun;
        set { _tun = value; Invalidate(); }
    }

    public string ElapsedText
    {
        get => _elapsed;
        set { _elapsed = value; Invalidate(ToDevice(_powerRect)); }
    }

    public void SetServer(IReadOnlyList<NamePart> parts, string? code)
    {
        _serverParts = parts;
        _serverCode = code;
        Invalidate();
    }

    public void SetSpeed(string? down, string? up)
    {
        _speedDown = down;
        _speedUp = up;
        Invalidate(new Rectangle(0, 0, ClientSize.Width / 2, Theme.Px(80)));
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        Invalidate(ToDevice(_pingResultRect));
    }

    public void SetPing(string text, Color color)
    {
        _busy = false;
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
        Invalidate(ToDevice(area));
        if (_busy)
            Invalidate(ToDevice(_pingResultRect));
    }

    private void ComputeLayout()
    {
        var w = ClientSize.Width / Theme.S;
        var h = ClientSize.Height / Theme.S;
        var scale = Math.Max(0.55f, Math.Min(1f, h / 660f));

        var diameter = 150 * scale;
        var kittenW = 190 * scale;
        var kittenH = 152 * scale;
        var gap = 72 * scale;
        var total = diameter + gap + kittenH + 12 + 34 + 18 + 44 + 26;
        var top = Math.Max(76, (h - total) / 2 + 24);
        var cx = w / 2;

        _powerRect = new RectangleF(cx - diameter / 2, top, diameter, diameter);
        _kittenRect = new RectangleF(cx - kittenW / 2, _powerRect.Bottom + gap, kittenW, kittenH);
        _nameRect = new RectangleF(16, _kittenRect.Bottom + 12, w - 32, 34);
        var buttonWidth = Math.Min(180f, (w - 48) / 2);
        _refreshRect = new RectangleF(cx - buttonWidth - 6, _nameRect.Bottom + 18, buttonWidth, 44);
        _pingRect = new RectangleF(cx + 6, _nameRect.Bottom + 18, buttonWidth, 44);
        _pingResultRect = new RectangleF(16, _pingRect.Bottom + 16, w - 32, 22);
        _toggleRect = new RectangleF(w - 24 - 190, 24, 190, 40);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width < 10 || ClientSize.Height < 10)
            return;

        ComputeLayout();
        var g = e.Graphics;
        Theme.Begin(g, Theme.HeroBottom);

        DrawBackground(g);
        DrawToggle(g);
        DrawSpeed(g);
        DrawPower(g);
        KittenPainter.Draw(g, _kittenRect, _connected, _time, _connected && _tick % 110 < 4);
        DrawServer(g);
        DrawButtons(g);
    }

    private void DrawBackground(Graphics g)
    {
        var rect = new RectangleF(0, 0, ClientSize.Width / Theme.S, ClientSize.Height / Theme.S);
        using (var brush = new LinearGradientBrush(rect, Theme.HeroTop, Theme.HeroBottom, 90f))
            g.FillRectangle(brush, rect);

        float w = rect.Width;
        float h = rect.Height;
        var glow = Theme.IsDark ? 45 : 90;
        DrawGlow(g, new RectangleF(w * 0.55f, -h * 0.25f, w * 0.8f, w * 0.8f), Color.FromArgb(glow, Theme.Pink));
        DrawGlow(g, new RectangleF(-w * 0.35f, h * 0.45f, w * 0.9f, w * 0.9f), Color.FromArgb(glow, Theme.Accent));
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

    private RectangleF ModeSegment(bool tun)
    {
        var half = (_toggleRect.Width - 8) / 2;
        return new RectangleF(_toggleRect.X + 4 + (tun ? half : 0), _toggleRect.Y + 4, half, _toggleRect.Height - 8);
    }

    private void DrawToggle(Graphics g)
    {
        var r = _toggleRect;
        Theme.FillRounded(g, Color.FromArgb(_hoverToggle ? 250 : 215, Theme.Card), r, r.Height / 2);
        Theme.DrawRounded(g, Theme.Border, r, r.Height / 2);

        foreach (var tun in new[] { false, true })
        {
            var segment = ModeSegment(tun);
            var active = tun == _tun;
            if (active)
                Theme.FillRounded(g, Theme.Accent, segment, segment.Height / 2);
            Theme.DrawText(g, tun ? "TUN" : "Прокси", Theme.BodyBold, active ? Color.White : Theme.TextMuted, segment, StringAlignment.Center);
        }
    }

    private void DrawSpeed(Graphics g)
    {
        if (!_connected || _speedDown == null || _speedUp == null)
            return;

        const float arrowWidth = 10;
        var downWidth = Theme.Measure(_speedDown, Theme.BodyBold).Width;
        var upWidth = Theme.Measure(_speedUp, Theme.BodyBold).Width;
        var r = new RectangleF(24, 24, arrowWidth * 2 + downWidth + upWidth + 52, 40);

        Theme.FillRounded(g, Color.FromArgb(225, Theme.Card), r, r.Height / 2);
        Theme.DrawRounded(g, Theme.Border, r, r.Height / 2);

        var cy = r.Y + r.Height / 2;
        var x = r.X + 15;
        Theme.DrawArrow(g, x, cy, true, Theme.AccentStrong);
        x += arrowWidth + 5;
        Theme.DrawText(g, _speedDown, Theme.BodyBold, Theme.Text, new RectangleF(x, r.Y, downWidth + 4, r.Height));
        x += downWidth + 10;
        Theme.DrawArrow(g, x, cy, false, Theme.PingBad);
        x += arrowWidth + 5;
        Theme.DrawText(g, _speedUp, Theme.BodyBold, Theme.Text, new RectangleF(x, r.Y, upWidth + 4, r.Height));
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
            g.DrawEllipse(pen, Inflate(r, 13 + i * 14 + pulse * 6));
        }

        DrawGlow(g, Inflate(r, 26), Color.FromArgb(_connected ? 110 : 60, ringColor));

        if (_connected)
        {
            using var brush = new LinearGradientBrush(r, Theme.Pink, Theme.Accent, 45f);
            g.FillEllipse(brush, r);
        }
        else
        {
            using (var fill = new SolidBrush(Theme.PowerOff))
                g.FillEllipse(fill, r);
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

        var textTop = r.Y + r.Height * 0.58f;
        if (_connected)
        {
            Theme.DrawText(g, "ПОДКЛЮЧЕНО", Theme.Status, Color.FromArgb(235, Color.White), new RectangleF(r.X, textTop, r.Width, 18), StringAlignment.Center);
            Theme.DrawText(g, _elapsed, Theme.Timer, Color.White, new RectangleF(r.X, textTop + 16, r.Width, 24), StringAlignment.Center);
        }
        else
        {
            Theme.DrawText(g, "ОТКЛЮЧЕНО", Theme.Status, Theme.TextMuted, new RectangleF(r.X, textTop, r.Width, 18), StringAlignment.Center);
        }
    }

    private void DrawServer(Graphics g)
    {
        if (_serverParts.Count == 0)
        {
            Theme.DrawText(g, "Выбери сервер", Theme.ServerName, Theme.TextMuted, _nameRect, StringAlignment.Center);
            return;
        }

        const float badge = 28;
        const float spacing = 10;
        var maxText = _nameRect.Width - badge - spacing;
        var textWidth = Math.Min(maxText, NamePainter.Measure(g, _serverParts, Theme.ServerName));
        var x = _nameRect.X + (_nameRect.Width - badge - spacing - textWidth) / 2;

        Theme.DrawBadge(g, new RectangleF(x, _nameRect.Y + (_nameRect.Height - badge) / 2, badge, badge), _serverCode);
        NamePainter.Draw(g, _serverParts, Theme.ServerName, Theme.Text, new RectangleF(x + badge + spacing, _nameRect.Y, textWidth + 6, _nameRect.Height));
    }

    private void DrawButtons(Graphics g)
    {
        var refresh = _refreshRect;
        Theme.FillRounded(g, _hoverRefresh ? Theme.Card : Color.FromArgb(215, Theme.Card), refresh, refresh.Height / 2);
        Theme.DrawRounded(g, _hoverRefresh ? Theme.Accent : Theme.Border, refresh, refresh.Height / 2, 1.4f);
        Theme.DrawText(g, "Обновить подписку", Theme.BodyBold, Theme.AccentStrong, refresh, StringAlignment.Center);

        var r = _pingRect;
        using (var brush = new LinearGradientBrush(r, _hoverPing ? Theme.Lighten(Theme.Accent, 0.15f) : Theme.Accent, _hoverPing ? Theme.Lighten(Theme.Pink, 0.15f) : Theme.Pink, 0f))
        using (var path = Theme.RoundedRect(r, r.Height / 2))
            g.FillPath(brush, path);

        Theme.DrawText(g, "Проверка пинга", Theme.BodyBold, Color.White, r, StringAlignment.Center);

        if (_busy)
            DrawBusy(g);
        else if (_pingText.Length > 0)
            Theme.DrawText(g, _pingText, Theme.CaptionBold, _pingColor, _pingResultRect, StringAlignment.Center);
    }

    private void DrawBusy(Graphics g)
    {
        var cx = _pingResultRect.X + _pingResultRect.Width / 2;
        var cy = _pingResultRect.Y + _pingResultRect.Height / 2;

        for (var i = 0; i < 3; i++)
        {
            var phase = (float)Math.Sin(_time * 6 - i * 0.9);
            var lift = Math.Max(0, phase) * 4;
            var alpha = (int)(170 + 85 * Math.Max(0, phase));
            using var brush = new SolidBrush(Color.FromArgb(alpha, i == 1 ? Theme.Pink : Theme.Accent));
            g.FillEllipse(brush, cx - 16 + i * 13, cy - 3.5f - lift, 7, 7);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = Theme.Design(e.Location);
        var power = InCircle(_powerRect, point);
        var ping = _pingRect.Contains(point);
        var refresh = _refreshRect.Contains(point);
        var toggle = _toggleRect.Contains(point);

        if (power != _hoverPower || ping != _hoverPing || refresh != _hoverRefresh || toggle != _hoverToggle)
        {
            _hoverPower = power;
            _hoverPing = ping;
            _hoverRefresh = refresh;
            _hoverToggle = toggle;
            Cursor = power || ping || refresh || toggle ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverPower = _hoverPing = _hoverRefresh = _hoverToggle = false;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left)
            return;

        var point = Theme.Design(e.Location);
        if (InCircle(_powerRect, point))
            PowerClicked?.Invoke(this, EventArgs.Empty);
        else if (_pingRect.Contains(point))
            PingClicked?.Invoke(this, EventArgs.Empty);
        else if (_refreshRect.Contains(point))
            RefreshClicked?.Invoke(this, EventArgs.Empty);
        else if (_toggleRect.Contains(point))
        {
            var tun = ModeSegment(true).Contains(point);
            if (tun != _tun)
                ModeSelected?.Invoke(tun);
        }
    }

    private static Rectangle ToDevice(RectangleF r) =>
        Rectangle.Ceiling(new RectangleF(r.X * Theme.S, r.Y * Theme.S, r.Width * Theme.S, r.Height * Theme.S));

    private static bool InCircle(RectangleF r, PointF p)
    {
        var dx = p.X - (r.X + r.Width / 2);
        var dy = p.Y - (r.Y + r.Height / 2);
        return dx * dx + dy * dy <= r.Width * r.Width / 4;
    }

    private static RectangleF Inflate(RectangleF r, float by) =>
        new(r.X - by, r.Y - by, r.Width + by * 2, r.Height + by * 2);
}
