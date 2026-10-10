using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public sealed class GlyphPath : Avalonia.Controls.Shapes.Path
{
    public GlyphPath(string data, string brushKey, double thickness = 2)
    {
        var geometry = Geometry.Parse(data);
        var bounds = geometry.Bounds;
        Data = geometry;
        Stretch = Stretch.Uniform;
        Width = bounds.Width + thickness;
        Height = bounds.Height + thickness;
        StrokeThickness = thickness;
        StrokeLineCap = PenLineCap.Round;
        StrokeJoin = PenLineJoin.Round;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        IsHitTestVisible = false;
        Tint(brushKey);
    }

    public void Tint(string brushKey) => SettingsTheme.Paint(this, StrokeProperty, brushKey);
}
