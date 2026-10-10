using Avalonia;
using Avalonia.Controls;

namespace Tunnelka.Desktop;

internal static class SettingsTheme
{
    public static IDisposable Paint(Control control, AvaloniaProperty property, string key) =>
        control.Bind(property, control.GetResourceObservable(key));
}
