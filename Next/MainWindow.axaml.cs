using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Tunnelka.Next.Controls;
using Tunnelka.Storage;

namespace Tunnelka.Next;

public partial class MainWindow : Window
{
    private const double BaseWidth = 1233;
    private const double BaseHeight = 760;

    private enum Page
    {
        Servers,
        Stats,
        About,
        Settings
    }

    private readonly Session _session;
    private readonly Dictionary<Page, Control> _pages = new();
    private readonly Dictionary<Page, Button> _buttons = new();
    private readonly HeroPanel _hero;
    private readonly ServersPage _servers;
    private readonly StatsPage _stats;
    private readonly AboutPage _about;
    private readonly SettingsHost _settings;
    private readonly TrayService _tray;
    private readonly UpdateWatcher _updates;
    private bool _exiting;

    public MainWindow() : this(new Session())
    {
    }

    public MainWindow(Session session)
    {
        _session = session;
        AvaloniaXamlLoader.Load(this);

        _hero = new HeroPanel(session);
        _servers = new ServersPage(session);
        _stats = new StatsPage(session);
        _about = new AboutPage(session);
        _settings = new SettingsHost(session);
        _tray = new TrayService(this, session);

        Part<Panel>("HeroHost").Children.Add(_hero);
        var host = Part<Panel>("PagesHost");
        _pages[Page.Servers] = _servers;
        _pages[Page.Stats] = _stats;
        _pages[Page.About] = _about;
        _pages[Page.Settings] = _settings;
        foreach (var page in _pages.Values)
        {
            page.IsVisible = false;
            host.Children.Add(page);
        }

        _buttons[Page.Servers] = Part<Button>("ServersButton");
        _buttons[Page.Stats] = Part<Button>("StatsButton");
        _buttons[Page.About] = Part<Button>("AboutButton");
        _buttons[Page.Settings] = Part<Button>("SettingsButton");
        foreach (var pair in _buttons)
        {
            var page = pair.Key;
            pair.Value.Click += (_, _) =>
            {
                if (page == Page.Settings)
                    _settings.ShowRoot();
                ShowPage(page);
            };
        }

        Part<Button>("AddButton").Click += async (_, _) => await AskKey();
        _servers.ManualRequested += async () => await AskKey();

        session.Dialogs = new Dialogs(this, _tray);
        session.Overlay.Attach(session);
        session.Overlay.ApplyHotkey();
        _updates = new UpdateWatcher(() => session.ProxyPort, update => UpdateDialog.Show(this, update, () => session.ProxyPort));

        session.ThemeChanged += ApplyTheme;
        session.ScaleChanged += () => ApplyScale(false);
        session.LanguageChanged += Localize;
        session.AddRequested += async () => await AskKey();
        session.ShowServersRequested += () => ShowPage(Page.Servers);
        session.ShowLogRequested += () =>
        {
            _settings.ShowLog();
            ShowPage(Page.Settings);
        };
        session.ExitRequested += () => Dispatcher.UIThread.Post(Quit);
        session.UpdateCheck += _updates.Start;

        AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
        KeyDown += OnKeyDown;

        ApplyTheme();
        ApplyScale(true);
        Localize();
        ShowPage(Page.Servers);

        Opened += async (_, _) =>
        {
            if (Program.Minimized)
                Hide();
            await _session.StartUp(Program.Connect);
        };
        Closing += OnClosing;
    }

    public Session Session => _session;

    private T Part<T>(string name) where T : Control => this.FindControl<T>(name)!;

    private void ShowPage(Page page)
    {
        foreach (var pair in _pages)
            pair.Value.IsVisible = pair.Key == page;

        foreach (var pair in _buttons)
            pair.Value.Classes.Set("active", pair.Key == page);
    }

    private async Task AskKey()
    {
        var text = await AddKeyDialog.Ask(this);
        if (!string.IsNullOrWhiteSpace(text))
            _session.AddInput(text);
    }

    private void ApplyTheme() =>
        Application.Current!.RequestedThemeVariant = _session.Data.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

    private void ApplyScale(bool first)
    {
        var factor = _session.Data.UiScale / 100.0;
        Part<LayoutTransformControl>("Scaler").LayoutTransform = new ScaleTransform(factor, factor);
        MinWidth = 920 * factor;
        MinHeight = 600 * factor;
        if (first)
        {
            Width = BaseWidth * factor;
            Height = BaseHeight * factor;
        }
    }

    private void Localize()
    {
        ToolTip.SetTip(Part<Button>("AddButton"), L.T("Добавить ключ"));
        ToolTip.SetTip(Part<Button>("ServersButton"), L.T("Серверы"));
        ToolTip.SetTip(Part<Button>("StatsButton"), L.T("Статистика"));
        ToolTip.SetTip(Part<Button>("AboutButton"), L.T("О приложении"));
        ToolTip.SetTip(Part<Button>("SettingsButton"), L.T("Настройки"));
        _hero.Localize();
        _servers.Localize();
        _stats.Localize();
        _about.Localize();
        _settings.Localize();
        _tray.Localize();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_exiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _session.Shutdown();
        _tray.Dispose();
    }

    private void Quit()
    {
        _exiting = true;
        Close();
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0)
            return;

        _session.StepScale(e.Delta.Y > 0 ? 1 : -1);
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0)
            return;

        switch (e.Key)
        {
            case Key.OemPlus or Key.Add:
                _session.StepScale(1);
                e.Handled = true;
                break;
            case Key.OemMinus or Key.Subtract:
                _session.StepScale(-1);
                e.Handled = true;
                break;
            case Key.D0 or Key.NumPad0:
                _session.SetScale(UiScaleMigration.DefaultPercent);
                e.Handled = true;
                break;
        }
    }
}
