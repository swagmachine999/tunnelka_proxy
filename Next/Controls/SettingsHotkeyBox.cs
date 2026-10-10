using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class SettingsHotkeyBox : UserControl
{
    private readonly HotkeyCapture _capture = new();
    private readonly Border _frame;
    private readonly TextBlock _text;
    private int _keys;

    public event EventHandler? KeysChanged;

    public SettingsHotkeyBox(int keys)
    {
        _keys = keys;
        Width = 170;
        Height = 34;
        Focusable = true;

        _text = new TextBlock
        {
            FontSize = 15.4,
            FontWeight = FontWeight.SemiBold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _frame = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = _text
        };
        _frame.PointerPressed += (_, _) =>
        {
            Focus();
            _capture.Begin();
            Refresh();
        };
        Content = _frame;
        KeyDown += OnKey;
        LostFocus += (_, _) =>
        {
            _capture.Cancel();
            Refresh();
        };
        Refresh();
    }

    public int Keys => _keys;

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (!_capture.Active)
            return;

        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            _capture.Cancel();
            Refresh();
            return;
        }

        if (!_capture.TryComplete(e.Key, e.KeyModifiers, out var keys))
            return;

        _keys = keys;
        Refresh();
        KeysChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Refresh()
    {
        var capturing = _capture.Active;
        _text.Text = capturing ? L.T("Нажмите клавиши…") : HotkeyKeyMap.Describe(_keys);
        SettingsTheme.Paint(_frame, Border.BackgroundProperty, capturing ? "CardSelectedBrush" : "SurfaceBrush");
        SettingsTheme.Paint(_frame, Border.BorderBrushProperty, capturing ? "AccentBrush" : "BorderBrush2");
        SettingsTheme.Paint(_text, TextBlock.ForegroundProperty, capturing ? "AccentStrongBrush" : "TextBrush");
    }
}
