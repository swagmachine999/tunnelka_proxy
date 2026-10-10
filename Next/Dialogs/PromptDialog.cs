using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class PromptDialog : Window
{
    private readonly Grid _buttons;
    private readonly ProgressStrip _progress = new();
    private readonly TextBlock _error;

    public event Action? Accepted;
    public event Action? Declined;

    public PromptDialog(string title, string text, string yesLabel, string noLabel, int scalePercent = 100)
    {
        var factor = scalePercent / 100.0;
        Title = "Tunnelka";
        Width = 460 * factor;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;
        SystemDecorations = SystemDecorations.BorderOnly;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SettingsTheme.Paint(this, BackgroundProperty, "HeroBrush");

        var top = BuildTop(title, text);
        _error = BuildError();
        _buttons = BuildButtons(yesLabel, noLabel);

        var root = new StackPanel { Margin = new Thickness(28, 28, 28, 26) };
        root.Children.Add(top);
        root.Children.Add(_error);
        root.Children.Add(_buttons);
        root.Children.Add(_progress);

        Content = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(factor, factor),
            Child = root
        };

        top.Background = Brushes.Transparent;
        top.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(top).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                Declined?.Invoke();
        };
    }

    public static async Task<bool> Ask(Window owner, string title, string text, string yesLabel, string noLabel, int scalePercent = 100)
    {
        var dialog = new PromptDialog(title, text, yesLabel, noLabel, scalePercent);
        dialog.Accepted += () => dialog.Close(true);
        dialog.Declined += () => dialog.Close(false);
        return await dialog.ShowDialog<bool>(owner);
    }

    public void ShowProgress(int percent)
    {
        _error.IsVisible = false;
        _buttons.IsVisible = false;
        _progress.IsVisible = true;
        _progress.Set(percent);
    }

    public void ShowError(string text)
    {
        _progress.IsVisible = false;
        _buttons.IsVisible = true;
        _error.Text = text;
        _error.IsVisible = true;
    }

    private static Grid BuildTop(string title, string text)
    {
        var tile = new BrandTile(64) { VerticalAlignment = VerticalAlignment.Top };
        var heading = new TextBlock { Text = title, FontSize = 16.5, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 2, 0, 6) };
        var body = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        SettingsTheme.Paint(body, TextBlock.ForegroundProperty, "TextMutedBrush");
        var texts = new StackPanel { Margin = new Thickness(20, 0, 0, 0), Children = { heading, body } };
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(texts, 1);
        top.Children.Add(tile);
        top.Children.Add(texts);
        return top;
    }

    private static TextBlock BuildError()
    {
        var error = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 0) };
        SettingsTheme.Paint(error, TextBlock.ForegroundProperty, "PingBadBrush");
        return error;
    }

    private Grid BuildButtons(string yesLabel, string noLabel)
    {
        var no = SettingsParts.Pill(noLabel, false);
        var yes = SettingsParts.Pill(yesLabel, true);
        no.HorizontalAlignment = HorizontalAlignment.Stretch;
        yes.HorizontalAlignment = HorizontalAlignment.Stretch;
        no.Margin = new Thickness(0, 0, 6, 0);
        yes.Margin = new Thickness(6, 0, 0, 0);
        no.Click += (_, _) => Declined?.Invoke();
        yes.Click += (_, _) => Accepted?.Invoke();

        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(0, 22, 0, 0) };
        Grid.SetColumn(yes, 1);
        buttons.Children.Add(no);
        buttons.Children.Add(yes);
        return buttons;
    }
}
