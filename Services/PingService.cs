using Tunnelka.Models;

namespace Tunnelka.Services;

public sealed class PingService
{
    private readonly Settings _settings;

    public PingService(Settings settings)
    {
        _settings = settings;
    }

    public async Task Ping(IReadOnlyList<ProxyServer> servers, Action<ProxyServer> done)
    {
        var real = _settings.Data.RealPing
            ? servers.ToList()
            : servers.Where(SingBoxRelay.Needs).ToList();
        var tcp = servers.Except(real).ToList();

        await Task.WhenAll(PingReal(real, done), PingTcp(tcp, done));
    }

    private async Task PingReal(IReadOnlyList<ProxyServer> servers, Action<ProxyServer> done)
    {
        if (servers.Count == 0 || !File.Exists(XrayRunner.XrayPath))
        {
            await PingTcp(servers, done);
            return;
        }

        var results = await RealPinger.PingAsync(servers, _settings.Data.PingUrl);
        foreach (var pair in results)
        {
            pair.Key.PingMs = pair.Value;
            done(pair.Key);
        }
    }

    private static Task PingTcp(IReadOnlyList<ProxyServer> servers, Action<ProxyServer> done) =>
        Task.WhenAll(servers.Select(async server =>
        {
            var ms = await Pinger.TcpPingAsync(server.Address, server.Port);
            server.PingMs = ms ?? -1;
            done(server);
        }));
}
