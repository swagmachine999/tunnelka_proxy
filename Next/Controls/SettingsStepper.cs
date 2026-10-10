using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class SettingsStepper : UserControl
{
    private readonly int[] _values;
    private readonly Func<int, string> _label;
    private readonly TextBlock _text;
    private readonly TextBlock _previousArrow;
    private readonly TextBlock _nextArrow;
    private readonly Border _previousHover;
    private readonly Border _nextHover;
    private int _index;
    private int _hover;

    public event EventHandler? ValueChanged;

    public SettingsStepper(int[] values, Func<int, string> label, int value, double width = 150)
    {
        _values = values;
        _label = label;
        _index = Nearest(value);
        Width = width;
        Height = 34;

        var previous = BuildCell(out _previousHover, out _previousArrow, "‹");
        var next = BuildCell(out _nextHover, out _nextArrow, "›");
        previous.PointerPressed += (_, e) => OnCell(e, previous, -1);
        next.PointerPressed += (_, e) => OnCell(e, next, 1);
        previous.PointerEntered += (_, _) => SetHover(-1);
        next.PointerEntered += (_, _) => SetHover(1);
        previous.PointerExited += (_, _) => SetHover(0);
        next.PointerExited += (_, _) => SetHover(0);

        _text = new TextBlock
        {
            FontSize = 15.4,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("34,*,34") };
        Grid.SetColumn(_text, 1);
        Grid.SetColumn(next, 2);
        grid.Children.Add(previous);
        grid.Children.Add(_text);
        grid.Children.Add(next);

        var root = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Child = grid
        };
        SettingsTheme.Paint(root, Border.BackgroundProperty, "SurfaceBrush");
        SettingsTheme.Paint(root, Border.BorderBrushProperty, "BorderBrush2");
        Content = root;
        Refresh();
    }

    public int Value => _values[_index];

    public void Select(int value)
    {
        _index = Nearest(value);
        Refresh();
    }

    public void Step(int direction)
    {
        var index = Math.Max(0, Math.Min(_values.Length - 1, _index + direction));
        if (index == _index)
            return;

        _index = index;
        Refresh();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private static Grid BuildCell(out Border hover, out TextBlock arrow, string glyph)
    {
        hover = new Border
        {
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(8),
            Opacity = 0.27,
            IsVisible = false,
            IsHitTestVisible = false
        };
        SettingsTheme.Paint(hover, Border.BackgroundProperty, "AccentBrush");
        arrow = new TextBlock
        {
            Text = glyph,
            FontSize = 22,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        var cell = new Grid
        {
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        cell.Children.Add(hover);
        cell.Children.Add(arrow);
        return cell;
    }

    private void OnCell(PointerPressedEventArgs e, Control cell, int direction)
    {
        if (e.GetCurrentPoint(cell).Properties.IsLeftButtonPressed)
            Step(direction);
    }

    private void SetHover(int hover)
    {
        _hover = hover;
        Refresh();
    }

    private int Nearest(int value)
    {
        var best = 0;
        for (var i = 1; i < _values.Length; i++)
        {
            if (Math.Abs(_values[i] - value) < Math.Abs(_values[best] - value))
                best = i;
        }

        return best;
    }

    private void Refresh()
    {
        var canPrevious = _index > 0;
        var canNext = _index < _values.Length - 1;
        _text.Text = _label(Value);
        SettingsTheme.Paint(_previousArrow, TextBlock.ForegroundProperty, canPrevious ? "AccentStrongBrush" : "TrackOffBrush");
        SettingsTheme.Paint(_nextArrow, TextBlock.ForegroundProperty, canNext ? "AccentStrongBrush" : "TrackOffBrush");
        _previousHover.IsVisible = _hover == -1 && canPrevious;
        _nextHover.IsVisible = _hover == 1 && canNext;
    }
}
