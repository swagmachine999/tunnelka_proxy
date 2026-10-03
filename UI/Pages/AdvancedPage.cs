using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class AdvancedPage : Panel
{
    public AdvancedPage(bool tun, bool killSwitch, bool refreshOnStart, bool pingOnStart, Action onBack)
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        ModeSelector.Size = new Size(Theme.Px(156), Theme.Px(34));
        ModeSelector.SelectedIndex = tun ? 1 : 0;
        KillSwitchToggle.Checked = killSwitch;
        RefreshToggle.Checked = refreshOnStart;
        PingToggle.Checked = pingOnStart;

        Controls.Add(new SettingRow(L.T("Пинг при запуске"), L.T("Проверять все серверы при открытии приложения"), PingToggle));
        Controls.Add(new SettingRow(L.T("Подписки при запуске"), L.T("Обновлять все подписки при открытии приложения"), RefreshToggle));
        Controls.Add(new SettingRow(L.T("Kill switch"), L.T("Блокирует интернет, если VPN упал (режим TUN)"), KillSwitchToggle));
        Controls.Add(new SettingRow(L.T("Режим"), L.T("TUN — весь трафик компьютера"), ModeSelector));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(PageParts.Header(L.T("Расширенное"), onBack));
    }

    public Segmented ModeSelector { get; } = new(L.T("Прокси"), "TUN");
    public ToggleSwitch KillSwitchToggle { get; } = new();
    public ToggleSwitch RefreshToggle { get; } = new();
    public ToggleSwitch PingToggle { get; } = new();
}
