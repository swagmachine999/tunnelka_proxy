using System.Reflection;

namespace Tunnelka.UI;

public static class EmbeddedAssets
{
    private const string Prefix = "Assets.";

    private static readonly Assembly Source = typeof(EmbeddedAssets).Assembly;

    public static Stream? Open(string name) => Source.GetManifestResourceStream(Prefix + name);

    public static byte[]? Read(string name)
    {
        using var stream = Open(name);
        if (stream == null)
            return null;

        var buffer = new byte[stream.Length];
        var read = 0;
        while (read < buffer.Length)
        {
            var count = stream.Read(buffer, read, buffer.Length - read);
            if (count <= 0)
                break;
            read += count;
        }

        return buffer;
    }

    public static IEnumerable<string> Names(string folder, string extension) =>
        Source.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix + folder + ".", StringComparison.Ordinal) && n.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            .Select(n => n.Substring(Prefix.Length));
}
