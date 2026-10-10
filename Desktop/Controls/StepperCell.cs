using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Tunnelka.Desktop;

internal sealed class StepperCell : Grid
{
    private readonly Border _hover;
    private readonly GlyphPath _glyph;
    private bool _enabled = true;
    private bool _hovered;

    public StepperCell(string glyph)
    {
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);

        _hover = new Border
        {
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(8),
            Opacity = 0.27,
            IsVisible = false,
            IsHitTestVisible = false
        };
        SettingsTheme.Paint(_hover, Border.BackgroundProperty, "AccentBrush");
        _glyph = new GlyphPath(glyph, "AccentStrongBrush");
        Children.Add(_hover);
        Children.Add(_glyph);

        PointerPressed += OnPressed;
        PointerEntered += (_, _) => SetHovered(true);
        PointerExited += (_, _) => SetHovered(false);
    }

    public event EventHandler? Pressed;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            Refresh();
        }
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            Pressed?.Invoke(this, EventArgs.Empty);
    }

    private void SetHovered(bool hovered)
    {
        _hovered = hovered;
        Refresh();
    }

    private void Refresh()
    {
        _glyph.Tint(_enabled ? "AccentStrongBrush" : "TrackOffBrush");
        _hover.IsVisible = _hovered && _enabled;
    }
}
