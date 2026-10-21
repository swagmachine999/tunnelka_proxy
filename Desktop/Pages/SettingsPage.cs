using Avalonia;
using Avalonia.Controls;
using Tunnelka.Services;
using Tunnelka.UI;

namespace Tunnelka.Desktop;

public sealed class SettingsPage : UserControl
{
    private static readonly int[] Intervals = { 1, 2, 3, 5, 10, 15, 30, 60, 300, 600, 1800, 3600 };

    private readonly Session _session;
    private readonly SettingsSegmented _mode;

    public SettingsPage(Session session, Action<SettingsSection> open)
    {
        _session = session;
        var data = session.Data;
        var panel = new StackPanel();

        _mode = new SettingsSegmented(156, L.T("Прокси"), "TUN");
        _mode.Select(data.Tun ? 1 : 0);
        _mode.SelectedIndexChanged += (_, _) => session.SetMode(_mode.SelectedIndex == 1);
        panel.Children.Add(Ui.Row(L.T("Режим"), "", _mode, out _, out _));

        var killSwitch = SettingsParts.Toggle(data.KillSwitch, on =>
        {
            data.KillSwitch = on;
            if (on && session.Active != null)
                _ = session.EngageKillSwitch();
            else if (!on)
                _ = session.ReleaseKillSwitch();
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Kill switch"), L.T("Блокирует интернет, если VPN упал (режим TUN)"), killSwitch, out _, out _));

        var autoSwitch = SettingsParts.Toggle(data.AutoSwitch, on => session.SetAutoSwitch(on));
        panel.Children.Add(Ui.Row(L.T("Автопереключение"), L.T("Если сервер перестал отвечать, перейти на другой из той же подписки"), autoSwitch, out _, out _));

        panel.Children.Add(Ui.ClickRow(L.T("Правила маршрута трафика"), L.T("Какие сайты идут напрямую, через VPN или в блок"), () => open(SettingsSection.Routing), out _, out _));
        panel.Children.Add(Gap());

        var autoStart = SettingsParts.Toggle(data.AutoStart, on =>
        {
            data.AutoStart = on;
            Autostart.Apply(on);
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Запускать приложение при запуске Windows"), L.T("Открываться свёрнутым в трей при входе в систему"), autoStart, out _, out _));

        var connect = SettingsParts.Toggle(data.ConnectOnStart, on =>
        {
            data.ConnectOnStart = on;
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Подключиться к последней локации при открытии приложения"), "", connect, out _, out _));

        var refresh = SettingsParts.Toggle(data.RefreshOnStart, on =>
        {
            data.RefreshOnStart = on;
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Подписки при запуске"), L.T("Обновлять все подписки при открытии приложения"), refresh, out _, out _));

        var ping = SettingsParts.Toggle(data.PingOnStart, on =>
        {
            data.PingOnStart = on;
            session.Save();
        });
        panel.Children.Add(Ui.Row(L.T("Пинг при запуске"), L.T("Проверить пинг локаций при открытии приложения"), ping, out _, out _));
        panel.Children.Add(Gap());

        var pingMode = data.RealPing ? L.T("Реальный (via proxy)") : L.T("Быстрый (TCP)");
        panel.Children.Add(Ui.ClickRow(L.T("Пинг"), pingMode, () => open(SettingsSection.Ping), out _, out _));
        panel.Children.Add(Gap());

        panel.Children.Add(Ui.ClickRow(L.T("Интерфейс"), L.T("Тема, масштаб и язык"), () => open(SettingsSection.Interface), out _, out _));
        panel.Children.Add(Ui.ClickRow(L.T("Оверлей"), L.T("Пинг и скорость поверх игр и окон"), () => open(SettingsSection.Overlay), out _, out _));

        var speed = new SettingsStepper(Intervals, ServerText.Duration, data.SpeedInterval, 150);
        speed.ValueChanged += (_, _) =>
        {
            data.SpeedInterval = speed.Value;
            session.RestartSpeedAveraging();
            session.Save();
        };
        panel.Children.Add(Ui.Row(L.T("Отображение скорости"), L.T("Как часто отображать"), speed, out _, out _));
        panel.Children.Add(Gap());

        panel.Children.Add(Ui.ClickRow(L.T("Журнал"), L.T("Сообщения приложения и Xray"), () => open(SettingsSection.Log), out _, out _));

        var repair = SettingsParts.Pill(L.T("Починить"), false);
        repair.Click += (_, _) => _ = session.Repair();
        panel.Children.Add(Ui.Row(L.T("Починить"), L.T("Остановить ядра, снять прокси и блокировку, если что-то зависло"), repair, out _, out _));

        Content = panel;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _session.ModeChanged += OnModeChanged;
        OnModeChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _session.ModeChanged -= OnModeChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnModeChanged() => _mode.Select(_session.Data.Tun ? 1 : 0);

    private static Control Gap() => new Border { Height = 10 };
}
