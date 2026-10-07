using System.Runtime.Versioning;

namespace Tunnelka.Services.Privileged;

[SupportedOSPlatform("windows")]
public sealed class TunnelkaServiceApp : IServiceApp
{
    private readonly ServiceLog _log = new(ServiceConstants.LogPath);
    private XrayRunner? _runner;
    private TunSupervisor? _tun;
    private FirewallKillSwitch? _killSwitch;
    private PipeServer? _pipe;

    public void Start()
    {
        SecureDirectory.Ensure(ServiceConstants.DataDir);
        Directory.CreateDirectory(ServiceConstants.ConfigDir);
        Directory.CreateDirectory(ServiceConstants.LogDir);
        XrayRunner.KillOrphans(ServiceConstants.SingBoxPath);

        var ownPath = Environment.ProcessPath ?? throw new InvalidOperationException("process path");
        _runner = new XrayRunner(ServiceConstants.SingBoxPath, "tun.json", ServiceConstants.ConfigDir);
        _tun = new TunSupervisor(_runner, new ProcessOwnerMonitor(), _log.Write);

        var marker = RegistryKillSwitchMarker.LocalMachine;
        _killSwitch = new FirewallKillSwitch(marker, () => new[] { ServiceConstants.XrayPath, ServiceConstants.SingBoxPath, ownPath }, _log.Write);

        var dispatcher = new ServiceDispatcher(_tun, _killSwitch, () => _killSwitch.WasLeftOn, ownPath, _log.Write);
        _pipe = new PipeServer(ServiceConstants.PipeName, new WindowsPipeFactory(ServiceConstants.PipeName), new WindowsPeerVerifier(ownPath), dispatcher, _log.Write);
        _pipe.Start();
        _log.Write("service started");
    }

    public void Stop()
    {
        _pipe?.Dispose();
        _tun?.Stop();
        _killSwitch?.Release();
        _runner?.Dispose();
        _log.Write("service stopped");
    }
}
