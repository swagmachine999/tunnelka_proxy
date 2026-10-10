using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

internal sealed class ConfirmView : UserControl
{
    private readonly Grid _buttons = new() { ColumnDefinitions = new ColumnDefinitions("*,*"), Height = 44, Margin = new Thickness(0, 22, 0, 0) };
    private readonly StackPanel _progress = new() { Margin = new Thickness(0, 22, 0, 0), IsVisible = false, Spacing = 8 };
    private readonly TextBlock _progressText = new() { HorizontalAlignment = HorizontalAlignment.Center, FontSize = 14.3, FontWeight = FontWeight.SemiBold };
    private readonly ProgressBar _bar = new() { Minimum = 0, Maximum = 100, Height = 16, CornerRadius = new CornerRadius(8) };
    private readonly TextBlock _error = new() { FontSize = 14.3, TextWrapping = TextWrapping.Wrap, IsVisible = false, Margin = new Thickness(0, 12, 0, 0) };

    public ConfirmView(string title, string text, string yes, string? no)
    {
        var tile = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(18),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 20, 0),
            Child = new TextBlock
            {
                Text = "T",
                FontSize = 32,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        tile.Paint(Border.BackgroundProperty, "AccentGradient");

        var heading = new TextBlock { Text = title, FontSize = 16.5, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
        var body = new TextBlock { Text = text, FontSize = 15.4, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
        body.Paint(TextBlock.ForegroundProperty, "TextMutedBrush");
        _error.Paint(TextBlock.ForegroundProperty, "PingBadBrush");
        _progressText.Paint(TextBlock.ForegroundProperty, "TextMutedBrush");
        _bar.Paint(ProgressBar.ForegroundProperty, "AccentBrush");
        _bar.Paint(ProgressBar.BackgroundProperty, "TrackOffBrush");

        var texts = new StackPanel { Children = { heading, body } };
        Grid.SetColumn(texts, 1);
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        top.Children.Add(tile);
        top.Children.Add(texts);

        _progress.Children.Add(_progressText);
        _progress.Children.Add(_bar);

        var accept = MakeButton(yes, true);
        accept.Click += (_, _) => Accepted?.Invoke(this, EventArgs.Empty);
        if (no != null)
        {
            var decline = MakeButton(no, false);
            decline.Margin = new Thickness(0, 0, 6, 0);
            accept.Margin = new Thickness(6, 0, 0, 0);
            decline.Click += (_, _) => Declined?.Invoke(this, EventArgs.Empty);
            Grid.SetColumn(accept, 1);
            _buttons.Children.Add(decline);
        }
        else
        {
            _buttons.ColumnDefinitions = new ColumnDefinitions("*");
        }

        _buttons.Children.Add(accept);

        var root = new StackPanel { Margin = new Thickness(28, 30, 28, 24) };
        root.Children.Add(top);
        root.Children.Add(_error);
        root.Children.Add(_buttons);
        root.Children.Add(_progress);
        Content = root;
    }

    public event EventHandler? Accepted;

    public event EventHandler? Declined;

    public void ShowProgress(int percent, string label)
    {
        _error.IsVisible = false;
        _buttons.IsVisible = false;
        _progress.IsVisible = true;
        _progressText.Text = label;
        _bar.Value = percent;
    }

    public void ShowError(string text)
    {
        _progress.IsVisible = false;
        _buttons.IsVisible = true;
        _error.Text = text;
        _error.IsVisible = true;
    }

    private static Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Content = text,
            Classes = { "soft" },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Height = 44,
            CornerRadius = new CornerRadius(22),
            FontSize = 15.4,
            Padding = new Thickness(0)
        };
        if (primary)
        {
            button.Classes.Add("accent");
        }
        else
        {
            button.BorderThickness = new Thickness(1);
            button.Paint(Button.BorderBrushProperty, "AccentBrush");
        }

        return button;
    }
}
