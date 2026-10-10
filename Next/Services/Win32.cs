using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace Tunnelka.Next;

internal static class Win32
{
    private const int ExStyleIndex = -20;
    private const int ExTransparent = 0x20;
    private const int ExToolWindow = 0x80;
    private const int ExNoActivate = 0x8000000;

    public static void NoActivate(Window window) => AddExStyle(window, ExToolWindow | ExNoActivate);

    public static void ClickThrough(Window window) => AddExStyle(window, ExTransparent | ExToolWindow | ExNoActivate);

    public static void ToolWindow(Window window) => AddExStyle(window, ExToolWindow);

    public static void Foreground(Window window)
    {
        var handle = Handle(window);
        if (handle != IntPtr.Zero)
            SetForegroundWindow(handle);
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
}
