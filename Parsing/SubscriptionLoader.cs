using VpnClient.Models;

namespace VpnClient.Parsing;

public static class SubscriptionLoader
{
    private static readonly HttpClient Http = CreateClient();

    public static async Task<List<ProxyServer>> LoadAsync(string url)
    {
        var text = (await Http.GetStringAsync(url)).Trim();
        if (!text.Contains("://"))
            text = Base64Helper.Decode(text);

        var servers = LinkParser.ParseMany(text);
        foreach (var server in servers)
            server.SubscriptionUrl = url;

        return servers;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("v2rayN/7.0");
        return client;
    }
}
