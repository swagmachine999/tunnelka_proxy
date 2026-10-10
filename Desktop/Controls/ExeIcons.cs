using Avalonia.Media.Imaging;

namespace Tunnelka.Desktop;

internal static class ExeIcons
{
    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Bitmap? Load(string path)
    {
        if (path.Length == 0)
            return null;

        lock (Cache)
        {
            if (Cache.TryGetValue(path, out var cached))
                return cached;
        }

        Bitmap? bitmap = null;
        try
        {
            if (File.Exists(path))
                bitmap = IconExtractor.Extract(path);
        }
        catch (Exception)
        {
        }

        lock (Cache)
            Cache[path] = bitmap;
        return bitmap;
    }
}
