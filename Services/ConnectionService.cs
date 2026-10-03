using System.Net.NetworkInformation;
using Tunnelka.Models;

namespace Tunnelka.Services;

public sealed class ConnectionService : IDisposable
{
    private readonly AppLog _log;
    private readonly XrayRunner _xray = new();
    private readonly XrayRunner _singBox = new(XrayRunner.SingBoxPath, "tun.json");
    private readonly XrayRunner _relay = new(XrayRunner.SingBoxPath, "relay.json");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _proxyEnabled;

    public ConnectionService(AppLog log)
    {
        _log = log;
        _xray.Output += _log.Write;
        _singBox.Output += line => _log.Write("[tun] " + line);
        _relay.Output += line => _log.Write("[relay] " + line);
        _xray.Exited += () => Exited?.Invoke();
        _singBox.Exited += () => Exited?.Invoke();
        _relay.Exited += () => Exited?.Invoke();
    }

    public event Action? Exited;

    public bool IsRunning => _xray.IsRunning;

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
        if (tun && !Elevation.IsAdministrator())
            return ConnectResult.NeedsAdministrator;

        StopCores();
        XrayRunner.KillOrphans(XrayRunner.XrayPath);
        XrayRunner.KillOrphans(XrayRunner.SingBoxPath);

        var result = StartXray(server, routing);
        if (result == ConnectResult.Ok && tun)
            result = StartTun(routing, server.Address);

        if (result != ConnectResult.Ok)
        {
            StopCores();
            return result;
        }

        ApplySystemProxy(!tun);
        return ConnectResult.Ok;
    }

    private void StopCores()
    {
        _singBox.Stop();
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
            if (KillSwitch.WasLeftOn() && Elevation.IsAdministrator())
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

    private ConnectResult StartXray(ProxyServer server, RoutingSettings routing)
    {
        if (!File.Exists(XrayRunner.XrayPath))
            return ConnectResult.XrayMissing;

        try
        {
            XrayConfigBuilder.ChoosePorts();
            if (XrayConfigBuilder.SocksPort != XrayConfigBuilder.PreferredSocksPort)
                _log.Write(L.F("Порт {0} занят другой программой (например, Happ или v2rayN), беру {1}", XrayConfigBuilder.PreferredSocksPort, XrayConfigBuilder.SocksPort));

            if (SingBoxRelay.Needs(server) && !StartRelay(server))
                return ConnectResult.Failed;

            _xray.Start(XrayConfigBuilder.Build(server, routing));
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

        _relay.Start(SingBoxRelay.Build(new[] { (server, XrayConfigBuilder.RelayPort) }));
        if (_relay.WaitForPort(XrayConfigBuilder.RelayPort, 5000))
            return true;

        _log.Write(L.F("sing-box не запустил {0}, код {1}", server.Protocol, _relay.ExitCode ?? -1));
        return false;
    }

    private ConnectResult StartTun(RoutingSettings routing, string serverHost)
    {
        if (!File.Exists(XrayRunner.SingBoxPath))
            return ConnectResult.SingBoxMissing;

        try
        {
            _singBox.Start(TunConfigBuilder.Build(XrayConfigBuilder.SocksPort, routing, serverHost));
            if (_singBox.WaitForExit(800))
            {
                _log.Write(L.F("sing-box сразу завершился, код {0}", _singBox.ExitCode ?? -1));
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
            if (!_singBox.IsRunning)
                return;

            if (FindAdapter() is { } adapter)
            {
                _log.Write(L.F("Адаптер TUN создан: {0}", adapter.Description));
                return;
            }
        }

        _log.Write(L.T("Адаптер TUN не появился за 10 секунд. Его может блокировать антивирус или другой VPN"));
    }

    private static NetworkInterface? FindAdapter() =>
        NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n =>
            n.OperationalStatus == OperationalStatus.Up &&
            n.GetIPProperties().UnicastAddresses.Any(a => a.Address.ToString() == TunConfigBuilder.Address));

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
        _singBox.Dispose();
        _relay.Dispose();
    }
}
