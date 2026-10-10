using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Tunnelka.Services;

namespace Tunnelka.Next;

public sealed class SettingsHost : UserControl
{
    private readonly Session _session;
    private readonly LogPage _log;
    private SettingsSection _section = SettingsSection.Root;
    private RoutingPage? _routing;
    private bool _reconnectHint;

    public SettingsHost(Session session)
    {
        _session = session;
        _log = new LogPage(session);
        session.ReconnectHint += OnReconnectHint;
        if (session.Data.AutoStart)
            Autostart.Apply(true);

        Show(SettingsSection.Root);
    }

    public void Localize()
    {
        _log.Localize();
        Show(_section);
    }

    public void ShowLog() => Show(SettingsSection.Log);

    public void ShowRoot() => Show(SettingsSection.Root);

    private void OnReconnectHint(bool show)
    {
        _reconnectHint = show;
        _routing?.ShowReconnectHint(show);
    }

    private void Show(SettingsSection section)
    {
        _section = section;
        _routing = null;
        if (Content is DockPanel previous)
            previous.Children.Clear();
        Detach(_log);
        Detach(_log.ClearButton);

        Control header;
        Control body;
        switch (section)
        {
            case SettingsSection.Interface:
                header = Back(L.T("Интерфейс"));
                body = Scrolled(new InterfacePage(_session));
                break;
            case SettingsSection.Overlay:
                header = Back(L.T("Оверлей"));
                body = Scrolled(new OverlayPage(_session));
                break;
            case SettingsSection.Advanced:
                header = Back(L.T("Расширенное"));
                body = Scrolled(new AdvancedPage(_session));
                break;
            case SettingsSection.Routing:
                _routing = new RoutingPage(_session, _reconnectHint);
                header = Back(L.T("Маршрутизация"));
                body = Scrolled(_routing);
                break;
            case SettingsSection.Ping:
                header = Back(L.T("Пинг"));
                body = Scrolled(new PingPage(_session));
                break;
            case SettingsSection.Log:
                header = BackWithAction(L.T("Журнал"), _log.ClearButton);
                body = _log;
                break;
            default:
                header = Ui.Title(L.T("Настройки"));
                body = Scrolled(new SettingsPage(_session, Show));
                break;
        }

        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(body);
        Content = dock;
    }

    private Control Back(string title) => Ui.BackHeader(title, ShowRoot, out _);

    private Control BackWithAction(string title, Control action)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var header = Ui.BackHeader(title, ShowRoot, out _);
        action.VerticalAlignment = VerticalAlignment.Center;
        action.Margin = new Thickness(0, 0, 10, 14);
        Grid.SetColumn(action, 1);
        grid.Children.Add(header);
        grid.Children.Add(action);
        return grid;
    }

    private static void Detach(Control control)
    {
        if (control.Parent is Panel panel)
            panel.Children.Remove(control);
    }

    private static Control Scrolled(Control page)
    {
        page.Margin = new Thickness(0, 0, 10, 0);
        return new ScrollViewer { Content = page };
    }
}
