using System.Net;
using System.Text;
using Tunnelka.Models;

namespace Tunnelka.Parsing;

public static class SubscriptionLoader
{
    private static readonly HttpClient Http = CreateClient(new HttpClientHandler());
    private static readonly Dictionary<int, HttpClient> Proxied = new();

    public static async Task<SubscriptionResult> LoadAsync(string url, int? proxyPort = null)
    {
        using var response = await Client(proxyPort).GetAsync(url);
        response.EnsureSuccessStatusCode();

        var text = (await response.Content.ReadAsStringAsync()).Trim();
        if (!text.Contains("://"))
            text = Base64Helper.Decode(text);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in new[] { "profile-title", "subscription-userinfo", "profile-update-interval", "announce", "support-url" })
        {
            var value = Header(response, name);
            if (value != null)
                values[name] = value;
        }

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("#"))
                continue;

            var colon = trimmed.IndexOf(':');
            if (colon > 1)
                values[trimmed[1..colon].Trim()] = trimmed[(colon + 1)..].Trim();
        }

        var info = new SubscriptionInfo
        {
            Url = url,
            Title = Decode(Get(values, "profile-title")) ?? new Uri(url).Host,
            Announce = (Decode(Get(values, "announce")) ?? "").Replace("\\n", "\n").Trim(),
            SupportUrl = Get(values, "support-url") ?? "",
            UpdatedAt = DateTime.Now
        };

        if (int.TryParse(Get(values, "profile-update-interval"), out var hours) && hours > 0)
            info.UpdateIntervalHours = hours;

        ParseUserInfo(Get(values, "subscription-userinfo"), info);

        var servers = LinkParser.ParseMany(text);
        foreach (var server in servers)
            server.SubscriptionUrl = url;

        return new SubscriptionResult(servers, info);
    }

    private static void ParseUserInfo(string? value, SubscriptionInfo info)
    {
        if (value == null)
            return;

        foreach (var pair in value.Split(';'))
        {
            var parts = pair.Split('=');
            if (parts.Length != 2 || !long.TryParse(parts[1].Trim(), out var number))
                continue;

            switch (parts[0].Trim().ToLowerInvariant())
            {
                case "upload":
                    info.Upload = number;
                    break;
                case "download":
                    info.Download = number;
                    break;
                case "total":
                    info.Total = number;
                    break;
                case "expire":
                    info.Expire = number > 0 ? DateTimeOffset.FromUnixTimeSeconds(number).LocalDateTime : null;
                    break;
            }
        }
    }

    private static string? Get(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : null;

    private static string? Header(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return FixEncoding(values.FirstOrDefault());

        try
        {
            if (response.Content.Headers.TryGetValues(name, out values))
                return FixEncoding(values.FirstOrDefault());
        }
        catch (InvalidOperationException)
        {
        }

        return null;
    }

    private static string? FixEncoding(string? value)
    {
        if (value == null || !value.Any(c => c >= 0x80) || value.Any(c => c > 0xFF))
            return value;

        return Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
    }

    private static string? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!value.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
            return value.Trim();

        try
        {
            return Base64Helper.Decode(value[7..]).Trim();
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static HttpClient Client(int? proxyPort)
    {
        if (proxyPort == null)
            return Http;

        if (!Proxied.TryGetValue(proxyPort.Value, out var client))
        {
            client = CreateClient(new HttpClientHandler { Proxy = new WebProxy($"http://127.0.0.1:{proxyPort}"), UseProxy = true });
            Proxied[proxyPort.Value] = client;
        }
        return client;
    }

    private static HttpClient CreateClient(HttpClientHandler handler)
    {
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("v2rayN/7.0");
        return client;
    }
}
