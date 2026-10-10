using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public sealed class ProgressStrip : Grid
{
    private readonly TextBlock _text;
    private readonly Grid _bar;

    public ProgressStrip()
    {
        IsVisible = false;
        Margin = new Thickness(0, 22, 0, 0);
        RowDefinitions = new RowDefinitions("Auto,Auto");

        _text = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
        SettingsTheme.Paint(_text, TextBlock.ForegroundProperty, "TextMutedBrush");

        var fill = new Border { CornerRadius = new CornerRadius(8) };
        SettingsTheme.Paint(fill, Border.BackgroundProperty, "AccentGradient");
        _bar = new Grid { ColumnDefinitions = new ColumnDefinitions("1*,99*") };
        _bar.Children.Add(fill);

        var track = new Border { Height = 16, CornerRadius = new CornerRadius(8), ClipToBounds = true, Child = _bar };
        SettingsTheme.Paint(track, Border.BackgroundProperty, "TrackOffBrush");
        Grid.SetRow(track, 1);

        Children.Add(_text);
        Children.Add(track);
    }

    public void Set(int percent)
    {
        percent = Math.Max(0, Math.Min(100, percent));
        _text.Text = L.F("Загрузка {0}%", percent);
        _bar.ColumnDefinitions[0].Width = new GridLength(Math.Max(1, percent), GridUnitType.Star);
        _bar.ColumnDefinitions[1].Width = new GridLength(Math.Max(0, 100 - percent), GridUnitType.Star);
    }
}
