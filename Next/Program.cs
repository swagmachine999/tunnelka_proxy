using Avalonia;
using Tunnelka.Services;
using Tunnelka.Services.Privileged;
using Tunnelka.Storage;

namespace Tunnelka.Next;

internal static class Program
{
    public static bool Connect { get; private set; }

    public static bool Minimized { get; private set; }

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains(ServiceConstants.RunArgument))
        {
            WindowsServiceHost.Run(new TunnelkaServiceApp());
            return;
        }

        if (args.Contains("--cleanup"))
        {
            ConnectionService.CleanUpAfterCrash();
            Autostart.Apply(false);
            return;
        }

        L.Use(AppStorage.Load().Language);
        Connect = args.Contains("--connect");
        Minimized = args.Contains("--minimized");

        using var instance = new Mutex(false, "Tunnelka.SingleInstance");
        if (!Acquire(instance, Connect || args.Contains("--elevated") ? 10000 : 0))
        {
            Trace.Write("mutex busy");
            Console.Error.WriteLine(L.T("Tunnelka уже запущена. Её значок — рядом с часами."));
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            CrashLog.Write(e.ExceptionObject as Exception);
            ConnectionService.CleanUpAfterCrash();
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLog.Write(e.Exception);
            e.SetObserved();
        };

        ConnectionService.CleanUpAfterCrash();
        Trace.Write("start " + string.Join(' ', args));
        try
        {
            BuildApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
            Console.Error.WriteLine(ex);
            throw;
        }
        finally
        {
            Trace.Write("main end");
            instance.ReleaseMutex();
        }
    }

    public static AppBuilder BuildApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();

    private static bool Acquire(Mutex mutex, int timeoutMs)
    {
        try
        {
            return mutex.WaitOne(timeoutMs);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }
}
