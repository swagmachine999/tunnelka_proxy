using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Tunnelka.Desktop;

public static class ServerRes
{
    public static IDisposable Bind(Control target, AvaloniaProperty property, string key) =>
        target.Bind(property, target.GetResourceObservable(key));

    public static IDisposable BindColor(Control target, AvaloniaProperty property, string colorKey) =>
        target.Bind(property, target.GetResourceObservable(colorKey, ToBrush));

    private static object? ToBrush(object? value)
    {
        if (value is Color color)
            return new SolidColorBrush(color);

        return null;
    }

    public static Color Alpha(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Max(0, Math.Min(255, opacity * 255)), color.R, color.G, color.B);

    public static Color Lighten(Color color, double amount) => Color.FromArgb(
        color.A,
        (byte)(color.R + (255 - color.R) * amount),
        (byte)(color.G + (255 - color.G) * amount),
        (byte)(color.B + (255 - color.B) * amount));

    public static Pen IconPen(IBrush brush, double thickness = 1.8) =>
        new(brush, thickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    public static LinearGradientBrush CardGradient(Control owner)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
        };
        var top = new GradientStop(Colors.White, 0);
        var bottom = new GradientStop(Colors.White, 1);
        top.Bind(GradientStop.ColorProperty, owner.GetResourceObservable("CardColor"));
        bottom.Bind(GradientStop.ColorProperty, owner.GetResourceObservable("CardSelectedColor", value =>
        {
            if (value is not Color color)
                return null;

            return owner.ActualThemeVariant == ThemeVariant.Dark ? color : Lighten(color, 0.3);
        }));
        brush.GradientStops.Add(top);
        brush.GradientStops.Add(bottom);
        return brush;
    }
}
