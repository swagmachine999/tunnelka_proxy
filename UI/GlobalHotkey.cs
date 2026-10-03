using System.Runtime.InteropServices;

namespace Tunnelka.UI;

public sealed class GlobalHotkey : NativeWindow, IDisposable
{
    private const int HotkeyMessage = 0x0312;
    private const int Id = 1;
    private const uint Alt = 0x1;
    private const uint Control = 0x2;
    private const uint Shift = 0x4;
    private const uint NoRepeat = 0x4000;

    private bool _registered;

    public GlobalHotkey()
    {
        CreateHandle(new CreateParams());
    }

    public event EventHandler? Pressed;

    public bool Set(Keys keys)
    {
        Clear();
        var key = keys & Keys.KeyCode;
        if (key == Keys.None)
            return false;

        uint modifiers = NoRepeat;
        if (keys.HasFlag(Keys.Control))
            modifiers |= Control;
        if (keys.HasFlag(Keys.Alt))
            modifiers |= Alt;
        if (keys.HasFlag(Keys.Shift))
            modifiers |= Shift;

        _registered = RegisterHotKey(Handle, Id, modifiers, (uint)key);
        return _registered;
    }

    public void Clear()
    {
        if (_registered)
            UnregisterHotKey(Handle, Id);
        _registered = false;
    }

    public static string Describe(Keys keys)
    {
        var parts = new List<string>();
        if (keys.HasFlag(Keys.Control))
            parts.Add("Ctrl");
        if (keys.HasFlag(Keys.Alt))
            parts.Add("Alt");
        if (keys.HasFlag(Keys.Shift))
            parts.Add("Shift");

        var key = keys & Keys.KeyCode;
        parts.Add(key switch
        {
            >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
            Keys.Oemtilde => "~",
            Keys.None => "…",
            _ => key.ToString()
        });
        return string.Join(" + ", parts);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == HotkeyMessage && (int)m.WParam == Id)
            Pressed?.Invoke(this, EventArgs.Empty);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Clear();
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
