namespace Tunnelka.Services;

public sealed record CorePackage(string Name, string Url, string Sha256, long Size, string Executable, IReadOnlyList<(string Entry, string Target)> Files);

public static class CoreManifest
{
    private const string XrayVersion = "v26.10.21";
    private const string SingBoxVersion = "1.14.3";

    public static string XrayDisplayVersion => XrayVersion.TrimStart('v');
    public static string SingBoxDisplayVersion => SingBoxVersion;

    public static CorePackage Xray { get; } = new(
        "Xray",
        $"https://github.com/XTLS/Xray-core/releases/download/{XrayVersion}/Xray-windows-64.zip",
        "fc58c39ee2f2717b4c54d28d374362389e5319b20ae44aea76ea45a5243ec827",
        15_029_603,
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
        $"https://github.com/SagerNet/sing-box/releases/download/v{SingBoxVersion}/sing-box-{SingBoxVersion}-windows-amd64.zip",
        "d8b86ef58936dba9230ec6a57ffa4243933f3c14ef4809828b6c553440ed1518",
        32_853_953,
        CoreFiles.Tun,
        new[]
        {
            ("sing-box.exe", CoreFiles.Tun),
            ("libcronet.dll", "libcronet.dll"),
            ("LICENSE", "LICENSE-sing-box.txt")
        });

    public static IReadOnlyList<CorePackage> All { get; } = new[] { Xray, SingBox };
}
