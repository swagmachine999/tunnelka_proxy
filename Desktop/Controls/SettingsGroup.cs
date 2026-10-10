using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public class SettingsGroup : Border
{
    private readonly StackPanel _body = new() { Spacing = 12 };

    public SettingsGroup(string title)
    {
        Classes.Add("card");
        Padding = new Thickness(16);
        Margin = new Thickness(0);

        var caption = new TextBlock
        {
            Text = title,
            Classes = { "muted" },
            FontSize = 12,
            FontWeight = FontWeight.SemiBold
        };
        _body.Children.Add(caption);
        Child = _body;
    }

    public void Add(Control content)
    {
        content.Margin = new Thickness(0);
        _body.Children.Add(content);
    }
}
