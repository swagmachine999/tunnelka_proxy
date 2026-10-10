using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class UpdateDialog : DialogBase
{
    private readonly ConfirmView _view;
    private readonly UpdateInfo _update;
    private readonly Func<int?> _proxyPort;

    private UpdateDialog(UpdateInfo update, Func<int?> proxyPort) : base("Tunnelka", 440)
    {
        _update = update;
        _proxyPort = proxyPort;
        ShowInTaskbar = true;
        Topmost = true;
        _view = new ConfirmView(L.T("Доступна новая версия"),
            L.F("Tunnelka {0}. Скачать и установить сейчас?", update.Version.ToString(3)), L.T("Да"), L.T("Нет"));
        this.Paint(BackgroundProperty, "HeroBrush");
        Content = _view;
        _view.Declined += (_, _) => Close();
        _view.Accepted += async (_, _) => await Install();
    }

    public static void Show(Window? owner, UpdateInfo update, Func<int?> proxyPort)
    {
        var dialog = new UpdateDialog(update, proxyPort);
        _ = dialog.Present(owner, false);
        dialog.Activate();
    }

    private async Task Install()
    {
        if (_update.DownloadUrl.Length == 0)
        {
            UpdateService.OpenPage(_update.PageUrl);
            Close();
            return;
        }

        try
        {
            ShowPercent(0);
            var progress = new Progress<int>(ShowPercent);
            var path = await UpdateService.DownloadAsync(_update, _proxyPort(), progress);
            UpdateService.RunInstaller(path);
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }
        catch (Exception ex)
        {
            _view.ShowError(L.F("Не удалось обновить: {0}", ex.Message));
        }
    }

    private void ShowPercent(int percent) => _view.ShowProgress(percent, L.F("Загрузка {0}%", percent));
}
