using VpnClient.Models;
using VpnClient.Parsing;
using VpnClient.Services;
using VpnClient.Storage;
using VpnClient.UI.Controls;
using VpnClient.UI.Pages;

namespace VpnClient.UI;

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

    private readonly FlowLayoutPanel _list = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true
    };

    private readonly StatsView _stats = new() { Dock = DockStyle.Fill };
    private readonly LogPage _logPage;
    private readonly RoutingPage _routingPage;
    private readonly SettingsPage _settingsPage;
    private readonly PingPage _pingPage;
    private readonly InterfacePage _interfacePage;

    private static readonly int[] StatsPeriods = { 3, 10, 30, 60, 180, 300, 1440, TrafficHistory.AllTime };
    private readonly Segmented _periodSelector = new(L.T("3 мин"), L.T("10 мин"), L.T("30 мин"), L.T("1 ч"), L.T("3 ч"), L.T("5 ч"), L.T("24 ч"), L.T("Всё")) { Dock = DockStyle.Top };
    private double _lastDown;
    private double _lastUp;

    private readonly ContextMenuStrip _cardMenu = new();
    private readonly ContextMenuStrip _subscriptionMenu = new();
    private readonly System.Windows.Forms.Timer _autoUpdate = new() { Interval = 60_000 };
    private SubscriptionCard? _menuSubscription;
    private readonly List<ServerCard> _cards = new();

    private ProxyServer? _selected;
    private ProxyServer? _active;
    private ServerCard? _menuCard;
    private DateTime _connectedAt;
    private bool _exiting;

    private readonly System.Windows.Forms.Timer _scaleDelay = new() { Interval = 350 };

    public event EventHandler? ReloadRequested;

    public MainForm(bool reconnect = false, IconKind startPage = IconKind.Servers, Rectangle? bounds = null, FormWindowState state = FormWindowState.Normal)
    {
        _settings = new Settings(_log);
        _connection = new ConnectionService(_log);
        _subscriptions = new SubscriptionService(_settings, _log);
        _pinger = new PingService(_settings);
        _trafficTracker = new TrafficTracker(_settings);
        _history = new TrafficHistory(_settings);

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
        _settingsPage = new SettingsPage(Data.Tun, Data.SpeedInterval, Data.RealPing, Data.RefreshOnStart, Data.PingOnStart);
        _interfacePage = new InterfacePage(Data.DarkTheme, Data.UiScale, Data.Language, () => ShowPage(IconKind.Settings));
        _pingPage = new PingPage(Data.RealPing, Data.PingUrl, () => ShowPage(IconKind.Settings));

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

        BuildCardMenu();
        BuildSubscriptionMenu();
        _autoUpdate.Tick += async (_, _) => await RefreshDueSubscriptions();
        _autoUpdate.Start();
        Shown += async (_, _) => await RunStartupTasks();

        _hero.Tun = Data.Tun;
        _hero.PowerClicked += (_, _) => ToggleConnection();
        _hero.PingClicked += async (_, _) => await PingCurrent();
        _hero.RefreshClicked += async (_, _) => await UpdateSubscriptions();
        _hero.ModeSelected += SetMode;
        _settingsPage.ModeSelector.SelectedIndexChanged += (_, _) => SetMode(_settingsPage.ModeSelector.SelectedIndex == 1);
        _interfacePage.DarkToggle.CheckedChanged += (_, _) => SetDarkTheme(_interfacePage.DarkToggle.Checked);
        _interfacePage.LanguageSelector.SelectedIndexChanged += (_, _) =>
        {
            Data.Language = _interfacePage.Language;
            Save();
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        };
        _settingsPage.InterfaceRow.Click += (_, _) => ShowPage(IconKind.Interface);
        _settingsPage.RefreshToggle.CheckedChanged += (_, _) =>
        {
            Data.RefreshOnStart = _settingsPage.RefreshToggle.Checked;
            Save();
        };
        _settingsPage.PingToggle.CheckedChanged += (_, _) =>
        {
            Data.PingOnStart = _settingsPage.PingToggle.Checked;
            Save();
        };
        _periodSelector.SelectedIndex = Math.Max(0, Array.IndexOf(StatsPeriods, Data.StatsPeriod));
        _periodSelector.SelectedIndexChanged += (_, _) =>
        {
            Data.StatsPeriod = StatsPeriods[_periodSelector.SelectedIndex];
            Save();
            RefreshStats();
        };
        _settingsPage.RoutingRow.Click += (_, _) => ShowPage(IconKind.Routing);
        _settingsPage.LogRow.Click += (_, _) => ShowPage(IconKind.Log);
        _settingsPage.PingRow.Click += (_, _) => ShowPage(IconKind.Ping);
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
        _routingPage.RulesChanged += (_, _) => OnRulesChanged();
        _search.QueryChanged += (_, _) => ApplyFilter();
        _list.Resize += (_, _) => ResizeCards();
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

            Disconnect();
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
            _connection.Dispose();
        };

        RefreshStats();
        _selected = Data.Servers.FirstOrDefault(s => s.Link == Data.LastServerLink) ?? Data.Servers.FirstOrDefault();
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

        if (reconnect)
            Shown += (_, _) => Connect();
    }

    public bool PrepareForReplace()
    {
        _exiting = true;
        var wasConnected = _connection.IsRunning;
        Disconnect();
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

        var settings = new IconButton(IconKind.Settings, L.T("Настройки")) { Location = new Point(Theme.Px(12), Theme.Px(600)) };
        AddNavButton(sidebar, settings);
        sidebar.Resize += (_, _) => settings.Top = sidebar.Height - settings.Height - Theme.Px(18);

        Theme.Bind(_middle, () => Theme.Surface);
        Theme.Bind(_list, () => Theme.Surface);
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

        var statsPage = Theme.Bind(new Panel { Dock = DockStyle.Fill }, () => Theme.Surface);
        var statsGap = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface);
        var reset = PageParts.Button(L.T("Сбросить"), false);
        reset.Dock = DockStyle.Right;
        reset.Width = Theme.Px(120);
        reset.Click += (_, _) => ResetStats();
        var statsHeader = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(52), Padding = Theme.Px(0, 10, 6, 8) }, () => Theme.Surface);
        var statsTitle = PageParts.Title(L.T("Статистика"));
        statsTitle.Dock = DockStyle.Fill;
        statsHeader.Controls.Add(statsTitle);
        statsHeader.Controls.Add(reset);
        var periodGap = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(12) }, () => Theme.Surface);
        statsPage.Controls.Add(_stats);
        statsPage.Controls.Add(statsGap);
        statsPage.Controls.Add(_periodSelector);
        statsPage.Controls.Add(periodGap);
        statsPage.Controls.Add(statsHeader);

        _pages[IconKind.Servers] = serversPage;
        _pages[IconKind.Stats] = statsPage;
        _pages[IconKind.Routing] = _routingPage;
        _pages[IconKind.Log] = _logPage;
        _pages[IconKind.Settings] = _settingsPage;
        _pages[IconKind.Ping] = _pingPage;
        _pages[IconKind.Interface] = _interfacePage;

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

        var active = kind is IconKind.Routing or IconKind.Log or IconKind.Ping or IconKind.Interface ? IconKind.Settings : kind;
        if (kind == IconKind.Stats)
            RefreshStats();
        foreach (var button in _navButtons)
            button.Active = button.Kind == active;
    }

    private void BuildCardMenu()
    {
        _cardMenu.Items.Add(L.T("Подключиться"), null, (_, _) =>
        {
            if (_menuCard == null)
                return;
            Select(_menuCard.Server);
            Connect();
        });
        _cardMenu.Items.Add(L.T("Удалить"), null, (_, _) =>
        {
            if (_menuCard != null)
                Delete(_menuCard.Server);
        });
        _cardMenu.Opening += (_, _) => _menuCard = _cardMenu.SourceControl as ServerCard;
    }

    private void BuildSubscriptionMenu()
    {
        _subscriptionMenu.Items.Add(L.T("Показать ключ"), null, (_, _) =>
        {
            if (_menuSubscription != null)
                LinkDialog.Show(this, _menuSubscription.Info.Title, new[] { (_menuSubscription.Info.Title, _menuSubscription.Info.Url) }, false);
        });
        _subscriptionMenu.Items.Add(L.T("Удалить ключ"), null, (_, _) =>
        {
            if (_menuSubscription == null)
                return;

            var answer = MessageBox.Show(this, L.F("Удалить ключ «{0}» и все его серверы?", _menuSubscription.Info.Title),
                "Tunnelka", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
                DeleteSubscription(_menuSubscription.Info.Url);
        });
        _subscriptionMenu.Opening += (_, _) => _menuSubscription = _subscriptionMenu.SourceControl as SubscriptionCard;
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
        _list.SuspendLayout();
        foreach (Control control in _list.Controls.Cast<Control>().ToList())
            control.Dispose();
        _list.Controls.Clear();
        _cards.Clear();

        var controls = new List<Control>();
        foreach (var info in _subscriptions.Profiles)
        {
            var url = info.Url;
            var servers = _subscriptions.Servers(url);
            var header = new SubscriptionCard(info) { ContextMenuStrip = _subscriptionMenu, ServerCount = servers.Count };
            header.RefreshClicked += async (_, _) => await RefreshSubscription(url);
            header.PingClicked += async (_, _) => await PingSubscription(url);
            header.MenuClicked += (_, point) => _subscriptionMenu.Show(header, point);
            header.CollapseClicked += (_, _) =>
            {
                _subscriptions.ToggleCollapsed(info);
                header.Invalidate();
                ApplyFilter();
            };
            controls.Add(header);
            controls.AddRange(servers.Select(CreateCard));
        }

        controls.AddRange(Data.Servers
            .Where(s => s.SubscriptionUrl == null || !_subscriptions.IsKnown(s.SubscriptionUrl))
            .Select(CreateCard));

        _list.Controls.AddRange(controls.ToArray());
        UpdateCards();
        ApplyFilter();
        ResizeCards();
        _list.ResumeLayout();

        _countLabel.Text = Data.Servers.Count == 0
            ? L.T("Нажми + или Ctrl+V, чтобы добавить ключ")
            : ServerText.Plural(Data.Servers.Count, L.T("сервер"), L.T("сервера"), L.T("серверов")).ToUpperInvariant();
    }

    private ServerCard CreateCard(ProxyServer server)
    {
        var card = new ServerCard(server) { ContextMenuStrip = _cardMenu };
        card.Click += (_, _) => Select(server);
        card.DoubleClick += (_, _) =>
        {
            Select(server);
            Connect();
        };
        _cards.Add(card);
        return card;
    }

    private void UpdateCards()
    {
        foreach (var card in _cards)
        {
            card.IsSelected = card.Server == _selected;
            card.IsActive = card.Server == _active;
            card.Invalidate();
        }
    }

    private void ResizeCards()
    {
        var width = _list.Width - SystemInformation.VerticalScrollBarWidth - Theme.Px(6);
        if (width <= 0)
            return;

        foreach (Control control in _list.Controls)
            control.Width = width;
    }

    private void ApplyFilter()
    {
        var query = _search.Query;
        foreach (var header in _list.Controls.OfType<SubscriptionCard>())
            header.Visible = query.Length == 0;

        foreach (var card in _cards)
        {
            card.Visible = query.Length == 0
                ? !_subscriptions.IsCollapsed(card.Server.SubscriptionUrl)
                : card.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || card.Server.Address.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

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
            _hero.SetServer(Array.Empty<NamePart>(), null);
            _hero.SetPing("", Theme.TextMuted);
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

        if (e.KeyCode == Keys.Delete && _selected != null && _pages[IconKind.Servers].Visible)
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

        Data.Servers.AddRange(servers);
        _selected ??= servers[0];
        Save();
        RebuildList();
        UpdateHero();
        ShowPage(IconKind.Servers);
        Log(L.F("Добавлено серверов: {0}", servers.Count));
    }

    private async Task AddSubscriptionUrl(string url)
    {
        _subscriptions.Add(url);
        ShowPage(IconKind.Servers);
        await RefreshSubscription(url);
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

    private async Task RefreshSubscription(string url)
    {
        _hero.SetBusy(true);
        ShowSubscriptionResult(await _subscriptions.Refresh(url, _active));
    }

    private void ShowSubscriptionResult(bool ok)
    {
        AfterServersChanged();
        if (ok)
            _hero.SetPing(L.T("Подписка обновлена"), Theme.PingGood);
        else
            _hero.SetPing(L.T("Не удалось обновить подписку"), Theme.PingBad);
    }

    private async Task RefreshDueSubscriptions()
    {
        if (await _subscriptions.RefreshDue(_active))
            AfterServersChanged();

        foreach (var header in _list.Controls.OfType<SubscriptionCard>())
            header.Invalidate();
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
        if (_selected == null || !Data.Servers.Contains(_selected))
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

        await PingServers(new List<ProxyServer> { server });
        if (server != (_active ?? _selected))
            _hero.SetPing($"{ServerText.CleanName(server)}: {PingText(server)}", Theme.PingColor(server.PingMs));
    }

    private async Task PingServers(List<ProxyServer> servers)
    {
        if (servers.Count == 0)
            return;

        _hero.SetBusy(true);
        SetBusy(servers, true);
        try
        {
            await _pinger.Ping(servers, server => SetBusy(new[] { server }, false));
        }
        finally
        {
            SetBusy(servers, false);
        }

        UpdateCards();
        UpdateHero();
    }

    private void SetBusy(IEnumerable<ProxyServer> servers, bool busy)
    {
        var set = new HashSet<ProxyServer>(servers);
        foreach (var card in _cards)
        {
            if (set.Contains(card.Server))
                card.IsBusy = busy;
        }
    }

    private void Delete(ProxyServer server)
    {
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
        if (_connection.IsRunning)
            Disconnect();
        else
            Connect();
    }

    private void Connect()
    {
        var server = _selected;
        if (server == null || !Start(server))
            return;

        _active = server;
        _connectedAt = DateTime.Now;
        _trafficTracker.Reset();
        _hero.SetSpeed(ServerText.Bytes(0) + L.T("/с"), ServerText.Bytes(0) + L.T("/с"));
        Data.LastServerLink = server.Link;
        Save();

        _hero.ElapsedText = "00:00:00";
        _hero.Connected = true;
        _clock.Start();
        _lastDown = _lastUp = 0;
        RefreshStats();
        _traffic.Start();
        UpdateCards();
        UpdateHero();
        Log(L.F("Подключено к {0}", ServerText.CleanName(server)));
    }

    private bool Start(ProxyServer server)
    {
        switch (_connection.Start(server, Data.Tun, Data.Rules))
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
            Elevation.RestartElevated("--connect");
            _exiting = true;
            Application.Exit();
        }
        catch (Exception ex)
        {
            Log(L.F("Перезапуск отменён: {0}", ex.Message));
        }
    }

    private void Disconnect()
    {
        var wasRunning = _connection.IsRunning;
        _connection.Stop();
        _clock.Stop();
        _traffic.Stop();

        _active = null;
        if (wasRunning)
            Save();

        if (IsDisposed)
            return;

        _hero.Connected = false;
        _hero.SetSpeed(null, null);
        _lastDown = _lastUp = 0;
        RefreshStats();
        UpdateCards();
        UpdateHero();

        if (wasRunning)
            Log(L.T("Отключено"));
    }

    private void OnRulesChanged()
    {
        Save();
        if (_active == null || !_connection.IsRunning)
            return;

        _trafficTracker.ResetCounters();
        if (Start(_active))
            Log(L.T("Правила применены"));
        else
            Disconnect();
    }

    private void RefreshStats()
    {
        if (CurrentPage != IconKind.Stats)
            return;

        var minutes = StatsPeriods[_periodSelector.SelectedIndex];
        var period = minutes == TrafficHistory.AllTime ? L.T("всё время") : ServerText.Duration(minutes * 60);
        var graphPeriod = minutes == TrafficHistory.AllTime ? ServerText.Duration(24 * 3600) : period;
        _stats.SetData(period, graphPeriod, _history.Sum(minutes), _history.Speeds(minutes, 120), _lastDown, _lastUp, _connection.IsRunning);
    }

    private void ResetStats()
    {
        var answer = MessageBox.Show(this, L.T("Сбросить всю статистику трафика?"), "Tunnelka", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        _history.Reset();
        RefreshStats();
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
        _lastDown = delta.ProxyDown + delta.DirectDown;
        _lastUp = delta.ProxyUp + delta.DirectUp;
        RefreshStats();

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
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnCoreExited));
            return;
        }

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

        if (_active != null && _connection.IsRunning)
        {
            Disconnect();
            Connect();
        }
    }

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
