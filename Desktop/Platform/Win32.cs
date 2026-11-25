using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;

namespace Tunnelka.Desktop;

internal static class Win32
{
    private const int ExStyleIndex = -20;
    private const int ExTransparent = 0x20;
    private const int ExToolWindow = 0x80;
    private const int ExNoActivate = 0x8000000;

    public static void NoActivate(Window window) => AddExStyle(window, ExToolWindow | ExNoActivate);

    public static void ClickThrough(Window window) => AddExStyle(window, ExTransparent | ExToolWindow | ExNoActivate);

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
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero || !IsWindowVisible(foreground) || IsIconic(foreground))
                return false;

            GetWindowThreadProcessId(foreground, out var processId);
            if (processId == Environment.ProcessId)
                return false;

            var className = new StringBuilder(64);
            GetClassName(foreground, className, className.Capacity);
            if (className.ToString() is "Progman" or "WorkerW")
                return false;

            if (SHQueryUserNotificationState(out var state) == 0 && state is 2 or 3 or 4)
                return true;

            var monitor = MonitorFromWindow(foreground, 2);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info) || !GetWindowRect(foreground, out var bounds))
                return false;

            const int tolerance = 2;
            return bounds.Left <= info.Monitor.Left + tolerance && bounds.Top <= info.Monitor.Top + tolerance
                && bounds.Right >= info.Monitor.Right - tolerance && bounds.Bottom >= info.Monitor.Bottom - tolerance;
        }
        catch (Exception)
        {
            return false;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

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
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int maxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int state);
}
