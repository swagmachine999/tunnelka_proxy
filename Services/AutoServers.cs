using Tunnelka.Models;

namespace Tunnelka.Services;

public sealed class AutoServers
{
    private const string LinkPrefix = "auto:";
    private const string LegacyLink = "auto";

    private readonly Settings _settings;
    private readonly SubscriptionService _subscriptions;
    private readonly Dictionary<string, ProxyServer> _autos = new();

    public AutoServers(Settings settings, SubscriptionService subscriptions)
    {
        _settings = settings;
        _subscriptions = subscriptions;
    }

    public static bool IsAuto(ProxyServer? server) => server?.Protocol == "auto";

    public static string Link(ProxyServer auto) => LinkPrefix + auto.SubscriptionUrl;

    public ProxyServer For(string? subscriptionUrl)
    {
        var key = subscriptionUrl ?? "";
        if (!_autos.TryGetValue(key, out var auto))
        {
            auto = new ProxyServer { Protocol = "auto", Name = "⚡ " + L.T("Авто"), SubscriptionUrl = subscriptionUrl };
            _autos[key] = auto;
        }
        return auto;
    }

    public List<ProxyServer> Members(ProxyServer auto) =>
        auto.SubscriptionUrl != null
            ? _subscriptions.Servers(auto.SubscriptionUrl)
            : _settings.Data.Servers.Where(s => s.SubscriptionUrl == null || !_subscriptions.IsKnown(s.SubscriptionUrl)).ToList();

    public ProxyServer? Restore(string link)
    {
        if (link != LegacyLink && !link.StartsWith(LinkPrefix))
            return null;

        var url = link == LegacyLink ? _subscriptions.Profiles.FirstOrDefault()?.Url : link.Substring(LinkPrefix.Length);
        var auto = For(string.IsNullOrEmpty(url) ? null : url);
        return Members(auto).Count > 0 ? auto : null;
    }
}
