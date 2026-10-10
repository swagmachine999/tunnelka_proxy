using System.IO.Compression;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Tunnelka.Desktop;

public sealed class EmojiArchive
{
    private readonly Dictionary<string, ZipArchiveEntry> _entries = new();

    public EmojiArchive(Uri source)
    {
        var archive = Open(source);
        if (archive == null)
            return;

        foreach (var entry in archive.Entries)
            _entries.TryAdd(EmojiKeys.Normalize(Path.GetFileNameWithoutExtension(entry.Name)), entry);
    }

    public Bitmap? Read(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return null;

        try
        {
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

    private static ZipArchive? Open(Uri source)
    {
        try
        {
            using var asset = AssetLoader.Open(source);
            var memory = new MemoryStream();
            asset.CopyTo(memory);
            memory.Position = 0;
            return new ZipArchive(memory, ZipArchiveMode.Read);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
