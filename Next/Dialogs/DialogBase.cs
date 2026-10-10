using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

internal static class Themed
{
    public static T Paint<T>(this T control, AvaloniaProperty property, string key) where T : Control
    {
        control.Bind(property, control.GetResourceObservable(key));
        return control;
    }
}

public abstract class DialogBase : Window
{
    protected DialogBase(string title, double width)
    {
        Title = title;
        Width = width;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        };
    }

    protected Task Present(Window? owner)
    {
        var done = new TaskCompletionSource();
        Closed += (_, _) => done.TrySetResult();
        if (owner is { IsVisible: true })
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _ = ShowDialog(owner);
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Show();
        }

        return done.Task;
    }

    protected static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontSize = 16.5,
        FontWeight = FontWeight.SemiBold,
        TextWrapping = TextWrapping.Wrap
    };

    protected static TextBlock Body(string text, bool muted = true)
    {
        var block = new TextBlock { Text = text, FontSize = 15.4, TextWrapping = TextWrapping.Wrap };
        if (muted)
            block.Paint(TextBlock.ForegroundProperty, "TextMutedBrush");
        return block;
    }

    protected static Button Primary(string text, double minWidth = 110) => new()
    {
        Content = text,
        Classes = { "soft", "accent" },
        MinWidth = minWidth,
        Height = 38,
        CornerRadius = new CornerRadius(19),
        FontSize = 15.4,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Padding = new Thickness(18, 0)
    };

    protected static Button Secondary(string text, double minWidth = 110)
    {
        var button = new Button
        {
            Content = text,
            Classes = { "soft" },
            MinWidth = minWidth,
            Height = 38,
            CornerRadius = new CornerRadius(19),
            FontSize = 15.4,
            BorderThickness = new Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(18, 0)
        };
        button.Paint(Button.BorderBrushProperty, "AccentBrush");
        return button;
    }
}
