using VpnClient.Models;
using VpnClient.Parsing;
using VpnClient.Services;
using VpnClient.Storage;
using VpnClient.UI.Controls;
using VpnClient.UI.Pages;

namespace VpnClient.UI;

public class MainForm : Form
{
    private readonly AppData _data = AppStorage.Load();
    private readonly XrayRunner _xray = new();
    private readonly TrafficMonitor _traffic = new();
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };

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
    private bool _proxyEnabledByUs;
    private TrafficCounters _lastCounters = new();
    private readonly List<(double Down, double Up)> _speedSamples = new();
    private int _speedTick;

    private readonly System.Windows.Forms.Timer _scaleDelay = new() { Interval = 700 };

    public bool RestartRequested { get; private set; }
    public bool WasConnected { get; private set; }

    public MainForm(bool reconnect = false, bool openSettings = false, Rectangle? bounds = null)
    {
        Theme.Use(_data.DarkTheme);

        Text = "Tunnelka";
        ClientSize = new Size(Theme.Px(1080), Theme.Px(720));
        MinimumSize = new Size(Theme.Px(900), Theme.Px(640));
        StartPosition = FormStartPosition.CenterScreen;
        Font = Theme.Scaled(Theme.Body);
        KeyPreview = true;
        Icon = LogoView.CreateAppIcon() ?? Icon;
        Theme.Bind(this, () => Theme.Window);

        _logPage = new LogPage(() => ShowPage(IconKind.Settings));
        _routingPage = new RoutingPage(_data.Rules, () => ShowPage(IconKind.Settings));
        _settingsPage = new SettingsPage(_data.DarkTheme, _data.UseSystemProxy, _data.SpeedInterval, _data.RealPing, _data.UiScale);
        _pingPage = new PingPage(_data.RealPing, _data.PingUrl, () => ShowPage(IconKind.Settings));

        BuildLayout();
        BuildCardMenu();
        BuildSubscriptionMenu();
        _autoUpdate.Tick += async (_, _) => await AutoUpdateSubscriptions();
        _autoUpdate.Start();
        Shown += async (_, _) => await AutoUpdateSubscriptions();

        _hero.ProxyEnabled = _data.UseSystemProxy;
        _hero.PowerClicked += (_, _) => ToggleConnection();
        _hero.PingClicked += async (_, _) => await PingCurrent();
        _hero.RefreshClicked += async (_, _) => await UpdateSubscriptions();
        _hero.ProxyToggled += (_, _) => SetSystemProxy(!_data.UseSystemProxy);
        _settingsPage.ProxyToggle.CheckedChanged += (_, _) => SetSystemProxy(_settingsPage.ProxyToggle.Checked);
        _settingsPage.DarkToggle.CheckedChanged += (_, _) => SetDarkTheme(_settingsPage.DarkToggle.Checked);
        _settingsPage.RoutingRow.Click += (_, _) => ShowPage(IconKind.Routing);
        _settingsPage.LogRow.Click += (_, _) => ShowPage(IconKind.Log);
        _settingsPage.PingRow.Click += (_, _) => ShowPage(IconKind.Ping);
        _pingPage.Changed += (_, _) =>
        {
            _data.RealPing = _pingPage.IsReal;
            var url = _pingPage.Url.Query;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                _data.PingUrl = url;
            _settingsPage.ShowPingMode(_data.RealPing);
            Save();
        };
        _settingsPage.SpeedSelector.SelectedIndexChanged += (_, _) =>
        {
            _data.SpeedInterval = _settingsPage.SpeedInterval;
            _speedTick = 0;
            Save();
        };
        _routingPage.RulesChanged += (_, _) => OnRulesChanged();
        _search.QueryChanged += (_, _) => ApplyFilter();
        _list.Resize += (_, _) => ResizeCards();
        _clock.Tick += (_, _) => UpdateClock();
        _traffic.Updated += OnTraffic;
        _xray.Output += Log;
        _xray.Exited += OnXrayExited;
        KeyDown += OnKeyDown;
        FormClosing += (_, _) => Disconnect();
        FormClosed += (_, _) =>
        {
            _autoUpdate.Dispose();
            _clock.Dispose();
            _scaleDelay.Dispose();
            _traffic.Dispose();
        };

        _stats.SetTotals(_data.TotalDownload, _data.TotalUpload);
        _selected = _data.Servers.FirstOrDefault(s => s.Link == _data.LastServerLink) ?? _data.Servers.FirstOrDefault();
        RebuildList();
        UpdateHero();
        ShowPage(openSettings ? IconKind.Settings : IconKind.Servers);

        if (bounds != null)
        {
            StartPosition = FormStartPosition.Manual;
            Location = bounds.Value.Location;
        }

        _settingsPage.ScaleSelector.ValueChanged += (_, _) =>
        {
            _scaleDelay.Stop();
            _scaleDelay.Start();
        };
        _scaleDelay.Tick += (_, _) =>
        {
            _scaleDelay.Stop();
            _data.UiScale = _settingsPage.ScaleSelector.Value;
            Save();
            WasConnected = _xray.IsRunning;
            RestartRequested = true;
            Close();
        };

        if (reconnect)
            Shown += (_, _) => Connect();
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
            Background = () => Theme.Surface
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
        _data.DarkTheme = dark;
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
        foreach (var url in _data.Subscriptions)
        {
            var info = _data.Profiles.FirstOrDefault(p => p.Url == url);
            if (info != null)
            {
                var header = new SubscriptionCard(info) { ContextMenuStrip = _subscriptionMenu };
                header.RefreshClicked += async (_, _) => await RefreshSubscription(url);
                controls.Add(header);
            }

            controls.AddRange(_data.Servers.Where(s => s.SubscriptionUrl == url).Select(CreateCard));
        }

        controls.AddRange(_data.Servers
            .Where(s => s.SubscriptionUrl == null || !_data.Subscriptions.Contains(s.SubscriptionUrl))
            .Select(CreateCard));

        _list.Controls.AddRange(controls.ToArray());
        UpdateCards();
        ApplyFilter();
        ResizeCards();
        _list.ResumeLayout();

        _countLabel.Text = _data.Servers.Count == 0
            ? "Нажми + или Ctrl+V, чтобы добавить ключ"
            : ServerText.Plural(_data.Servers.Count, "сервер", "сервера", "серверов").ToUpperInvariant();
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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
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

        if (!text.Contains("://"))
        {
            try
            {
                text = Base64Helper.Decode(text);
            }
            catch (FormatException)
            {
            }
        }

        var servers = LinkParser.ParseMany(text);
        if (servers.Count == 0)
        {
            Log("Не нашёл поддерживаемых ссылок (vless, vmess, trojan, ss)");
            _hero.SetPing("Ключ не распознан", Theme.PingBad);
            return;
        }

        _data.Servers.AddRange(servers);
        _selected ??= servers[0];
        Save();
        RebuildList();
        UpdateHero();
        ShowPage(IconKind.Servers);
        Log($"Добавлено серверов: {servers.Count}");
    }

    private async Task AddSubscriptionUrl(string url)
    {
        if (!_data.Subscriptions.Contains(url))
            _data.Subscriptions.Add(url);

        ShowPage(IconKind.Servers);
        await LoadSubscription(url);
    }

    private async Task UpdateSubscriptions()
    {
        if (_data.Subscriptions.Count == 0)
        {
            _hero.SetPing("Подписок нет: добавь её через +", Theme.TextMuted);
            return;
        }

        _hero.SetPing("Обновляю подписку...", Theme.TextMuted);
        var ok = true;
        foreach (var url in _data.Subscriptions.ToList())
            ok &= await LoadSubscription(url);

        if (ok)
            _hero.SetPing("Подписка обновлена", Theme.PingGood);
        else
            _hero.SetPing("Не удалось обновить подписку", Theme.PingBad);
    }

    private async Task<bool> LoadSubscription(string url)
    {
        try
        {
            Log("Обновляю подписку");
            var result = await SubscriptionLoader.LoadAsync(url);
            if (!_data.Subscriptions.Contains(url))
                return false;

            _data.Servers.RemoveAll(s => s.SubscriptionUrl == url && s != _active);
            _data.Servers.AddRange(result.Servers);
            _data.Profiles.RemoveAll(p => p.Url == url);
            _data.Profiles.Add(result.Info);

            if (_selected == null || !_data.Servers.Contains(_selected))
                _selected = result.Servers.FirstOrDefault() ?? _data.Servers.FirstOrDefault();

            Save();
            RebuildList();
            UpdateHero();
            Log($"{result.Info.Title}: серверов {result.Servers.Count}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Ошибка подписки: {ex.Message}");
            return false;
        }
    }

    private async Task RefreshSubscription(string url)
    {
        _hero.SetPing("Обновляю подписку...", Theme.TextMuted);
        if (await LoadSubscription(url))
            _hero.SetPing("Подписка обновлена", Theme.PingGood);
        else
            _hero.SetPing("Не удалось обновить подписку", Theme.PingBad);
    }

    private async Task AutoUpdateSubscriptions()
    {
        foreach (var url in _data.Subscriptions.ToList())
        {
            var info = _data.Profiles.FirstOrDefault(p => p.Url == url);
            if (info == null || DateTime.Now - info.UpdatedAt >= TimeSpan.FromHours(info.UpdateIntervalHours))
                await LoadSubscription(url);
        }

        foreach (var header in _list.Controls.OfType<SubscriptionCard>())
            header.Invalidate();
    }

    private void DeleteSubscription(string url)
    {
        if (_active?.SubscriptionUrl == url)
            Disconnect();

        _data.Subscriptions.Remove(url);
        _data.Profiles.RemoveAll(p => p.Url == url);
        _data.Servers.RemoveAll(s => s.SubscriptionUrl == url);
        if (_selected != null && !_data.Servers.Contains(_selected))
            _selected = _data.Servers.FirstOrDefault();

        Save();
        RebuildList();
        UpdateHero();
    }

    private async Task PingAll()
    {
        if (_data.Servers.Count == 0)
            return;

        _hero.SetPing("Проверяю пинг...", Theme.TextMuted);
        await Ping(_data.Servers.ToList());
        UpdateCards();
        UpdateHero();
    }

    private async Task PingCurrent()
    {
        var server = _active ?? _selected;
        if (server == null)
            return;

        _hero.SetPing("Проверяю пинг...", Theme.TextMuted);
        await Ping(new List<ProxyServer> { server });
        UpdateCards();
        UpdateHero();
    }

    private async Task Ping(List<ProxyServer> servers)
    {
        if (_data.RealPing && File.Exists(XrayRunner.XrayPath))
        {
            var results = await RealPinger.PingAsync(servers, _data.PingUrl);
            foreach (var pair in results)
                pair.Key.PingMs = pair.Value;
            return;
        }

        await Task.WhenAll(servers.Select(async server =>
        {
            var ms = await Pinger.TcpPingAsync(server.Address, server.Port);
            server.PingMs = ms ?? -1;
        }));
    }

    private void Delete(ProxyServer server)
    {
        if (server == _active)
            Disconnect();

        _data.Servers.Remove(server);
        if (_selected == server)
            _selected = _data.Servers.FirstOrDefault();

        Save();
        RebuildList();
        UpdateHero();
    }

    private void ToggleConnection()
    {
        if (_xray.IsRunning)
            Disconnect();
        else
            Connect();
    }

    private void Connect()
    {
        var server = _selected;
        if (server == null)
            return;

        if (!StartXray(server))
            return;

        _active = server;
        _connectedAt = DateTime.Now;
        _lastCounters = new TrafficCounters();
        _speedSamples.Clear();
        _speedTick = 0;
        _hero.SetSpeed(ServerText.Bytes(0) + "/с", ServerText.Bytes(0) + "/с");
        _data.LastServerLink = server.Link;
        Save();
        ApplySystemProxy();

        _hero.ElapsedText = "00:00:00";
        _hero.Connected = true;
        _clock.Start();
        _stats.SetConnected(true);
        _traffic.Start();
        UpdateCards();
        UpdateHero();
        Log($"Подключено к {ServerText.CleanName(server)}");
    }

    private bool StartXray(ProxyServer server)
    {
        try
        {
            _xray.Stop();
            XrayRunner.KillOrphans();
            XrayConfigBuilder.ChoosePorts();
            if (XrayConfigBuilder.SocksPort != XrayConfigBuilder.PreferredSocksPort)
                Log($"Порт {XrayConfigBuilder.PreferredSocksPort} занят другой программой (например, Happ или v2rayN), беру {XrayConfigBuilder.SocksPort}");

            _xray.Start(XrayConfigBuilder.Build(server, _data.Rules));
            _settingsPage.ShowPorts(XrayConfigBuilder.SocksPort, XrayConfigBuilder.HttpPort);
            return true;
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show(this, $"Не найден {XrayRunner.XrayPath}", "Tunnelka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            Log($"Не удалось запустить xray: {ex.Message}");
            ShowPage(IconKind.Log);
        }

        return false;
    }

    private void Disconnect()
    {
        var wasRunning = _xray.IsRunning;
        _xray.Stop();
        _clock.Stop();
        _traffic.Stop();

        if (_proxyEnabledByUs)
        {
            SystemProxy.Disable();
            _proxyEnabledByUs = false;
        }

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
        if (_active == null || !_xray.IsRunning)
            return;

        _lastCounters = new TrafficCounters();
        if (StartXray(_active))
        {
            if (_proxyEnabledByUs)
                ApplySystemProxy();
            Log("Правила применены");
        }
        else
            Disconnect();
    }

    private void OnTraffic(TrafficCounters counters)
    {
        var down = Math.Max(0, counters.ProxyDown - _lastCounters.ProxyDown) + Math.Max(0, counters.DirectDown - _lastCounters.DirectDown);
        var up = Math.Max(0, counters.ProxyUp - _lastCounters.ProxyUp) + Math.Max(0, counters.DirectUp - _lastCounters.DirectUp);

        _data.TotalDownload += Math.Max(0, counters.ProxyDown - _lastCounters.ProxyDown);
        _data.TotalUpload += Math.Max(0, counters.ProxyUp - _lastCounters.ProxyUp);
        _lastCounters = counters;

        _stats.Push(counters, down, up);
        _stats.SetTotals(_data.TotalDownload, _data.TotalUpload);
        UpdateSpeed(down, up);
    }

    private void UpdateSpeed(double down, double up)
    {
        var interval = _data.SpeedInterval;
        _speedSamples.Add((down, up));
        if (_speedSamples.Count > interval)
            _speedSamples.RemoveRange(0, _speedSamples.Count - interval);

        _speedTick++;
        if (_speedTick < interval)
            return;

        _speedTick = 0;
        _hero.SetSpeed(
            ServerText.Bytes(_speedSamples.Average(s => s.Down)) + "/с",
            ServerText.Bytes(_speedSamples.Average(s => s.Up)) + "/с");
    }

    private void UpdateClock()
    {
        var elapsed = DateTime.Now - _connectedAt;
        _hero.ElapsedText = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    private void OnXrayExited()
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnXrayExited));
            return;
        }

        Log("Xray завершился. Причина обычно видна в строках выше");
        Disconnect();
        ShowPage(IconKind.Log);
    }

    private void SetSystemProxy(bool enabled)
    {
        if (_data.UseSystemProxy == enabled)
            return;

        _data.UseSystemProxy = enabled;
        _hero.ProxyEnabled = enabled;
        _settingsPage.ProxyToggle.Checked = enabled;
        Save();

        if (_xray.IsRunning)
            ApplySystemProxy();
    }

    private void ApplySystemProxy()
    {
        try
        {
            if (_data.UseSystemProxy)
            {
                SystemProxy.Enable($"127.0.0.1:{XrayConfigBuilder.HttpPort}");
                _proxyEnabledByUs = true;
                Log("Системный прокси включён");
            }
            else if (_proxyEnabledByUs)
            {
                SystemProxy.Disable();
                _proxyEnabledByUs = false;
                Log("Системный прокси выключен");
            }
        }
        catch (Exception ex)
        {
            Log($"Не удалось изменить системный прокси: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            AppStorage.Save(_data);
        }
        catch (Exception ex)
        {
            Log($"Не удалось сохранить настройки: {ex.Message}");
        }
    }

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
