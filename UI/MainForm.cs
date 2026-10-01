using VpnClient.Core;
using VpnClient.Models;
using VpnClient.Parsing;
using VpnClient.Storage;

namespace VpnClient.UI;

public class MainForm : Form
{
    private readonly AppData _data = AppStorage.Load();
    private readonly XrayRunner _xray = new();

    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = true,
        HideSelection = false,
        Dock = DockStyle.Fill
    };

    private readonly TextBox _log = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Bottom,
        Height = 160,
        Font = new Font("Consolas", 9)
    };

    private readonly Button _connectButton = new() { Text = "Подключить", Width = 140, Height = 34 };
    private readonly CheckBox _proxyCheck = new() { Text = "Системный прокси", AutoSize = true, Margin = new Padding(12, 9, 3, 3) };
    private readonly Label _status = new() { Text = "Отключено", AutoSize = true, Margin = new Padding(12, 10, 3, 3) };

    private ProxyServer? _active;
    private bool _proxyEnabledByUs;

    public MainForm()
    {
        Text = "VpnClient";
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(640, 400);
        StartPosition = FormStartPosition.CenterScreen;

        _list.Columns.Add("Имя", 320);
        _list.Columns.Add("Адрес", 240);
        _list.Columns.Add("Протокол", 90);
        _list.Columns.Add("Пинг", 100);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6) };
        toolbar.Controls.Add(MakeButton("Вставить из буфера", PasteFromClipboard));
        toolbar.Controls.Add(MakeButton("Добавить подписку", AddSubscription));
        toolbar.Controls.Add(MakeButton("Обновить подписки", async () => await UpdateSubscriptions()));
        toolbar.Controls.Add(MakeButton("Пинг", async () => await PingAll()));
        toolbar.Controls.Add(MakeButton("Удалить", DeleteSelected));

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(6) };
        bottom.Controls.AddRange(new Control[] { _connectButton, _proxyCheck, _status });

        Controls.Add(_list);
        Controls.Add(_log);
        Controls.Add(bottom);
        Controls.Add(toolbar);

        _proxyCheck.Checked = _data.UseSystemProxy;
        _proxyCheck.CheckedChanged += (_, _) => OnProxyCheckChanged();
        _connectButton.Click += (_, _) => ToggleConnection();
        _list.DoubleClick += (_, _) => Connect();
        _xray.Output += Log;
        _xray.Exited += OnXrayExited;
        FormClosing += (_, _) => Disconnect();

        RefreshList();
        SelectLastServer();
    }

    private static Button MakeButton(string text, Action onClick)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30 };
        button.Click += (_, _) => onClick();
        return button;
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
            return;
        }

        _data.Servers.AddRange(servers);
        Save();
        RefreshList();
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
            Save();
            RefreshList();
            Log($"Из подписки получено серверов: {servers.Count}");
        }
        catch (Exception ex)
        {
            Log($"Ошибка подписки: {ex.Message}");
        }
    }

    private async Task PingAll()
    {
        Log("Проверяю пинг...");
        var tasks = _data.Servers.Select(async server =>
        {
            var ms = await Pinger.TcpPingAsync(server.Address, server.Port);
            server.PingText = ms is int value ? $"{value} мс" : "нет ответа";
        });

        await Task.WhenAll(tasks);
        RefreshList();
        Log("Пинг готов");
    }

    private void DeleteSelected()
    {
        var selected = SelectedServers();
        if (selected.Count == 0)
            return;

        foreach (var server in selected)
            _data.Servers.Remove(server);

        Save();
        RefreshList();
        Log($"Удалено серверов: {selected.Count}");
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
        var server = SelectedServers().FirstOrDefault();
        if (server == null)
        {
            MessageBox.Show(this, "Выбери сервер в списке", "VpnClient");
            return;
        }

        try
        {
            _xray.Start(XrayConfigBuilder.Build(server));
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show(this, $"Не найден {XrayRunner.XrayPath}", "VpnClient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (Exception ex)
        {
            Log($"Не удалось запустить xray: {ex.Message}");
            return;
        }

        _active = server;
        _data.LastServerLink = server.Link;
        Save();

        ApplySystemProxy();

        _connectButton.Text = "Отключить";
        _status.Text = $"Подключено: {server.Name}";
        Log($"Подключено к {server.Name}. SOCKS 127.0.0.1:{XrayConfigBuilder.SocksPort}, HTTP 127.0.0.1:{XrayConfigBuilder.HttpPort}");
        RefreshList();
    }

    private void Disconnect()
    {
        var wasRunning = _xray.IsRunning;
        _xray.Stop();

        if (_proxyEnabledByUs)
        {
            SystemProxy.Disable();
            _proxyEnabledByUs = false;
        }

        _active = null;
        _connectButton.Text = "Подключить";
        _status.Text = "Отключено";

        if (wasRunning)
            Log("Отключено");

        if (!IsDisposed)
            RefreshList();
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

        Log("Xray завершился. Причина обычно видна в строках лога выше");
        Disconnect();
    }

    private void OnProxyCheckChanged()
    {
        _data.UseSystemProxy = _proxyCheck.Checked;
        Save();

        if (_xray.IsRunning)
            ApplySystemProxy();
    }

    private void ApplySystemProxy()
    {
        try
        {
            if (_proxyCheck.Checked)
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

    private List<ProxyServer> SelectedServers() =>
        _list.SelectedItems.Cast<ListViewItem>().Select(item => (ProxyServer)item.Tag!).ToList();

    private void RefreshList()
    {
        var selected = SelectedServers();

        _list.BeginUpdate();
        _list.Items.Clear();

        foreach (var server in _data.Servers)
        {
            var name = server == _active ? $"● {server.Name}" : server.Name;
            var item = new ListViewItem(new[] { name, $"{server.Address}:{server.Port}", server.Protocol, server.PingText })
            {
                Tag = server,
                Selected = selected.Contains(server)
            };
            _list.Items.Add(item);
        }

        _list.EndUpdate();
    }

    private void SelectLastServer()
    {
        foreach (ListViewItem item in _list.Items)
        {
            if (((ProxyServer)item.Tag!).Link == _data.LastServerLink)
            {
                item.Selected = true;
                item.EnsureVisible();
                return;
            }
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
        if (IsDisposed || !IsHandleCreated && InvokeRequired)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Log(text)));
            return;
        }

        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }
}
