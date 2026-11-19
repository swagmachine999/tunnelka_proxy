using System.Net.Http.Headers;

namespace Tunnelka.Services;

public static class DeviceInfo
{
    public static string Platform => OperatingSystem.IsWindows() ? "Windows"
        : OperatingSystem.IsMacOS() ? "macOS"
        : OperatingSystem.IsAndroid() ? "Android"
        : OperatingSystem.IsIOS() ? "iOS"
        : OperatingSystem.IsLinux() ? "Linux"
        : "Other";

    public static string UserAgent => "Tunnelka/" + UpdateService.Current;

    public static void Apply(HttpRequestHeaders headers)
    {
        headers.TryAddWithoutValidation("x-device-os", Platform);
        headers.TryAddWithoutValidation("x-device-model", Platform);
    }
}
