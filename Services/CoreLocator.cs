using Tunnelka.Models;
using Tunnelka.Storage;

namespace Tunnelka.Services;

public static class CoreLocator
{
    public static string BundledDir => Path.Combine(AppContext.BaseDirectory, "core");

    public static string DownloadedDir => Path.Combine(AppStorage.Folder, "core");

    public static string Find(string file)
    {
        var bundled = Path.Combine(BundledDir, file);
        return File.Exists(bundled) ? bundled : Path.Combine(DownloadedDir, file);
    }

    public static IReadOnlyList<CorePackage> Missing(bool tun, IEnumerable<ProxyServer> servers)
    {
        var needsSingBox = tun || servers.Any(SingBoxRelay.Needs);
        return CoreManifest.All
            .Where(p => (p != CoreManifest.SingBox || needsSingBox) && !File.Exists(Find(p.Executable)))
            .ToList();
    }
}
