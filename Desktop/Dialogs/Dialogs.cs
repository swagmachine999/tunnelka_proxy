using Avalonia.Controls;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class Dialogs : IDialogs
{
    private readonly Window _owner;
    private readonly TrayService _tray;

    public Dialogs(Window owner, TrayService tray)
    {
        _owner = owner;
        _tray = tray;
        SyncScale();
        tray.Session.ScaleChanged += SyncScale;
    }

    public Task<bool> AskCoreDownload(IReadOnlyList<CorePackage> missing) => CoreDownloadDialog.Ask(_owner, missing);

    public void ShowMessage(string text) => _ = MessageDialog.Show(_owner, text);

    public Task<bool> AskYesNo(string text) => MessageDialog.Ask(_owner, text);

    public void Balloon(string text) => _tray.Balloon(text);

    private void SyncScale() => DialogScale.Percent = _tray.Session.Data.UiScale;
}
