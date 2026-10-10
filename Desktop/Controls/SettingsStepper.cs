using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public sealed class SettingsStepper : UserControl
{
    private readonly int[] _values;
    private readonly Func<int, string> _label;
    private readonly TextBlock _text;
    private readonly StepperCell _previous = new(Glyphs.ChevronLeft);
    private readonly StepperCell _next = new(Glyphs.ChevronRight);
    private int _index;

    public event EventHandler? ValueChanged;

    public SettingsStepper(int[] values, Func<int, string> label, int value, double width = 150)
    {
        _values = values;
        _label = label;
        _index = Nearest(value);
        Width = width;
        Height = 34;

        _previous.Pressed += (_, _) => Step(-1);
        _next.Pressed += (_, _) => Step(1);

        _text = new TextBlock
        {
            FontSize = 15.4,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("34,*,34") };
        Grid.SetColumn(_text, 1);
        Grid.SetColumn(_next, 2);
        grid.Children.Add(_previous);
        grid.Children.Add(_text);
        grid.Children.Add(_next);

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

    private void Step(int direction)
    {
        var index = Math.Max(0, Math.Min(_values.Length - 1, _index + direction));
        if (index == _index)
            return;

        _index = index;
        Refresh();
        ValueChanged?.Invoke(this, EventArgs.Empty);
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
        _text.Text = _label(Value);
        _previous.Enabled = _index > 0;
        _next.Enabled = _index < _values.Length - 1;
    }
}
