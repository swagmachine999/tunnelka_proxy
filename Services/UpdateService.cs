using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Tunnelka.Services;

public sealed record UpdateInfo(Version Version, string DownloadUrl, string PageUrl);

public static class UpdateService
{
    public const string Repository = "swagmachine999/tunnelka_proxy";
    public const string ReleasesPage = "https://github.com/" + Repository + "/releases";

    public static Version Current
    {
        get
        {
            var version = typeof(UpdateService).Assembly.GetName().Version ?? new Version(1, 0, 0);
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build));
        }
    }

    public static async Task<UpdateInfo?> CheckAsync(int? proxyPort)
    {
        using var client = CreateClient(proxyPort, TimeSpan.FromSeconds(15));
        using var response = await client.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var version) || version <= Current)
            return null;

        var installer = root.GetProperty("assets").EnumerateArray()
            .Select(a => (Name: a.GetProperty("name").GetString() ?? "", Url: a.GetProperty("browser_download_url").GetString() ?? ""))
            .FirstOrDefault(a => a.Name.StartsWith("Tunnelka-Setup", StringComparison.OrdinalIgnoreCase) && a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

        var page = root.TryGetProperty("html_url", out var html) ? html.GetString() ?? ReleasesPage : ReleasesPage;
        return new UpdateInfo(version, installer.Url ?? "", page);
    }

    public static async Task<string> DownloadAsync(UpdateInfo update, int? proxyPort, IProgress<int> progress)
    {
        using var client = CreateClient(proxyPort, TimeSpan.FromMinutes(10));
        using var response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var path = Path.Combine(Path.GetTempPath(), $"Tunnelka-Setup-{update.Version}.exe");
        var total = response.Content.Headers.ContentLength ?? 0;
        await using (var source = await response.Content.ReadAsStreamAsync())
        await using (var target = File.Create(path))
        {
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await source.ReadAsync(buffer)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read));
                done += read;
                if (total > 0)
                    progress.Report((int)(done * 100 / total));
            }
        }

        return path;
    }

    public static void RunInstaller(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

    public static void OpenPage(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }

    private static HttpClient CreateClient(int? proxyPort, TimeSpan timeout)
    {
        var handler = new HttpClientHandler();
        if (proxyPort != null)
        {
            handler.Proxy = new WebProxy($"http://127.0.0.1:{proxyPort}");
            handler.UseProxy = true;
        }

        var client = new HttpClient(handler) { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Tunnelka/" + Current);
        return client;
    }
}
