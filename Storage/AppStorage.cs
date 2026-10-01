using System.Text.Json;
using VpnClient.Models;

namespace VpnClient.Storage;

public class AppData
{
    public List<ProxyServer> Servers { get; set; } = new();
    public List<string> Subscriptions { get; set; } = new();
    public bool UseSystemProxy { get; set; } = true;
    public string LastServerLink { get; set; } = "";
    public bool DarkTheme { get; set; }
    public int SpeedInterval { get; set; } = 3;
    public List<RoutingRule> Rules { get; set; } = new();
    public long TotalDownload { get; set; }
    public long TotalUpload { get; set; }
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
            return JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath)) ?? new AppData();
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
}
