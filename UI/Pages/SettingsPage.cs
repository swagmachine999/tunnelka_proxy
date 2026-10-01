using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class SettingRow : Control
{
    private readonly Control? _accessory;
    private readonly bool _chevron;
    private bool _hover;

    public SettingRow(string title, string subtitle, Control? accessory = null, bool chevron = false)
    {
        Text = title;
        Subtitle = subtitle;
        _accessory = accessory;
        _chevron = chevron;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        Height = 76;
        Theme.Bind(this, () => Theme.Card);

        if (chevron)
            Cursor = Cursors.Hand;

        if (accessory != null)
        {
            Controls.Add(accessory);
            Resize += (_, _) => accessory.Location = new Point(Width - accessory.Width - 18, (Height - 8 - accessory.Height) / 2);
        }
    }

    public string Subtitle { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = _chevron;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 9.5f);
        Theme.FillRounded(g, _hover ? Theme.CardHover : Theme.Card, rect, 14);
        Theme.DrawRounded(g, Theme.Border, rect, 14);

        var right = _accessory != null ? _accessory.Width + 16 : _chevron ? 30 : 0;
        var textWidth = Width - 32 - right;
        Theme.DrawText(g, Text, Theme.BodyBold, Theme.Text, new RectangleF(16, 12, textWidth, 22));
        Theme.DrawText(g, Subtitle, Theme.Caption, Theme.TextMuted, new RectangleF(16, 36, textWidth, 20));

        if (_chevron)
        {
            using var pen = new Pen(Theme.TextMuted, 2f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            var cx = Width - 24f;
            var cy = rect.Height / 2;
            g.DrawLine(pen, cx - 3, cy - 6, cx + 3, cy);
            g.DrawLine(pen, cx + 3, cy, cx - 3, cy + 6);
        }
    }
}

public class SettingsPage : Panel
{
    private static readonly int[] Intervals = { 3, 5, 10 };

    public SettingsPage(bool dark, bool proxy, int speedInterval)
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        DarkToggle.Checked = dark;
        ProxyToggle.Checked = proxy;
        SpeedSelector.Size = new Size(156, 34);
        SpeedSelector.SelectedIndex = Math.Max(0, Array.IndexOf(Intervals, speedInterval));

        RoutingRow = new SettingRow("Маршрутизация", "Какие сайты идут напрямую, через VPN или в блок", chevron: true);
        LogRow = new SettingRow("Журнал", "Сообщения приложения и Xray", chevron: true);

        Controls.Add(new SettingRow("Порты", "SOCKS5 127.0.0.1:10808 · HTTP 127.0.0.1:10809"));
        Controls.Add(LogRow);
        Controls.Add(RoutingRow);
        Controls.Add(new SettingRow("Скорость в окне", "Как часто обновлять", SpeedSelector));
        Controls.Add(new SettingRow("Системный прокси", "Браузер и программы пойдут через VPN", ProxyToggle));
        Controls.Add(new SettingRow("Тёмная тема", "Мягкие тёмные цвета", DarkToggle));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = 10 }, () => Theme.Surface));
        Controls.Add(PageParts.Title("Настройки"));
    }

    public ToggleSwitch DarkToggle { get; } = new();
    public ToggleSwitch ProxyToggle { get; } = new();
    public Segmented SpeedSelector { get; } = new("3 с", "5 с", "10 с");
    public SettingRow RoutingRow { get; }
    public SettingRow LogRow { get; }

    public int SpeedInterval => Intervals[SpeedSelector.SelectedIndex];
}

public class LogPage : Panel
{
    private readonly TextBox _log = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        WordWrap = true,
        BorderStyle = BorderStyle.None,
        ScrollBars = ScrollBars.Vertical,
        Font = Theme.Log
    };

    public LogPage(Action onBack)
    {
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);
        Theme.Bind(_log, () => Theme.Card, () => Theme.Text);

        var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 12, 6, 12) };
        Theme.Bind(frame, () => Theme.Card);
        frame.Controls.Add(_log);

        var clear = PageParts.Button("Очистить", false);
        clear.Dock = DockStyle.Right;
        clear.Width = 110;
        clear.Click += (_, _) => _log.Clear();

        var header = PageParts.Header("Журнал", onBack);
        var buttonHolder = new Panel { Dock = DockStyle.Right, Width = 110, Padding = new Padding(0, 10, 0, 8) };
        Theme.Bind(buttonHolder, () => Theme.Surface);
        buttonHolder.Controls.Add(clear);
        header.Controls.Add(buttonHolder);

        var gap = new Panel { Dock = DockStyle.Top, Height = 8 };
        Theme.Bind(gap, () => Theme.Surface);

        Controls.Add(frame);
        Controls.Add(gap);
        Controls.Add(header);
    }

    public TextBox Box => _log;

    public void Append(string line) => _log.AppendText(line + Environment.NewLine);
}
