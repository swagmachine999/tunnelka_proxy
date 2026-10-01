using System.Text.Json;
using System.Text.Json.Serialization;
using VpnClient.Models;

namespace VpnClient.Storage;

public class AppData
{
    public List<ProxyServer> Servers { get; set; } = new();
    public List<SubscriptionInfo> Profiles { get; set; } = new();
    public bool Tun { get; set; }
    public string LastServerLink { get; set; } = "";
    public bool DarkTheme { get; set; }
    public int SpeedInterval { get; set; } = 3;
    public int UiScale { get; set; } = 90;
    public bool RealPing { get; set; } = true;
    public string PingUrl { get; set; } = "https://www.gstatic.com/generate_204";
    public List<RoutingRule> Rules { get; set; } = new();
    public long TotalDownload { get; set; }
    public long TotalUpload { get; set; }

    [JsonPropertyName("Subscriptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacySubscriptions { get; set; }
}

public static class AppStorage
{
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "data.json");

    public static AppData Load()
    {
        if (!File.Exists(FilePath))
            return new AppData();

        try
        {
            var data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath)) ?? new AppData();
            Migrate(data);
            return data;
        }
        catch (JsonException)
        {
            return new AppData();
        }
    }

    public static void Save(AppData data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    private static void Migrate(AppData data)
    {
        if (data.LegacySubscriptions == null)
            return;

        var ordered = new List<SubscriptionInfo>();
        foreach (var url in data.LegacySubscriptions)
            ordered.Add(data.Profiles.FirstOrDefault(p => p.Url == url) ?? SubscriptionInfo.Placeholder(url));

        ordered.AddRange(data.Profiles.Where(p => !data.LegacySubscriptions.Contains(p.Url)));
        data.Profiles = ordered;
        data.LegacySubscriptions = null;
    }
}
