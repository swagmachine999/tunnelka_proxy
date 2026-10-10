using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Tunnelka.Next.Controls;

internal static class HeroStyle
{
    public const double BodyFont = 15.4;
    public const double CaptionFont = 14.3;
    public const double NameFont = 18.7;

    public static Color WithAlpha(Color color, double alpha) =>
        Color.FromArgb((byte)Math.Max(0, Math.Min(255, (int)alpha)), color.R, color.G, color.B);

    public static IBrush Solid(Color color, double alpha) => new ImmutableSolidColorBrush(WithAlpha(color, alpha));

    public static Color Lighten(Color color, double amount) => Color.FromArgb(
        color.A,
        (byte)(color.R + (255 - color.R) * amount),
        (byte)(color.G + (255 - color.G) * amount),
        (byte)(color.B + (255 - color.B) * amount));

    public static TextBlock StyleText(TextBlock block, double size)
    {
        block.FontFamily = new FontFamily("Segoe UI");
        block.FontSize = size;
        block.FontWeight = FontWeight.SemiBold;
        block.HorizontalAlignment = HorizontalAlignment.Center;
        block.VerticalAlignment = VerticalAlignment.Center;
        return block;
    }
}
