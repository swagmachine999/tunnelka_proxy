using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

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
        Controls.Add(new SettingRow("Режим", "Режим туннелирования", ModeSelector));
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
        PingRow.Subtitle = real ? "Реальный (via proxy)" : "Быстрый (TCP)";
        PingRow.Invalidate();
    }

    public int SpeedInterval => Intervals[SpeedSelector.SelectedIndex];
}
