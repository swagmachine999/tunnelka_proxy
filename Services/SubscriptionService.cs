using Tunnelka.Models;
using Tunnelka.Parsing;

namespace Tunnelka.Services;

public sealed class SubscriptionService
{
    private readonly Settings _settings;
    private readonly AppLog _log;
    private readonly Func<int?> _proxyPort;
    private readonly HashSet<string> _warned = new();
    private readonly Dictionary<string, RefreshStatus> _statuses = new();

    public event Action<string>? StatusChanged;

    public SubscriptionService(Settings settings, AppLog log, Func<int?> proxyPort)
    {
        _settings = settings;
        _log = log;
        _proxyPort = proxyPort;
    }

    public IReadOnlyList<SubscriptionInfo> Profiles => _settings.Data.Profiles;

    public bool IsKnown(string url) => Profiles.Any(p => p.Url == url);

    public bool IsCollapsed(string? url) => Profiles.Any(p => p.Url == url && p.Collapsed);

    public List<string> TakeNewlyExpiring() =>
        Profiles.Where(p => p.ExpiresSoon && _warned.Add(p.Url)).Select(p => p.Title).ToList();

    public RefreshStatus? StatusOf(string url) => _statuses.TryGetValue(url, out var status) ? status : null;

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
            SetStatus(url, RefreshState.Busy, "");
            var result = await SubscriptionLoader.LoadAsync(url, _proxyPort());
            var index = _settings.Data.Profiles.FindIndex(p => p.Url == url);
            if (index < 0)
                return false;
            if (result.Servers.Count == 0)
                throw new InvalidDataException(L.T("В ответе нет серверов"));

            var servers = _settings.Data.Servers;
            servers.RemoveAll(s => s.SubscriptionUrl == url && s != keep);
            servers.AddRange(result.Servers);
            result.Info.Collapsed = _settings.Data.Profiles[index].Collapsed;
            _settings.Data.Profiles[index] = result.Info;
            _settings.Save();

            _log.Write(L.F("{0}: серверов {1}", result.Info.Title, result.Servers.Count));
            SetStatus(url, RefreshState.Done, "");
            return true;
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Ошибка подписки: {0}", ex.Message));
            SetStatus(url, RefreshState.Failed, Explain(ex));
            return false;
        }
    }

    private void SetStatus(string url, RefreshState state, string message)
    {
        _statuses[url] = new RefreshStatus(state, message, DateTime.Now);
        StatusChanged?.Invoke(url);
    }

    private static string Explain(Exception ex) => ex switch
    {
        HttpRequestException => L.T("нет связи с сервером подписки"),
        TaskCanceledException => L.T("сервер подписки не ответил вовремя"),
        InvalidDataException => ex.Message,
        FormatException => L.T("не удалось прочитать ответ сервера"),
        _ => ex.Message
    };

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
        _statuses.Remove(url);
        _settings.Save();
    }
}
