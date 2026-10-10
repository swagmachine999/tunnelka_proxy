using System.Runtime.InteropServices;

namespace Tunnelka.UI;

public static class NativeTheme
{
    private const int ImmersiveDarkMode = 20;
    private const int CornerPreference = 33;
    private const int BorderColor = 34;
    private const int CaptionColor = 35;
    private const int TextColor = 36;
    private const int RoundCorners = 2;

    public static void TitleBar(Form form, bool dark)
    {
        try
        {
            Set(form, ImmersiveDarkMode, dark ? 1 : 0);
            Set(form, CornerPreference, RoundCorners);
            Set(form, CaptionColor, ColorRef(Theme.Sidebar));
            Set(form, TextColor, ColorRef(Theme.Text));
            Set(form, BorderColor, ColorRef(Theme.Border));
        }
        catch (Exception)
        {
        }
    }

    private static void Set(Form form, int attribute, int value) =>
        DwmSetWindowAttribute(form.Handle, attribute, ref value, sizeof(int));

    private static int ColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    public static bool ExcludeFromCapture(Form form, bool exclude)
    {
        try
        {
            return SetWindowDisplayAffinity(form.Handle, exclude ? 0x11u : 0u);
        }
        catch (Exception)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    public static void Scrollbars(Control control, bool dark)
    {
        try
        {
            SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
        }
        catch (Exception)
        {
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? appName, string? idList);
}
