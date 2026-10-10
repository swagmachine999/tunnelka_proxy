using Avalonia;
using Avalonia.Controls;

namespace Tunnelka.Next.Controls;

internal readonly struct HeroGeometry
{
    public const double HeaderHeight = 72;
    public const double RingMargin = 52;
    public const double PowerSize = 200;
    public const double BelowRing = 142;
    public const double KittenHeight = 152;
    public const double KittenWidth = 190;
    public const double GroupScale = 0.94;
    public const double FooterHeight = 146;
    public const double BottomMargin = 4;
    public const double GroupHeight = RingMargin + PowerSize + BelowRing + KittenHeight;

    private HeroGeometry(double scale, Rect power, Rect kitten, double footerTop)
    {
        Scale = scale;
        Power = power;
        Kitten = kitten;
        FooterTop = footerTop;
    }

    public double Scale { get; }

    public Rect Power { get; }

    public Rect Kitten { get; }

    public double FooterTop { get; }

    public static HeroGeometry Compute(double width, double height)
    {
        var available = height - HeaderHeight - BottomMargin - FooterHeight;
        var scale = Math.Min(GroupScale, Math.Min(available / GroupHeight, (width - 32) / (PowerSize + RingMargin * 2)));
        scale = Math.Max(0.45, scale);

        var extra = Math.Max(0, available - GroupHeight * scale);
        var groupTop = HeaderHeight + extra / 2;
        var diameter = PowerSize * scale;
        var cx = width / 2;
        var power = new Rect(cx - diameter / 2, groupTop + RingMargin * scale, diameter, diameter);
        var kittenWidth = KittenWidth * scale;
        var kitten = new Rect(cx - kittenWidth / 2, power.Bottom + BelowRing * scale, kittenWidth, KittenHeight * scale);
        return new HeroGeometry(scale, power, kitten, groupTop + GroupHeight * scale);
    }
}

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
