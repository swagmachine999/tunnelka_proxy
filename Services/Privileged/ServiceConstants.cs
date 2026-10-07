namespace Tunnelka.Services.Privileged;

public static class ServiceConstants
{
    public const string ServiceName = "TunnelkaService";
    public const string DisplayName = "Tunnelka Service";
    public const string PipeName = "TunnelkaService";
    public const string RunArgument = "--service";
    public const int ProtocolVersion = 1;

    public static string DataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tunnelka");

    public static string ConfigDir => Path.Combine(DataDir, "config");

    public static string ConfigPath => Path.Combine(ConfigDir, "tun.json");

    public static string LogDir => Path.Combine(DataDir, "logs");

    public static string LogPath => Path.Combine(LogDir, "service.log");

    public static string InstalledCore => Path.Combine(AppContext.BaseDirectory, "core");

    public static string SingBoxPath => Path.Combine(InstalledCore, "sing-box.exe");

    public static string XrayPath => Path.Combine(InstalledCore, "xray.exe");
}
