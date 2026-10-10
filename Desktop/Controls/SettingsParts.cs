using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

internal static class SettingsParts
{
    public static ToggleSwitch Toggle(bool value, Action<bool> changed)
    {
        var toggle = new ToggleSwitch { IsChecked = value, OnContent = "", OffContent = "" };
        toggle.IsCheckedChanged += (_, _) => changed(toggle.IsChecked == true);
        return toggle;
    }

    public static TextBlock Caption(string text) => new()
    {
        Text = text,
        Classes = { "muted" },
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(2, 8, 0, 6)
    };

    public static TextBlock Paragraph(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(2, 4, 0, 6)
    };

    public static Button Pill(string text, bool primary)
    {
        var button = new Button
        {
            Content = text,
            Classes = { "soft" },
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        if (primary)
            button.Classes.Add("accent");
        return button;
    }

    public static Border Divider()
    {
        var line = new Border { Height = 1 };
        SettingsTheme.Paint(line, Border.BackgroundProperty, "BorderBrush2");
        return line;
    }
}
