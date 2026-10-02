using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace VpnClient.Services;

public static class SystemProxy
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private const string Bypass = "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.2*;172.30.*;172.31.*;192.168.*;<local>";

    private const int OptionSettingsChanged = 39;
    private const int OptionRefresh = 37;

    public static void Enable(string address)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true)
            ?? throw new InvalidOperationException(L.T("Не удалось открыть настройки прокси Windows"));

        key.SetValue("ProxyServer", address);
        key.SetValue("ProxyOverride", Bypass);
        key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
        Refresh();
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
        if (key == null)
            return;

        key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
        Refresh();
    }

    private static void Refresh()
    {
        InternetSetOption(IntPtr.Zero, OptionSettingsChanged, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, OptionRefresh, IntPtr.Zero, 0);
    }

    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(IntPtr hInternet, int option, IntPtr buffer, int bufferLength);
}
