using Avalonia.Controls;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class AboutUpdateFlow
{
    private readonly Session _session;
    private readonly Func<Window?> _owner;

    public AboutUpdateFlow(Session session, Func<Window?> owner)
    {
        _session = session;
        _owner = owner;
    }

    public async Task<string> CheckAsync()
    {
        try
        {
            var update = await UpdateService.CheckAsync(_session.ProxyPort);
            if (update == null)
                return L.F("У вас последняя версия {0}", UpdateService.Current.ToString(3));

            ShowPrompt(update);
            return L.F("Доступна версия {0}", update.Version.ToString(3));
        }
        catch (Exception ex)
        {
            return L.F("Не удалось проверить: {0}", ex.Message);
        }
    }

    private void ShowPrompt(UpdateInfo update)
    {
        var dialog = new PromptDialog(
            L.T("Доступна новая версия"),
            L.F("Tunnelka {0}. Скачать и установить сейчас?", update.Version.ToString(3)),
            L.T("Да"),
            L.T("Нет"),
            _session.Data.UiScale);
        dialog.Declined += () => dialog.Close();
        dialog.Accepted += async () => await Install(dialog, update);
        if (_owner() is { } owner)
            dialog.Show(owner);
        else
            dialog.Show();
    }

    private async Task Install(PromptDialog dialog, UpdateInfo update)
    {
        if (update.DownloadUrl.Length == 0)
        {
            UpdateService.OpenPage(update.PageUrl);
            dialog.Close();
            return;
        }

        try
        {
            dialog.ShowProgress(0);
            var progress = new Progress<int>(dialog.ShowProgress);
            var path = await UpdateService.DownloadAsync(update, _session.ProxyPort, progress);
            UpdateService.RunInstaller(path);
            _session.RequestExit();
        }
        catch (Exception ex)
        {
            dialog.ShowError(L.F("Не удалось обновить: {0}", ex.Message));
        }
    }
}
