using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Tunnelka.Desktop;

public static class FlagCache
{
    private static readonly Dictionary<string, Bitmap?> Cache = new();

    public static Bitmap? Get(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        var key = code.ToLowerInvariant();
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        Bitmap? image = null;
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Tunnelka/Assets/Flags/" + key + ".png"));
            image = new Bitmap(stream);
        }
        catch (Exception)
        {
        }

        Cache[key] = image;
        return image;
    }
}
