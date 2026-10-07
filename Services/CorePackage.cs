namespace Tunnelka.Services;

public sealed record CorePackage(string Name, string Url, string Sha256, long Size, string Executable, IReadOnlyList<(string Entry, string Target)> Files);

public static class CoreManifest
{
    private const string XrayVersion = "v26.9.30";
    private const string SingBoxVersion = "1.14.2";

    public static CorePackage Xray { get; } = new(
        "Xray",
        $"https://github.com/XTLS/Xray-core/releases/download/{XrayVersion}/Xray-windows-64.zip",
        "b17a619343c11b89d8faf36278298856749b15c52277d875d32051b33dcda617",
        21_392_686,
        "xray.exe",
        new[]
        {
            ("xray.exe", "xray.exe"),
            ("geoip.dat", "geoip.dat"),
            ("geosite.dat", "geosite.dat"),
            ("LICENSE", "LICENSE-xray.txt")
        });

    public static CorePackage SingBox { get; } = new(
        "sing-box",
        $"https://github.com/SagerNet/sing-box/releases/download/v{SingBoxVersion}/sing-box-{SingBoxVersion}-windows-amd64.zip",
        "c2d8bfff918755808781dfdeeb8581b6c91eb3a243d9a7b55483cfc0c0684d32",
        32_857_903,
        "sing-box.exe",
        new[]
        {
            ("sing-box.exe", "sing-box.exe"),
            ("libcronet.dll", "libcronet.dll"),
            ("LICENSE", "LICENSE-sing-box.txt")
        });

    public static IReadOnlyList<CorePackage> All { get; } = new[] { Xray, SingBox };
}
