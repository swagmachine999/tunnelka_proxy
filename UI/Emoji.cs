using System.IO.Compression;

namespace Tunnelka.UI;

public static class Emoji
{
    private static readonly Dictionary<string, Image?> Cache = new();
    private static ZipArchive? _archive;
    private static bool _opened;

    public static Image? Get(string symbol)
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

        Image? image = null;
        foreach (var name in candidates.Distinct())
        {
            image = Load(name);
            if (image != null)
                break;
        }

        Cache[symbol] = image;
        return image;
    }

    private static Image? Load(string name)
    {
        if (name.Length == 0)
            return null;

        try
        {
            if (!_opened)
            {
                _opened = true;
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "emoji.zip");
                if (File.Exists(path))
                    _archive = ZipFile.OpenRead(path);
            }

            var entry = _archive?.GetEntry(name + ".png");
            if (entry == null)
                return null;

            using var stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            memory.Position = 0;
            using var image = Image.FromStream(memory);
            return new Bitmap(image);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
