namespace Tunnelka.Services.Privileged;

public interface IServiceHandler
{
    ServiceResponse Handle(ServiceRequest request, int callerProcessId);
}

public sealed class ServiceDispatcher : IServiceHandler
{
    private readonly ITunSupervisor _tun;
    private readonly IKillSwitchEngine _killSwitch;
    private readonly Func<bool> _killSwitchLeftOn;
    private readonly Action<string> _log;
    private readonly string _servicePath;
    private readonly object _gate = new();

    public ServiceDispatcher(ITunSupervisor tun, IKillSwitchEngine killSwitch, Func<bool> killSwitchLeftOn, string servicePath, Action<string> log)
    {
        _tun = tun;
        _killSwitch = killSwitch;
        _killSwitchLeftOn = killSwitchLeftOn;
        _servicePath = servicePath;
        _log = log;
    }

    public ServiceResponse Handle(ServiceRequest request, int callerProcessId)
    {
        lock (_gate)
        {
            try
            {
                return request.Command switch
                {
                    ServiceCommand.Ping => Describe(ServiceResponse.Success()),
                    ServiceCommand.Status => Status(request.Cursor),
                    ServiceCommand.TunStart => StartTun(request, callerProcessId),
                    ServiceCommand.TunStop => StopTun(),
                    ServiceCommand.KillSwitchOn => KillSwitch(true),
                    ServiceCommand.KillSwitchOff => KillSwitch(false),
                    _ => ServiceResponse.Fail("unknown command")
                };
            }
            catch (Exception ex)
            {
                _log($"{request.Command} failed: {ex.Message}");
                return ServiceResponse.Fail(ex.Message);
            }
        }
    }

    private ServiceResponse Describe(ServiceResponse response)
    {
        response.ServiceExe = _servicePath;
        response.TunRunning = _tun.IsRunning;
        response.ExitCode = _tun.ExitCode;
        response.KillSwitchOn = _killSwitch.IsEngaged || _killSwitchLeftOn();
        return response;
    }

    private ServiceResponse Status(long cursor)
    {
        var (next, lines) = _tun.Read(cursor);
        var response = Describe(ServiceResponse.Success());
        response.Cursor = next;
        response.Lines = lines;
        return response;
    }

    private ServiceResponse StartTun(ServiceRequest request, int callerProcessId)
    {
        if (!TunRequestValidator.TryValidate(request, out var parameters, out var error))
        {
            _log($"tun-start rejected: {error}");
            return ServiceResponse.Fail(error);
        }

        _tun.Start(parameters!, callerProcessId);
        var response = Describe(ServiceResponse.Success());
        response.Cursor = _tun.Cursor;
        return response;
    }

    private ServiceResponse StopTun()
    {
        _tun.Stop();
        return Describe(ServiceResponse.Success());
    }

    private ServiceResponse KillSwitch(bool on)
    {
        if (on)
        {
            if (!_killSwitch.Engage())
                return ServiceResponse.Fail("kill switch not engaged");
        }
        else
        {
            _killSwitch.Release();
        }

        return Describe(ServiceResponse.Success());
    }
}
