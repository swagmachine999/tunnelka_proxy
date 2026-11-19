using System.Net.NetworkInformation;
using Tunnelka.Models;
using Tunnelka.Services.Privileged;

namespace Tunnelka.Services;

public sealed class ConnectionService : IDisposable
{
    private readonly AppLog _log;
    private readonly XrayRunner _xray = new();
    private readonly LocalTunBackend _localTun = new(new XrayRunner(XrayRunner.SingBoxPath, "tun.json"));
    private readonly ServiceTunBackend _serviceTun;
    private readonly IServiceChannel _service;
    private ITunBackend _tun;
    private readonly XrayRunner _relay = new(XrayRunner.SingBoxPath, "relay.json");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _proxyEnabled;
    private ProxyServer? _activeServer;
    private RoutingSettings? _activeRouting;

    public ConnectionService(AppLog log)
        : this(log, ServiceClient.Default)
    {
    }

    public ConnectionService(AppLog log, IServiceChannel service)
    {
        _log = log;
        _service = service;
        _serviceTun = new ServiceTunBackend(service);
        _tun = _localTun;
        _xray.Output += _log.Write;
        foreach (var backend in new ITunBackend[] { _localTun, _serviceTun })
        {
            backend.Output += line => _log.Write("[tun] " + line);
            backend.Exited += () => Exited?.Invoke();
        }
        _relay.Output += line => _log.Write("[relay] " + line);
        _xray.Exited += () => Exited?.Invoke();
        _relay.Exited += () => Exited?.Invoke();
    }

    public event Action? Exited;
    public event Action<string>? Warning;

    public bool IsRunning => _xray.IsRunning;

    public bool CanSwitchLive => _xray.IsRunning && _tun.IsRunning && LocalAccess.Enabled;

    public bool CanRecover => !_xray.IsRunning && _tun.IsRunning && LocalAccess.Enabled;

    public string DescribeExit() =>
        L.F("Xray: {0}; sing-box: {1}", DescribeCore(_xray.IsRunning, _xray.ExitCode), DescribeCore(_serviceTun.IsRunning || _localTun.IsRunning, _serviceTun.ExitCode ?? _localTun.ExitCode));

    private static string DescribeCore(bool running, int? exitCode) =>
        running ? L.T("работает") : exitCode is { } code ? L.F("завершился, код {0}", code) : L.T("не запущен");

    public async Task<ConnectResult> StartAsync(ProxyServer server, bool tun, RoutingSettings routing, string? probeUrl = null)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() => StartCore(server, tun, routing, probeUrl));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ConnectResult> SwitchAsync(ProxyServer server, RoutingSettings routing, string? probeUrl = null)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() => SwitchCore(server, routing, false, probeUrl));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await Task.Run(StopCore);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Stop()
    {
        _gate.Wait();
        try
        {
            StopCore();
        }
        finally
        {
            _gate.Release();
        }
    }

    private ConnectResult StartCore(ProxyServer server, bool tun, RoutingSettings routing, string? probeUrl)
    {
        var backend = tun ? ChooseTun() : null;
        if (tun && backend == null)
            return ConnectResult.NeedsAdministrator;

        StopCores();
        _activeServer = null;
        _activeRouting = null;
        XrayRunner.KillOrphans(XrayRunner.XrayPath);
        XrayRunner.KillOrphans(XrayRunner.SingBoxPath);
        if (tun)
            CloseForeignCores();

        LocalAccess.Renew(tun);
        var result = StartXray(server, XrayRoutingPolicy.For(tun, routing), tun);
        if (result == ConnectResult.Ok && tun)
            result = StartTun(backend!, routing, server.Address);
        if (result == ConnectResult.Ok && !string.IsNullOrWhiteSpace(probeUrl)
            && !VerifyConnection(probeUrl, tun))
            result = ConnectResult.NoTraffic;

        if (result != ConnectResult.Ok)
        {
            StopCores();
            return result;
        }

        ApplySystemProxy(!tun);
        _activeServer = server;
        _activeRouting = routing;
        return ConnectResult.Ok;
    }

    private void CloseForeignCores()
    {
        var stopped = ForeignCores.Stop();
        if (stopped.Count == 0)
            return;

        _log.Write(L.F("Закрыты ядра другого VPN: {0}", string.Join(", ", stopped)));
        Thread.Sleep(1000);
    }

    public async Task<ConnectResult> RecoverAsync(ProxyServer server, RoutingSettings routing, string? probeUrl = null)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() => SwitchCore(server, routing, true, probeUrl));
        }
        finally
        {
            _gate.Release();
        }
    }

    private ConnectResult SwitchCore(ProxyServer server, RoutingSettings routing, bool recover = false, string? probeUrl = null)
    {
        if (recover ? !CanRecover : !CanSwitchLive)
            return ConnectResult.Failed;

        _xray.Stop();
        _relay.Stop();
        var result = StartXray(server, XrayRoutingPolicy.For(true, routing), true, true);
        if (result == ConnectResult.Ok && !_xray.WaitForPort(XrayConfigBuilder.SocksPort, 3000))
            result = ConnectResult.Failed;
        if (result == ConnectResult.Ok && !string.IsNullOrWhiteSpace(probeUrl)
            && !VerifyConnection(probeUrl, true))
            result = ConnectResult.NoTraffic;

        if (result != ConnectResult.Ok)
        {
            _xray.Stop();
            _relay.Stop();
            if (!recover && RestorePrevious(routing, probeUrl))
                return ConnectResult.Reverted;
            _activeServer = null;
            _activeRouting = null;
            return result;
        }

        _activeServer = server;
        _activeRouting = routing;
        _log.Write(recover ? L.T("Xray перезапущен, TUN не прерывался") : L.T("Сервер сменён без перезапуска TUN"));
        return ConnectResult.Ok;
    }

    private bool RestorePrevious(RoutingSettings routing, string? probeUrl)
    {
        if (_activeServer == null)
            return false;

        var old = _activeServer;
        var oldRouting = _activeRouting ?? routing;
        var result = StartXray(old, XrayRoutingPolicy.For(true, oldRouting), true, true);
        if (result == ConnectResult.Ok && _xray.WaitForPort(XrayConfigBuilder.SocksPort, 3000)
            && (string.IsNullOrWhiteSpace(probeUrl) || VerifyConnection(probeUrl, true)))
        {
            _log.Write(L.T("Новая локация не ответила, прежнее подключение восстановлено"));
            return true;
        }

        _xray.Stop();
        _relay.Stop();
        return false;
    }

    private ITunBackend? ChooseTun()
    {
        if (_service.IsAvailable && File.Exists(ServiceConstants.SingBoxPath))
            return _serviceTun;

        _log.Write(L.F("Служба Tunnelka не используется: {0}", WhyServiceUnused()));

        return Elevation.IsAdministrator() ? _localTun : null;
    }

    private string WhyServiceUnused()
    {
        if (_service.IsAvailable)
            return L.F("нет {0}", ServiceConstants.SingBoxPath);

        return _service is ServiceClient client ? client.WhyUnavailable() : L.T("служба недоступна");
    }

    private void StopCores()
    {
        _localTun.Stop();
        if (_serviceTun.IsRunning)
            _serviceTun.Stop();
        _xray.Stop();
        _relay.Stop();
    }

    public static void CleanUpAfterCrash()
    {
        try
        {
            SystemProxy.RestoreIfLeftOver();
            XrayRunner.KillOrphans(XrayRunner.XrayPath);
            XrayRunner.KillOrphans(XrayRunner.SingBoxPath);
            XrayRunner.DeleteConfigs();
            if (KillSwitch.WasLeftOn() && KillSwitch.CanRelease)
                new KillSwitch(new AppLog()).Release();
        }
        catch (Exception)
        {
        }
    }

    private void StopCore()
    {
        StopCores();
        _activeServer = null;
        _activeRouting = null;
        ApplySystemProxy(false);
    }

    private ConnectResult StartXray(ProxyServer server, RoutingSettings routing, bool tun, bool keepPorts = false)
    {
        if (!File.Exists(XrayRunner.XrayPath))
            return ConnectResult.XrayMissing;

        try
        {
            if (!keepPorts)
                XrayConfigBuilder.ChoosePorts();
            if (SingBoxRelay.Needs(server) && !File.Exists(XrayRunner.SingBoxPath))
                return ConnectResult.SingBoxMissing;

            if (SingBoxRelay.Needs(server) && !StartRelay(server, tun))
                return ConnectResult.Failed;

            var physicalInterface = tun ? NetworkAdapters.Physical() : null;
            _xray.Start(XrayConfigBuilder.Build(server, routing, tun, physicalInterface));
            _log.Write(L.F("Порты: SOCKS5 127.0.0.1:{0}, HTTP 127.0.0.1:{1}", XrayConfigBuilder.SocksPort, XrayConfigBuilder.HttpPort));
            return ConnectResult.Ok;
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Не удалось запустить xray: {0}", ex.Message));
            return ConnectResult.Failed;
        }
    }

    private bool StartRelay(ProxyServer server, bool tun)
    {
        if (!File.Exists(XrayRunner.SingBoxPath))
        {
            _log.Write(L.F("Для {0} нужен {1} в папке core", server.Protocol, CoreFiles.Tun));
            return false;
        }

        var physicalInterface = tun ? NetworkAdapters.Physical() : null;
        if (tun && physicalInterface == null)
            _log.Write(L.T("Не найден основной сетевой адаптер для Hysteria2/TUIC; проверяем маршрут автоматически"));
        _relay.Start(SingBoxRelay.Build(new[] { (server, XrayConfigBuilder.RelayPort) }, true, physicalInterface));
        if (_relay.WaitForPort(XrayConfigBuilder.RelayPort, 5000))
            return true;

        _log.Write(L.F("sing-box не запустил {0}, код {1}", server.Protocol, _relay.ExitCode ?? -1));
        return false;
    }

    private bool VerifyConnection(string url, bool tun)
    {
        if (tun && !NetworkAdapters.OwnTunnelActive())
        {
            for (var i = 0; i < 20 && !NetworkAdapters.OwnTunnelActive() && _tun.IsRunning; i++)
                Thread.Sleep(500);
            if (!NetworkAdapters.OwnTunnelActive())
            {
                _log.Write(L.T("Адаптер TUN не появился; доступ через сервер не проверен"));
                return false;
            }
        }

        var probe = ServerWatchdog.TunnelProbe(() => XrayConfigBuilder.HttpPort, () => url);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (probe("", 0, CancellationToken.None).GetAwaiter().GetResult())
                return true;
            Thread.Sleep(300);
        }

        _log.Write(L.T("Сервер запущен, но запросы через него не проходят"));
        return false;
    }

    private ConnectResult StartTun(ITunBackend backend, RoutingSettings routing, string serverHost)
    {
        if (backend == _localTun && !File.Exists(XrayRunner.SingBoxPath))
            return ConnectResult.SingBoxMissing;

        try
        {
            var other = NetworkAdapters.OtherTunnel();
            if (other != null)
                _log.Write(L.F("Работает другой VPN в режиме TUN ({0}). Он может мешать адаптеру Tunnelka", other));

            var physical = other != null ? NetworkAdapters.Physical() : null;
            if (physical != null)
                _log.Write(L.F("Трафик ядра идёт напрямую через адаптер {0}, мимо другого VPN", physical));
            _tun = backend;
            backend.Start(new TunParameters(XrayConfigBuilder.SocksPort, serverHost, physical, routing, LocalAccess.User, LocalAccess.Password));
            if (backend.WaitForExit(800))
            {
                _log.Write(L.F("sing-box сразу завершился, код {0}", backend.ExitCode ?? -1));
                return ConnectResult.Failed;
            }

            _log.Write(L.T("TUN включён"));
            _ = Task.Run(CheckAdapter);
            return ConnectResult.Ok;
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Не удалось запустить TUN: {0}", ex.Message));
            return ConnectResult.Failed;
        }
    }

    private async Task CheckAdapter()
    {
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(500);
            if (!_tun.IsRunning)
                return;

            if (FindAdapter() is { } adapter)
            {
                _log.Write(L.F("Адаптер TUN создан: {0}", adapter.Description));
                return;
            }
        }

        var blocker = NetworkAdapters.OtherTunnel();
        var text = blocker != null
            ? L.F("Адаптер TUN не создан: мешает другой VPN ({0}). Закройте его и переподключитесь", blocker)
            : L.T("Адаптер TUN не появился за 10 секунд. Его может блокировать антивирус или другой VPN");
        _log.Write(text);
        Warning?.Invoke(text);
    }

    private static NetworkInterface? FindAdapter() =>
        NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n =>
            n.OperationalStatus == OperationalStatus.Up && NetworkAdapters.HasAddress(n, TunConfigBuilder.Address));

    private void ApplySystemProxy(bool enable)
    {
        if (enable == _proxyEnabled)
            return;

        try
        {
            if (enable)
                SystemProxy.Enable($"127.0.0.1:{XrayConfigBuilder.HttpPort}");
            else
                SystemProxy.Disable();

            _proxyEnabled = enable;
            _log.Write(enable ? L.T("Системный прокси включён") : L.T("Системный прокси выключен"));
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Не удалось изменить системный прокси: {0}", ex.Message));
        }
    }

    public void Dispose()
    {
        Stop();
        _xray.Dispose();
        _localTun.Dispose();
        _serviceTun.Dispose();
        _relay.Dispose();
    }
}
