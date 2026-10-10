using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Tunnelka.Desktop;

public sealed class TrayService : IDisposable
{
    private readonly Window _window;
    private readonly ShellNotifyIcon _icon;
    private TrayMenu? _menu;

    public TrayService(Window window, Session session)
    {
        _window = window;
        Session = session;

        using var stream = AssetLoader.Open(new Uri("avares://Tunnelka/Assets/tunnelka.ico"));
        _icon = new ShellNotifyIcon(stream, "Tunnelka");
        _icon.Clicked += () => Dispatcher.UIThread.Post(Restore);
        _icon.ContextRequested += point => Dispatcher.UIThread.Post(() => ShowMenu(point));
    }

    public Session Session { get; }

    public void Balloon(string text) => Toast.Popup(text);

    public void Localize() => CloseMenu();

    public void Restore()
    {
        CloseMenu();
        if (!_window.IsVisible)
            _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
        Win32.Foreground(_window);
    }

    public void Dispose()
    {
        CloseMenu();
        _icon.Dispose();
    }

    private void ShowMenu(PixelPoint cursor)
    {
        CloseMenu();
        var items = new[]
        {
            new TrayMenuItem(L.English ? "Open Tunnelka" : "Открыть Tunnelka", Restore),
            new TrayMenuItem(L.T("Выход"), Session.RequestExit)
        };
        var menu = new TrayMenu(items, Session.Data.UiScale / 100.0);
        menu.Closed += (_, _) =>
        {
            if (_menu == menu)
                _menu = null;
        };
        _menu = menu;
        menu.ShowAt(cursor);
    }

    private void CloseMenu()
    {
        var menu = _menu;
        _menu = null;
        menu?.Close();
    }
}
