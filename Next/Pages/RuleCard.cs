using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Models;

namespace Tunnelka.Next;

public sealed class RuleCard : Border
{
    private const double SlotSize = 28;

    public event EventHandler? DeleteClicked;

    public RuleCard(RoutingRule rule, bool dimmed)
    {
        Rule = rule;
        Classes.Add("card");
        Padding = new Thickness(16, 10);
        Margin = new Thickness(0);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var icon = BuildIcon(rule, dimmed);
        var name = BuildName(rule, dimmed);
        var remove = BuildRemove();
        Grid.SetColumn(name, 1);
        Grid.SetColumn(remove, 2);
        grid.Children.Add(icon);
        grid.Children.Add(name);
        grid.Children.Add(remove);
        Child = grid;
    }

    public RoutingRule Rule { get; }

    private static Control BuildIcon(RoutingRule rule, bool dimmed)
    {
        Control content;
        var bitmap = ExeIcons.Load(rule.IconPath);
        if (bitmap != null)
        {
            content = new Image { Source = bitmap, Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }
        else
        {
            content = new GlyphPath(rule.IsProcess ? Glyphs.Process : Glyphs.Site, dimmed ? "TextMutedBrush" : "AccentStrongBrush");
        }

        return new Grid
        {
            Width = SlotSize,
            Height = SlotSize,
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children = { content }
        };
    }

    private static TextBlock BuildName(RoutingRule rule, bool dimmed)
    {
        var name = new TextBlock
        {
            Text = rule.DisplayName,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        SettingsTheme.Paint(name, TextBlock.ForegroundProperty, dimmed ? "TextMutedBrush" : "TextBrush");
        return name;
    }

    private Control BuildRemove()
    {
        var cross = new GlyphPath(Glyphs.Cross, "TextMutedBrush");
        var remove = new Border
        {
            Width = SlotSize,
            Height = SlotSize,
            CornerRadius = new CornerRadius(8),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            VerticalAlignment = VerticalAlignment.Center,
            Child = cross
        };
        remove.PointerEntered += (_, _) => cross.Tint("PingBadBrush");
        remove.PointerExited += (_, _) => cross.Tint("TextMutedBrush");
        remove.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
                DeleteClicked?.Invoke(this, EventArgs.Empty);
        };
        return remove;
    }
}
