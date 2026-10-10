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

public interface IDialogs
{
    Task<bool> AskCoreDownload(IReadOnlyList<CorePackage> missing);

    void ShowMessage(string text);

    Task<bool> AskYesNo(string text);

    void Balloon(string text);
}

public sealed class Session : IDisposable
{
    private readonly Settings _settings;
    private readonly PingService _pinger;
    private readonly TrafficTracker _tracker;
    private readonly TrafficPoller _poller = new();
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(60) };
    private readonly HashSet<ProxyServer> _pinging = new();
    private DateTime _connectedAt;
    private bool _exiting;

    public Session()
    {
        Log = new AppLog();
        _settings = new Settings(Log);
        Data.AutoStart = Autostart.IsEnabled();
        Connection = new ConnectionService(Log);
        KillSwitch = new KillSwitch(Log);
        Subscriptions = new SubscriptionService(_settings, Log, () => Connection.IsRunning ? XrayConfigBuilder.HttpPort : null);
        Autos = new AutoServers(_settings, Subscriptions);
        History = new TrafficHistory(_settings);
        _pinger = new PingService(_settings);
        _tracker = new TrafficTracker(_settings);
        Selected = Autos.Restore(Data.LastServerLink)
            ?? Data.Servers.FirstOrDefault(s => s.Link == Data.LastServerLink)
            ?? Data.Servers.FirstOrDefault();

        _clock.Tick += (_, _) => ClockTick?.Invoke(Elapsed);
        _poller.Updated += OnTraffic;
        _refreshTimer.Tick += async (_, _) => await RefreshDue();
        Connection.Exited += () => Dispatcher.UIThread.Post(OnCoreExited);
        Connection.Warning += text => Dispatcher.UIThread.Post(() => Hint?.Invoke(text, Tone.Mid));
        Subscriptions.StatusChanged += _ => Dispatcher.UIThread.Post(() => CardsChanged?.Invoke());
        Log.Written += text => Dispatcher.UIThread.Post(() => LogWritten?.Invoke($"[{DateTime.Now:HH:mm:ss}] {text}"));
        _refreshTimer.Start();
    }

    public AppLog Log { get; }

    public ConnectionService Connection { get; }

    public KillSwitch KillSwitch { get; }

    public SubscriptionService Subscriptions { get; }

    public AutoServers Autos { get; }

    public TrafficHistory History { get; }

    public AppData Data => _settings.Data;

    public IDialogs? Dialogs { get; set; }

    public OverlayService Overlay { get; } = new();

    public ProxyServer? Selected { get; private set; }

    public ProxyServer? Active { get; private set; }

    public ProxyServer? HeroServer => Active ?? Selected;

    public bool Busy { get; private set; }

    public bool HeroBusy { get; private set; }

    public bool Connecting { get; private set; }

    public string Elapsed
    {
        get
        {
            var span = DateTime.Now - _connectedAt;
            return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }
    }

    public int? ProxyPort => Connection.IsRunning ? XrayConfigBuilder.HttpPort : null;

    public bool IsPinging(ProxyServer server) => _pinging.Contains(server);

    public event Action? StateChanged;
    public event Action? ServersChanged;
    public event Action? CardsChanged;
    public event Action<string, Tone>? Hint;
    public event Action<string?, string?>? Speed;
    public event Action<string>? ClockTick;
    public event Action<string>? LogWritten;
    public event Action<long, long, bool>? Traffic;
    public event Action<bool>? ReconnectHint;
    public event Action? ThemeChanged;
    public event Action? LanguageChanged;
    public event Action? ScaleChanged;
    public event Action? ModeChanged;
    public event Action? ShowLogRequested;
    public event Action? ExitRequested;

    public void Save() => _settings.Save();

    public void SetDark(bool dark)
    {
        Data.DarkTheme = dark;
        Save();
        ThemeChanged?.Invoke();
    }

    public void SetLanguage(string language)
    {
        Data.Language = language;
        Save();
        L.Use(language);
        LanguageChanged?.Invoke();
    }

    public void SetScale(int percent)
    {
        Data.UiScale = UiScaleMigration.Nearest(percent);
        Save();
        ScaleChanged?.Invoke();
    }

    public void StepScale(int direction)
    {
        var steps = UiScaleMigration.Steps;
        var index = Array.IndexOf(steps, UiScaleMigration.Nearest(Data.UiScale));
        SetScale(steps[Math.Max(0, Math.Min(steps.Length - 1, index + direction))]);
    }

    public void Select(ProxyServer server)
    {
        Selected = server;
        CardsChanged?.Invoke();
        StateChanged?.Invoke();
    }

    public string SelectedLink() => AutoServers.IsAuto(Selected) ? AutoServers.Link(Selected!) : Selected?.Link ?? "";

    public void Toggle()
    {
        if (Busy)
            return;

        if (Active == null && Data.Servers.Count == 0)
        {
            AddRequested?.Invoke();
            return;
        }

        if (Active != null)
            Disconnect();
        else
            _ = Connect();
    }

    public event Action? AddRequested;

    public async Task Connect()
    {
        if (Busy)
            return;

        Busy = true;
        StateChanged?.Invoke();
        try
        {
            await ConnectTo(Selected);
        }
        finally
        {
            Busy = false;
            Connecting = false;
            StateChanged?.Invoke();
        }
    }

    private async Task ConnectTo(ProxyServer? server)
    {
        if (AutoServers.IsAuto(server))
        {
            Hint?.Invoke(L.T("Ищу самый быстрый сервер…"), Tone.Muted);
            server = await FindFastest(server!);
            if (server == null)
            {
                Hint?.Invoke(L.T("Ни один сервер не ответил"), Tone.Bad);
                Failed?.Invoke();
                return;
            }
        }

        if (server == null)
            return;

        Connecting = true;
        StateChanged?.Invoke();
        if (!await Start(server))
        {
            Failed?.Invoke();
            if (Active != null)
                Disconnect();
            return;
        }

        Active = server;
        _connectedAt = DateTime.Now;
        _tracker.Reset();
        Speed?.Invoke(ServerText.Bytes(0) + L.T("/с"), ServerText.Bytes(0) + L.T("/с"));
        Data.LastServerLink = SelectedLink();
        Save();
        Connecting = false;
        _clock.Start();
        _poller.Start();
        Traffic?.Invoke(0, 0, true);
        Overlay.SetConnected(true);
        await EngageKillSwitch();
        CardsChanged?.Invoke();
        StateChanged?.Invoke();
        Log.Write(L.F("Подключено к {0}", ServerText.CleanName(server)));
    }

    public event Action? Failed;

    private async Task<bool> Start(ProxyServer server)
    {
        var result = await Connection.StartAsync(server, Data.Tun, Data.Routing);
        switch (result)
        {
            case ConnectResult.Ok:
                return true;
            case ConnectResult.XrayMissing:
            case ConnectResult.SingBoxMissing:
                var missing = CoreLocator.Missing();
                if (missing.Count > 0 && Dialogs != null && await Dialogs.AskCoreDownload(missing))
                    return await Start(server);

                Hint?.Invoke(L.F("Не найден {0}", result == ConnectResult.XrayMissing ? XrayRunner.XrayPath : XrayRunner.SingBoxPath), Tone.Bad);
                break;
            case ConnectResult.NeedsAdministrator:
                RestartAsAdministrator("--connect");
                break;
            default:
                ShowLogRequested?.Invoke();
                Hint?.Invoke(L.T("Не удалось подключиться, подробности в журнале"), Tone.Bad);
                break;
        }

        return false;
    }

    private void RestartAsAdministrator(string argument)
    {
        try
        {
            Data.LastServerLink = SelectedLink();
            Save();
            Elevation.RestartElevated(argument);
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
        ReconnectHint?.Invoke(false);
        if (wasRunning)
            Save();

        Speed?.Invoke(null, null);
        Traffic?.Invoke(0, 0, false);
        Overlay.SetConnected(false);
        CardsChanged?.Invoke();
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
        ModeChanged?.Invoke();
        StateChanged?.Invoke();
        if (Active != null && !Busy)
        {
            Disconnect();
            _ = Connect();
        }
    }

    public void OnRulesChanged()
    {
        Save();
        ModeChanged?.Invoke();
        ReconnectHint?.Invoke(Active != null && Connection.IsRunning);
    }

    public async Task Reconnect()
    {
        ReconnectHint?.Invoke(false);
        if (Busy || Active == null || !Connection.IsRunning)
            return;

        Busy = true;
        Connecting = true;
        StateChanged?.Invoke();
        try
        {
            _tracker.ResetCounters();
            if (await Start(Active))
                Log.Write(L.T("Правила применены"));
            else
                Disconnect();
        }
        finally
        {
            Busy = false;
            Connecting = false;
            StateChanged?.Invoke();
        }
    }

    public async Task StartUp(bool reconnect)
    {
        if (!KillSwitch.IsEngaged && KillSwitch.WasLeftOn())
        {
            if (KillSwitch.CanRelease)
                await ReleaseKillSwitch();
            else if (Dialogs != null && await Dialogs.AskYesNo(L.T("Kill switch остался включённым после сбоя, поэтому интернет может не работать. Перезапустить Tunnelka от имени администратора, чтобы снять блокировку?")))
            {
                RestartAsAdministrator("--elevated");
                return;
            }
        }

        var resume = Data.ResumeAfterRestart;
        if (resume)
        {
            Data.ResumeAfterRestart = false;
            Save();
        }

        if (Active == null && (reconnect || resume || Data.ConnectOnStart))
            await Connect();

        UpdateCheck?.Invoke();
        if (Data.RefreshOnStart && Subscriptions.Profiles.Count > 0)
            await UpdateSubscriptions();
        else
            await RefreshDue();

        if (Data.PingOnStart)
            await PingAll();
    }

    public event Action? UpdateCheck;

    public void AddInput(string text)
    {
        text = text.Trim();
        if (text.StartsWith("http://") || text.StartsWith("https://"))
        {
            _ = AddSubscriptionUrl(SubscriptionUrl.Normalize(text));
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
        ShowServersRequested?.Invoke();
        Log.Write(L.F("Добавлено серверов: {0}", servers.Count));
        if (first)
            Hint?.Invoke(L.T("Готово! Нажми большую кнопку, чтобы подключиться"), Tone.Good);
    }

    public event Action? ShowServersRequested;

    private async Task AddSubscriptionUrl(string url)
    {
        var first = Data.Servers.Count == 0;
        Subscriptions.Add(url);
        ShowServersRequested?.Invoke();
        await RefreshSubscription(url);
        if (first && Data.Servers.Count > 0)
            Hint?.Invoke(L.T("Готово! Нажми большую кнопку, чтобы подключиться"), Tone.Good);
    }

    public async Task UpdateSubscriptions()
    {
        if (Subscriptions.Profiles.Count == 0)
        {
            Hint?.Invoke(L.T("Подписок нет: добавь её через +"), Tone.Muted);
            return;
        }

        SetHeroBusy(true);
        var ok = await Subscriptions.RefreshAll(Active);
        AfterServersChanged();
        WarnAboutExpiring();
        ShowResult(ok);
    }

    public async Task RefreshCurrentSubscription()
    {
        var url = HeroServer?.SubscriptionUrl;
        if (url != null && Subscriptions.IsKnown(url))
            await RefreshSubscription(url);
        else
            await UpdateSubscriptions();
    }

    public async Task RefreshSubscription(string url)
    {
        if (Subscriptions.StatusOf(url)?.State == RefreshState.Busy)
            return;

        var shown = HeroServer?.SubscriptionUrl == url;
        if (shown)
            SetHeroBusy(true);
        var ok = await Subscriptions.Refresh(url, Active);
        AfterServersChanged();
        WarnAboutExpiring();
        if (shown)
            ShowResult(ok);
    }

    private void ShowResult(bool ok)
    {
        SetHeroBusy(false);
        Hint?.Invoke(ok ? L.T("Подписка обновлена") : L.T("Не удалось обновить подписку"), ok ? Tone.Good : Tone.Bad);
    }

    private void SetHeroBusy(bool busy)
    {
        HeroBusy = busy;
        StateChanged?.Invoke();
    }

    public async Task RefreshDue()
    {
        if (await Subscriptions.RefreshDue(Active))
            AfterServersChanged();

        CardsChanged?.Invoke();
        WarnAboutExpiring();
    }

    private void WarnAboutExpiring()
    {
        var titles = Subscriptions.TakeNewlyExpiring();
        if (titles.Count == 0)
            return;

        var text = titles.Count == 1
            ? L.F("Подписка «{0}» скоро закончится. Продлите её, иначе доступ будет приостановлен.", titles[0])
            : L.F("Подписки {0} скоро закончатся. Продлите их, иначе доступ будет приостановлен.", string.Join(", ", titles.Select(t => $"«{t}»")));
        Dialogs?.Balloon(text);
    }

    public void DeleteSubscription(string url)
    {
        if (Active?.SubscriptionUrl == url)
            Disconnect();

        Subscriptions.Delete(url);
        AfterServersChanged();
    }

    private void AfterServersChanged()
    {
        if (Selected == null || (AutoServers.IsAuto(Selected) ? Autos.Members(Selected).Count == 0 : !Data.Servers.Contains(Selected)))
            Selected = Data.Servers.FirstOrDefault();

        ServersChanged?.Invoke();
        StateChanged?.Invoke();
    }

    public void Delete(ProxyServer server)
    {
        if (AutoServers.IsAuto(server))
            return;

        if (server == Active)
            Disconnect();

        Data.Servers.Remove(server);
        if (Selected == server)
            Selected = Data.Servers.FirstOrDefault();

        Save();
        ServersChanged?.Invoke();
        StateChanged?.Invoke();
    }

    public Task PingAll() => PingServers(Data.Servers.ToList());

    public Task PingSubscription(string url) => PingServers(Subscriptions.Servers(url));

    public async Task PingCurrent()
    {
        var server = Selected ?? Active;
        if (server == null)
            return;

        if (AutoServers.IsAuto(server))
        {
            await FindFastest(server);
            CardsChanged?.Invoke();
            StateChanged?.Invoke();
            return;
        }

        await PingServers(new List<ProxyServer> { server });
        if (server != HeroServer)
            Hint?.Invoke($"{ServerText.CleanName(server)}: {PingText(server)}", PingTone(server.PingMs));
    }

    private async Task<ProxyServer?> FindFastest(ProxyServer auto)
    {
        var servers = Autos.Members(auto);
        await PingServers(servers);
        var best = servers.Where(s => s.PingMs >= 0).OrderBy(s => s.PingMs).FirstOrDefault();
        auto.PingMs = best?.PingMs ?? -1;
        return best;
    }

    private async Task PingServers(List<ProxyServer> servers)
    {
        if (servers.Count == 0)
            return;

        var hero = HeroServer;
        var shown = hero != null && (servers.Contains(hero) || (AutoServers.IsAuto(hero) && Autos.Members(hero).Any(servers.Contains)));
        if (shown)
            SetHeroBusy(true);
        foreach (var server in servers)
            _pinging.Add(server);
        CardsChanged?.Invoke();
        try
        {
            await _pinger.Ping(servers, server =>
                Dispatcher.UIThread.Post(() =>
                {
                    _pinging.Remove(server);
                    CardsChanged?.Invoke();
                }));
        }
        finally
        {
            foreach (var server in servers)
                _pinging.Remove(server);
        }

        CardsChanged?.Invoke();
        if (shown)
            SetHeroBusy(false);
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

    public (string Host, int Port)? LossTarget() => Active is { } s ? (s.Address, s.Port) : null;

    public void RestartSpeedAveraging() => _tracker.RestartAveraging();

    private void OnTraffic(TrafficCounters counters)
    {
        var delta = _tracker.Process(counters);
        History.Add(delta);
        var down = delta.ProxyDown + delta.DirectDown;
        var up = delta.ProxyUp + delta.DirectUp;
        Traffic?.Invoke(down, up, true);
        Overlay.SetSpeed(down, up);
        if (_tracker.TryGetAverage(out var averageDown, out var averageUp))
            Speed?.Invoke(ServerText.Bytes(averageDown) + L.T("/с"), ServerText.Bytes(averageUp) + L.T("/с"));
    }

    private void OnCoreExited()
    {
        if (_exiting || Busy || Active == null)
            return;

        Log.Write(L.T("Ядро VPN завершилось. Причина обычно видна в строках выше"));
        var blocked = KillSwitch.IsEngaged;
        Disconnect(keepKillSwitch: blocked);
        ShowLogRequested?.Invoke();
        if (!blocked)
            return;

        Hint?.Invoke(L.T("VPN упал — интернет заблокирован. Нажми кнопку, чтобы переподключиться"), Tone.Bad);
        Failed?.Invoke();
        Dialogs?.Balloon(L.T("VPN отключился, kill switch заблокировал интернет. Переподключитесь или выключите kill switch в «Расширенное»."));
    }

    public async Task EngageKillSwitch()
    {
        if (!Data.KillSwitch || !Data.Tun)
            return;

        if (!await Task.Run(KillSwitch.Engage))
            Hint?.Invoke(L.T("Kill switch не включился, подробности в журнале"), Tone.Mid);
    }

    public Task ReleaseKillSwitch() =>
        KillSwitch.IsEngaged || KillSwitch.WasLeftOn() ? Task.Run(KillSwitch.Release) : Task.CompletedTask;

    public void Shutdown()
    {
        if (Active != null)
        {
            Data.ResumeAfterRestart = false;
            Save();
        }

        _exiting = true;
        Disconnect(wait: true);
    }

    public void RequestExit()
    {
        _exiting = true;
        ExitRequested?.Invoke();
    }

    public void Dispose()
    {
        _clock.Stop();
        _refreshTimer.Stop();
        _poller.Dispose();
        Overlay.Dispose();
        Connection.Dispose();
    }
}
