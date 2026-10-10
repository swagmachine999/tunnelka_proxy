using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Tunnelka.Desktop;

public static class ServerScrollStyle
{
    private const double ThumbWidth = 6;
    private const double Gutter = 10;
    private const double EndInset = 8;

    public static void Apply(ScrollViewer scroll)
    {
        scroll.AllowAutoHide = false;
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

        ApplyResources(scroll);
        scroll.Styles.Add(BarStyle());
        scroll.Styles.Add(ThumbStyle());
        scroll.Styles.Add(HiddenButtonStyle("PART_LineUpButton"));
        scroll.Styles.Add(HiddenButtonStyle("PART_LineDownButton"));
    }

    private static void ApplyResources(ScrollViewer scroll)
    {
        var thumb = AccentBrush(scroll, "AccentColor", 0.55);
        scroll.Resources["ScrollBarSize"] = ThumbWidth;
        scroll.Resources["ScrollBarPanningThumbBackground"] = thumb;
        scroll.Resources["ScrollBarThumbBackgroundColor"] = thumb;
        scroll.Resources["ScrollBarThumbFillPointerOver"] = AccentBrush(scroll, "AccentStrongColor", 0.85);
        scroll.Resources["ScrollBarThumbFillPressed"] = AccentBrush(scroll, "AccentStrongColor", 1);
        scroll.Resources["ScrollBarTrackFill"] = Brushes.Transparent;
        scroll.Resources["ScrollBarTrackFillPointerOver"] = Brushes.Transparent;
    }

    private static Style BarStyle()
    {
        var style = new Style(x => x.OfType<ScrollBar>().Class(":vertical"));
        style.Setters.Add(new Setter(Layoutable.WidthProperty, Gutter));
        style.Setters.Add(new Setter(Layoutable.MarginProperty, new Thickness(0, EndInset)));
        return style;
    }

    private static Style ThumbStyle()
    {
        var style = new Style(x => x.OfType<ScrollBar>().Class(":vertical").Template().OfType<Thumb>());
        style.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(ThumbWidth / 2)));
        return style;
    }

    private static Style HiddenButtonStyle(string name)
    {
        var style = new Style(x => x.OfType<ScrollBar>().Template().OfType<RepeatButton>().Name(name));
        style.Setters.Add(new Setter(Visual.IsVisibleProperty, false));
        return style;
    }

    private static SolidColorBrush AccentBrush(ScrollViewer scroll, string colorKey, double opacity)
    {
        var brush = new SolidColorBrush();
        brush.Bind(SolidColorBrush.ColorProperty, scroll.GetResourceObservable(colorKey, value => Tint(value, opacity)));
        return brush;
    }

    private static object? Tint(object? value, double opacity) =>
        value is Color color ? ServerRes.Alpha(color, opacity) : null;
}
