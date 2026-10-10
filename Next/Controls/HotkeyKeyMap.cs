using Avalonia.Input;

namespace Tunnelka.Next;

internal static class HotkeyKeyMap
{
    private const int FirstFunctionKey = 0x70;
    private const int LastFunctionKey = 0x87;

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

    public static bool IsFunctionKey(int code) => code >= FirstFunctionKey && code <= LastFunctionKey;

    public static bool IsModifier(Key key) =>
        key == Key.LeftCtrl || key == Key.RightCtrl
        || key == Key.LeftShift || key == Key.RightShift
        || key == Key.LeftAlt || key == Key.RightAlt
        || key == Key.LWin || key == Key.RWin
        || key == Key.System;

    public static int? CodeOf(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
            return 0x41 + (key - Key.A);
        if (key >= Key.D0 && key <= Key.D9)
            return 0x30 + (key - Key.D0);
        if (key >= Key.F1 && key <= Key.F24)
            return FirstFunctionKey + (key - Key.F1);
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return 0x60 + (key - Key.NumPad0);

        return Codes.TryGetValue(key, out var code) ? code : null;
    }

    public static string Describe(int keys)
    {
        var parts = new List<string>();
        if ((keys & GlobalHotkey.ControlFlag) != 0)
            parts.Add("Ctrl");
        if ((keys & GlobalHotkey.AltFlag) != 0)
            parts.Add("Alt");
        if ((keys & GlobalHotkey.ShiftFlag) != 0)
            parts.Add("Shift");

        parts.Add(NameOf(keys & GlobalHotkey.KeyMask));
        return string.Join(" + ", parts);
    }

    private static string NameOf(int code)
    {
        if (code >= 0x30 && code <= 0x39 || code >= 0x41 && code <= 0x5A)
            return ((char)code).ToString();
        if (IsFunctionKey(code))
            return "F" + (code - (FirstFunctionKey - 1));
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
}
