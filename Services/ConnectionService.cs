using VpnClient.Models;

namespace VpnClient.Services;

public enum ConnectResult
{
    Ok,
    XrayMissing,
    SingBoxMissing,
    NeedsAdministrator,
    Failed
}

public sealed class ConnectionService : IDisposable
{
    private readonly AppLog _log;
    private readonly XrayRunner _xray = new();
    private readonly XrayRunner _singBox = new(XrayRunner.SingBoxPath, "tun.json");
    private bool _proxyEnabled;

    public ConnectionService(AppLog log)
    {
        _log = log;
        _xray.Output += _log.Write;
        _singBox.Output += line => _log.Write("[tun] " + line);
        _xray.Exited += () => Exited?.Invoke();
        _singBox.Exited += () => Exited?.Invoke();
    }

    public event Action? Exited;

    public bool IsRunning => _xray.IsRunning;

    public ConnectResult Start(ProxyServer server, bool tun, IReadOnlyList<RoutingRule> rules)
    {
        if (tun && !Elevation.IsAdministrator())
            return ConnectResult.NeedsAdministrator;

        var result = StartXray(server, rules);
        if (result != ConnectResult.Ok)
            return result;

        if (tun)
        {
            result = StartTun(rules);
            if (result != ConnectResult.Ok)
            {
                _xray.Stop();
                return result;
            }
        }

        ApplySystemProxy(!tun);
        return ConnectResult.Ok;
    }

    public void Stop()
    {
        _singBox.Stop();
        _xray.Stop();
        ApplySystemProxy(false);
    }

    private ConnectResult StartXray(ProxyServer server, IReadOnlyList<RoutingRule> rules)
    {
        if (!File.Exists(XrayRunner.XrayPath))
            return ConnectResult.XrayMissing;

        try
        {
            _xray.Stop();
            XrayRunner.KillOrphans(XrayRunner.XrayPath);
            XrayConfigBuilder.ChoosePorts();
            if (XrayConfigBuilder.SocksPort != XrayConfigBuilder.PreferredSocksPort)
                _log.Write($"Порт {XrayConfigBuilder.PreferredSocksPort} занят другой программой (например, Happ или v2rayN), беру {XrayConfigBuilder.SocksPort}");

            _xray.Start(XrayConfigBuilder.Build(server, rules));
            _log.Write($"Порты: SOCKS5 127.0.0.1:{XrayConfigBuilder.SocksPort}, HTTP 127.0.0.1:{XrayConfigBuilder.HttpPort}");
            return ConnectResult.Ok;
        }
        catch (Exception ex)
        {
            _log.Write($"Не удалось запустить xray: {ex.Message}");
            return ConnectResult.Failed;
        }
    }

    private ConnectResult StartTun(IReadOnlyList<RoutingRule> rules)
    {
        if (!File.Exists(XrayRunner.SingBoxPath))
            return ConnectResult.SingBoxMissing;

        try
        {
            XrayRunner.KillOrphans(XrayRunner.SingBoxPath);
            _singBox.Start(TunConfigBuilder.Build(XrayConfigBuilder.SocksPort, rules));
            _log.Write("TUN включён");
            return ConnectResult.Ok;
        }
        catch (Exception ex)
        {
            _log.Write($"Не удалось запустить TUN: {ex.Message}");
            return ConnectResult.Failed;
        }
    }

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
            _log.Write(enable ? "Системный прокси включён" : "Системный прокси выключен");
        }
        catch (Exception ex)
        {
            _log.Write($"Не удалось изменить системный прокси: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _xray.Dispose();
        _singBox.Dispose();
    }
}
