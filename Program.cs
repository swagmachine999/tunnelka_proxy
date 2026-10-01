using VpnClient.Storage;
using VpnClient.UI;

namespace VpnClient;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainContext(args.Contains("--connect")));
    }
}

internal sealed class MainContext : ApplicationContext
{
    public MainContext(bool connect)
    {
        Theme.SetScale(AppStorage.Load().UiScale / 100f);
        MainForm = Attach(new MainForm(connect));
    }

    private MainForm Attach(MainForm form)
    {
        form.ScaleChangeRequested += (_, _) => Replace(form);
        return form;
    }

    private void Replace(MainForm old)
    {
        var bounds = old.WindowState == FormWindowState.Normal ? old.Bounds : old.RestoreBounds;
        var state = old.WindowState;
        var reconnect = old.PrepareForReplace();

        Theme.SetScale(AppStorage.Load().UiScale / 100f);
        var next = Attach(new MainForm(reconnect, true, bounds, state));
        MainForm = next;
        next.Show();
        old.Close();
    }
}
