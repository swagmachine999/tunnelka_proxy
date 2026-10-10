using Avalonia;

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
