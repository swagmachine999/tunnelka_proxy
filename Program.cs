using Tunnelka.Services;
using Tunnelka.Storage;

namespace Tunnelka;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        L.Use(AppStorage.Load().Language);

        using var instance = new Mutex(false, "Tunnelka.SingleInstance");
        if (!Acquire(instance, args.Contains("--connect") ? 10000 : 0))
        {
            MessageBox.Show(L.T("Tunnelka уже запущена. Её значок — рядом с часами."), "Tunnelka", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowError(e.Exception);
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
        Application.Run(new MainContext(args.Contains("--connect"), args.Contains("--minimized")));
        instance.ReleaseMutex();
    }

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

    private static void ShowError(Exception exception)
    {
        CrashLog.Write(exception);
        MessageBox.Show(L.F("Что-то пошло не так: {0}\n\nПодробности записаны в {1}", exception.Message, CrashLog.FilePath),
            "Tunnelka", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
