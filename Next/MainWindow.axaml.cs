using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Tunnelka.Models;
using Tunnelka.Storage;
using Tunnelka.UI;

namespace Tunnelka.Next;

public partial class MainWindow : Window
{
    private const double BaseWidth = 1233;
    private const double BaseHeight = 720;

    private readonly Session _session;
    private bool _loading = true;
    private bool _showSettings;
    private int _scale;
    private DateTime _hintUntil;

    public MainWindow() : this(new Session())
    {
    }

    public MainWindow(Session session)
    {
        _session = session;
        AvaloniaXamlLoader.Load(this);
        _scale = session.Data.UiScale;

        Wire();
        FillScales();
        LoadSettings();
        Localize();
        ApplyScale(first: true);
        RebuildList();
        Refresh();
        _loading = false;

        Opened += async (_, _) => await _session.StartUp(Program.Connect);
        Closing += (_, _) => _session.Shutdown();
    }

    public Session Session => _session;

    private T Part<T>(string name) where T : Control => this.FindControl<T>(name)!;

    private void Wire()
    {
        Part<Button>("PowerButton").Click += (_, _) => OnPower();
        Part<Button>("AddButton").Click += async (_, _) => await Paste();
        Part<Button>("PasteButton").Click += async (_, _) => await Paste();
        Part<Button>("PingAllButton").Click += async (_, _) => await _session.PingAll();
        Part<Button>("ServersButton").Click += (_, _) => ShowSettings(false);
        Part<Button>("SettingsButton").Click += (_, _) => ShowSettings(true);

        Part<ToggleButton>("ProxyMode").Click += (_, _) =>
        {
            _session.SetMode(false);
            Refresh();
        };
        Part<ToggleButton>("TunMode").Click += (_, _) =>
        {
            _session.SetMode(true);
            Refresh();
        };

        Part<ListBox>("ServerList").SelectionChanged += (_, _) =>
        {
            if (!_loading && Part<ListBox>("ServerList").SelectedItem is ServerItem item)
                _session.Select(item.Server);
        };

        Part<ComboBox>("LanguageBox").SelectionChanged += (_, _) =>
        {
            if (_loading)
                return;

            _session.Data.Language = Part<ComboBox>("LanguageBox").SelectedIndex == 1 ? "en" : "ru";
            _session.Save();
            L.Use(_session.Data.Language);
            Localize();
            RebuildList();
            Refresh();
        };
        Part<ComboBox>("ScaleBox").SelectionChanged += (_, _) =>
        {
            if (_loading || Part<ComboBox>("ScaleBox").SelectedItem is not int percent)
                return;

            SetScale(percent);
        };
        Part<ToggleSwitch>("DarkSwitch").IsCheckedChanged += (_, _) =>
        {
            if (_loading)
                return;

            _session.Data.DarkTheme = Part<ToggleSwitch>("DarkSwitch").IsChecked == true;
            _session.Save();
            ApplyTheme();
            Refresh();
        };

        _session.StateChanged += Refresh;
        _session.ServersChanged += RebuildList;
        _session.Hint += (text, tone) =>
        {
            _hintUntil = DateTime.Now.AddSeconds(8);
            ShowHint(text, tone);
        };
        _session.Speed += OnSpeed;
        _session.ClockTick += text => Part<TextBlock>("TimerText").Text = text;
        _session.ExitRequested += () => Dispatcher.UIThread.Post(Close);

        AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
        KeyDown += OnKeyDown;
    }

    private void FillScales()
    {
        var box = Part<ComboBox>("ScaleBox");
        box.ItemsSource = UiScaleMigration.Steps;
        box.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<int>((value, _) => new TextBlock { Text = $"{value}%" });
    }

    private void LoadSettings()
    {
        Part<ComboBox>("LanguageBox").SelectedIndex = _session.Data.Language == "en" ? 1 : 0;
        Part<ComboBox>("ScaleBox").SelectedItem = UiScaleMigration.Nearest(_scale);
        Part<ToggleSwitch>("DarkSwitch").IsChecked = _session.Data.DarkTheme;
        ApplyTheme();
    }

    private void ApplyTheme() =>
        Application.Current!.RequestedThemeVariant = _session.Data.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

    private void Localize()
    {
        Part<TextBlock>("ServersTitle").Text = L.T("Серверы");
        Part<Button>("PasteButton").Content = L.T("Вставить ключ из буфера");
        Part<Button>("PingAllButton").Content = L.T("Проверить пинг всех серверов");
        Part<TextBlock>("InterfaceTitle").Text = L.T("Интерфейс");
        Part<TextBlock>("LanguageTitle").Text = L.T("Язык");
        Part<TextBlock>("ScaleTitle").Text = L.T("Масштаб интерфейса");
        Part<TextBlock>("ScaleHint").Text = L.T("Ctrl + колесо мыши, Ctrl и +/−, Ctrl+0");
        Part<TextBlock>("DarkTitle").Text = L.T("Тёмная тема");
        Part<TextBlock>("DarkHint").Text = L.T("Мягкие тёмные цвета");
        Part<ToggleButton>("ProxyMode").Content = L.T("Прокси");
        Part<TextBlock>("EmptyText").Text = L.T("Ключей пока нет");
    }

    private void ShowSettings(bool settings)
    {
        _showSettings = settings;
        Part<Control>("ServersPage").IsVisible = !settings;
        Part<Control>("InterfacePage").IsVisible = settings;
        Part<Button>("ServersButton").Classes.Set("active", !settings);
        Part<Button>("SettingsButton").Classes.Set("active", settings);
    }

    private async Task Paste()
    {
        var text = await (GetTopLevel(this)?.Clipboard?.GetTextAsync() ?? Task.FromResult<string?>(null));
        if (string.IsNullOrWhiteSpace(text))
        {
            ShowHint(L.T("Буфер обмена пуст: сначала скопируй ключ"), Tone.Mid);
            return;
        }

        _session.AddInput(text);
        ShowSettings(false);
    }

    private void OnPower()
    {
        if (_session.Active == null && _session.Data.Servers.Count == 0)
        {
            _ = Paste();
            return;
        }

        _session.Toggle();
    }

    private void RebuildList()
    {
        var list = Part<ListBox>("ServerList");
        list.ItemTemplate ??= new Avalonia.Controls.Templates.FuncDataTemplate<ServerItem>((item, _) => BuildRow(item));
        var wasLoading = _loading;
        _loading = true;
        list.ItemsSource = _session.Data.Servers.Select(s => new ServerItem(s)).ToList();
        _loading = wasLoading;
        SelectCurrent();
        Part<TextBlock>("EmptyText").IsVisible = _session.Data.Servers.Count == 0;
    }

    private Control BuildRow(ServerItem? item)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(14, 12) };
        if (item == null)
            return grid;

        grid.Children.Add(new TextBlock
        {
            Text = ServerText.CleanName(item.Server),
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var ms = item.Server.PingMs;
        var ping = new TextBlock
        {
            Text = ms == null ? "" : ms < 0 ? "n/a" : $"{ms} {L.T("мс")}",
            Foreground = Brush(Session.PingTone(ms) switch
            {
                Tone.Good => "PingGoodBrush",
                Tone.Mid => "PingMidBrush",
                Tone.Bad => "PingBadBrush",
                _ => "TextMutedBrush"
            }),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(ping, 1);
        grid.Children.Add(ping);
        return grid;
    }

    private void SelectCurrent()
    {
        var list = Part<ListBox>("ServerList");
        var wasLoading = _loading;
        _loading = true;
        list.SelectedItem = (list.ItemsSource as IEnumerable<ServerItem>)?.FirstOrDefault(i => i.Server == _session.Selected);
        _loading = wasLoading;
    }

    private void Refresh()
    {
        var connected = _session.Active != null;
        var connecting = _session.Busy;
        var power = Part<Button>("PowerButton");
        power.Classes.Set("on", connected);

        Part<TextBlock>("StatusText").Text = connecting ? L.T("Подключение") : connected ? L.T("Подключено") : L.T("Отключено");
        Part<TextBlock>("StatusText").Foreground = connected ? Brushes.White : Brush("TextMutedBrush");
        Part<TextBlock>("TimerText").IsVisible = connected;
        Part<TextBlock>("TimerText").Foreground = Brushes.White;
        if (connected)
            Part<TextBlock>("TimerText").Text = _session.Elapsed;

        Part<ToggleButton>("ProxyMode").IsChecked = !_session.Data.Tun;
        Part<ToggleButton>("TunMode").IsChecked = _session.Data.Tun;

        var server = _session.Active ?? _session.Selected;
        var keep = DateTime.Now < _hintUntil;
        if (server == null)
        {
            Part<TextBlock>("ServerName").Text = _session.Data.Servers.Count == 0 ? L.T("Сначала добавь ключ") : L.T("Выбери сервер");
            if (!keep)
                ShowHint(_session.Data.Servers.Count == 0 ? L.T("Нажми на кнопку или на +, чтобы добавить ключ") : "", Tone.Muted);
        }
        else
        {
            Part<TextBlock>("ServerName").Text = ServerText.CleanName(server);
            if (!keep)
                ShowHint(Session.PingText(server), Session.PingTone(server.PingMs));
        }

        SelectCurrent();
    }

    private void ShowHint(string text, Tone tone)
    {
        var block = Part<TextBlock>("PingText");
        block.Text = text;
        block.Foreground = tone switch
        {
            Tone.Good => Brush("PingGoodBrush"),
            Tone.Mid => Brush("PingMidBrush"),
            Tone.Bad => Brush("PingBadBrush"),
            _ => Brush("TextMutedBrush")
        };
    }

    private void OnSpeed(string? down, string? up)
    {
        Part<TextBlock>("DownText").Text = down == null ? "" : "↓ " + down;
        Part<TextBlock>("UpText").Text = up == null ? "" : "↑ " + up;
    }

    private IBrush Brush(string key) =>
        TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush ? brush : Brushes.Gray;

    private void SetScale(int percent)
    {
        _scale = UiScaleMigration.Nearest(percent);
        _session.Data.UiScale = _scale;
        _session.Save();
        ApplyScale(first: false);
        var box = Part<ComboBox>("ScaleBox");
        if ((int?)box.SelectedItem != _scale)
        {
            _loading = true;
            box.SelectedItem = _scale;
            _loading = false;
        }
    }

    private void ApplyScale(bool first)
    {
        var factor = _scale / 100.0;
        var scaler = Part<LayoutTransformControl>("Scaler");
        scaler.LayoutTransform = new ScaleTransform(factor, factor);
        MinWidth = 920 * factor;
        MinHeight = 560 * factor;
        if (first)
        {
            Width = BaseWidth * factor;
            Height = BaseHeight * factor;
        }
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0)
            return;

        StepScale(e.Delta.Y > 0 ? 1 : -1);
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0)
            return;

        switch (e.Key)
        {
            case Key.OemPlus or Key.Add:
                StepScale(1);
                e.Handled = true;
                break;
            case Key.OemMinus or Key.Subtract:
                StepScale(-1);
                e.Handled = true;
                break;
            case Key.D0 or Key.NumPad0:
                SetScale(UiScaleMigration.DefaultPercent);
                e.Handled = true;
                break;
        }
    }

    private void StepScale(int direction)
    {
        var steps = UiScaleMigration.Steps;
        var index = Array.IndexOf(steps, UiScaleMigration.Nearest(_scale));
        SetScale(steps[Math.Max(0, Math.Min(steps.Length - 1, index + direction))]);
    }

    private sealed class ServerItem
    {
        public ServerItem(ProxyServer server) => Server = server;

        public ProxyServer Server { get; }

        public override string ToString() => ServerText.CleanName(Server);
    }
}
