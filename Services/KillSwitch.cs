using Tunnelka.Services.Privileged;

namespace Tunnelka.Services;

public sealed class KillSwitch
{
    private readonly AppLog _log;
    private readonly IServiceChannel _service;
    private readonly FirewallKillSwitch _local;
    private readonly FirewallKillSwitch _machine;
    private bool _viaService;

    public KillSwitch(AppLog log)
        : this(log, ServiceClient.Default)
    {
    }

    public KillSwitch(AppLog log, IServiceChannel service)
    {
        _log = log;
        _service = service;
        _local = new FirewallKillSwitch(RegistryKillSwitchMarker.CurrentUser, LocalPrograms, log.Write);
        _machine = new FirewallKillSwitch(RegistryKillSwitchMarker.LocalMachine, LocalPrograms, log.Write);
    }

    public bool IsEngaged => _viaService || _local.IsEngaged;

    public static bool CanRelease => Elevation.IsAdministrator() || ServiceClient.Default.IsAvailable;

    public bool Engage()
    {
        if (IsEngaged)
            return true;

        if (_service.IsAvailable)
        {
            var reply = _service.Send(new ServiceRequest { Command = ServiceCommand.KillSwitchOn });
            if (reply is { Ok: true })
            {
                _viaService = true;
                _log.Write(L.T("Kill switch включён: без VPN интернет заблокирован"));
                return true;
            }

            _log.Write(L.T("Kill switch: служба Tunnelka не включила блокировку"));
            return false;
        }

        return _local.Engage();
    }

    public void Release()
    {
        if (_service.IsAvailable)
        {
            var reply = _service.Send(new ServiceRequest { Command = ServiceCommand.KillSwitchOff });
            if (reply is { Ok: true } && _viaService)
                _log.Write(L.T("Kill switch выключен: интернет снова работает без VPN"));
            _viaService = false;
        }

        if (_local.WasLeftOn || _local.IsEngaged)
            _local.Release();

        if (_machine.WasLeftOn && Elevation.IsAdministrator())
            _machine.Release();
    }

    public static bool WasLeftOn()
    {
        if (RegistryKillSwitchMarker.CurrentUser.Read() != null || RegistryKillSwitchMarker.LocalMachine.Read() != null)
            return true;

        var client = ServiceClient.Default;
        return client.IsAvailable && client.Send(new ServiceRequest { Command = ServiceCommand.Status })?.KillSwitchOn == true;
    }

    private static IEnumerable<string> LocalPrograms() =>
        new[] { XrayRunner.XrayPath, XrayRunner.SingBoxPath, Environment.ProcessPath ?? "" };
}
