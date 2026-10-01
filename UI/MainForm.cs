using VpnClient.Models;
using VpnClient.Parsing;
using VpnClient.Services;
using VpnClient.Storage;
using VpnClient.UI.Controls;

namespace VpnClient.UI;

public class MainForm : Form
{
    private readonly AppData _data = AppStorage.Load();
    private readonly XrayRunner _xray = new();
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };
    private readonly ToolTip _tips = new();

    private readonly HeroView _hero = new() { Dock = DockStyle.Fill };
    private readonly SearchBox _search = new() { Dock = DockStyle.Top };
    private readonly Label _countLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 34,
        ForeColor = Theme.TextMuted,
        Font = Theme.CaptionBold,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly FlowLayoutPanel _list = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        BackColor = Theme.Surface
    };

    private readonly Panel _logPanel = new()
    {
        Dock = DockStyle.Bottom,
        Height = 180,
        BackColor = Theme.LogBack,
        Padding = new Padding(18, 12, 12, 12),
        Visible = false
    };

    private readonly TextBox _log = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        ScrollBars = ScrollBars.Vertical,
        BackColor = Theme.LogBack,
        ForeColor = Theme.Text,
        Font = Theme.Log
    };

    private readonly ContextMenuStrip _cardMenu = new();
    private readonly List<ServerCard> _cards = new();
    private IconButton _logButton = null!;

    private ProxyServer? _selected;
    private ProxyServer? _active;
    private ServerCard? _menuCard;
    private DateTime _connectedAt;
    private bool _proxyEnabledByUs;

    public MainForm()
    {
        Text = "Tunnelka";
        ClientSize = new Size(1080, 720);
        MinimumSize = new Size(880, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Window;
        Font = Theme.Body;
        KeyPreview = true;
        Icon = LogoView.CreateAppIcon() ?? Icon;

        BuildLayout();
        BuildCardMenu();

        _hero.ProxyEnabled = _data.UseSystemProxy;
        _hero.PowerClicked += (_, _) => ToggleConnection();
        _hero.PingClicked += async (_, _) => await PingCurrent();
        _hero.ProxyToggled += (_, _) => ToggleSystemProxy();
        _search.QueryChanged += (_, _) => ApplyFilter();
        _list.Resize += (_, _) => ResizeCards();
        _clock.Tick += (_, _) => UpdateClock();
        _xray.Output += Log;
        _xray.Exited += OnXrayExited;
        KeyDown += OnKeyDown;
        FormClosing += (_, _) => Disconnect();

        _selected = _data.Servers.FirstOrDefault(s => s.Link == _data.LastServerLink) ?? _data.Servers.FirstOrDefault();
        RebuildList();
        UpdateHero();
    }

    private void BuildLayout()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 68, BackColor = Theme.Sidebar };
        sidebar.Controls.Add(new LogoView { Location = new Point(12, 18) });

        var buttons = new (IconKind Kind, string Tip, Action OnClick)[]
        {
            (IconKind.Add, "Вставить из буфера (Ctrl+V)", PasteFromClipboard),
            (IconKind.Link, "Добавить подписку", AddSubscription),
            (IconKind.Refresh, "Обновить подписки", async () => await UpdateSubscriptions()),
            (IconKind.Gauge, "Пинг всех серверов", async () => await PingAll()),
            (IconKind.Log, "Журнал", ToggleLog)
        };

        for (var i = 0; i < buttons.Length; i++)
        {
            var (kind, tip, onClick) = buttons[i];
            var button = new IconButton(kind) { Location = new Point(12, 92 + i * 54) };
            button.Click += (_, _) => onClick();
            _tips.SetToolTip(button, tip);
            sidebar.Controls.Add(button);
            if (kind == IconKind.Log)
                _logButton = button;
        }

        var listPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 410,
            BackColor = Theme.Surface,
            Padding = new Padding(22, 20, 14, 10)
        };

        var title = new Label
        {
            Text = "Серверы",
            Dock = DockStyle.Top,
            Height = 52,
            Font = Theme.Title,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var gap = new Panel { Dock = DockStyle.Top, Height = 8 };

        listPanel.Controls.Add(_list);
        listPanel.Controls.Add(_countLabel);
        listPanel.Controls.Add(gap);
        listPanel.Controls.Add(_search);
        listPanel.Controls.Add(title);

        var divider = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Border };

        var right = new Panel { Dock = DockStyle.Fill };
        _logPanel.Controls.Add(_log);
        right.Controls.Add(_hero);
        right.Controls.Add(_logPanel);

        Controls.Add(right);
        Controls.Add(divider);
        Controls.Add(listPanel);
        Controls.Add(sidebar);
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

    private void RebuildList()
    {
        _list.SuspendLayout();
        foreach (var card in _cards)
            card.Dispose();
        _list.Controls.Clear();
        _cards.Clear();

        foreach (var server in _data.Servers)
        {
            var card = new ServerCard(server) { ContextMenuStrip = _cardMenu };
            card.Click += (_, _) => Select(server);
            card.DoubleClick += (_, _) =>
            {
                Select(server);
                Connect();
            };
            _cards.Add(card);
        }

        _list.Controls.AddRange(_cards.ToArray());
        UpdateCards();
        ApplyFilter();
        ResizeCards();
        _list.ResumeLayout();

        _countLabel.Text = _data.Servers.Count == 0
            ? "Нажми + или Ctrl+V, чтобы добавить ключ"
            : ServerText.Plural(_data.Servers.Count, "сервер", "сервера", "серверов").ToUpperInvariant();
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
        var width = _list.Width - SystemInformation.VerticalScrollBarWidth - 6;
        if (width <= 0)
            return;

        foreach (var card in _cards)
            card.Width = width;
    }

    private void ApplyFilter()
    {
        var query = _search.Query;
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
            _hero.SetServer("", null);
            _hero.SetPing("", Theme.TextMuted);
            return;
        }

        _hero.SetServer(ServerText.CleanName(server), ServerText.CountryCode(server.Name));
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

        if (e.KeyCode == Keys.Delete && _selected != null)
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

    private void PasteFromClipboard()
    {
        var text = Clipboard.GetText().Trim();
        if (text.Length == 0)
        {
            Log("Буфер обмена пуст");
            return;
        }

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
            Log("В буфере нет поддерживаемых ссылок (vless, vmess, trojan, ss)");
            ShowLog(true);
            return;
        }

        _data.Servers.AddRange(servers);
        _selected ??= servers[0];
        Save();
        RebuildList();
        UpdateHero();
        Log($"Добавлено серверов: {servers.Count}");
    }

    private void AddSubscription()
    {
        var url = InputDialog.Show(this, "Подписка", "Ссылка на подписку:");
        if (url != null)
            _ = AddSubscriptionUrl(url);
    }

    private async Task AddSubscriptionUrl(string url)
    {
        if (!_data.Subscriptions.Contains(url))
            _data.Subscriptions.Add(url);

        await LoadSubscription(url);
    }

    private async Task UpdateSubscriptions()
    {
        if (_data.Subscriptions.Count == 0)
        {
            Log("Подписок нет");
            return;
        }

        foreach (var url in _data.Subscriptions.ToList())
            await LoadSubscription(url);
    }

    private async Task LoadSubscription(string url)
    {
        try
        {
            Log($"Загружаю подписку: {url}");
            var servers = await SubscriptionLoader.LoadAsync(url);
            _data.Servers.RemoveAll(s => s.SubscriptionUrl == url && s != _active);
            _data.Servers.AddRange(servers);
            if (_selected == null || !_data.Servers.Contains(_selected))
                _selected = servers.FirstOrDefault() ?? _data.Servers.FirstOrDefault();
            Save();
            RebuildList();
            UpdateHero();
            Log($"Из подписки получено серверов: {servers.Count}");
        }
        catch (Exception ex)
        {
            Log($"Ошибка подписки: {ex.Message}");
            ShowLog(true);
        }
    }

    private async Task PingAll()
    {
        Log("Проверяю пинг...");
        await Task.WhenAll(_data.Servers.Select(PingServer));
        UpdateCards();
        UpdateHero();
        Log("Пинг готов");
    }

    private async Task PingCurrent()
    {
        var server = _active ?? _selected;
        if (server == null)
            return;

        _hero.SetPing("Проверяю...", Theme.TextMuted);
        await PingServer(server);
        UpdateCards();
        UpdateHero();
    }

    private static async Task PingServer(ProxyServer server)
    {
        var ms = await Pinger.TcpPingAsync(server.Address, server.Port);
        server.PingMs = ms ?? -1;
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

        try
        {
            _xray.Start(XrayConfigBuilder.Build(server));
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show(this, $"Не найден {XrayRunner.XrayPath}", "Tunnelka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (Exception ex)
        {
            Log($"Не удалось запустить xray: {ex.Message}");
            ShowLog(true);
            return;
        }

        _active = server;
        _connectedAt = DateTime.Now;
        _data.LastServerLink = server.Link;
        Save();
        ApplySystemProxy();

        _hero.ElapsedText = "00:00:00";
        _hero.Connected = true;
        _clock.Start();
        UpdateCards();
        UpdateHero();
        Log($"Подключено к {ServerText.CleanName(server)}. SOCKS 127.0.0.1:{XrayConfigBuilder.SocksPort}, HTTP 127.0.0.1:{XrayConfigBuilder.HttpPort}");
    }

    private void Disconnect()
    {
        var wasRunning = _xray.IsRunning;
        _xray.Stop();
        _clock.Stop();

        if (_proxyEnabledByUs)
        {
            SystemProxy.Disable();
            _proxyEnabledByUs = false;
        }

        _active = null;
        if (IsDisposed)
            return;

        _hero.Connected = false;
        UpdateCards();
        UpdateHero();

        if (wasRunning)
            Log("Отключено");
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
        ShowLog(true);
    }

    private void ToggleSystemProxy()
    {
        _data.UseSystemProxy = !_data.UseSystemProxy;
        _hero.ProxyEnabled = _data.UseSystemProxy;
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

    private void ToggleLog() => ShowLog(!_logPanel.Visible);

    private void ShowLog(bool visible)
    {
        _logPanel.Visible = visible;
        _logButton.Active = visible;
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

        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }
}
