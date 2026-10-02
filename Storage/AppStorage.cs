using System.Text.Json;
using Tunnelka.Models;

namespace Tunnelka.Storage;

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
