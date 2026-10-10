using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Tunnelka.Next;

public sealed class SettingsSegmented : UserControl
{
    private readonly ToggleButton[] _buttons;
    private int _selected;

    public event EventHandler? SelectedIndexChanged;

    public SettingsSegmented(double width, params string[] options)
    {
        Width = width;
        _buttons = new ToggleButton[options.Length];
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(string.Join(",", options.Select(_ => "*"))) };
        for (var i = 0; i < options.Length; i++)
        {
            var index = i;
            var button = new ToggleButton
            {
                Content = options[i],
                Classes = { "seg" },
                MinHeight = 0,
                Padding = new Thickness(8, 5),
                CornerRadius = new CornerRadius(11),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            button.Click += (_, _) => Choose(index);
            Grid.SetColumn(button, i);
            grid.Children.Add(button);
            _buttons[i] = button;
        }

        var root = new Border
        {
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(3),
            Child = grid
        };
        SettingsTheme.Paint(root, Border.BackgroundProperty, "CardBrush");
        SettingsTheme.Paint(root, Border.BorderBrushProperty, "BorderBrush2");
        Content = root;
        Apply();
    }

    public int SelectedIndex => _selected;

    public void Select(int index)
    {
        _selected = Math.Max(0, Math.Min(_buttons.Length - 1, index));
        Apply();
    }

    private void Choose(int index)
    {
        var changed = index != _selected;
        _selected = index;
        Apply();
        if (changed)
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Apply()
    {
        for (var i = 0; i < _buttons.Length; i++)
            _buttons[i].IsChecked = i == _selected;
    }
}
