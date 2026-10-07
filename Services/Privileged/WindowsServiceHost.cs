using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Tunnelka.Services.Privileged;

[SupportedOSPlatform("windows")]
public sealed class WindowsServiceHost
{
    private const int OwnProcess = 0x10;
    private const int StatePending = 2;
    private const int StateRunning = 4;
    private const int StateStopPending = 3;
    private const int StateStopped = 1;
    private const int AcceptStop = 1;
    private const int AcceptShutdown = 4;
    private const int ControlStop = 1;
    private const int ControlShutdown = 5;

    private static readonly ManualResetEvent StopRequested = new(false);
    private static ServiceMainProc? _main;
    private static HandlerEx? _handler;
    private static IServiceApp? _app;
    private static IntPtr _statusHandle;

    private delegate void ServiceMainProc(int argc, IntPtr argv);

    private delegate int HandlerEx(int control, int eventType, IntPtr eventData, IntPtr context);

    public static int Run(IServiceApp app)
    {
        _app = app;
        _main = ServiceMain;
        _handler = Control;

        var table = new[]
        {
            new ServiceTableEntry { Name = ServiceConstants.ServiceName, Proc = Marshal.GetFunctionPointerForDelegate(_main) },
            new ServiceTableEntry()
        };

        return StartServiceCtrlDispatcher(table) ? 0 : Marshal.GetLastWin32Error();
    }

    private static void ServiceMain(int argc, IntPtr argv)
    {
        _statusHandle = RegisterServiceCtrlHandlerEx(ServiceConstants.ServiceName, _handler!, IntPtr.Zero);
        if (_statusHandle == IntPtr.Zero)
            return;

        Report(StatePending, 0, 1, 10000);
        try
        {
            _app!.Start();
        }
        catch (Exception)
        {
            Report(StateStopped, 1, 0, 0);
            return;
        }

        Report(StateRunning, 0, 0, 0);
        StopRequested.WaitOne();

        Report(StateStopPending, 0, 1, 15000);
        try
        {
            _app.Stop();
        }
        finally
        {
            Report(StateStopped, 0, 0, 0);
        }
    }

    private static int Control(int control, int eventType, IntPtr eventData, IntPtr context)
    {
        if (control is ControlStop or ControlShutdown)
            StopRequested.Set();
        return 0;
    }

    private static void Report(int state, int exitCode, int checkPoint, int waitHint)
    {
        var status = new ServiceStatus
        {
            ServiceType = OwnProcess,
            CurrentState = state,
            ControlsAccepted = state == StateRunning ? AcceptStop | AcceptShutdown : 0,
            Win32ExitCode = exitCode,
            CheckPoint = checkPoint,
            WaitHint = waitHint
        };
        SetServiceStatus(_statusHandle, ref status);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ServiceTableEntry
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Name;
        public IntPtr Proc;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatus
    {
        public int ServiceType;
        public int CurrentState;
        public int ControlsAccepted;
        public int Win32ExitCode;
        public int ServiceSpecificExitCode;
        public int CheckPoint;
        public int WaitHint;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool StartServiceCtrlDispatcher([In] ServiceTableEntry[] table);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr RegisterServiceCtrlHandlerEx(string name, HandlerEx handler, IntPtr context);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus status);
}
