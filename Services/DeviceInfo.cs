using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Tunnelka.Storage;

namespace Tunnelka.Services;

public static class DeviceInfo
{
    private static readonly Lazy<string> SavedHwid = new(() => LoadOrCreateHwid(Path.Combine(AppStorage.Folder, "device-id.dat")));

    public static string Platform => OperatingSystem.IsWindows() ? "Windows"
        : OperatingSystem.IsMacOS() ? "macOS"
        : OperatingSystem.IsAndroid() ? "Android"
        : OperatingSystem.IsIOS() ? "iOS"
        : OperatingSystem.IsLinux() ? "Linux"
        : "Other";

    public static string UserAgent => "Tunnelka/" + UpdateService.Current;

    public static string Hwid => SavedHwid.Value;

    public static void Apply(HttpRequestHeaders headers, bool sendHwid) => Apply(headers, sendHwid ? Hwid : null);

    internal static void Apply(HttpRequestHeaders headers, string? hwid)
    {
        if (hwid != null)
            headers.TryAddWithoutValidation("x-hwid", hwid);
        headers.TryAddWithoutValidation("x-device-os", Platform);
        headers.TryAddWithoutValidation("x-device-model", Platform);
    }

    public static string LoadOrCreateHwid(string path)
    {
        if (File.Exists(path))
            return Read(path);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp, Dpapi.Protect(Encoding.ASCII.GetBytes(id)));
            try
            {
                File.Move(temp, path);
                return id;
            }
            catch (IOException) when (File.Exists(path))
            {
                return Read(path);
            }
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static string Read(string path)
    {
        var id = Encoding.ASCII.GetString(Dpapi.Unprotect(File.ReadAllBytes(path)));
        if (id.Length != 32 || !id.All(Uri.IsHexDigit))
            throw new InvalidDataException("Saved device ID is invalid");
        return id;
    }
}
