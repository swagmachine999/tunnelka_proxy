using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public sealed class BrandTile : Border
{
    public BrandTile(double size)
    {
        Width = size;
        Height = size;
        CornerRadius = new CornerRadius(size * 0.28);
        Child = new TextBlock
        {
            Text = "T",
            FontSize = size / 2,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        SettingsTheme.Paint(this, BackgroundProperty, "AccentGradient");
    }
}
