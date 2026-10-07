using System.Runtime.InteropServices;

namespace Tunnelka.UI;

public static class DisplayScale
{
    private const float ReferenceHeight = 1080f;
    private const int ReferenceDpi = 96;
    private const int ScrollBarMetric = 2;

    public static float Of(int dpi, int screenHeight) =>
        Math.Max(dpi / (float)ReferenceDpi, screenHeight / ReferenceHeight);

    public static float Primary() =>
        Of(SystemDpi(), Screen.PrimaryScreen?.Bounds.Height ?? (int)ReferenceHeight);

    public static float Of(Control control) =>
        Of(control.DeviceDpi, Screen.FromControl(control).Bounds.Height);

    public static int ScrollBarWidth(Control control)
    {
        try
        {
            var width = GetSystemMetricsForDpi(ScrollBarMetric, (uint)control.DeviceDpi);
            if (width > 0)
                return width;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
        }

        return SystemInformation.VerticalScrollBarWidth;
    }

    private static int SystemDpi()
    {
        try
        {
            var dpi = GetDpiForSystem();
            if (dpi > 0)
                return (int)dpi;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
        }

        return ReferenceDpi;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);
}
