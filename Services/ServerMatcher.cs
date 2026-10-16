using Tunnelka.Models;

namespace Tunnelka.Services;

public static class ServerMatcher
{
    public static ProxyServer? Successor(ProxyServer? old, IEnumerable<ProxyServer> servers)
    {
        if (old == null || old.SubscriptionUrl == null)
            return null;

        var pool = servers.Where(s => s.SubscriptionUrl == old.SubscriptionUrl).ToList();
        return pool.FirstOrDefault(s => s.IsSameAs(old))
            ?? pool.FirstOrDefault(s => s.Name == old.Name)
            ?? pool.FirstOrDefault();
    }
}
