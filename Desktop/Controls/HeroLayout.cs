using Avalonia;
using Avalonia.Controls;

namespace Tunnelka.Desktop.Controls;

internal sealed class HeroLayout : Panel
{
    private readonly Control _stage;
    private readonly Control _header;
    private readonly Control _footer;

    public HeroLayout(Control stage, Control header, Control footer)
    {
        _stage = stage;
        _header = header;
        _footer = footer;
        Children.Add(stage);
        Children.Add(header);
        Children.Add(footer);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width;
        var height = double.IsInfinity(availableSize.Height) ? 700 : availableSize.Height;
        _stage.Measure(new Size(width, height));
        _header.Measure(new Size(width, HeroGeometry.HeaderHeight));
        _footer.Measure(new Size(width, HeroGeometry.FooterHeight));
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var geometry = HeroGeometry.Compute(finalSize.Width, finalSize.Height);
        _stage.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        _header.Arrange(new Rect(0, 0, finalSize.Width, HeroGeometry.HeaderHeight));
        _footer.Arrange(new Rect(0, geometry.FooterTop, finalSize.Width, HeroGeometry.FooterHeight));
        return finalSize;
    }
}
