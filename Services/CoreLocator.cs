using System.Diagnostics;
using System.Text.RegularExpressions;
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
        var downloaded = Path.Combine(DownloadedDir, file);
        var package = CoreManifest.All.FirstOrDefault(p => p.Executable == file);
        if (package != null && File.Exists(downloaded) && IsCurrent(downloaded, package))
            return File.Exists(bundled) && IsCurrent(bundled, package) ? bundled : downloaded;
        return File.Exists(bundled) ? bundled : Path.Combine(DownloadedDir, file);
    }

    public static IReadOnlyList<CorePackage> Missing(bool tun, IEnumerable<ProxyServer> servers)
    {
        var needsSingBox = tun || servers.Any(SingBoxRelay.Needs);
        return CoreManifest.All
            .Where(p => (p != CoreManifest.SingBox || needsSingBox)
                && (!File.Exists(Find(p.Executable)) || (tun && !IsCurrent(Find(p.Executable), p))))
            .ToList();
    }

    private static bool IsCurrent(string path, CorePackage package)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path, "version")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            if (process == null)
                return false;
            if (!process.WaitForExit(2000))
            {
                process.Kill();
                return false;
            }

            var version = Regex.Match(process.StandardOutput.ReadToEnd(), @"\b\d+\.\d+\.\d+\b").Value;
            return version == package.Version;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
