using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class ServerSearchBox : Border
{
    private readonly TextBox _box;
    private readonly TextBlock _placeholder;
    private IDisposable? _borderBinding;

    public ServerSearchBox()
    {
        Height = 42;
        CornerRadius = new CornerRadius(12);
        BorderThickness = new Thickness(1);
        ServerRes.Bind(this, BackgroundProperty, "CardBrush");

        _box = new TextBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MinHeight = 0,
            FontSize = 14 * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(40, 0, 14, 0)
        };
        foreach (var key in new[]
                 {
                     "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
                     "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused"
                 })
            _box.Resources[key] = Brushes.Transparent;
        _box.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
        _box.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);
        ServerRes.Bind(_box, TextBox.ForegroundProperty, "TextBrush");

        _placeholder = new TextBlock
        {
            Text = L.T("Поиск сервера"),
            FontSize = 14 * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(40, 0, 14, 0),
            IsHitTestVisible = false
        };
        ServerRes.Bind(_placeholder, TextBlock.ForegroundProperty, "TextMutedBrush");

        var icon = new ServerIcon(ServerIconKind.Search, 40, 42) { HorizontalAlignment = HorizontalAlignment.Left, IsHitTestVisible = false };

        var grid = new Grid();
        grid.Children.Add(icon);
        grid.Children.Add(_placeholder);
        grid.Children.Add(_box);
        Child = grid;

        _box.TextChanged += (_, _) =>
        {
            _placeholder.IsVisible = string.IsNullOrEmpty(_box.Text);
            QueryChanged?.Invoke();
        };
        _box.GotFocus += (_, _) => SetFocused(true);
        _box.LostFocus += (_, _) => SetFocused(false);
        SetFocused(false);
    }

    public event Action? QueryChanged;

    public string Query => (_box.Text ?? "").Trim();

    public void Localize() => _placeholder.Text = L.T("Поиск сервера");

    private void SetFocused(bool focused)
    {
        BorderThickness = new Thickness(focused ? 1.6 : 1);
        _borderBinding?.Dispose();
        _borderBinding = ServerRes.Bind(this, BorderBrushProperty, focused ? "AccentBrush" : "BorderBrush2");
    }
}
