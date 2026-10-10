using System.Runtime.InteropServices;

namespace Tunnelka.Desktop;

internal sealed class MessageWindow : IDisposable
{
    private static readonly IntPtr MessageParent = new(-3);
    private static int _serial;

    private readonly WindowProc _proc;
    private readonly Func<uint, IntPtr, IntPtr, bool> _handler;
    private readonly string _className = "TunnelkaSink" + Interlocked.Increment(ref _serial);
    private readonly IntPtr _instance = GetModuleHandle(null);
    private bool _registered;

    public MessageWindow(Func<uint, IntPtr, IntPtr, bool> handler, bool messageOnly)
    {
        _handler = handler;
        _proc = OnMessage;
        var info = new WindowClass
        {
            Size = Marshal.SizeOf<WindowClass>(),
            Proc = _proc,
            Instance = _instance,
            ClassName = _className
        };
        _registered = RegisterClassEx(ref info) != 0;
        if (_registered)
            Handle = CreateWindowEx(0, _className, _className, 0, 0, 0, 0, 0, messageOnly ? MessageParent : IntPtr.Zero, IntPtr.Zero, _instance, IntPtr.Zero);
    }

    public IntPtr Handle { get; private set; }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            DestroyWindow(Handle);
            Handle = IntPtr.Zero;
        }

        if (_registered)
        {
            UnregisterClass(_className, _instance);
            _registered = false;
        }
    }

    private IntPtr OnMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam) =>
        _handler(message, wParam, lParam) ? IntPtr.Zero : DefWindowProc(hwnd, message, wParam, lParam);

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

    [DllImport("user32.dll", EntryPoint = "UnregisterClassW", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClass(string className, IntPtr instance);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style, int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);
}
