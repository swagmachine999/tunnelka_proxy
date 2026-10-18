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
        var order = new Dictionary<ProxyServer, int>();
        for (var i = 0; i < servers.Count; i++)
            order[servers[i]] = i;

        var real = _settings.Data.RealPing
            ? servers.ToList()
            : servers.Where(SingBoxRelay.Needs).ToList();
        var tcp = servers.Except(real).ToList();

        await Task.WhenAll(PingReal(real, order, done), PingTcp(tcp, order, done));
    }

    private async Task PingReal(IReadOnlyList<ProxyServer> servers, Dictionary<ProxyServer, int> order, Action<ProxyServer> done)
    {
        if (servers.Count == 0 || !File.Exists(XrayRunner.XrayPath))
        {
            await PingTcp(servers, order, done);
            return;
        }

        var reported = new HashSet<ProxyServer>();
        void Report(ProxyServer server, int ms)
        {
            lock (reported)
            {
                if (!reported.Add(server))
                    return;
            }

            server.PingMs = ms;
            done(server);
        }

        var results = await RealPinger.PingAsync(servers, _settings.Data.PingUrl, Report);
        foreach (var pair in results)
            Report(pair.Key, pair.Value);
    }

    private static Task PingTcp(IReadOnlyList<ProxyServer> servers, Dictionary<ProxyServer, int> order, Action<ProxyServer> done) =>
        Task.WhenAll(servers.Select(async server =>
        {
            await Task.Delay(PingOrder.Delay(order.TryGetValue(server, out var index) ? index : 0));
            var ms = await Pinger.TcpPingAsync(server.Address, server.Port);
            server.PingMs = ms ?? -1;
            done(server);
        }));
}
