using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class SettingRow : Control
{
    public SettingRow(string title, string subtitle, ToggleSwitch? toggle = null)
    {
        Text = title;
        Subtitle = subtitle;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        Height = 76;
        Theme.Bind(this, () => Theme.Card);

        if (toggle != null)
        {
            Toggle = toggle;
            Controls.Add(toggle);
            Resize += (_, _) => toggle.Location = new Point(Width - toggle.Width - 18, (Height - 8 - toggle.Height) / 2);
        }
    }

    public string Subtitle { get; set; }
    public ToggleSwitch? Toggle { get; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 9.5f);
        Theme.FillRounded(g, Theme.Card, rect, 14);
        Theme.DrawRounded(g, Theme.Border, rect, 14);

        var textWidth = Width - 32 - (Toggle != null ? Toggle.Width + 16 : 0);
        Theme.DrawText(g, Text, Theme.BodyBold, Theme.Text, new RectangleF(16, 12, textWidth, 22));
        Theme.DrawText(g, Subtitle, Theme.Caption, Theme.TextMuted, new RectangleF(16, 36, textWidth, 20));
    }
}

public class SettingsPage : Panel
{
    public SettingsPage(bool dark, bool proxy)
    {
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);

        DarkToggle.Checked = dark;
        ProxyToggle.Checked = proxy;

        Controls.Add(new SettingRow("Порты", "SOCKS5 127.0.0.1:10808 · HTTP 127.0.0.1:10809"));
        Controls.Add(new SettingRow("Системный прокси", "Браузер и программы пойдут через VPN", ProxyToggle));
        Controls.Add(new SettingRow("Тёмная тема", "Мягкие тёмные цвета", DarkToggle));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = 10 }, () => Theme.Surface));
        Controls.Add(PageParts.Title("Настройки"));
    }

    public ToggleSwitch DarkToggle { get; } = new();
    public ToggleSwitch ProxyToggle { get; } = new();
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

    public LogPage()
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

        var header = new Panel { Dock = DockStyle.Top, Height = 52 };
        Theme.Bind(header, () => Theme.Surface);
        var title = PageParts.Title("Журнал");
        title.Dock = DockStyle.Fill;
        var buttonHolder = new Panel { Dock = DockStyle.Right, Width = 110, Padding = new Padding(0, 10, 0, 8) };
        Theme.Bind(buttonHolder, () => Theme.Surface);
        buttonHolder.Controls.Add(clear);
        header.Controls.Add(title);
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
