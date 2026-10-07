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

    public static IReadOnlyList<CorePackage> Missing() =>
        CoreManifest.All.Where(p => !File.Exists(Find(p.Executable))).ToList();
}
