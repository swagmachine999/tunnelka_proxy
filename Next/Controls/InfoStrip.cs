using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class InfoStrip : Border
{
    public InfoStrip(string text, string toneBrush, Button? action = null)
    {
        CornerRadius = new CornerRadius(12);
        Padding = new Thickness(16, 12);
        BorderThickness = new Thickness(1);
        IsVisible = false;
        SettingsTheme.Paint(this, BackgroundProperty, "CardSelectedBrush");
        SettingsTheme.Paint(this, BorderBrushProperty, toneBrush);

        var bar = new Border { Width = 4, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 12, 0) };
        SettingsTheme.Paint(bar, BackgroundProperty, toneBrush);

        var label = new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        SettingsTheme.Paint(label, TextBlock.ForegroundProperty, toneBrush);
        Grid.SetColumn(label, 1);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        grid.Children.Add(bar);
        grid.Children.Add(label);
        if (action != null)
        {
            action.Margin = new Thickness(12, 0, 0, 0);
            action.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(action, 2);
            grid.Children.Add(action);
        }

        Child = grid;
    }
}
