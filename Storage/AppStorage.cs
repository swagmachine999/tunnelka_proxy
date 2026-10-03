using System.Text.Json;
using Tunnelka.Models;

namespace Tunnelka.Storage;

public static class AppStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Folder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tunnelka");

    private static string DataPath => Path.Combine(Folder, "data.dat");
    private static string LegacyPath => Path.Combine(Folder, "data.json");

    public static AppData Load()
    {
        MoveFromAppFolder();
        if (File.Exists(LegacyPath))
            return Read(LegacyPath, File.ReadAllBytes);
        if (File.Exists(DataPath))
            return Read(DataPath, path => Dpapi.Unprotect(File.ReadAllBytes(path)));
        return new AppData();
    }

    public static void Save(AppData data)
    {
        Directory.CreateDirectory(Folder);
        var temp = DataPath + ".tmp";
        File.WriteAllBytes(temp, Dpapi.Protect(JsonSerializer.SerializeToUtf8Bytes(data, Options)));
        File.Move(temp, DataPath, true);

        if (File.Exists(LegacyPath) && IsReadable(DataPath))
            File.Delete(LegacyPath);
    }

    private static void MoveFromAppFolder()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            foreach (var name in new[] { "data.dat", "data.json" })
            {
                var old = Path.Combine(AppContext.BaseDirectory, name);
                var target = Path.Combine(Folder, name);
                if (File.Exists(old) && !File.Exists(DataPath) && !File.Exists(LegacyPath))
                    File.Copy(old, target);
                if (File.Exists(old) && File.Exists(target))
                    File.Move(old, old + ".moved", true);
            }
        }
        catch (Exception)
        {
        }
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
        MigrateSubscriptions(data);
        MigrateRouting(data);
    }

    private static void MigrateRouting(AppData data)
    {
        var legacy = data.LegacyRules ?? new List<RoutingRule>();
        var source = legacy.Where(r => r.Enabled != false).Concat(data.Routing.Rules).ToList();
        var rules = new List<RoutingRule>();

        foreach (var rule in source.Where(r => r.Action != RoutingRule.Block))
        {
            foreach (var value in RoutingValues.Split(rule.Value))
            {
                var clean = value.StartsWith(RoutingRule.ProcessPrefix, StringComparison.OrdinalIgnoreCase)
                    ? RoutingRule.ForProcess(value[RoutingRule.ProcessPrefix.Length..], rule.Action)
                    : new RoutingRule { Value = RoutingValues.Clean(value), Action = rule.Action };
                if (clean.Target.Length > 0 && !rules.Any(r => string.Equals(r.Value, clean.Value, StringComparison.OrdinalIgnoreCase)))
                    rules.Add(clean);
            }
        }

        data.Routing.Rules = rules;
        data.LegacyRules = null;
    }

    private static void MigrateSubscriptions(AppData data)
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
