using Avalonia.Threading;
using Tunnelka.Models;
using Tunnelka.Parsing;
using Tunnelka.Services;
using Tunnelka.Storage;
using Tunnelka.UI;

namespace Tunnelka.Next;

public enum Tone
{
    Muted,
    Good,
    Mid,
    Bad
}

public sealed class Session : IDisposable
{
    private readonly Settings _settings;
    private readonly PingService _pinger;
    private readonly TrafficTracker _tracker;
    private readonly TrafficHistory _history;
    private readonly TrafficPoller _poller = new();
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _connectedAt;
    private bool _exiting;

    public Session()
    {
        Log = new AppLog();
        _settings = new Settings(Log);
        Data.AutoStart = Autostart.IsEnabled();
        Connection = new ConnectionService(Log);
        KillSwitch = new KillSwitch(Log);
        _pinger = new PingService(_settings);
        _tracker = new TrafficTracker(_settings);
        _history = new TrafficHistory(_settings);
        Selected = Data.Servers.FirstOrDefault(s => s.Link == Data.LastServerLink) ?? Data.Servers.FirstOrDefault();

        _clock.Tick += (_, _) => ClockTick?.Invoke(Elapsed);
        _poller.Updated += OnTraffic;
        Connection.Exited += () => Dispatcher.UIThread.Post(OnCoreExited);
        Connection.Warning += text => Dispatcher.UIThread.Post(() => Hint?.Invoke(text, Tone.Mid));
    }

    public AppLog Log { get; }

    public ConnectionService Connection { get; }

    public KillSwitch KillSwitch { get; }

    public AppData Data => _settings.Data;

    public ProxyServer? Selected { get; private set; }

    public ProxyServer? Active { get; private set; }

    public bool Busy { get; private set; }

    public string Elapsed
    {
        get
        {
            var span = DateTime.Now - _connectedAt;
            return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }
    }

    public event Action? StateChanged;
    public event Action? ServersChanged;
    public event Action<string, Tone>? Hint;
    public event Action<string?, string?>? Speed;
    public event Action<string>? ClockTick;
    public event Action? ExitRequested;

    public void Save() => _settings.Save();

    public void Select(ProxyServer server)
    {
        Selected = server;
        StateChanged?.Invoke();
    }

    public void Toggle()
    {
        if (Busy)
            return;

        if (Active != null)
            Disconnect();
        else
            _ = Connect();
    }

    public async Task Connect()
    {
        if (Busy || Selected == null)
            return;

        var server = Selected;
        Busy = true;
        StateChanged?.Invoke();
        try
        {
            if (!await Start(server))
            {
                if (Active != null)
                    Disconnect();
                return;
            }

            Active = server;
            _connectedAt = DateTime.Now;
            _tracker.Reset();
            Speed?.Invoke(ServerText.Bytes(0) + L.T("/с"), ServerText.Bytes(0) + L.T("/с"));
            Data.LastServerLink = server.Link;
            Save();
            _clock.Start();
            _poller.Start();
            await EngageKillSwitch();
            Log.Write(L.F("Подключено к {0}", ServerText.CleanName(server)));
        }
        finally
        {
            Busy = false;
            StateChanged?.Invoke();
        }
    }

    private async Task<bool> Start(ProxyServer server)
    {
        var result = await Connection.StartAsync(server, Data.Tun, Data.Routing);
        switch (result)
        {
            case ConnectResult.Ok:
                return true;
            case ConnectResult.XrayMissing:
                Hint?.Invoke(L.F("Не найден {0}", XrayRunner.XrayPath), Tone.Bad);
                break;
            case ConnectResult.SingBoxMissing:
                Hint?.Invoke(L.F("Не найден {0}", XrayRunner.SingBoxPath), Tone.Bad);
                break;
            case ConnectResult.NeedsAdministrator:
                RestartAsAdministrator();
                break;
            default:
                Hint?.Invoke(L.T("Не удалось подключиться, подробности в журнале"), Tone.Bad);
                break;
        }

        return false;
    }

    private void RestartAsAdministrator()
    {
        try
        {
            Data.LastServerLink = Selected?.Link ?? "";
            Save();
            Elevation.RestartElevated("--connect");
            _exiting = true;
            ExitRequested?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Write(L.F("Перезапуск отменён: {0}", ex.Message));
            Hint?.Invoke(L.T("Для режима TUN нужны права администратора"), Tone.Mid);
        }
    }

    public void Disconnect(bool wait = false, bool keepKillSwitch = false)
    {
        var wasRunning = Active != null || Connection.IsRunning;
        if (wait)
            Connection.Stop();
        else
            _ = Connection.StopAsync();

        if (!keepKillSwitch)
        {
            if (wait)
                KillSwitch.Release();
            else
                _ = ReleaseKillSwitch();
        }

        _clock.Stop();
        _poller.Stop();
        Active = null;
        if (wasRunning)
            Save();

        Speed?.Invoke(null, null);
        StateChanged?.Invoke();
        if (wasRunning)
            Log.Write(L.T("Отключено"));
    }

    public void SetMode(bool tun)
    {
        if (Data.Tun == tun)
            return;

        Data.Tun = tun;
        Save();
        Log.Write(tun ? L.T("Режим TUN: через VPN идёт весь трафик") : L.T("Режим прокси: через VPN идут браузер и программы"));
        StateChanged?.Invoke();
        if (Active != null && !Busy)
        {
            Disconnect();
            _ = Connect();
        }
    }

    public async Task StartUp(bool reconnect)
    {
        if (!KillSwitch.IsEngaged && KillSwitch.WasLeftOn() && KillSwitch.CanRelease)
            await ReleaseKillSwitch();

        var resume = Data.ResumeAfterRestart;
        if (resume)
        {
            Data.ResumeAfterRestart = false;
            Save();
        }

        if (Active == null && (reconnect || resume || Data.ConnectOnStart))
            await Connect();
    }

    public void AddInput(string text)
    {
        text = text.Trim();
        if (text.StartsWith("http://") || text.StartsWith("https://"))
        {
            Hint?.Invoke(L.T("Подписки появятся на следующем этапе"), Tone.Mid);
            return;
        }

        var servers = LinkParser.ParseInput(text);
        if (servers.Count == 0)
        {
            Log.Write(L.T("Не нашёл поддерживаемых ключей (vless, vmess, trojan, ss, hysteria2, tuic)"));
            Hint?.Invoke(L.T("Ключ не распознан"), Tone.Bad);
            return;
        }

        var first = Data.Servers.Count == 0;
        Data.Servers.AddRange(servers);
        Selected ??= servers[0];
        Save();
        ServersChanged?.Invoke();
        StateChanged?.Invoke();
        Log.Write(L.F("Добавлено серверов: {0}", servers.Count));
        if (first)
            Hint?.Invoke(L.T("Готово! Нажми большую кнопку, чтобы подключиться"), Tone.Good);
    }

    public void Delete(ProxyServer server)
    {
        if (server == Active)
            Disconnect();

        Data.Servers.Remove(server);
        if (Selected == server)
            Selected = Data.Servers.FirstOrDefault();

        Save();
        ServersChanged?.Invoke();
        StateChanged?.Invoke();
    }

    public async Task PingAll()
    {
        var servers = Data.Servers.ToList();
        if (servers.Count == 0)
            return;

        await _pinger.Ping(servers, _ => Dispatcher.UIThread.Post(() => ServersChanged?.Invoke()));
        ServersChanged?.Invoke();
        StateChanged?.Invoke();
    }

    public static string PingText(ProxyServer server) => server.PingMs switch
    {
        null => "",
        < 0 => L.T("Сервер не ответил"),
        var ms => L.F("Пинг {0} мс", ms)
    };

    public static Tone PingTone(int? ms) => ms switch
    {
        null => Tone.Muted,
        < 0 => Tone.Bad,
        < 150 => Tone.Good,
        < 400 => Tone.Mid,
        _ => Tone.Bad
    };

    private void OnTraffic(TrafficCounters counters)
    {
        var delta = _tracker.Process(counters);
        _history.Add(delta);
        if (_tracker.TryGetAverage(out var down, out var up))
            Speed?.Invoke(ServerText.Bytes(down) + L.T("/с"), ServerText.Bytes(up) + L.T("/с"));
    }

    private void OnCoreExited()
    {
        if (_exiting || Busy || Active == null)
            return;

        Log.Write(L.T("Ядро VPN завершилось. Причина обычно видна в строках выше"));
        var blocked = KillSwitch.IsEngaged;
        Disconnect(keepKillSwitch: blocked);
        if (blocked)
            Hint?.Invoke(L.T("VPN упал — интернет заблокирован. Нажми кнопку, чтобы переподключиться"), Tone.Bad);
    }

    private async Task EngageKillSwitch()
    {
        if (!Data.KillSwitch || !Data.Tun)
            return;

        if (!await Task.Run(KillSwitch.Engage))
            Hint?.Invoke(L.T("Kill switch не включился, подробности в журнале"), Tone.Mid);
    }

    private Task ReleaseKillSwitch() =>
        KillSwitch.IsEngaged || KillSwitch.WasLeftOn() ? Task.Run(KillSwitch.Release) : Task.CompletedTask;

    public void Shutdown()
    {
        _exiting = true;
        if (Active != null)
        {
            Data.ResumeAfterRestart = false;
            Save();
        }

        Disconnect(wait: true);
    }

    public void Dispose()
    {
        _clock.Stop();
        _poller.Dispose();
        Connection.Dispose();
    }
}
