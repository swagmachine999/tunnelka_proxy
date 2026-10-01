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
    private readonly Panel _middle = new() { Dock = DockStyle.Left, Width = Theme.Px(410), Padding = Theme.Px(22, 20, 14, 10) };
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

    public event EventHandler? ScaleChangeRequested;

    public MainForm(bool reconnect = false, bool openSettings = false, Rectangle? bounds = null, FormWindowState state = FormWindowState.Normal)
    {
        _settings = new Settings(_log);
        _connection = new ConnectionService(_log);
        _subscriptions = new SubscriptionService(_settings, _log);
        _pinger = new PingService(_settings);
        _trafficTracker = new TrafficTracker(_settings);

        Theme.Use(Data.DarkTheme);

        Text = "Tunnelka";
        ClientSize = new Size(1000, 660);
        MinimumSize = new Size(760, 540);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Theme.Scaled(Theme.Body);
        KeyPreview = true;
        Icon = LogoView.CreateAppIcon() ?? Icon;
        Theme.Bind(this, () => Theme.Window);

        _log.Written += Log;
        _logPage = new LogPage(() => ShowPage(IconKind.Settings));
        _routingPage = new RoutingPage(Data.Rules, () => ShowPage(IconKind.Settings));
        _settingsPage = new SettingsPage(Data.DarkTheme, Data.Tun, Data.SpeedInterval, Data.RealPing, Data.UiScale);
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
        _tray.ContextMenuStrip.Items.Add("Выход", null, (_, _) =>
        {
            _exiting = true;
            Close();
        });

        BuildCardMenu();
        BuildSubscriptionMenu();
        _autoUpdate.Tick += async (_, _) => await RefreshDueSubscriptions();
        _autoUpdate.Start();
        Shown += async (_, _) => await RefreshDueSubscriptions();

        _hero.Tun = Data.Tun;
        _hero.PowerClicked += (_, _) => ToggleConnection();
        _hero.PingClicked += async (_, _) => await PingCurrent();
        _hero.RefreshClicked += async (_, _) => await UpdateSubscriptions();
        _hero.ModeSelected += SetMode;
        _settingsPage.ModeSelector.SelectedIndexChanged += (_, _) => SetMode(_settingsPage.ModeSelector.SelectedIndex == 1);
        _settingsPage.DarkToggle.CheckedChanged += (_, _) => SetDarkTheme(_settingsPage.DarkToggle.Checked);
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
        _settingsPage.SpeedSelector.SelectedIndexChanged += (_, _) =>
        {
            Data.SpeedInterval = _settingsPage.SpeedInterval;
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

        _stats.SetTotals(Data.TotalDownload, Data.TotalUpload);
        _selected = Data.Servers.FirstOrDefault(s => s.Link == Data.LastServerLink) ?? Data.Servers.FirstOrDefault();
        RebuildList();
        UpdateHero();
        ShowPage(openSettings ? IconKind.Settings : IconKind.Servers);

        if (bounds != null)
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds.Value;
            WindowState = state;
        }

        _settingsPage.ScaleSelector.ValueChanged += (_, _) =>
        {
            _scaleDelay.Stop();
            _scaleDelay.Start();
        };
        _scaleDelay.Tick += (_, _) =>
        {
            _scaleDelay.Stop();
            Data.UiScale = _settingsPage.ScaleSelector.Value;
            Save();
            ScaleChangeRequested?.Invoke(this, EventArgs.Empty);
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

        var add = new IconButton(IconKind.Add, "Добавить ключ или подписку") { Location = new Point(Theme.Px(12), Theme.Px(84)) };
        add.Click += (_, _) => ShowAddDialog();
        AttachTip(add);
        sidebar.Controls.Add(add);

        var separator = Theme.Bind(new Panel { Location = new Point(Theme.Px(20), Theme.Px(140)), Size = new Size(Theme.Px(28), Math.Max(1, Theme.Px(2))) }, () => Theme.Border);
        sidebar.Controls.Add(separator);

        var nav = new (IconKind Kind, string Title)[]
        {
            (IconKind.Servers, "Серверы"),
            (IconKind.Stats, "Статистика")
        };

        for (var i = 0; i < nav.Length; i++)
            AddNavButton(sidebar, new IconButton(nav[i].Kind, nav[i].Title) { Location = new Point(Theme.Px(12), Theme.Px(156 + i * 54)) });

        var settings = new IconButton(IconKind.Settings, "Настройки") { Location = new Point(Theme.Px(12), Theme.Px(600)) };
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
        var pingAll = new IconButton(IconKind.Gauge, "Проверить пинг всех серверов")
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
        serversPage.Controls.Add(searchRow);
        serversPage.Controls.Add(PageParts.Title("Серверы"));

        var statsPage = Theme.Bind(new Panel { Dock = DockStyle.Fill }, () => Theme.Surface);
        var statsGap = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface);
        statsPage.Controls.Add(_stats);
        statsPage.Controls.Add(statsGap);
        statsPage.Controls.Add(PageParts.Title("Статистика"));

        _pages[IconKind.Servers] = serversPage;
        _pages[IconKind.Stats] = statsPage;
        _pages[IconKind.Routing] = _routingPage;
        _pages[IconKind.Log] = _logPage;
        _pages[IconKind.Settings] = _settingsPage;
        _pages[IconKind.Ping] = _pingPage;

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

    private void ShowPage(IconKind kind)
    {
        foreach (var pair in _pages)
            pair.Value.Visible = pair.Key == kind;

        var active = kind is IconKind.Routing or IconKind.Log or IconKind.Ping ? IconKind.Settings : kind;
        foreach (var button in _navButtons)
            button.Active = button.Kind == active;
    }

    private void BuildCardMenu()
    {
        _cardMenu.Items.Add("Подключиться", null, (_, _) =>
        {
            if (_menuCard == null)
                return;
            Select(_menuCard.Server);
            Connect();
        });
        _cardMenu.Items.Add("Удалить", null, (_, _) =>
        {
            if (_menuCard != null)
                Delete(_menuCard.Server);
        });
        _cardMenu.Opening += (_, _) => _menuCard = _cardMenu.SourceControl as ServerCard;
    }

    private void BuildSubscriptionMenu()
    {
        _subscriptionMenu.Items.Add("Обновить", null, async (_, _) =>
        {
            if (_menuSubscription != null)
                await RefreshSubscription(_menuSubscription.Info.Url);
        });
        _subscriptionMenu.Items.Add("Удалить подписку", null, (_, _) =>
        {
            if (_menuSubscription != null)
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
            var header = new SubscriptionCard(info) { ContextMenuStrip = _subscriptionMenu };
            header.RefreshClicked += async (_, _) => await RefreshSubscription(url);
            controls.Add(header);
            controls.AddRange(Data.Servers.Where(s => s.SubscriptionUrl == url).Select(CreateCard));
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
            ? "Нажми + или Ctrl+V, чтобы добавить ключ"
            : ServerText.Plural(Data.Servers.Count, "сервер", "сервера", "серверов").ToUpperInvariant();
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
                || card.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
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
        var ping = server.PingMs switch
        {
            null => "",
            < 0 => "Сервер не ответил",
            var ms => $"Пинг {ms} мс"
        };
        _hero.SetPing(ping, Theme.PingColor(server.PingMs));
    }

    public bool PreFilterMessage(ref Message m)
    {
        const int WheelMessage = 0x020A;
        if (m.Msg != WheelMessage || (ModifierKeys & Keys.Control) == 0 || ActiveForm != this)
            return false;

        var delta = (short)((long)m.WParam >> 16);
        ChangeScale(delta > 0 ? ScaleStepper.Step : -ScaleStepper.Step);
        return true;
    }

    private void ChangeScale(int delta) =>
        _settingsPage.ScaleSelector.SetValue(_settingsPage.ScaleSelector.Value + delta);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode is Keys.Oemplus or Keys.Add)
        {
            ChangeScale(ScaleStepper.Step);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode is Keys.OemMinus or Keys.Subtract)
        {
            ChangeScale(-ScaleStepper.Step);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode is Keys.D0 or Keys.NumPad0)
        {
            _settingsPage.ScaleSelector.SetValue(90);
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
            Log("Буфер обмена пуст");
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
            Log("Не нашёл поддерживаемых ссылок (vless, vmess, trojan, ss)");
            _hero.SetPing("Ключ не распознан", Theme.PingBad);
            return;
        }

        Data.Servers.AddRange(servers);
        _selected ??= servers[0];
        Save();
        RebuildList();
        UpdateHero();
        ShowPage(IconKind.Servers);
        Log($"Добавлено серверов: {servers.Count}");
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
            _hero.SetPing("Подписок нет: добавь её через +", Theme.TextMuted);
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
            _hero.SetPing("Подписка обновлена", Theme.PingGood);
        else
            _hero.SetPing("Не удалось обновить подписку", Theme.PingBad);
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

    private async Task PingAll()
    {
        if (Data.Servers.Count == 0)
            return;

        _hero.SetBusy(true);
        await Ping(Data.Servers.ToList());
        UpdateCards();
        UpdateHero();
    }

    private async Task PingCurrent()
    {
        var server = _active ?? _selected;
        if (server == null)
            return;

        _hero.SetBusy(true);
        await Ping(new List<ProxyServer> { server });
        UpdateCards();
        UpdateHero();
    }

    private async Task Ping(List<ProxyServer> servers)
    {
        SetBusy(servers, true);
        try
        {
            await _pinger.Ping(servers, server => SetBusy(new[] { server }, false));
        }
        finally
        {
            SetBusy(servers, false);
        }
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
        _hero.SetSpeed(ServerText.Bytes(0) + "/с", ServerText.Bytes(0) + "/с");
        Data.LastServerLink = server.Link;
        Save();

        _hero.ElapsedText = "00:00:00";
        _hero.Connected = true;
        _clock.Start();
        _stats.SetConnected(true);
        _traffic.Start();
        UpdateCards();
        UpdateHero();
        Log($"Подключено к {ServerText.CleanName(server)}");
    }

    private bool Start(ProxyServer server)
    {
        switch (_connection.Start(server, Data.Tun, Data.Rules))
        {
            case ConnectResult.Ok:
                return true;
            case ConnectResult.XrayMissing:
                MessageBox.Show(this, $"Не найден {XrayRunner.XrayPath}", "Tunnelka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                break;
            case ConnectResult.SingBoxMissing:
                MessageBox.Show(this,
                    $"Не найден {XrayRunner.SingBoxPath}\n\nСкачай sing-box-windows-amd64.zip на github.com/SagerNet/sing-box/releases и положи sing-box.exe в папку core рядом с xray.exe.",
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
            "Для режима TUN нужны права администратора. Перезапустить Tunnelka от имени администратора?",
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
            Log($"Перезапуск отменён: {ex.Message}");
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
        _stats.SetConnected(false);
        UpdateCards();
        UpdateHero();

        if (wasRunning)
            Log("Отключено");
    }

    private void OnRulesChanged()
    {
        Save();
        if (_active == null || !_connection.IsRunning)
            return;

        _trafficTracker.ResetCounters();
        if (Start(_active))
            Log("Правила применены");
        else
            Disconnect();
    }

    private void OnTraffic(TrafficCounters counters)
    {
        var (down, up) = _trafficTracker.Process(counters);
        _stats.Push(counters, down, up);
        _stats.SetTotals(Data.TotalDownload, Data.TotalUpload);

        if (_trafficTracker.TryGetAverage(out var averageDown, out var averageUp))
            _hero.SetSpeed(ServerText.Bytes(averageDown) + "/с", ServerText.Bytes(averageUp) + "/с");
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

        Log("Ядро VPN завершилось. Причина обычно видна в строках выше");
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
        Log(tun ? "Режим TUN: через VPN идёт весь трафик" : "Режим прокси: через VPN идут браузер и программы");

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
