using System.Text.Json;
using Tunnelka.Models;

namespace Tunnelka.Storage;

public static class AppStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private static string DataPath => Path.Combine(AppContext.BaseDirectory, "data.dat");
    private static string LegacyPath => Path.Combine(AppContext.BaseDirectory, "data.json");

    public static AppData Load()
    {
        if (File.Exists(LegacyPath))
            return Read(LegacyPath, File.ReadAllBytes);
        if (File.Exists(DataPath))
            return Read(DataPath, path => Dpapi.Unprotect(File.ReadAllBytes(path)));
        return new AppData();
    }

    public static void Save(AppData data)
    {
        var temp = DataPath + ".tmp";
        File.WriteAllBytes(temp, Dpapi.Protect(JsonSerializer.SerializeToUtf8Bytes(data, Options)));
        File.Move(temp, DataPath, true);

        if (File.Exists(LegacyPath) && IsReadable(DataPath))
            File.Delete(LegacyPath);
    }

    private static AppData Read(string path, Func<string, byte[]> read)
    {
        try
        {
            var data = JsonSerializer.Deserialize<AppData>(read(path)) ?? new AppData();
            Migrate(data);
            return data;
        }
        catch (Exception)
        {
            SetAside(path);
            return new AppData();
        }
    }

    private static void SetAside(string path)
    {
        try
        {
            File.Move(path, path + ".broken", true);
        }
        catch (Exception)
        {
        }
    }

    private static bool IsReadable(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<AppData>(Dpapi.Unprotect(File.ReadAllBytes(path))) != null;
        }
        catch (Exception)
        {
            return false;
        }
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
