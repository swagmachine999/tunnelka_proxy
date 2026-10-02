using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class SettingsPage : Panel
{
    private static readonly int[] Intervals = { 1, 2, 3, 5, 10, 15, 30, 60, 300, 600, 1800, 3600 };

    public SettingsPage(bool tun, int speedInterval, bool realPing, bool refreshOnStart, bool pingOnStart)
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        ModeSelector.Size = new Size(Theme.Px(156), Theme.Px(34));
        ModeSelector.SelectedIndex = tun ? 1 : 0;
        SpeedSelector = new OptionStepper(Intervals, ServerText.Duration, speedInterval, 150);
        RefreshToggle.Checked = refreshOnStart;
        PingToggle.Checked = pingOnStart;

        InterfaceRow = new SettingRow(L.T("Интерфейс"), L.T("Тема, масштаб и язык"), chevron: true);
        RoutingRow = new SettingRow(L.T("Маршрутизация"), L.T("Какие сайты идут напрямую, через VPN или в блок"), chevron: true);
        LogRow = new SettingRow(L.T("Журнал"), L.T("Сообщения приложения и Xray"), chevron: true);
        PingRow = new SettingRow(L.T("Пинг"), "", chevron: true);
        ShowPingMode(realPing);

        Controls.Add(LogRow);
        Controls.Add(RoutingRow);
        Controls.Add(PingRow);
        Controls.Add(new SettingRow(L.T("Пинг при запуске"), L.T("Проверять все серверы при открытии приложения"), PingToggle));
        Controls.Add(new SettingRow(L.T("Подписки при запуске"), L.T("Обновлять все подписки при открытии приложения"), RefreshToggle));
        Controls.Add(new SettingRow(L.T("Скорость в окне"), L.T("Как часто обновлять"), SpeedSelector));
        Controls.Add(new SettingRow(L.T("Режим"), L.T("Режим туннелирования"), ModeSelector));
        Controls.Add(InterfaceRow);
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(PageParts.Title(L.T("Настройки")));
    }

    public Segmented ModeSelector { get; } = new(L.T("Прокси"), "TUN");
    public OptionStepper SpeedSelector { get; }
    public ToggleSwitch RefreshToggle { get; } = new();
    public ToggleSwitch PingToggle { get; } = new();
    public SettingRow InterfaceRow { get; }
    public SettingRow RoutingRow { get; }
    public SettingRow LogRow { get; }
    public SettingRow PingRow { get; }

    public void ShowPingMode(bool real)
    {
        PingRow.Subtitle = real ? L.T("Реальный (via proxy)") : L.T("Быстрый (TCP)");
        PingRow.Invalidate();
    }
}
