using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class SettingRow : Control
{
    private float W => Width / Theme.S;
    private float H => Height / Theme.S;

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
        Height = Theme.Px(76);
        Theme.Bind(this, () => Theme.Card);

        if (chevron)
            Cursor = Cursors.Hand;

        if (accessory != null)
        {
            Controls.Add(accessory);
            Resize += (_, _) => accessory.Location = new Point(Width - accessory.Width - Theme.Px(18), (Height - Theme.Px(8) - accessory.Height) / 2);
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
        Theme.Begin(g, Theme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 9.5f);
        Theme.FillRounded(g, _hover ? Theme.CardHover : Theme.Card, rect, 14);
        Theme.DrawRounded(g, Theme.Border, rect, 14);

        var right = _accessory != null ? _accessory.Width / Theme.S + 16 : _chevron ? 30 : 0;
        var textWidth = W - 32 - right;
        Theme.DrawText(g, Text, Theme.BodyBold, Theme.Text, new RectangleF(16, 12, textWidth, 22));
        Theme.DrawText(g, Subtitle, Theme.Caption, Theme.TextMuted, new RectangleF(16, 36, textWidth, 20));

        if (_chevron)
        {
            using var pen = new Pen(Theme.TextMuted, 2f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            var cx = W - 24f;
            var cy = rect.Height / 2;
            g.DrawLine(pen, cx - 3, cy - 6, cx + 3, cy);
            g.DrawLine(pen, cx + 3, cy, cx - 3, cy + 6);
        }
    }
}

public class SettingsPage : Panel
{
    private static readonly int[] Intervals = { 3, 5, 10 };

    public SettingsPage(bool dark, bool tun, int speedInterval, bool realPing, int uiScale)
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        DarkToggle.Checked = dark;
        ModeSelector.Size = new Size(Theme.Px(156), Theme.Px(34));
        ModeSelector.SelectedIndex = tun ? 1 : 0;
        SpeedSelector.Size = new Size(Theme.Px(156), Theme.Px(34));
        SpeedSelector.SelectedIndex = Math.Max(0, Array.IndexOf(Intervals, speedInterval));

        RoutingRow = new SettingRow("Маршрутизация", "Какие сайты идут напрямую, через VPN или в блок", chevron: true);
        LogRow = new SettingRow("Журнал", "Сообщения приложения и Xray", chevron: true);
        PingRow = new SettingRow("Пинг", "", chevron: true);
        ShowPingMode(realPing);

        Controls.Add(LogRow);
        Controls.Add(RoutingRow);
        Controls.Add(PingRow);
        Controls.Add(new SettingRow("Скорость в окне", "Как часто обновлять", SpeedSelector));
        Controls.Add(new SettingRow("Режим", "Прокси: браузер и программы. TUN: весь трафик", ModeSelector));
        ScaleSelector = new ScaleStepper(uiScale);
        Controls.Add(new SettingRow("Масштаб интерфейса", "Ctrl + колесо мыши, Ctrl и +/−, Ctrl+0", ScaleSelector));
        Controls.Add(new SettingRow("Тёмная тема", "Мягкие тёмные цвета", DarkToggle));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(PageParts.Title("Настройки"));
    }

    public ToggleSwitch DarkToggle { get; } = new();
    public Segmented ModeSelector { get; } = new("Прокси", "TUN");
    public Segmented SpeedSelector { get; } = new("3 с", "5 с", "10 с");
    public SettingRow RoutingRow { get; }
    public SettingRow LogRow { get; }
    public ScaleStepper ScaleSelector { get; }
    public SettingRow PingRow { get; }

    public void ShowPingMode(bool real)
    {
        PingRow.Subtitle = real ? "Реальный, через VPN" : "Быстрый, TCP";
        PingRow.Invalidate();
    }

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
        Font = Theme.Scaled(Theme.Log)
    };

    public LogPage(Action onBack)
    {
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);
        Theme.Bind(_log, () => Theme.Card, () => Theme.Text);

        var frame = new Panel { Dock = DockStyle.Fill, Padding = Theme.Px(14, 12, 6, 12) };
        Theme.Bind(frame, () => Theme.Card);
        frame.Controls.Add(_log);

        var clear = PageParts.Button("Очистить", false);
        clear.Dock = DockStyle.Right;
        clear.Width = Theme.Px(110);
        clear.Click += (_, _) => _log.Clear();

        var header = PageParts.Header("Журнал", onBack);
        var buttonHolder = new Panel { Dock = DockStyle.Right, Width = Theme.Px(110), Padding = Theme.Px(0, 10, 0, 8) };
        Theme.Bind(buttonHolder, () => Theme.Surface);
        buttonHolder.Controls.Add(clear);
        header.Controls.Add(buttonHolder);

        var gap = new Panel { Dock = DockStyle.Top, Height = Theme.Px(8) };
        Theme.Bind(gap, () => Theme.Surface);

        Controls.Add(frame);
        Controls.Add(gap);
        Controls.Add(header);
    }

    public TextBox Box => _log;

    public void Append(string line) => _log.AppendText(line + Environment.NewLine);
}

public class PingPage : Panel
{
    public event EventHandler? Changed;

    public PingPage(bool real, string url, Action onBack)
    {
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);

        Mode = new Segmented("Реальный (через VPN)", "Быстрый (TCP)") { Dock = DockStyle.Top, Height = Theme.Px(40) };
        Mode.SelectedIndex = real ? 0 : 1;
        Mode.SelectedIndexChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

        Url = new SearchBox(RealPingDefault, false) { Dock = DockStyle.Top };
        Url.SetText(url);
        Url.QueryChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

        var reset = PageParts.Button("Сбросить адрес", false);
        reset.Dock = DockStyle.Left;
        reset.Width = Theme.Px(150);
        reset.Click += (_, _) => Url.SetText(RealPingDefault);
        var resetRow = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(44), Padding = Theme.Px(0, 8, 0, 2) }, () => Theme.Surface);
        resetRow.Controls.Add(reset);

        Controls.Add(resetRow);
        Controls.Add(Url);
        Controls.Add(PageParts.Caption("Тестовый адрес для реального пинга", 30));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(Wrapped("Быстрый: проверяет только, открыт ли порт сервера. Мгновенно, но может показать пинг у сервера, через который VPN не работает.", 54));
        Controls.Add(Wrapped("Реальный: запрос идёт через сам сервер, как при работе VPN. Делается два запроса, берётся лучший. Нерабочий сервер покажет n/a.", 54));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(8) }, () => Theme.Surface));
        Controls.Add(Mode);
        Controls.Add(PageParts.Caption("Тип пинга", 30));
        Controls.Add(PageParts.Header("Пинг", onBack));
    }

    public const string RealPingDefault = "https://www.gstatic.com/generate_204";

    public Segmented Mode { get; }
    public SearchBox Url { get; }

    public bool IsReal => Mode.SelectedIndex == 0;

    private static Label Wrapped(string text, int height)
    {
        var label = PageParts.Caption(text, height);
        label.TextAlign = ContentAlignment.TopLeft;
        label.Padding = Theme.Px(0, 4, 0, 0);
        return label;
    }
}
