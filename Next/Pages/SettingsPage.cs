using Avalonia.Controls;
using Tunnelka.Services;
using Tunnelka.UI;

namespace Tunnelka.Next;

public enum SettingsSection
{
    Root,
    Interface,
    Overlay,
    Advanced,
    Routing,
    Log,
    Ping
}

public sealed class SettingsPage : UserControl
{
    private static readonly int[] Intervals = { 1, 2, 3, 5, 10, 15, 30, 60, 300, 600, 1800, 3600 };

    public SettingsPage(Session session, Action<SettingsSection> open)
    {
        var data = session.Data;
        var panel = new StackPanel();

        panel.Children.Add(Ui.ClickRow(L.T("Интерфейс"), L.T("Тема, масштаб и язык"), () => open(SettingsSection.Interface), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Оверлей"), L.T("Пинг и скорость поверх игр и окон"), () => open(SettingsSection.Overlay), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Расширенное"), L.T("Режим туннеля, kill switch, действия при запуске"), () => open(SettingsSection.Advanced), out _, out _));

        var speed = new SettingsStepper(Intervals, ServerText.Duration, data.SpeedInterval, 150);
        speed.ValueChanged += (_, _) =>
        {
            data.SpeedInterval = speed.Value;
            session.RestartSpeedAveraging();
            session.Save();
        };
        panel.Children.Add(Ui.Row(L.T("Скорость в окне"), L.T("Как часто обновлять"), speed, out _, out _));

        var autoStart = SettingsParts.Toggle(data.AutoStart, on =>
        {
            data.AutoStart = on;
            Autostart.Apply(on);
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Запуск с Windows"), L.T("Открываться свёрнутым в трей при входе в систему"), autoStart, out _, out _));

        var connect = SettingsParts.Toggle(data.ConnectOnStart, on =>
        {
            data.ConnectOnStart = on;
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Подключаться при запуске"), L.T("Сразу включать VPN к последнему серверу"), connect, out _, out _));

        var pingMode = data.RealPing ? L.T("Реальный (via proxy)") : L.T("Быстрый (TCP)");
        panel.Children.Add(Ui.ClickRow(L.T("Пинг"), pingMode, () => open(SettingsSection.Ping), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Маршрутизация"), L.T("Какие сайты идут напрямую, через VPN или в блок"), () => open(SettingsSection.Routing), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Журнал"), L.T("Сообщения приложения и Xray"), () => open(SettingsSection.Log), out _, out _));

        Content = panel;
    }
}
