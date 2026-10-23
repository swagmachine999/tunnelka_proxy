using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace Tunnelka.Desktop;

internal static class Win32
{
    private const int ExStyleIndex = -20;
    private const int ExTransparent = 0x20;
    private const int ExToolWindow = 0x80;
    private const int ExNoActivate = 0x8000000;
    private const int ShowRestore = 9;
    private const uint SwpNoSize = 0x1;
    private const uint SwpNoMove = 0x2;
    private const uint SwpShowWindow = 0x40;
    private static readonly IntPtr HwndTopmost = new(-1);

    public static void NoActivate(Window window) => AddExStyle(window, ExToolWindow | ExNoActivate);

    public static void ClickThrough(Window window) => AddExStyle(window, ExTransparent | ExToolWindow | ExNoActivate);

    public static void ToolWindow(Window window) => AddExStyle(window, ExToolWindow);

    public static void Foreground(Window window)
    {
        var handle = Handle(window);
        if (handle != IntPtr.Zero)
            SetForegroundWindow(handle);
    }

    public static bool FullscreenAppActive()
    {
        try
        {
            return SHQueryUserNotificationState(out var state) == 0 && state is 2 or 3 or 4;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void BringToFront(Window window)
    {
        var handle = Handle(window);
        if (handle == IntPtr.Zero)
            return;

        try
        {
            ShowWindow(handle, ShowRestore);
            var foreground = GetForegroundWindow();
            var foregroundThread = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);
            var thisThread = GetCurrentThreadId();
            var attached = foregroundThread != 0 && foregroundThread != thisThread && AttachThreadInput(thisThread, foregroundThread, true);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
            if (attached)
                AttachThreadInput(thisThread, foregroundThread, false);
        }
        catch (Exception)
        {
        }
    }

    public static bool ExcludeFromCapture(Window? window, bool exclude)
    {
        var handle = Handle(window);
        if (handle == IntPtr.Zero)
            return false;

        try
        {
            return SetWindowDisplayAffinity(handle, exclude ? 0x11u : 0u);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static IntPtr Handle(Window? window) => window?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

    private static void AddExStyle(Window window, int flags)
    {
        var handle = Handle(window);
        if (handle == IntPtr.Zero)
            return;

        try
        {
            SetWindowLong(handle, ExStyleIndex, GetWindowLong(handle, ExStyleIndex) | flags);
        }
        catch (Exception)
        {
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attach, uint attachTo, bool on);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int state);
}
