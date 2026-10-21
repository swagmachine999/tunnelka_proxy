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

    public async Task<ConnectResult> StartAsync(ProxyServer server, bool tun, RoutingSettings routing)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() => StartCore(server, tun, routing));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ConnectResult> SwitchAsync(ProxyServer server, RoutingSettings routing)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() => SwitchCore(server, routing));
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

    private ConnectResult StartCore(ProxyServer server, bool tun, RoutingSettings routing)
    {
        var backend = tun ? ChooseTun() : null;
        if (tun && backend == null)
            return ConnectResult.NeedsAdministrator;

        StopCores();
        XrayRunner.KillOrphans(XrayRunner.XrayPath);
        XrayRunner.KillOrphans(XrayRunner.SingBoxPath);
        if (tun)
            CloseForeignCores();

        LocalAccess.Renew(tun);
        var result = StartXray(server, XrayRoutingPolicy.For(tun, routing), tun);
        if (result == ConnectResult.Ok && tun)
            result = StartTun(backend!, routing, server.Address);

        if (result != ConnectResult.Ok)
        {
            StopCores();
            return result;
        }

        ApplySystemProxy(!tun);
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

    public async Task<ConnectResult> RecoverAsync(ProxyServer server, RoutingSettings routing)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() => SwitchCore(server, routing, true));
        }
        finally
        {
            _gate.Release();
        }
    }

    private ConnectResult SwitchCore(ProxyServer server, RoutingSettings routing, bool recover = false)
    {
        if (recover ? !CanRecover : !CanSwitchLive)
            return ConnectResult.Failed;

        _xray.Stop();
        _relay.Stop();
        var result = StartXray(server, XrayRoutingPolicy.For(true, routing), true, true);
        if (result == ConnectResult.Ok && !_xray.WaitForPort(XrayConfigBuilder.SocksPort, 3000))
            result = ConnectResult.Failed;

        if (result != ConnectResult.Ok)
        {
            _xray.Stop();
            _relay.Stop();
            return result;
        }

        _log.Write(recover ? L.T("Xray перезапущен, TUN не прерывался") : L.T("Сервер сменён без перезапуска TUN"));
        return ConnectResult.Ok;
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

            if (SingBoxRelay.Needs(server) && !StartRelay(server))
                return ConnectResult.Failed;

            _xray.Start(XrayConfigBuilder.Build(server, routing, tun));
            _log.Write(L.F("Порты: SOCKS5 127.0.0.1:{0}, HTTP 127.0.0.1:{1}", XrayConfigBuilder.SocksPort, XrayConfigBuilder.HttpPort));
            return ConnectResult.Ok;
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Не удалось запустить xray: {0}", ex.Message));
            return ConnectResult.Failed;
        }
    }

    private bool StartRelay(ProxyServer server)
    {
        if (!File.Exists(XrayRunner.SingBoxPath))
        {
            _log.Write(L.F("Для {0} нужен sing-box.exe в папке core", server.Protocol));
            return false;
        }

        _relay.Start(SingBoxRelay.Build(new[] { (server, XrayConfigBuilder.RelayPort) }, true));
        if (_relay.WaitForPort(XrayConfigBuilder.RelayPort, 5000))
            return true;

        _log.Write(L.F("sing-box не запустил {0}, код {1}", server.Protocol, _relay.ExitCode ?? -1));
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
