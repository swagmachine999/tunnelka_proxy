using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class SettingsPage : Panel
{
    private static readonly int[] Intervals = { 1, 2, 3, 5, 10, 15, 30, 60, 300, 600, 1800, 3600 };

    public SettingsPage(int speedInterval, bool realPing, bool autoStart, bool connectOnStart)
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        SpeedSelector = new OptionStepper(Intervals, ServerText.Duration, speedInterval, 150);
        AutoStartToggle.Checked = autoStart;
        ConnectToggle.Checked = connectOnStart;

        InterfaceRow = new SettingRow(L.T("Интерфейс"), L.T("Тема, масштаб и язык"), chevron: true);
        OverlayRow = new SettingRow(L.T("Оверлей"), L.T("Пинг и скорость поверх игр и окон"), chevron: true);
        AdvancedRow = new SettingRow(L.T("Расширенное"), L.T("Режим туннеля, kill switch, действия при запуске"), chevron: true);
        RoutingRow = new SettingRow(L.T("Маршрутизация"), L.T("Какие сайты идут напрямую, через VPN или в блок"), chevron: true);
        LogRow = new SettingRow(L.T("Журнал"), L.T("Сообщения приложения и Xray"), chevron: true);
        PingRow = new SettingRow(L.T("Пинг"), "", chevron: true);
        ShowPingMode(realPing);

        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(16) }, () => Theme.Surface));
        Controls.Add(LogRow);
        Controls.Add(RoutingRow);
        Controls.Add(PingRow);
        Controls.Add(new SettingRow(L.T("Подключаться при запуске"), L.T("Сразу включать VPN к последнему серверу"), ConnectToggle));
        Controls.Add(new SettingRow(L.T("Запуск с Windows"), L.T("Открываться свёрнутым в трей при входе в систему"), AutoStartToggle));
        Controls.Add(new SettingRow(L.T("Скорость в окне"), L.T("Как часто обновлять"), SpeedSelector));
        Controls.Add(AdvancedRow);
        Controls.Add(OverlayRow);
        Controls.Add(InterfaceRow);
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(PageParts.Title(L.T("Настройки")));
    }

    public OptionStepper SpeedSelector { get; }
    public ToggleSwitch AutoStartToggle { get; } = new();
    public ToggleSwitch ConnectToggle { get; } = new();
    public SettingRow InterfaceRow { get; }
    public SettingRow OverlayRow { get; }
    public SettingRow AdvancedRow { get; }
    public SettingRow RoutingRow { get; }
    public SettingRow LogRow { get; }
    public SettingRow PingRow { get; }

    public void ShowPingMode(bool real)
    {
        PingRow.Subtitle = real ? L.T("Реальный (via proxy)") : L.T("Быстрый (TCP)");
        PingRow.Invalidate();
    }
}
