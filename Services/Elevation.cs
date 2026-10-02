using System.Diagnostics;
using System.Security.Principal;

namespace VpnClient.Services;

public static class Elevation
{
    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void RestartElevated(string arguments)
    {
        var path = Environment.ProcessPath ?? throw new InvalidOperationException("Не удалось определить путь к программе");
        Process.Start(new ProcessStartInfo(path, arguments)
        {
            UseShellExecute = true,
            Verb = "runas"
        });
    }
}
