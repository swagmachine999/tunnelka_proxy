using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public sealed class SettingsRadioCard : Border
{
    private readonly TextBlock _title;
    private readonly TextBlock _subtitle;
    private readonly Ellipse _ring;
    private readonly Ellipse _dot;
    private readonly string _normalSubtitle;
    private IDisposable? _backBinding;
    private IDisposable? _borderBinding;
    private bool _checked;
    private string? _lockedHint;

    public event EventHandler? Selected;

    public SettingsRadioCard(string title, string subtitle)
    {
        _normalSubtitle = subtitle;
        Classes.Add("card");
        Classes.Add("clickable");
        Cursor = new Cursor(StandardCursorType.Hand);

        _ring = new Ellipse { Width = 18, Height = 18, StrokeThickness = 1.8, VerticalAlignment = VerticalAlignment.Center };
        _dot = new Ellipse { Width = 9, Height = 9, IsVisible = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        SettingsTheme.Paint(_dot, Shape.FillProperty, "AccentBrush");
        var indicator = new Grid { Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
        indicator.Children.Add(_ring);
        indicator.Children.Add(_dot);

        _title = new TextBlock { Text = title, Classes = { "rowTitle" } };
        _subtitle = new TextBlock { Text = subtitle, Classes = { "muted" }, TextWrapping = TextWrapping.Wrap };
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { _title, _subtitle } };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(texts, 1);
        grid.Children.Add(indicator);
        grid.Children.Add(texts);
        Child = grid;

        PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton != MouseButton.Left || _checked || _lockedHint != null)
                return;

            Checked = true;
            Selected?.Invoke(this, EventArgs.Empty);
        };
        Refresh();
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            Refresh();
        }
    }

    public string? LockedHint
    {
        get => _lockedHint;
        set
        {
            _lockedHint = value;
            Refresh();
        }
    }

    private void Refresh()
    {
        var locked = _lockedHint != null;
        _backBinding?.Dispose();
        _borderBinding?.Dispose();
        _backBinding = null;
        _borderBinding = null;
        if (_checked)
        {
            _backBinding = SettingsTheme.Paint(this, BackgroundProperty, "CardSelectedBrush");
            _borderBinding = SettingsTheme.Paint(this, BorderBrushProperty, "AccentBrush");
        }

        Classes.Set("clickable", !locked && !_checked);
        Cursor = new Cursor(locked ? StandardCursorType.Arrow : StandardCursorType.Hand);
        _dot.IsVisible = _checked;
        _subtitle.Text = _lockedHint ?? _normalSubtitle;
        SettingsTheme.Paint(_ring, Shape.StrokeProperty, _checked ? "AccentBrush" : "TextMutedBrush");
        SettingsTheme.Paint(_title, TextBlock.ForegroundProperty, locked ? "TextMutedBrush" : "TextBrush");
    }
}
