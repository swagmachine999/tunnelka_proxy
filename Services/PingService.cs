using Tunnelka.Models;

namespace Tunnelka.Services;

public sealed class PingService
{
    public async Task Ping(IReadOnlyList<ProxyServer> servers, Action<ProxyServer> done)
    {
        await PingReal(servers, done);
    }

    private async Task PingReal(IReadOnlyList<ProxyServer> servers, Action<ProxyServer> done)
    {
        if (servers.Count == 0)
            return;

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

        var results = await RealPinger.PingAsync(servers, PingTarget.Url, Report);
        foreach (var pair in results)
            Report(pair.Key, pair.Value);
    }
}
