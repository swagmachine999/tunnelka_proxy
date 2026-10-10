using Avalonia.Media.Imaging;

namespace Tunnelka.Next;

internal sealed class AppItem
{
    public AppItem(string name, string path, Bitmap? icon)
    {
        Name = name;
        Path = path;
        Icon = icon;
    }

    public string Name { get; }
    public string Path { get; }
    public Bitmap? Icon { get; }
}
