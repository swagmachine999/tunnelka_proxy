using Tunnelka.Parsing;

namespace Tunnelka.Storage;

public static class SubscriptionUrlMigration
{
    public static void Apply(AppData data)
    {
        foreach (var profile in data.Profiles)
            profile.Url = SubscriptionUrl.Normalize(profile.Url);

        foreach (var server in data.Servers.Where(s => s.SubscriptionUrl != null))
            server.SubscriptionUrl = SubscriptionUrl.Normalize(server.SubscriptionUrl!);

        var seen = new HashSet<string>();
        data.Profiles = data.Profiles.Where(p => seen.Add(p.Url)).ToList();
    }
}
