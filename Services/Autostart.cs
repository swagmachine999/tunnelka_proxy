using Microsoft.Win32;

namespace Tunnelka.Services;

public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Name = "Tunnelka";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null)
                return;

            if (enabled && Environment.ProcessPath != null)
                key.SetValue(Name, $"\"{Environment.ProcessPath}\" --minimized");
            else
                key.DeleteValue(Name, false);
        }
        catch (Exception)
        {
        }
    }
}
