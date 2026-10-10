using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Models;

namespace Tunnelka.Desktop;

public sealed class RoutingRuleList : StackPanel
{
    public RoutingRuleList()
    {
        Spacing = 12;
    }

    public event Action<RoutingRule>? RuleRemoved;

    public void Show(IReadOnlyCollection<RoutingRule> rules, bool dimmed)
    {
        Children.Clear();
        if (rules.Count == 0)
        {
            Children.Add(BuildEmptyState());
            return;
        }

        foreach (var rule in rules)
        {
            var card = new RuleCard(rule, dimmed);
            card.DeleteClicked += (_, _) => RuleRemoved?.Invoke(rule);
            Children.Add(card);
        }
    }

    private static Border BuildEmptyState()
    {
        var text = new TextBlock
        {
            Text = L.T("Список пуст. Добавь программу или сайт"),
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var box = new Border { Padding = new Thickness(16, 28), CornerRadius = new CornerRadius(12), Child = text };
        SettingsTheme.Paint(box, Border.BackgroundProperty, "SurfaceBrush");
        return box;
    }
}
