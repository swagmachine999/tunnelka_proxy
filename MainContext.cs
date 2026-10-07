using Tunnelka.Storage;
using Tunnelka.UI;

namespace Tunnelka;

internal sealed class MainContext : ApplicationContext
{
    public MainContext(bool connect, bool minimized)
    {
        ApplySettings(DisplayScale.Primary());
        MainForm = Attach(new MainForm(connect, startHidden: minimized));
    }

    private static void ApplySettings(float display)
    {
        var data = AppStorage.Load();
        Theme.SetScale(display, data.UiScale / 100f);
        Theme.Use(data.DarkTheme);
        L.Use(data.Language);
    }

    private MainForm Attach(MainForm form)
    {
        form.ReloadRequested += (_, _) => Replace(form);
        return form;
    }

    private void Replace(MainForm old)
    {
        var bounds = old.WindowState == FormWindowState.Normal ? old.Bounds : old.RestoreBounds;
        var state = old.WindowState;
        var reconnect = old.PrepareForReplace();

        ApplySettings(DisplayScale.Of(old));
        var next = Attach(new MainForm(reconnect, old.CurrentPage, bounds, state));
        MainForm = next;
        next.Show();
        old.Close();
    }
}
