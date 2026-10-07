namespace Tunnelka.Services.Privileged;

public static class ServiceCommand
{
    public const string Ping = "ping";
    public const string Status = "status";
    public const string TunStart = "tun-start";
    public const string TunStop = "tun-stop";
    public const string KillSwitchOn = "killswitch-on";
    public const string KillSwitchOff = "killswitch-off";
}

public sealed class ServiceRequest
{
    public string Command { get; set; } = "";
    public int SocksPort { get; set; }
    public string ServerHost { get; set; } = "";
    public string? PhysicalInterface { get; set; }
    public string Mode { get; set; } = "AllVpn";
    public List<string> Rules { get; set; } = new();
    public long Cursor { get; set; }
}

public sealed class ServiceResponse
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int Protocol { get; set; } = ServiceConstants.ProtocolVersion;
    public string? ServiceExe { get; set; }
    public bool TunRunning { get; set; }
    public int? ExitCode { get; set; }
    public bool KillSwitchOn { get; set; }
    public long Cursor { get; set; }
    public List<string> Lines { get; set; } = new();

    public static ServiceResponse Success() => new() { Ok = true };

    public static ServiceResponse Fail(string error) => new() { Ok = false, Error = error };
}
