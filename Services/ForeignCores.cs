using System.Diagnostics;

namespace Tunnelka.Services;

public static class ForeignCores
{
    private static readonly string[] Names =
    {
        "xray", "sing-box", "mihomo", "clash", "clash-meta", "v2ray", "hysteria", "hysteria2", "tun2socks"
    };

    public static List<string> Stop()
    {
        var own = new[] { XrayRunner.XrayPath, XrayRunner.SingBoxPath }
            .Select(SafeFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stopped = new List<string>();

        foreach (var name in Names)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try
                {
                    var path = process.MainModule?.FileName;
                    if (path == null || own.Contains(SafeFullPath(path)))
                        continue;

                    process.Kill(true);
                    process.WaitForExit(3000);
                    stopped.Add(path);
                }
                catch (Exception)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        return stopped;
    }

    private static string SafeFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return path;
        }
    }
}
