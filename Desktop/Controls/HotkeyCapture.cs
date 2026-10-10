using Avalonia.Input;

namespace Tunnelka.Desktop;

internal sealed class HotkeyCapture
{
    public bool Active { get; private set; }

    public void Begin() => Active = true;

    public void Cancel() => Active = false;

    public bool TryComplete(Key key, KeyModifiers modifiers, out int keys)
    {
        keys = 0;
        if (HotkeyKeyMap.IsModifier(key) || HotkeyKeyMap.CodeOf(key) is not { } code)
            return false;

        var flags = FlagsOf(modifiers);
        if (flags == 0 && !HotkeyKeyMap.IsFunctionKey(code))
            return false;

        keys = flags | code;
        Active = false;
        return true;
    }

    private static int FlagsOf(KeyModifiers modifiers)
    {
        var flags = 0;
        if ((modifiers & KeyModifiers.Control) != 0)
            flags |= GlobalHotkey.ControlFlag;
        if ((modifiers & KeyModifiers.Alt) != 0)
            flags |= GlobalHotkey.AltFlag;
        if ((modifiers & KeyModifiers.Shift) != 0)
            flags |= GlobalHotkey.ShiftFlag;
        return flags;
    }
}
