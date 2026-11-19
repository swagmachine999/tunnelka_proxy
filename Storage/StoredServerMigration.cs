using Tunnelka.Parsing;

namespace Tunnelka.Storage;

public static class StoredServerMigration
{
    public static void Apply(AppData data)
    {
        foreach (var server in data.Servers)
        {
            if (server.FinalMask.Length > 0 || !server.Link.Contains("fm=", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                server.FinalMask = LinkParser.Parse(server.Link).FinalMask;
            }
            catch (Exception)
            {
                // A malformed old link should not prevent the rest of the profile from loading.
            }
        }
    }
}
