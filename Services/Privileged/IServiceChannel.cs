namespace Tunnelka.Services.Privileged;

public interface IServiceChannel
{
    bool IsAvailable { get; }

    ServiceResponse? Send(ServiceRequest request);

    void Forget();
}
