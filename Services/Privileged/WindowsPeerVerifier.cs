using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Tunnelka.Services.Privileged;

[SupportedOSPlatform("windows")]
public sealed class WindowsPeerVerifier : IPeerVerifier
{
    private const uint QueryLimitedInformation = 0x1000;

    private readonly string _trustedPath;

    public WindowsPeerVerifier(string trustedPath)
    {
        _trustedPath = Path.GetFullPath(trustedPath);
    }

    public bool TryGetTrustedProcess(NamedPipeServerStream pipe, out int processId)
    {
        processId = 0;
        if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle.DangerousGetHandle(), out var pid))
            return false;

        var path = ImagePath(pid);
        if (path == null || !string.Equals(Path.GetFullPath(path), _trustedPath, StringComparison.OrdinalIgnoreCase))
            return false;

        processId = (int)pid;
        return true;
    }

    private static string? ImagePath(uint pid)
    {
        var handle = OpenProcess(QueryLimitedInformation, false, pid);
        if (handle == IntPtr.Zero)
            return null;

        try
        {
            var builder = new StringBuilder(1024);
            var size = builder.Capacity;
            return QueryFullProcessImageName(handle, 0, builder, ref size) ? builder.ToString(0, size) : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetNamedPipeClientProcessId(IntPtr pipe, out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder name, ref int size);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);
}
