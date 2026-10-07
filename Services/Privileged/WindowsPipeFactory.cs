using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Tunnelka.Services.Privileged;

[SupportedOSPlatform("windows")]
public sealed class WindowsPipeFactory : IPipeFactory
{
    private const int MaxInstances = 16;

    private readonly string _name;

    public WindowsPipeFactory(string name)
    {
        _name = name;
    }

    public NamedPipeServerStream Create(bool first)
    {
        var options = PipeOptions.Asynchronous | (first ? PipeOptions.FirstPipeInstance : PipeOptions.None);
        return NamedPipeServerStreamAcl.Create(_name, PipeDirection.InOut, MaxInstances, PipeTransmissionMode.Byte, options, 0, 0, Security());
    }

    private static PipeSecurity Security()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.InteractiveSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        return security;
    }
}
