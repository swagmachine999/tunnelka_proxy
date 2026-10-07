using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Tunnelka.Services.Privileged;

[SupportedOSPlatform("windows")]
public static class SecureDirectory
{
    public static void Ensure(string path)
    {
        Directory.CreateDirectory(path);

        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);
        var inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        foreach (var sid in new[] { WellKnownSidType.LocalSystemSid, WellKnownSidType.BuiltinAdministratorsSid })
        {
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid, null), FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        }

        new DirectoryInfo(path).SetAccessControl(security);
    }
}
