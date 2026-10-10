using System.IO.Compression;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Tunnelka.Next;

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
            using var stream = AssetLoader.Open(new Uri("avares://Tunnelka.Next/Assets/Flags/" + key + ".png"));
            image = new Bitmap(stream);
        }
        catch (Exception)
        {
        }

        Cache[key] = image;
        return image;
    }
}

public static class EmojiCache
{
    private static readonly Dictionary<string, Bitmap?> Cache = new();
    private static ZipArchive? _archive;
    private static bool _opened;

    public static Bitmap? Get(string symbol)
    {
        if (Cache.TryGetValue(symbol, out var cached))
            return cached;

        var codes = new List<string>();
        for (var i = 0; i < symbol.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(symbol, i);
            if (char.IsHighSurrogate(symbol[i]))
                i++;
            codes.Add(codePoint.ToString("x"));
        }

        var candidates = new[]
        {
            string.Join("-", codes),
            string.Join("-", codes.Where(c => c != "fe0f")),
            codes.Count > 0 ? codes[0] : ""
        };

        Bitmap? image = null;
        foreach (var name in candidates.Distinct())
        {
            image = Load(name);
            if (image != null)
                break;
        }

        Cache[symbol] = image;
        return image;
    }

    private static Bitmap? Load(string name)
    {
        if (name.Length == 0)
            return null;

        try
        {
            if (!_opened)
            {
                _opened = true;
                using var source = AssetLoader.Open(new Uri("avares://Tunnelka.Next/Assets/emoji.zip"));
                var memory = new MemoryStream();
                source.CopyTo(memory);
                memory.Position = 0;
                _archive = new ZipArchive(memory, ZipArchiveMode.Read);
            }

            var entry = _archive?.GetEntry(name + ".png");
            if (entry == null)
                return null;

            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;
            return new Bitmap(buffer);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
