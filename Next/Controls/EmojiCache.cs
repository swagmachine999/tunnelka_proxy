using Avalonia.Media.Imaging;

namespace Tunnelka.Next;

public static class EmojiCache
{
    private static readonly Dictionary<string, Bitmap?> Cache = new();
    private static readonly Lazy<EmojiArchive> Archive = new(() => new EmojiArchive(new Uri("avares://Tunnelka.Next/Assets/emoji.zip")));

    public static Bitmap? Get(string symbol)
    {
        if (Cache.TryGetValue(symbol, out var cached))
            return cached;

        var image = Load(symbol);
        Cache[symbol] = image;
        return image;
    }

    private static Bitmap? Load(string symbol)
    {
        foreach (var key in EmojiKeys.For(symbol))
        {
            var image = Archive.Value.Read(key);
            if (image != null)
                return image;
        }

        return null;
    }
}
