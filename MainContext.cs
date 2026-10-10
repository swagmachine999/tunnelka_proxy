using Tunnelka.Storage;
using Tunnelka.UI;

namespace Tunnelka;

internal sealed class MainContext : ApplicationContext
{
    public MainContext(bool connect, bool minimized)
    {
        var data = AppStorage.Load();
        Theme.SetScale(DisplayScale.Primary(), data.UiScale / 100f);
        Theme.Use(data.DarkTheme);
        L.Use(data.Language);
        MainForm = new MainForm(connect, startHidden: minimized);
    }
}
