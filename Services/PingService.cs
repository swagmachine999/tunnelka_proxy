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
        if (_settings.Data.RealPing && File.Exists(XrayRunner.XrayPath))
        {
            var results = await RealPinger.PingAsync(servers, _settings.Data.PingUrl);
            foreach (var pair in results)
            {
                pair.Key.PingMs = pair.Value;
                done(pair.Key);
            }
            return;
        }

        await Task.WhenAll(servers.Select(async server =>
        {
            var ms = await Pinger.TcpPingAsync(server.Address, server.Port);
            server.PingMs = ms ?? -1;
            done(server);
        }));
    }
}
