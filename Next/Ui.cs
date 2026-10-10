using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public static class Ui
{
    public static IBrush Brush(Control control, string key) =>
        control.TryFindResource(key, control.ActualThemeVariant, out var value) && value is IBrush brush ? brush : Brushes.Gray;

    public static Color Color(Control control, string key) =>
        control.TryFindResource(key, control.ActualThemeVariant, out var value) && value is Color color ? color : Colors.Gray;

    public static string ToneBrush(Tone tone) => tone switch
    {
        Tone.Good => "PingGoodBrush",
        Tone.Mid => "PingMidBrush",
        Tone.Bad => "PingBadBrush",
        _ => "TextMutedBrush"
    };

    public static TextBlock Title(string text) =>
        new() { Text = text, Classes = { "title" }, Margin = new Thickness(0, 0, 0, 14) };

    public static Border Card(Control content) =>
        new() { Classes = { "card" }, Child = content };

    public static Border Row(string title, string hint, Control control, out TextBlock titleBlock, out TextBlock hintBlock)
    {
        titleBlock = new TextBlock { Text = title, Classes = { "rowTitle" } };
        hintBlock = new TextBlock { Text = hint, Classes = { "muted" }, TextWrapping = TextWrapping.Wrap };
        var texts = new StackPanel { Children = { titleBlock, hintBlock }, VerticalAlignment = VerticalAlignment.Center };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(texts);
        control.VerticalAlignment = VerticalAlignment.Center;
        control.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return Card(grid);
    }

    public static Border ClickRow(string title, string hint, Action onClick, out TextBlock titleBlock, out TextBlock hintBlock)
    {
        var arrow = new GlyphPath(Glyphs.ChevronRight, "TextMutedBrush");
        var card = Row(title, hint, arrow, out titleBlock, out hintBlock);
        card.Classes.Add("clickable");
        card.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        card.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == Avalonia.Input.MouseButton.Left)
                onClick();
        };
        return card;
    }

    public static Control BackHeader(string title, Action onBack, out TextBlock titleBlock)
    {
        var back = new Button { Content = new GlyphPath(Glyphs.ChevronLeft, "AccentStrongBrush"), Classes = { "soft" }, Padding = new Thickness(14, 10) };
        back.Click += (_, _) => onBack();
        titleBlock = new TextBlock { Text = title, Classes = { "title" }, VerticalAlignment = VerticalAlignment.Center };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 0, 0, 14) };
        panel.Children.Add(back);
        panel.Children.Add(titleBlock);
        return panel;
    }
}
