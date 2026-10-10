using System.Runtime.InteropServices;

namespace Tunnelka.Next;

public sealed class GlobalHotkey : IDisposable
{
    public const int ControlFlag = 0x20000;
    public const int ShiftFlag = 0x10000;
    public const int AltFlag = 0x40000;
    public const int KeyMask = 0xFFFF;

    private const int HotkeyMessage = 0x0312;
    private const int HotkeyId = 1;
    private const uint Alt = 0x1;
    private const uint Control = 0x2;
    private const uint Shift = 0x4;
    private const uint NoRepeat = 0x4000;
    private const string ClassName = "TunnelkaHotkeySink";

    private static readonly IntPtr MessageParent = new(-3);
    private static readonly WindowProc SharedProc = OnMessage;
    private static readonly Dictionary<IntPtr, GlobalHotkey> Sinks = new();
    private static bool _classRegistered;

    private IntPtr _handle;
    private bool _registered;

    public GlobalHotkey()
    {
        Create();
    }

    public event EventHandler? Pressed;

    public bool Set(int keys)
    {
        Clear();
        var key = keys & KeyMask;
        if (key == 0 || _handle == IntPtr.Zero)
            return false;

        var modifiers = NoRepeat;
        if ((keys & ControlFlag) != 0)
            modifiers |= Control;
        if ((keys & AltFlag) != 0)
            modifiers |= Alt;
        if ((keys & ShiftFlag) != 0)
            modifiers |= Shift;

        _registered = RegisterHotKey(_handle, HotkeyId, modifiers, (uint)key);
        return _registered;
    }

    public void Clear()
    {
        if (_registered && _handle != IntPtr.Zero)
            UnregisterHotKey(_handle, HotkeyId);
        _registered = false;
    }

    public static string Describe(int keys)
    {
        var parts = new List<string>();
        if ((keys & ControlFlag) != 0)
            parts.Add("Ctrl");
        if ((keys & AltFlag) != 0)
            parts.Add("Alt");
        if ((keys & ShiftFlag) != 0)
            parts.Add("Shift");

        var key = keys & KeyMask;
        parts.Add(key switch
        {
            0 => "…",
            >= 0x30 and <= 0x39 => ((char)key).ToString(),
            >= 0x41 and <= 0x5A => ((char)key).ToString(),
            >= 0x70 and <= 0x87 => "F" + (key - 0x6F),
            0xC0 => "~",
            0x20 => "Space",
            0x0D => "Enter",
            0x09 => "Tab",
            0x2D => "Insert",
            0x2E => "Delete",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "PageUp",
            0x22 => "PageDown",
            _ => "0x" + key.ToString("X")
        });
        return string.Join(" + ", parts);
    }

    public void Dispose()
    {
        Clear();
        if (_handle != IntPtr.Zero)
        {
            Sinks.Remove(_handle);
            DestroyWindow(_handle);
            _handle = IntPtr.Zero;
        }
    }

    private void Create()
    {
        var instance = GetModuleHandle(null);
        if (!_classRegistered)
        {
            var info = new WindowClass
            {
                Size = Marshal.SizeOf<WindowClass>(),
                Proc = SharedProc,
                Instance = instance,
                ClassName = ClassName
            };
            RegisterClassEx(ref info);
            _classRegistered = true;
        }

        _handle = CreateWindowEx(0, ClassName, ClassName, 0, 0, 0, 0, 0, MessageParent, IntPtr.Zero, instance, IntPtr.Zero);
        if (_handle != IntPtr.Zero)
            Sinks[_handle] = this;
    }

    private static IntPtr OnMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == HotkeyMessage && wParam.ToInt32() == HotkeyId && Sinks.TryGetValue(hwnd, out var sink))
        {
            sink.Pressed?.Invoke(sink, EventArgs.Empty);
            return IntPtr.Zero;
        }

        return DefWindowProc(hwnd, message, wParam, lParam);
    }

    private delegate IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public int Size;
        public int Style;
        public WindowProc Proc;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string? MenuName;
        public string ClassName;
        public IntPtr SmallIcon;
    }

    [DllImport("user32.dll", EntryPoint = "RegisterClassExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WindowClass windowClass);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style, int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
