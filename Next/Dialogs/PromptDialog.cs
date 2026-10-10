using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class PromptDialog : Window
{
    private readonly Grid _buttons;
    private readonly Grid _progress;
    private readonly Grid _bar;
    private readonly TextBlock _progressText;
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

        var tile = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(18),
            VerticalAlignment = VerticalAlignment.Top,
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
        SettingsTheme.Paint(tile, Border.BackgroundProperty, "AccentGradient");

        var heading = new TextBlock { Text = title, FontSize = 16.5, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 2, 0, 6) };
        var body = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        SettingsTheme.Paint(body, TextBlock.ForegroundProperty, "TextMutedBrush");
        var texts = new StackPanel { Margin = new Thickness(20, 0, 0, 0), Children = { heading, body } };
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(texts, 1);
        top.Children.Add(tile);
        top.Children.Add(texts);

        _error = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 0) };
        SettingsTheme.Paint(_error, TextBlock.ForegroundProperty, "PingBadBrush");

        var no = SettingsParts.Pill(noLabel, false);
        var yes = SettingsParts.Pill(yesLabel, true);
        no.HorizontalAlignment = HorizontalAlignment.Stretch;
        yes.HorizontalAlignment = HorizontalAlignment.Stretch;
        no.Margin = new Thickness(0, 0, 6, 0);
        yes.Margin = new Thickness(6, 0, 0, 0);
        no.Click += (_, _) => Declined?.Invoke();
        yes.Click += (_, _) => Accepted?.Invoke();
        _buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(0, 22, 0, 0) };
        Grid.SetColumn(yes, 1);
        _buttons.Children.Add(no);
        _buttons.Children.Add(yes);

        _progressText = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
        SettingsTheme.Paint(_progressText, TextBlock.ForegroundProperty, "TextMutedBrush");
        var track = new Border { Height = 16, CornerRadius = new CornerRadius(8), ClipToBounds = true };
        SettingsTheme.Paint(track, Border.BackgroundProperty, "TrackOffBrush");
        var fill = new Border { CornerRadius = new CornerRadius(8) };
        SettingsTheme.Paint(fill, Border.BackgroundProperty, "AccentGradient");
        _bar = new Grid { ColumnDefinitions = new ColumnDefinitions("1*,99*") };
        _bar.Children.Add(fill);
        track.Child = _bar;
        _progress = new Grid { IsVisible = false, Margin = new Thickness(0, 22, 0, 0), RowDefinitions = new RowDefinitions("Auto,Auto") };
        Grid.SetRow(track, 1);
        _progress.Children.Add(_progressText);
        _progress.Children.Add(track);

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
        percent = Math.Max(0, Math.Min(100, percent));
        _error.IsVisible = false;
        _buttons.IsVisible = false;
        _progress.IsVisible = true;
        _progressText.Text = L.F("Загрузка {0}%", percent);
        _bar.ColumnDefinitions[0].Width = new GridLength(Math.Max(1, percent), GridUnitType.Star);
        _bar.ColumnDefinitions[1].Width = new GridLength(Math.Max(0, 100 - percent), GridUnitType.Star);
    }

    public void ShowError(string text)
    {
        _progress.IsVisible = false;
        _buttons.IsVisible = true;
        _error.Text = text;
        _error.IsVisible = true;
    }
}
