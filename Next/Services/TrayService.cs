using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Tunnelka.Next;

public sealed class TrayService : IDisposable
{
    private readonly Window _window;
    private readonly Session _session;
    private readonly TrayIcon _icon;
    private readonly NativeMenuItem _exit;

    public TrayService(Window window, Session session)
    {
        _window = window;
        _session = session;

        _icon = new TrayIcon { ToolTipText = "Tunnelka", IsVisible = true };
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Tunnelka.Next/Assets/tunnelka.ico"));
            _icon.Icon = new WindowIcon(stream);
        }
        catch (Exception)
        {
        }

        _exit = new NativeMenuItem(L.T("Выход"));
        _exit.Click += (_, _) => _session.RequestExit();
        var menu = new NativeMenu();
        menu.Add(_exit);
        _icon.Menu = menu;
        _icon.Clicked += (_, _) => Restore();

        if (Application.Current != null)
            TrayIcon.SetIcons(Application.Current, new TrayIcons { _icon });
    }

    public void Balloon(string text) => Toast.Popup(text);

    public void Localize() => _exit.Header = L.T("Выход");

    public void Restore()
    {
        if (!_window.IsVisible)
            _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    public void Dispose()
    {
        _icon.IsVisible = false;
        _icon.Dispose();
    }
}
