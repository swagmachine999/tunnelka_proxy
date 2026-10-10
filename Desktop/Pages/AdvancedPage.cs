using Avalonia;
using Avalonia.Controls;

namespace Tunnelka.Desktop;

public sealed class AdvancedPage : UserControl
{
    private readonly Session _session;
    private readonly SettingsSegmented _mode;

    public AdvancedPage(Session session)
    {
        _session = session;
        var data = session.Data;
        var panel = new StackPanel();

        _mode = new SettingsSegmented(156, L.T("Прокси"), "TUN");
        _mode.Select(data.Tun ? 1 : 0);
        _mode.SelectedIndexChanged += (_, _) => session.SetMode(_mode.SelectedIndex == 1);
        panel.Children.Add(Ui.Row(L.T("Режим"), L.T("TUN — весь трафик компьютера"), _mode, out _, out _));

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
        panel.Children.Add(Ui.Row(L.T("Пинг при запуске"), L.T("Проверять все серверы при открытии приложения"), ping, out _, out _));

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
}
