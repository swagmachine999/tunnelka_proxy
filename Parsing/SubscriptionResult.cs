using Tunnelka.Models;

namespace Tunnelka.Parsing;

public sealed class SubscriptionResult
{
    public SubscriptionResult(List<ProxyServer> servers, SubscriptionInfo info)
    {
        Servers = servers;
        Info = info;
    }

    public List<ProxyServer> Servers { get; }
    public SubscriptionInfo Info { get; }
}
