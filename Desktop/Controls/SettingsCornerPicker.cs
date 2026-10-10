using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Tunnelka.Models;

namespace Tunnelka.Desktop;

public sealed class SettingsCornerPicker : UserControl
{
    private readonly Dictionary<OverlayCorner, Border> _spots = new();
    private readonly Border _frame;
    private OverlayCorner _corner;
    private OverlayCorner? _hover;

    public event EventHandler? CornerChanged;

    public SettingsCornerPicker(OverlayCorner corner)
    {
        _corner = corner;
        Width = 100;
        Height = 54;

        var grid = new Grid();
        foreach (OverlayCorner value in Enum.GetValues(typeof(OverlayCorner)))
        {
            var left = value == OverlayCorner.TopLeft || value == OverlayCorner.BottomLeft;
            var top = value == OverlayCorner.TopLeft || value == OverlayCorner.TopRight;
            var spot = new Border
            {
                Width = 24,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(6),
                HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                VerticalAlignment = top ? VerticalAlignment.Top : VerticalAlignment.Bottom,
                IsHitTestVisible = false
            };
            _spots[value] = spot;
            grid.Children.Add(spot);
        }

        _frame = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = grid
        };
        SettingsTheme.Paint(_frame, Border.BackgroundProperty, "SurfaceBrush");
        SettingsTheme.Paint(_frame, Border.BorderBrushProperty, "BorderBrush2");
        _frame.PointerMoved += (_, e) =>
        {
            _hover = CornerAt(e.GetPosition(_frame));
            Refresh();
        };
        _frame.PointerExited += (_, _) =>
        {
            _hover = null;
            Refresh();
        };
        _frame.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(_frame).Properties.IsLeftButtonPressed)
                return;

            var picked = CornerAt(e.GetPosition(_frame));
            if (picked == _corner)
                return;

            _corner = picked;
            Refresh();
            CornerChanged?.Invoke(this, EventArgs.Empty);
        };
        Content = _frame;
        Refresh();
    }

    public OverlayCorner Corner => _corner;

    private OverlayCorner CornerAt(Point point)
    {
        var left = point.X < _frame.Bounds.Width / 2;
        var top = point.Y < _frame.Bounds.Height / 2;
        return left
            ? top ? OverlayCorner.TopLeft : OverlayCorner.BottomLeft
            : top ? OverlayCorner.TopRight : OverlayCorner.BottomRight;
    }

    private void Refresh()
    {
        foreach (var pair in _spots)
        {
            var selected = pair.Key == _corner;
            var hover = !selected && _hover == pair.Key;
            SettingsTheme.Paint(pair.Value, Border.BackgroundProperty, selected || hover ? "AccentBrush" : "TrackOffBrush");
            pair.Value.Opacity = hover ? 0.55 : 1;
        }
    }
}
