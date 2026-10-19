using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Tunnelka.Services;

public static class DeviceInfo
{
    private static string? _hwid;

    public static bool Enabled { get; set; } = true;

    public static string Hwid => _hwid ??= BuildHwid(ReadMachineId());

    public static string Name => Ascii(Environment.MachineName, "Windows PC");

    public static string OsVersion => Environment.OSVersion.Version.ToString();

    public static void Apply(HttpRequestHeaders headers)
    {
        if (!Enabled)
            return;

        headers.TryAddWithoutValidation("x-hwid", Hwid);
        headers.TryAddWithoutValidation("x-device-os", "Windows");
        headers.TryAddWithoutValidation("x-ver-os", OsVersion);
        headers.TryAddWithoutValidation("x-device-model", Name);
    }

    public static string BuildHwid(string machineId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("Tunnelka|" + machineId));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }

    public static string Ascii(string value, string fallback)
    {
        var clean = new string(value.Where(c => c is >= ' ' and <= '~').ToArray()).Trim();
        return clean.Length == 0 || clean.Length != value.Trim().Length ? fallback : clean;
    }

    private static string ReadMachineId()
    {
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            if (key?.GetValue("MachineGuid") is string guid && guid.Length > 0)
                return guid;
        }
        catch (Exception)
        {
        }

        return Environment.MachineName;
    }
}
