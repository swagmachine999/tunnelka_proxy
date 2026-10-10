using System.Runtime.InteropServices;

namespace Tunnelka.Next;

public sealed class GlobalHotkey : IDisposable
{
    public const int ShiftFlag = 0x10000;
    public const int ControlFlag = 0x20000;
    public const int AltFlag = 0x40000;
    public const int KeyMask = 0xFFFF;

    private const int HotkeyMessage = 0x0312;
    private const int HotkeyId = 1;
    private const uint Alt = 0x1;
    private const uint Control = 0x2;
    private const uint Shift = 0x4;
    private const uint NoRepeat = 0x4000;

    private readonly MessageWindow _window;
    private bool _registered;

    public GlobalHotkey()
    {
        _window = new MessageWindow(OnMessage, true);
    }

    public event EventHandler? Pressed;

    public bool Set(int keys)
    {
        Clear();
        var key = keys & KeyMask;
        if (key == 0 || _window.Handle == IntPtr.Zero)
            return false;

        var modifiers = NoRepeat;
        if ((keys & ControlFlag) != 0)
            modifiers |= Control;
        if ((keys & AltFlag) != 0)
            modifiers |= Alt;
        if ((keys & ShiftFlag) != 0)
            modifiers |= Shift;

        _registered = RegisterHotKey(_window.Handle, HotkeyId, modifiers, (uint)key);
        return _registered;
    }

    public void Clear()
    {
        if (_registered && _window.Handle != IntPtr.Zero)
            UnregisterHotKey(_window.Handle, HotkeyId);
        _registered = false;
    }

    public void Dispose()
    {
        Clear();
        _window.Dispose();
    }

    private bool OnMessage(uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message != HotkeyMessage || wParam.ToInt32() != HotkeyId)
            return false;

        Pressed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
