using Avalonia;
using Avalonia.Controls;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class SettingsPage : UserControl
{
    public SettingsPage(Session session, Action<SettingsSection> open)
    {
        var data = session.Data;
        var panel = new StackPanel();

        var autoStart = SettingsParts.Toggle(data.AutoStart, on =>
        {
            data.AutoStart = on;
            Autostart.Apply(on);
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Автозапуск"), L.T("Запуск приложения с Windows"), autoStart, out _, out _));

        var connect = SettingsParts.Toggle(data.ConnectOnStart, on =>
        {
            data.ConnectOnStart = on;
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Автоподключение"), L.T("Подключиться к последней локации при открытии приложения"), connect, out _, out _));
        panel.Children.Add(Gap());

        panel.Children.Add(Ui.ClickRow(L.T("Интерфейс"), L.T("Язык, масштаб, тема"), () => open(SettingsSection.Interface), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Оверлей"), L.T("Пинг и скорость поверх игр и окон"), () => open(SettingsSection.Overlay), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Маршрутизация"), L.T("Правила маршрута трафика"), () => open(SettingsSection.Routing), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Пинг и скорость"), L.T("Тип пинга и отображение скорости"), () => open(SettingsSection.Ping), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Расширенные"), L.T("Режим, kill switch, автопереключение, запуск"), () => open(SettingsSection.Advanced), out _, out _));
        panel.Children.Add(Gap());

        panel.Children.Add(Ui.ClickRow(L.T("Журнал"), L.T("Сообщения приложения и Xray"), () => open(SettingsSection.Log), out _, out _));

        var repair = SettingsParts.Pill(L.T("Починить"), false);
        repair.Click += (_, _) => _ = session.Repair();
        panel.Children.Add(Ui.Row(L.T("Починить"), L.T("Сбросить сеть и исправить подключение"), repair, out _, out _));

        Content = panel;
    }

    private static Control Gap() => new Border { Height = 10 };
}
