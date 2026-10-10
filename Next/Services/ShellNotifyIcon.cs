using System.Runtime.InteropServices;
using Avalonia;

namespace Tunnelka.Next;

internal sealed class ShellNotifyIcon : IDisposable
{
    private const uint CallbackMessage = 0x8001;
    private const uint Add = 0;
    private const uint Delete = 2;
    private const uint FlagMessage = 1;
    private const uint FlagIcon = 2;
    private const uint FlagTip = 4;
    private const int LeftButtonUp = 0x0202;
    private const int RightButtonUp = 0x0205;
    private const int SmallIconWidth = 49;
    private const int SmallIconHeight = 50;
    private const int IconId = 1;

    private readonly MessageWindow _window;
    private readonly uint _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private readonly System.Drawing.Icon _icon;
    private readonly bool _ownsIcon;
    private readonly string _tip;
    private bool _added;

    public ShellNotifyIcon(Stream iconStream, string tip)
    {
        _tip = tip;
        try
        {
            using var memory = new MemoryStream();
            iconStream.CopyTo(memory);
            memory.Position = 0;
            _icon = new System.Drawing.Icon(memory, GetSystemMetrics(SmallIconWidth), GetSystemMetrics(SmallIconHeight));
            _ownsIcon = true;
        }
        catch (Exception)
        {
            _icon = System.Drawing.SystemIcons.Application;
        }

        _window = new MessageWindow(OnMessage, false);
        Register();
    }

    public event Action? Clicked;

    public event Action<PixelPoint>? ContextRequested;

    public void Dispose()
    {
        if (_added)
        {
            var data = Describe(0);
            Shell_NotifyIcon(Delete, ref data);
            _added = false;
        }

        _window.Dispose();
        if (_ownsIcon)
            _icon.Dispose();
    }

    private void Register()
    {
        if (_window.Handle == IntPtr.Zero)
            return;

        var data = Describe(FlagMessage | FlagIcon | FlagTip);
        if (Shell_NotifyIcon(Add, ref data))
            _added = true;
    }

    private NotifyIconData Describe(uint flags) => new()
    {
        Size = Marshal.SizeOf<NotifyIconData>(),
        Window = _window.Handle,
        Id = IconId,
        Flags = flags,
        CallbackMessage = CallbackMessage,
        Icon = _icon.Handle,
        Tip = _tip,
        Info = "",
        InfoTitle = ""
    };

    private bool OnMessage(uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == CallbackMessage)
        {
            OnMouse((int)(lParam.ToInt64() & 0xFFFF));
            return true;
        }

        if (_taskbarCreated != 0 && message == _taskbarCreated)
        {
            _added = false;
            Register();
            return true;
        }

        return false;
    }

    private void OnMouse(int mouseMessage)
    {
        if (mouseMessage == LeftButtonUp)
        {
            Clicked?.Invoke();
        }
        else if (mouseMessage == RightButtonUp && GetCursorPos(out var point))
        {
            ContextRequested?.Invoke(new PixelPoint(point.X, point.Y));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;
        public uint InfoFlags;
        public Guid Item;
        public IntPtr BalloonIcon;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string name);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out CursorPoint point);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
