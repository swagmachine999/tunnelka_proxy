using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Models;

namespace Tunnelka.Next;

public sealed class RuleCard : Border
{
    private const string ProcessGlyph = "M5,4 L19,4 A2,2 0 0 1 21,6 L21,18 A2,2 0 0 1 19,20 L5,20 A2,2 0 0 1 3,18 L3,6 A2,2 0 0 1 5,4 M3,9 L21,9";
    private const string SiteGlyph = "M3,12 A9,9 0 1 0 21,12 A9,9 0 1 0 3,12 M3,12 L21,12 M12,3 C8,7 8,17 12,21 M12,3 C16,7 16,17 12,21";

    public event EventHandler? DeleteClicked;

    public RuleCard(RoutingRule rule, bool dimmed)
    {
        Rule = rule;
        Classes.Add("card");
        Padding = new Thickness(14, 10);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        Control icon;
        var bitmap = SettingsIcons.Load(rule.IconPath);
        if (bitmap != null)
        {
            icon = new Image { Source = bitmap, Width = 24, Height = 24 };
        }
        else
        {
            var glyph = new Avalonia.Controls.Shapes.Path
            {
                Data = Geometry.Parse(rule.IsProcess ? ProcessGlyph : SiteGlyph),
                Width = 22,
                Height = 22,
                Stretch = Stretch.Uniform,
                StrokeThickness = 2,
                StrokeLineCap = PenLineCap.Round,
                StrokeJoin = PenLineJoin.Round
            };
            SettingsTheme.Paint(glyph, Avalonia.Controls.Shapes.Shape.StrokeProperty, dimmed ? "TextMutedBrush" : "AccentStrongBrush");
            icon = glyph;
        }

        icon.VerticalAlignment = VerticalAlignment.Center;
        icon.Margin = new Thickness(0, 0, 12, 0);

        var name = new TextBlock
        {
            Text = rule.DisplayName,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        SettingsTheme.Paint(name, TextBlock.ForegroundProperty, dimmed ? "TextMutedBrush" : "TextBrush");
        Grid.SetColumn(name, 1);

        var cross = new Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse("M0,0 L10,10 M10,0 L0,10"),
            Width = 10,
            Height = 10,
            StrokeThickness = 2,
            StrokeLineCap = PenLineCap.Round
        };
        SettingsTheme.Paint(cross, Avalonia.Controls.Shapes.Shape.StrokeProperty, "TextMutedBrush");
        var remove = new Border
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            VerticalAlignment = VerticalAlignment.Center,
            Child = cross
        };
        remove.PointerEntered += (_, _) => SettingsTheme.Paint(cross, Avalonia.Controls.Shapes.Shape.StrokeProperty, "PingBadBrush");
        remove.PointerExited += (_, _) => SettingsTheme.Paint(cross, Avalonia.Controls.Shapes.Shape.StrokeProperty, "TextMutedBrush");
        remove.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
                DeleteClicked?.Invoke(this, EventArgs.Empty);
        };
        Grid.SetColumn(remove, 2);

        grid.Children.Add(icon);
        grid.Children.Add(name);
        grid.Children.Add(remove);
        Child = grid;
    }

    public RoutingRule Rule { get; }
}
