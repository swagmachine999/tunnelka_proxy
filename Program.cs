using VpnClient.Storage;
using VpnClient.UI;

namespace VpnClient;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var reconnect = false;
        var openSettings = false;
        Rectangle? bounds = null;

        while (true)
        {
            Theme.SetScale(AppStorage.Load().UiScale / 100f);
            using var form = new MainForm(reconnect, openSettings, bounds);
            Application.Run(form);

            if (!form.RestartRequested)
                break;

            reconnect = form.WasConnected;
            openSettings = true;
            bounds = form.Bounds;
        }
    }
}
