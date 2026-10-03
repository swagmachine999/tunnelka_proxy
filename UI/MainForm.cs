using Tunnelka.Models;
using Tunnelka.Parsing;
using Tunnelka.Services;
using Tunnelka.Storage;
using Tunnelka.UI.Controls;
using Tunnelka.UI.Pages;

namespace Tunnelka.UI;

public class MainForm : Form, IMessageFilter
{
    private readonly AppLog _log = new();
    private readonly Settings _settings;
    private readonly ConnectionService _connection;
    private readonly SubscriptionService _subscriptions;
    private readonly PingService _pinger;
    private readonly TrafficTracker _trafficTracker;
    private readonly TrafficHistory _history;
    private readonly TrafficMonitor _traffic = new();
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };

    private readonly NotifyIcon _tray = new()
    {
        Icon = LogoView.CreateAppIcon() ?? SystemIcons.Application,
        Text = "Tunnelka",
        ContextMenuStrip = new ContextMenuStrip(),
        Visible = true
    };

    private readonly HeroView _hero = new() { Dock = DockStyle.Fill };
    private readonly TipBubble _tip = new();
    private readonly Panel _middle = new() { Dock = DockStyle.Left, Width = Theme.Px(535), Padding = Theme.Px(22, 20, 14, 10) };
    private readonly Dictionary<IconKind, Control> _pages = new();
    private readonly List<IconButton> _navButtons = new();

    private readonly SearchBox _search = new() { Dock = DockStyle.Top };
    private readonly Label _countLabel = new()
    {
        Dock = DockStyle.Top,
        Height = Theme.Px(34),
        Font = Theme.Scaled(Theme.CaptionBold),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly ServerListView _list;

    private readonly LogPage _logPage;
    private readonly RoutingPage _routingPage;
    private readonly SettingsPage _settingsPage;
    private readonly PingPage _pingPage;
    private readonly InterfacePage _interfacePage;
    private readonly StatsPage _statsPage;
    private readonly OverlayPage _overlayPage;
    private readonly AboutPage _aboutPage;
    private readonly OverlayController _overlay;
    private readonly UpdateWatcher _updates;

    private readonly System.Windows.Forms.Timer _autoUpdate = new() { Interval = 60_000 };

    private readonly AutoServers _autos;
    private bool _busy;
    private ProxyServer? _selected;
    private ProxyServer? _active;
    private DateTime _connectedAt;
    private bool _exiting;
    private bool _startHidden;
    private bool _started;

    private readonly System.Windows.Forms.Timer _scaleDelay = new() { Interval = 350 };

    public event EventHandler? ReloadRequested;

    public MainForm(bool reconnect = false, IconKind startPage = IconKind.Servers, Rectangle? bounds = null, FormWindowState state = FormWindowState.Normal, bool startHidden = false)
    {
        _settings = new Settings(_log);
        Data.AutoStart = Autostart.IsEnabled();
        _connection = new ConnectionService(_log);
        _subscriptions = new SubscriptionService(_settings, _log, ProxyPort);
        _autos = new AutoServers(_settings, _subscriptions);
        _list = new ServerListView(_subscriptions);
        _pinger = new PingService(_settings);
        _trafficTracker = new TrafficTracker(_settings);
        _history = new TrafficHistory(_settings);
        _statsPage = new StatsPage(_history, Data.StatsPeriod);

        Theme.Use(Data.DarkTheme);

        Text = "Tunnelka";
        ClientSize = new Size(1110, 660);
        MinimumSize = new Size(870, 540);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Theme.Scaled(Theme.Body);
        KeyPreview = true;
        Icon = LogoView.CreateAppIcon() ?? Icon;
        Theme.Bind(this, () => Theme.Window);

        _log.Written += Log;
        _logPage = new LogPage(() => ShowPage(IconKind.Settings));
        _routingPage = new RoutingPage(Data.Rules, () => ShowPage(IconKind.Settings));
        _settingsPage = new SettingsPage(Data.Tun, Data.SpeedInterval, Data.RealPing, Data.RefreshOnStart, Data.PingOnStart, Data.AutoStart, Data.ConnectOnStart);
        _interfacePage = new InterfacePage(Data.DarkTheme, Data.UiScale, Data.Language, () => ShowPage(IconKind.Settings));
        _pingPage = new PingPage(Data.RealPing, Data.PingUrl, () => ShowPage(IconKind.Settings));
        _overlayPage = new OverlayPage(Data.Overlay, () => ShowPage(IconKind.Settings));
        _aboutPage = new AboutPage(ProxyPort);
        _overlay = new OverlayController(Data.Overlay, ProxyPort, () => Data.PingUrl);
        _updates = new UpdateWatcher(ProxyPort);

        BuildLayout();

        _tray.MouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;

            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        };
        _tray.ContextMenuStrip.Items.Add(L.T("Выход"), null, (_, _) =>
        {
            _exiting = true;
            Close();
        });

        _list.ServerSelected += Select;
        _list.ServerConnectRequested += server =>
        {
            Select(server);
            Connect();
        };
        _list.ServerDeleteRequested += Delete;
        _list.SubscriptionRefreshRequested += async url => await RefreshSubscription(url);
        _list.SubscriptionPingRequested += async url => await PingSubscription(url);
        _list.SubscriptionDeleteRequested += DeleteSubscription;
        _list.PasteRequested += (_, _) => PasteFromClipboard();
        _list.AddRequested += (_, _) => ShowAddDialog();
        _autoUpdate.Tick += async (_, _) => await RefreshDueSubscriptions();
        _autoUpdate.Start();
        _startHidden = startHidden;
        Shown += async (_, _) => await StartUp(reconnect);

        _hero.Tun = Data.Tun;
        _hero.PowerClicked += (_, _) => ToggleConnection();
        _hero.PingClicked += async (_, _) => await PingCurrent();
        _hero.RefreshClicked += async (_, _) => await RefreshCurrentSubscription();
        _hero.ModeSelected += SetMode;
        WireSettings();
        WireOverlay();
        _routingPage.RulesChanged += (_, _) => OnRulesChanged();
        _routingPage.ReconnectRequested += (_, _) => Reconnect();
        _search.QueryChanged += (_, _) => _list.Filter(_search.Query);
        _clock.Tick += (_, _) => UpdateClock();
        _traffic.Updated += OnTraffic;
        _connection.Exited += OnCoreExited;
        KeyDown += OnKeyDown;
        FormClosing += (_, e) =>
        {
            if (!_exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            Disconnect(true);
        };
        Application.AddMessageFilter(this);
        FormClosed += (_, _) =>
        {
            Application.RemoveMessageFilter(this);
            _autoUpdate.Dispose();
            _clock.Dispose();
            _scaleDelay.Dispose();
            _tray.Dispose();
            _traffic.Dispose();
            _overlay.Dispose();
            _updates.Dispose();
            _connection.Dispose();
        };

        _selected = RestoreSelection();
        RebuildList();
        UpdateHero();
        ShowPage(startPage);

        if (bounds != null)
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds.Value;
            WindowState = state;
        }

        _interfacePage.ScaleSelector.ValueChanged += (_, _) =>
        {
            _scaleDelay.Stop();
            _scaleDelay.Start();
        };
        _scaleDelay.Tick += (_, _) =>
        {
            _scaleDelay.Stop();
            Data.UiScale = _interfacePage.ScaleSelector.Value;
            Save();
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        };

    }

    public bool PrepareForReplace()
    {
        _exiting = true;
        var wasConnected = _connection.IsRunning;
        Disconnect(true);
        return wasConnected;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyNativeTheme();
    }

    private void BuildLayout()
    {
        var sidebar = Theme.Bind(new Panel { Dock = DockStyle.Left, Width = Theme.Px(68) }, () => Theme.Sidebar);
        sidebar.Controls.Add(new LogoView { Location = new Point(Theme.Px(12), Theme.Px(18)) });

        var add = new IconButton(IconKind.Add, L.T("Добавить ключ")) { Location = new Point(Theme.Px(12), Theme.Px(84)) };
        add.Click += (_, _) => ShowAddDialog();
        AttachTip(add);
        sidebar.Controls.Add(add);

        var separator = Theme.Bind(new Panel { Location = new Point(Theme.Px(20), Theme.Px(140)), Size = new Size(Theme.Px(28), Math.Max(1, Theme.Px(2))) }, () => Theme.Border);
        sidebar.Controls.Add(separator);

        var nav = new (IconKind Kind, string Title)[]
        {
            (IconKind.Servers, L.T("Серверы")),
            (IconKind.Stats, L.T("Статистика"))
        };

        for (var i = 0; i < nav.Length; i++)
            AddNavButton(sidebar, new IconButton(nav[i].Kind, nav[i].Title) { Location = new Point(Theme.Px(12), Theme.Px(156 + i * 54)) });

        var about = new IconButton(IconKind.About, L.T("О приложении")) { Location = new Point(Theme.Px(12), Theme.Px(546)) };
        AddNavButton(sidebar, about);
        var settings = new IconButton(IconKind.Settings, L.T("Настройки")) { Location = new Point(Theme.Px(12), Theme.Px(600)) };
        AddNavButton(sidebar, settings);
        sidebar.Resize += (_, _) =>
        {
            settings.Top = sidebar.Height - settings.Height - Theme.Px(18);
            about.Top = settings.Top - about.Height - Theme.Px(10);
        };

        Theme.Bind(_middle, () => Theme.Surface);
        Theme.Bind(_countLabel, () => Theme.Surface, () => Theme.TextMuted);

        var serversPage = Theme.Bind(new Panel { Dock = DockStyle.Fill }, () => Theme.Surface);
        var gap = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(8) }, () => Theme.Surface);
        serversPage.Controls.Add(_list);
        serversPage.Controls.Add(_countLabel);
        serversPage.Controls.Add(gap);
        var pingAll = new IconButton(IconKind.Gauge, L.T("Проверить пинг всех серверов"))
        {
            Dock = DockStyle.Right,
            Backdrop = () => Theme.Surface
        };
        pingAll.Click += async (_, _) => await PingAll();
        AttachTip(pingAll);

        var searchRow = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(42) }, () => Theme.Surface);
        var searchGap = Theme.Bind(new Panel { Dock = DockStyle.Right, Width = Theme.Px(8) }, () => Theme.Surface);
        _search.Dock = DockStyle.Fill;
        searchRow.Controls.Add(_search);
        searchRow.Controls.Add(searchGap);
        searchRow.Controls.Add(pingAll);

        var scan = PageParts.Button(L.T("Сканировать QR"), false);
        scan.Click += async (_, _) => await ScanQr();
        var share = PageParts.Button(L.T("Поделиться ключом"), false);
        share.Click += (_, _) => ShareKey();
        var actionRow = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(46) }, () => Theme.Surface);
        actionRow.Controls.AddRange(new Control[] { scan, share });
        actionRow.Resize += (_, _) =>
        {
            var half = (actionRow.Width - Theme.Px(8)) / 2;
            scan.SetBounds(0, Theme.Px(10), half, Theme.Px(36));
            share.SetBounds(half + Theme.Px(8), Theme.Px(10), actionRow.Width - half - Theme.Px(8), Theme.Px(36));
        };

        serversPage.Controls.Add(actionRow);
        serversPage.Controls.Add(searchRow);
        serversPage.Controls.Add(PageParts.Title(L.T("Серверы")));

        _pages[IconKind.Servers] = serversPage;
        _pages[IconKind.Stats] = _statsPage;
        _pages[IconKind.Routing] = _routingPage;
        _pages[IconKind.Log] = _logPage;
        _pages[IconKind.Settings] = _settingsPage;
        _pages[IconKind.Ping] = _pingPage;
        _pages[IconKind.Interface] = _interfacePage;
        _pages[IconKind.Overlay] = _overlayPage;
        _pages[IconKind.About] = _aboutPage;

        foreach (var page in _pages.Values)
        {
            page.Visible = false;
            _middle.Controls.Add(page);
        }

        var divider = Theme.Bind(new Panel { Dock = DockStyle.Left, Width = 1 }, () => Theme.Border);

        Controls.Add(_hero);
        Controls.Add(divider);
        Controls.Add(_middle);
        Controls.Add(sidebar);
        Controls.Add(_tip);
        _tip.BringToFront();
    }

    private void AddNavButton(Panel sidebar, IconButton button)
    {
        button.Click += (_, _) => ShowPage(button.Kind);
        AttachTip(button);
        _navButtons.Add(button);
        sidebar.Controls.Add(button);
    }

    private void AttachTip(IconButton button)
    {
        button.MouseEnter += (_, _) => _tip.ShowNear(button, button.Title);
        button.MouseLeave += (_, _) => _tip.Visible = false;
    }

    public IconKind CurrentPage { get; private set; }

    private void ShowPage(IconKind kind)
    {
        CurrentPage = kind;
        foreach (var pair in _pages)
            pair.Value.Visible = pair.Key == kind;

        var active = kind is IconKind.Routing or IconKind.Log or IconKind.Ping or IconKind.Interface or IconKind.Overlay ? IconKind.Settings : kind;
        foreach (var button in _navButtons)
            button.Active = button.Kind == active;
    }

    private void SetDarkTheme(bool dark)
    {
        Data.DarkTheme = dark;
        Save();
        Theme.Use(dark);
        ApplyNativeTheme();
        InvalidateAll(this);
    }

    private void ApplyNativeTheme()
    {
        NativeTheme.TitleBar(this, Theme.IsDark);
        NativeTheme.Scrollbars(_list, Theme.IsDark);
        NativeTheme.Scrollbars(_logPage.Box, Theme.IsDark);
    }

    private static void InvalidateAll(Control control)
    {
        control.Invalidate();
        foreach (Control child in control.Controls)
            InvalidateAll(child);
    }

    private void RebuildList()
    {
        _list.Rebuild(Data.Servers, _autos.For, _selected, _active);
        _countLabel.Text = Data.Servers.Count == 0
            ? L.T("Ключей пока нет").ToUpperInvariant()
            : ServerText.Plural(Data.Servers.Count, L.T("сервер"), L.T("сервера"), L.T("серверов")).ToUpperInvariant();
    }

    private void UpdateCards() => _list.Mark(_selected, _active);

    private void Select(ProxyServer server)
    {
        _selected = server;
        UpdateCards();
        UpdateHero();
    }

    private void UpdateHero()
    {
        var server = _active ?? _selected;
        if (server == null)
        {
            _hero.EmptyText = Data.Servers.Count == 0 ? L.T("Сначала добавь ключ") : L.T("Выбери сервер");
            _hero.SetServer(Array.Empty<NamePart>(), null);
            _hero.SetPing(Data.Servers.Count == 0 ? L.T("Нажми на кнопку или на +, чтобы добавить ключ") : "", Theme.TextMuted);
            return;
        }

        _hero.SetServer(ServerText.Parts(server), ServerText.CountryCode(server.Name));
        _hero.SetPing(PingText(server), Theme.PingColor(server.PingMs));
    }

    private static string PingText(ProxyServer server) => server.PingMs switch
    {
        null => "",
        < 0 => L.T("Сервер не ответил"),
        var ms => L.F("Пинг {0} мс", ms)
    };

    private async Task ScanQr()
    {
        Hide();
        await Task.Delay(300);
        string? text;
        try
        {
            text = QrScanner.FromScreen();
        }
        finally
        {
            Show();
            Activate();
        }

        if (text == null)
        {
            using var dialog = new OpenFileDialog
            {
                Title = L.T("QR-код на экране не найден. Выбери картинку с QR-кодом"),
                Filter = L.T("Картинки|*.png;*.jpg;*.jpeg;*.bmp;*.gif")
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            text = QrScanner.FromFile(dialog.FileName);
        }

        if (text == null)
        {
            _hero.SetPing(L.T("QR-код не найден"), Theme.PingBad);
            return;
        }

        AddInput(text);
    }

    private void ShareKey()
    {
        var keys = _subscriptions.Profiles.Select(p => (p.Title, p.Url)).ToList();
        if (keys.Count == 0)
        {
            _hero.SetPing(L.T("Нет подписок, чтобы поделиться"), Theme.TextMuted);
            return;
        }

        LinkDialog.Show(this, L.T("Поделиться ключом"), keys, true);
    }

    public bool PreFilterMessage(ref Message m)
    {
        const int WheelMessage = 0x020A;
        if (m.Msg != WheelMessage || (ModifierKeys & Keys.Control) == 0 || ActiveForm != this)
            return false;

        var delta = (short)((long)m.WParam >> 16);
        ChangeScale(delta > 0 ? 1 : -1);
        return true;
    }

    private void ChangeScale(int direction) => _interfacePage.ScaleSelector.Step(direction);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ActiveControl is HotkeyBox)
            return;

        if (e.Control && e.KeyCode is Keys.Oemplus or Keys.Add)
        {
            ChangeScale(1);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode is Keys.OemMinus or Keys.Subtract)
        {
            ChangeScale(-1);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode is Keys.D0 or Keys.NumPad0)
        {
            _interfacePage.ScaleSelector.SetValue(90);
            e.Handled = true;
            return;
        }

        if (ActiveControl is TextBoxBase)
            return;

        if (e.KeyCode == Keys.Delete && _selected != null && !AutoServers.IsAuto(_selected) && _pages[IconKind.Servers].Visible)
        {
            Delete(_selected);
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.V)
        {
            PasteFromClipboard();
            e.Handled = true;
        }
    }

    private void ShowAddDialog()
    {
        var text = AddDialog.Show(this);
        if (text != null)
            AddInput(text);
    }

    private void PasteFromClipboard()
    {
        var text = Clipboard.GetText().Trim();
        if (text.Length == 0)
        {
            Log(L.T("Буфер обмена пуст"));
            _hero.SetPing(L.T("Буфер обмена пуст: сначала скопируй ключ"), Theme.PingBad);
            return;
        }

        AddInput(text);
    }

    private void AddInput(string text)
    {
        text = text.Trim();

        if (text.StartsWith("http://") || text.StartsWith("https://"))
        {
            _ = AddSubscriptionUrl(text);
            return;
        }

        var servers = LinkParser.ParseInput(text);
        if (servers.Count == 0)
        {
            Log(L.T("Не нашёл поддерживаемых ключей (vless, vmess, trojan, ss)"));
            _hero.SetPing(L.T("Ключ не распознан"), Theme.PingBad);
            return;
        }

        var first = Data.Servers.Count == 0;
        Data.Servers.AddRange(servers);
        _selected ??= servers[0];
        Save();
        RebuildList();
        UpdateHero();
        ShowPage(IconKind.Servers);
        Log(L.F("Добавлено серверов: {0}", servers.Count));
        if (first)
            _hero.SetPing(L.T("Готово! Нажми большую кнопку, чтобы подключиться"), Theme.PingGood);
    }

    private async Task AddSubscriptionUrl(string url)
    {
        var first = Data.Servers.Count == 0;
        _subscriptions.Add(url);
        ShowPage(IconKind.Servers);
        await RefreshSubscription(url);
        if (first && Data.Servers.Count > 0)
            _hero.SetPing(L.T("Готово! Нажми большую кнопку, чтобы подключиться"), Theme.PingGood);
    }

    private async Task UpdateSubscriptions()
    {
        if (_subscriptions.Profiles.Count == 0)
        {
            _hero.SetPing(L.T("Подписок нет: добавь её через +"), Theme.TextMuted);
            return;
        }

        _hero.SetBusy(true);
        ShowSubscriptionResult(await _subscriptions.RefreshAll(_active));
    }

    private async Task RefreshCurrentSubscription()
    {
        var url = (_active ?? _selected)?.SubscriptionUrl;
        if (url != null && _subscriptions.IsKnown(url))
            await RefreshSubscription(url);
        else
            await UpdateSubscriptions();
    }

    private async Task RefreshSubscription(string url)
    {
        if (_subscriptions.StatusOf(url)?.State == RefreshState.Busy)
            return;

        _hero.SetBusy(true);
        ShowSubscriptionResult(await _subscriptions.Refresh(url, _active));
    }

    private void ShowSubscriptionResult(bool ok)
    {
        AfterServersChanged();
        WarnAboutExpiring();
        if (ok)
            _hero.SetPing(L.T("Подписка обновлена"), Theme.PingGood);
        else
            _hero.SetPing(L.T("Не удалось обновить подписку"), Theme.PingBad);
    }

    private async Task RefreshDueSubscriptions()
    {
        if (await _subscriptions.RefreshDue(_active))
            AfterServersChanged();

        _list.RefreshSubscriptionCards();

        WarnAboutExpiring();
    }

    private void WarnAboutExpiring()
    {
        var titles = _subscriptions.TakeNewlyExpiring();
        if (titles.Count == 0)
            return;

        var text = titles.Count == 1
            ? L.F("Подписка «{0}» скоро закончится. Продлите её, иначе доступ будет приостановлен.", titles[0])
            : L.F("Подписки {0} скоро закончатся. Продлите их, иначе доступ будет приостановлен.", string.Join(", ", titles.Select(t => $"«{t}»")));
        _tray.ShowBalloonTip(10000, "Tunnelka", text, ToolTipIcon.Warning);
    }

    private void DeleteSubscription(string url)
    {
        if (_active?.SubscriptionUrl == url)
            Disconnect();

        _subscriptions.Delete(url);
        AfterServersChanged();
    }

    private void AfterServersChanged()
    {
        if (_selected == null || (AutoServers.IsAuto(_selected) ? _autos.Members(_selected).Count == 0 : !Data.Servers.Contains(_selected)))
            _selected = Data.Servers.FirstOrDefault();

        RebuildList();
        UpdateHero();
    }

    private Task PingAll() => PingServers(Data.Servers.ToList());

    private Task PingSubscription(string url) => PingServers(_subscriptions.Servers(url));

    private async Task PingCurrent()
    {
        var server = _selected ?? _active;
        if (server == null)
            return;

        if (AutoServers.IsAuto(server))
        {
            await FindFastest(server);
            UpdateCards();
            UpdateHero();
            return;
        }

        await PingServers(new List<ProxyServer> { server });
        if (server != (_active ?? _selected))
            _hero.SetPing($"{ServerText.CleanName(server)}: {PingText(server)}", Theme.PingColor(server.PingMs));
    }

    private string SelectedLink() => AutoServers.IsAuto(_selected) ? AutoServers.Link(_selected!) : _selected?.Link ?? "";

    private ProxyServer? RestoreSelection() =>
        _autos.Restore(Data.LastServerLink)
        ?? Data.Servers.FirstOrDefault(s => s.Link == Data.LastServerLink)
        ?? Data.Servers.FirstOrDefault();

    private async Task<ProxyServer?> FindFastest(ProxyServer auto)
    {
        var servers = _autos.Members(auto);
        await PingServers(servers);
        var best = servers.Where(s => s.PingMs >= 0).OrderBy(s => s.PingMs).FirstOrDefault();
        auto.PingMs = best?.PingMs ?? -1;
        return best;
    }

    private async Task PingServers(List<ProxyServer> servers)
    {
        if (servers.Count == 0)
            return;

        _hero.SetBusy(true);
        _list.SetBusy(servers, true);
        try
        {
            await _pinger.Ping(servers, server => _list.SetBusy(new[] { server }, false));
        }
        finally
        {
            _list.SetBusy(servers, false);
        }

        UpdateCards();
        UpdateHero();
    }

    private void Delete(ProxyServer server)
    {
        if (AutoServers.IsAuto(server))
            return;

        if (server == _active)
            Disconnect();

        Data.Servers.Remove(server);
        if (_selected == server)
            _selected = Data.Servers.FirstOrDefault();

        Save();
        RebuildList();
        UpdateHero();
    }

    private void ToggleConnection()
    {
        if (_busy)
            return;

        if (_active == null && Data.Servers.Count == 0)
        {
            ShowAddDialog();
            return;
        }

        if (_active != null)
            Disconnect();
        else
            Connect();
    }

    private async void Connect()
    {
        if (_busy)
            return;

        _busy = true;
        try
        {
            await ConnectTo(_selected);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed)
                _hero.Connecting = false;
        }
    }

    private async Task ConnectTo(ProxyServer? server)
    {
        if (AutoServers.IsAuto(server))
        {
            _hero.SetPing(L.T("Ищу самый быстрый сервер…"), Theme.TextMuted);
            server = await FindFastest(server!);
            if (server == null)
            {
                _hero.SetPing(L.T("Ни один сервер не ответил"), Theme.PingBad);
                return;
            }
        }

        if (server == null)
            return;

        _hero.Connecting = true;
        if (!await Start(server))
        {
            if (_active != null)
                Disconnect();
            return;
        }

        _active = server;
        _connectedAt = DateTime.Now;
        _trafficTracker.Reset();
        _hero.SetSpeed(ServerText.Bytes(0) + L.T("/с"), ServerText.Bytes(0) + L.T("/с"));
        Data.LastServerLink = SelectedLink();
        Save();

        _hero.ElapsedText = "00:00:00";
        _hero.Connecting = false;
        _hero.Connected = true;
        _overlay.SetConnected(true);
        _clock.Start();
        _statsPage.SetSpeed(0, 0, true);
        _traffic.Start();
        UpdateCards();
        UpdateHero();
        Log(L.F("Подключено к {0}", ServerText.CleanName(server)));
    }

    private async Task<bool> Start(ProxyServer server)
    {
        var result = await _connection.StartAsync(server, Data.Tun, Data.Rules);
        if (IsDisposed)
            return false;

        switch (result)
        {
            case ConnectResult.Ok:
                return true;
            case ConnectResult.XrayMissing:
                MessageBox.Show(this, L.F("Не найден {0}", XrayRunner.XrayPath), "Tunnelka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                break;
            case ConnectResult.SingBoxMissing:
                MessageBox.Show(this,
                    L.F("Не найден {0}\n\nСкачай sing-box-windows-amd64.zip на github.com/SagerNet/sing-box/releases и положи sing-box.exe в папку core рядом с xray.exe.", XrayRunner.SingBoxPath),
                    "Tunnelka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                break;
            case ConnectResult.NeedsAdministrator:
                OfferElevation();
                break;
            default:
                ShowPage(IconKind.Log);
                break;
        }

        return false;
    }

    private void OfferElevation()
    {
        var answer = MessageBox.Show(this,
            L.T("Для режима TUN нужны права администратора. Перезапустить Tunnelka от имени администратора?"),
            "Tunnelka", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        try
        {
            Data.LastServerLink = SelectedLink();
            Save();
            Elevation.RestartElevated("--connect");
            _exiting = true;
            Application.Exit();
        }
        catch (Exception ex)
        {
            Log(L.F("Перезапуск отменён: {0}", ex.Message));
        }
    }

    private void Disconnect(bool wait = false)
    {
        var wasRunning = _active != null || _connection.IsRunning;
        if (wait)
            _connection.Stop();
        else
            _ = _connection.StopAsync();
        _clock.Stop();
        _traffic.Stop();

        _active = null;
        _routingPage.ShowReconnectHint(false);
        if (wasRunning)
            Save();

        if (IsDisposed)
            return;

        _hero.Connected = false;
        _hero.SetSpeed(null, null);
        _overlay.SetConnected(false);
        _statsPage.SetSpeed(0, 0, false);
        UpdateCards();
        UpdateHero();

        if (wasRunning)
            Log(L.T("Отключено"));
    }

    private void OnRulesChanged()
    {
        Save();
        _routingPage.ShowReconnectHint(_active != null && _connection.IsRunning);
    }

    private async void Reconnect()
    {
        _routingPage.ShowReconnectHint(false);
        if (_busy || _active == null || !_connection.IsRunning)
            return;

        _busy = true;
        _hero.Connecting = true;
        try
        {
            _trafficTracker.ResetCounters();
            if (await Start(_active))
                Log(L.T("Правила применены"));
            else
                Disconnect();
        }
        finally
        {
            _busy = false;
            if (!IsDisposed)
                _hero.Connecting = false;
        }
    }

    protected override void SetVisibleCore(bool value)
    {
        if (_startHidden && value)
        {
            _startHidden = false;
            if (!IsHandleCreated)
                CreateHandle();
            BeginInvoke(new Action(() => OnShown(EventArgs.Empty)));
            value = false;
        }
        base.SetVisibleCore(value);
    }

    private async Task StartUp(bool reconnect)
    {
        if (_started)
            return;
        _started = true;

        if (reconnect || Data.ConnectOnStart)
            Connect();
        _updates.Start();
        await RunStartupTasks();
    }

    private async Task RunStartupTasks()
    {
        if (Data.RefreshOnStart && _subscriptions.Profiles.Count > 0)
            await UpdateSubscriptions();
        else
            await RefreshDueSubscriptions();

        if (Data.PingOnStart)
            await PingAll();
    }

    private void OnTraffic(TrafficCounters counters)
    {
        var delta = _trafficTracker.Process(counters);
        _history.Add(delta);
        _statsPage.SetSpeed(delta.ProxyDown + delta.DirectDown, delta.ProxyUp + delta.DirectUp, true);
        _overlay.SetSpeed(delta.ProxyDown + delta.DirectDown, delta.ProxyUp + delta.DirectUp);

        if (_trafficTracker.TryGetAverage(out var averageDown, out var averageUp))
            _hero.SetSpeed(ServerText.Bytes(averageDown) + L.T("/с"), ServerText.Bytes(averageUp) + L.T("/с"));
    }

    private void UpdateClock()
    {
        var elapsed = DateTime.Now - _connectedAt;
        _hero.ElapsedText = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    private void OnCoreExited()
    {
        if (IsDisposed || _exiting)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnCoreExited));
            return;
        }

        if (_busy || _active == null)
            return;

        Log(L.T("Ядро VPN завершилось. Причина обычно видна в строках выше"));
        Disconnect();
        ShowPage(IconKind.Log);
    }

    private void SetMode(bool tun)
    {
        if (Data.Tun == tun)
            return;

        Data.Tun = tun;
        _hero.Tun = tun;
        _settingsPage.ModeSelector.SelectedIndex = tun ? 1 : 0;
        Save();
        Log(tun ? L.T("Режим TUN: через VPN идёт весь трафик") : L.T("Режим прокси: через VPN идут браузер и программы"));

        if (_active != null && !_busy)
        {
            Disconnect();
            Connect();
        }
    }

    private void WireSettings()
    {
        _settingsPage.ModeSelector.SelectedIndexChanged += (_, _) => SetMode(_settingsPage.ModeSelector.SelectedIndex == 1);
        _interfacePage.DarkToggle.CheckedChanged += (_, _) => SetDarkTheme(_interfacePage.DarkToggle.Checked);
        _interfacePage.LanguageSelector.SelectedIndexChanged += (_, _) =>
        {
            Data.Language = _interfacePage.Language;
            Save();
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        };

        _settingsPage.InterfaceRow.Click += (_, _) => ShowPage(IconKind.Interface);
        _settingsPage.OverlayRow.Click += (_, _) => ShowPage(IconKind.Overlay);
        _settingsPage.RoutingRow.Click += (_, _) => ShowPage(IconKind.Routing);
        _settingsPage.LogRow.Click += (_, _) => ShowPage(IconKind.Log);
        _settingsPage.PingRow.Click += (_, _) => ShowPage(IconKind.Ping);

        BindToggle(_settingsPage.RefreshToggle, on => Data.RefreshOnStart = on);
        BindToggle(_settingsPage.PingToggle, on => Data.PingOnStart = on);
        BindToggle(_settingsPage.ConnectToggle, on => Data.ConnectOnStart = on);
        BindToggle(_settingsPage.AutoStartToggle, on =>
        {
            Data.AutoStart = on;
            Autostart.Apply(on);
        });
        if (Data.AutoStart)
            Autostart.Apply(true);

        _statsPage.PeriodChanged += minutes =>
        {
            Data.StatsPeriod = minutes;
            Save();
        };
        _pingPage.Changed += (_, _) =>
        {
            Data.RealPing = _pingPage.IsReal;
            var url = _pingPage.Url.Query;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                Data.PingUrl = url;
            _settingsPage.ShowPingMode(Data.RealPing);
            Save();
        };
        _settingsPage.SpeedSelector.ValueChanged += (_, _) =>
        {
            Data.SpeedInterval = _settingsPage.SpeedSelector.Value;
            _trafficTracker.RestartAveraging();
            Save();
        };
    }

    private void BindToggle(ToggleSwitch toggle, Action<bool> apply) =>
        toggle.CheckedChanged += (_, _) =>
        {
            apply(toggle.Checked);
            Save();
        };

    private void WireOverlay()
    {
        _overlay.ApplyHotkey();
        _overlayPage.ShowHotkeyState(_overlay.HotkeyFailed);
        _overlayPage.HotkeyChanged += (_, _) =>
        {
            Save();
            _overlay.ApplyHotkey();
            _overlayPage.ShowHotkeyState(_overlay.HotkeyFailed);
        };
        _overlayPage.OptionsChanged += (_, _) =>
        {
            Save();
            _overlay.Redraw();
        };
        _overlayPage.ShowToggle.CheckedChanged += (_, _) =>
        {
            if (_overlayPage.ShowToggle.Checked != _overlay.IsShown)
                _overlay.Toggle();
        };
        _overlay.VisibilityChanged += (_, _) => _overlayPage.ShowToggle.Checked = _overlay.IsShown;
    }

    private int? ProxyPort() => _connection.IsRunning ? XrayConfigBuilder.HttpPort : null;

    private AppData Data => _settings.Data;

    private void Save() => _settings.Save();

    private void Log(string text)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            if (IsHandleCreated)
                BeginInvoke(new Action(() => Log(text)));
            return;
        }

        _logPage.Append($"[{DateTime.Now:HH:mm:ss}] {text}");
    }
}
