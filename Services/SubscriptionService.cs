using VpnClient.Models;
using VpnClient.Parsing;

namespace VpnClient.Services;

public sealed class SubscriptionService
{
    private readonly Settings _settings;
    private readonly AppLog _log;

    public SubscriptionService(Settings settings, AppLog log)
    {
        _settings = settings;
        _log = log;
    }

    public IReadOnlyList<SubscriptionInfo> Profiles => _settings.Data.Profiles;

    public bool IsKnown(string url) => Profiles.Any(p => p.Url == url);

    public bool IsCollapsed(string? url) => Profiles.Any(p => p.Url == url && p.Collapsed);

    public List<ProxyServer> Servers(string url) =>
        _settings.Data.Servers.Where(s => s.SubscriptionUrl == url).ToList();

    public void ToggleCollapsed(SubscriptionInfo info)
    {
        info.Collapsed = !info.Collapsed;
        _settings.Save();
    }

    public void Add(string url)
    {
        if (IsKnown(url))
            return;

        _settings.Data.Profiles.Add(SubscriptionInfo.Placeholder(url));
        _settings.Save();
    }

    public async Task<bool> Refresh(string url, ProxyServer? keep)
    {
        try
        {
            _log.Write(L.T("Обновляю подписку"));
            var result = await SubscriptionLoader.LoadAsync(url);
            var index = _settings.Data.Profiles.FindIndex(p => p.Url == url);
            if (index < 0)
                return false;

            var servers = _settings.Data.Servers;
            servers.RemoveAll(s => s.SubscriptionUrl == url && s != keep);
            servers.AddRange(result.Servers);
            result.Info.Collapsed = _settings.Data.Profiles[index].Collapsed;
            _settings.Data.Profiles[index] = result.Info;
            _settings.Save();

            _log.Write(L.F("{0}: серверов {1}", result.Info.Title, result.Servers.Count));
            return true;
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Ошибка подписки: {0}", ex.Message));
            return false;
        }
    }

    public async Task<bool> RefreshAll(ProxyServer? keep)
    {
        var ok = true;
        foreach (var url in Profiles.Select(p => p.Url).ToList())
            ok &= await Refresh(url, keep);
        return ok;
    }

    public async Task<bool> RefreshDue(ProxyServer? keep)
    {
        var changed = false;
        foreach (var info in Profiles.ToList())
        {
            if (DateTime.Now - info.UpdatedAt >= TimeSpan.FromHours(info.UpdateIntervalHours))
                changed |= await Refresh(info.Url, keep);
        }
        return changed;
    }

    public void Delete(string url)
    {
        _settings.Data.Profiles.RemoveAll(p => p.Url == url);
        _settings.Data.Servers.RemoveAll(s => s.SubscriptionUrl == url);
        _settings.Save();
    }
}
