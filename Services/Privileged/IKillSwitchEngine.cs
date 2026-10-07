namespace Tunnelka.Services.Privileged;

public interface IKillSwitchEngine
{
    bool IsEngaged { get; }

    bool Engage();

    void Release();
}
