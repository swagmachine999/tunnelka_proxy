using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class SettingsHotkeyBox : UserControl
{
    public const int ControlFlag = 0x20000;
    public const int ShiftFlag = 0x10000;
    public const int AltFlag = 0x40000;
    public const int KeyMask = 0xFFFF;

    private static readonly Dictionary<Key, int> Codes = new()
    {
        [Key.Space] = 0x20,
        [Key.Tab] = 0x09,
        [Key.Return] = 0x0D,
        [Key.Back] = 0x08,
        [Key.Insert] = 0x2D,
        [Key.Delete] = 0x2E,
        [Key.Home] = 0x24,
        [Key.End] = 0x23,
        [Key.Prior] = 0x21,
        [Key.Next] = 0x22,
        [Key.Left] = 0x25,
        [Key.Up] = 0x26,
        [Key.Right] = 0x27,
        [Key.Down] = 0x28,
        [Key.OemTilde] = 0xC0,
        [Key.OemMinus] = 0xBD,
        [Key.OemPlus] = 0xBB,
        [Key.OemComma] = 0xBC,
        [Key.OemPeriod] = 0xBE
    };

    private readonly Border _frame;
    private readonly TextBlock _text;
    private int _keys;
    private bool _capturing;

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
            _capturing = true;
            Refresh();
        };
        Content = _frame;
        KeyDown += OnKey;
        LostFocus += (_, _) =>
        {
            _capturing = false;
            Refresh();
        };
        Refresh();
    }

    public int Keys => _keys;

    public static string Describe(int keys)
    {
        var parts = new List<string>();
        if ((keys & ControlFlag) != 0)
            parts.Add("Ctrl");
        if ((keys & AltFlag) != 0)
            parts.Add("Alt");
        if ((keys & ShiftFlag) != 0)
            parts.Add("Shift");

        var code = keys & KeyMask;
        parts.Add(NameOf(code));
        return string.Join(" + ", parts);
    }

    private static string NameOf(int code)
    {
        if (code >= 0x30 && code <= 0x39 || code >= 0x41 && code <= 0x5A)
            return ((char)code).ToString();
        if (code >= 0x70 && code <= 0x87)
            return "F" + (code - 0x6F);
        if (code >= 0x60 && code <= 0x69)
            return "NumPad" + (code - 0x60);

        return code switch
        {
            0 => "…",
            0xC0 => "~",
            0x20 => "Space",
            0x09 => "Tab",
            0x0D => "Return",
            0x08 => "Back",
            0x2D => "Insert",
            0x2E => "Delete",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "Prior",
            0x22 => "Next",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0xBD => "OemMinus",
            0xBB => "Oemplus",
            0xBC => "Oemcomma",
            0xBE => "OemPeriod",
            _ => "0x" + code.ToString("X")
        };
    }

    private static int? CodeOf(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
            return 0x41 + (key - Key.A);
        if (key >= Key.D0 && key <= Key.D9)
            return 0x30 + (key - Key.D0);
        if (key >= Key.F1 && key <= Key.F24)
            return 0x70 + (key - Key.F1);
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return 0x60 + (key - Key.NumPad0);

        return Codes.TryGetValue(key, out var code) ? code : null;
    }

    private static bool IsModifier(Key key) =>
        key == Key.LeftCtrl || key == Key.RightCtrl
        || key == Key.LeftShift || key == Key.RightShift
        || key == Key.LeftAlt || key == Key.RightAlt
        || key == Key.LWin || key == Key.RWin
        || key == Key.System;

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (!_capturing)
            return;

        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            _capturing = false;
            Refresh();
            return;
        }

        if (IsModifier(e.Key) || CodeOf(e.Key) is not { } code)
            return;

        var modifiers = 0;
        if ((e.KeyModifiers & KeyModifiers.Control) != 0)
            modifiers |= ControlFlag;
        if ((e.KeyModifiers & KeyModifiers.Alt) != 0)
            modifiers |= AltFlag;
        if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            modifiers |= ShiftFlag;

        var functionKey = code >= 0x70 && code <= 0x87;
        if (modifiers == 0 && !functionKey)
            return;

        _keys = modifiers | code;
        _capturing = false;
        Refresh();
        KeysChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Refresh()
    {
        _text.Text = _capturing ? L.T("Нажмите клавиши…") : Describe(_keys);
        SettingsTheme.Paint(_frame, Border.BackgroundProperty, _capturing ? "CardSelectedBrush" : "SurfaceBrush");
        SettingsTheme.Paint(_frame, Border.BorderBrushProperty, _capturing ? "AccentBrush" : "BorderBrush2");
        SettingsTheme.Paint(_text, TextBlock.ForegroundProperty, _capturing ? "AccentStrongBrush" : "TextBrush");
    }
}
