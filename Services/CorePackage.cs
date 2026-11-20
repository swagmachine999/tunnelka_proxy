namespace Tunnelka.Services;

public sealed record CorePackage(string Name, string Version, string Url, string Sha256, long Size, string Executable, IReadOnlyList<(string Entry, string Target)> Files);

public static class CoreManifest
{
    private const string XrayVersion = "v26.11.9";
    private const string SingBoxVersion = "1.14.4";

    public static string XrayDisplayVersion => XrayVersion.TrimStart('v');
    public static string SingBoxDisplayVersion => SingBoxVersion;

    public static CorePackage Xray { get; } = new(
        "Xray",
        XrayDisplayVersion,
        $"https://github.com/XTLS/Xray-core/releases/download/{XrayVersion}/Xray-windows-64.zip",
        "1262522266337b824f07beb8e1375786b437683b3890105469088f8e17e0a69d",
        16_697_596,
        CoreFiles.Proxy,
        new[]
        {
            ("xray.exe", CoreFiles.Proxy),
            ("geoip.dat", "geoip.dat"),
            ("geosite.dat", "geosite.dat"),
            ("LICENSE", "LICENSE-xray.txt")
        });

    public static CorePackage SingBox { get; } = new(
        "sing-box",
        SingBoxVersion,
        $"https://github.com/SagerNet/sing-box/releases/download/v{SingBoxVersion}/sing-box-{SingBoxVersion}-windows-amd64.zip",
        "99185d75d314a07dbba4a130c8ad986cbeb629a07927975636b341ca61f991df",
        32_891_280,
        CoreFiles.Tun,
        new[]
        {
            ("sing-box.exe", CoreFiles.Tun),
            ("libcronet.dll", "libcronet.dll"),
            ("LICENSE", "LICENSE-sing-box.txt")
        });

    public static IReadOnlyList<CorePackage> All { get; } = new[] { Xray, SingBox };
}
