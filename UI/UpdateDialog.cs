using Tunnelka.Services;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI;

public sealed class UpdateDialog : Form
{
    private readonly DialogPrompt _prompt;

    private UpdateDialog(UpdateInfo update, Func<int?> proxyPort)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        TopMost = true;
        Text = "Tunnelka";
        Icon = LogoView.CreateAppIcon() ?? Icon;
        ClientSize = new Size(Theme.Px(440), Theme.Px(220));

        _prompt = new DialogPrompt(L.T("Доступна новая версия"),
            L.F("Tunnelka {0}. Скачать и установить сейчас?", update.Version.ToString(3)), L.T("Да"), L.T("Нет")) { Dock = DockStyle.Fill };
        _prompt.Declined += (_, _) => Close();
        _prompt.Accepted += async (_, _) => await Install(update, proxyPort);
        Controls.Add(_prompt);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeTheme.TitleBar(this, Theme.IsDark);
    }

    public static void Show(UpdateInfo update, Func<int?> proxyPort)
    {
        var dialog = new UpdateDialog(update, proxyPort);
        dialog.FormClosed += (_, _) => dialog.Dispose();
        dialog.Show();
        dialog.Activate();
    }

    private async Task Install(UpdateInfo update, Func<int?> proxyPort)
    {
        if (update.DownloadUrl.Length == 0)
        {
            UpdateService.OpenPage(update.PageUrl);
            Close();
            return;
        }

        try
        {
            _prompt.ShowProgress(0);
            var progress = new Progress<int>(_prompt.ShowProgress);
            var path = await UpdateService.DownloadAsync(update, proxyPort(), progress);
            UpdateService.RunInstaller(path);
            Application.Exit();
        }
        catch (Exception ex)
        {
            _prompt.ShowError(L.F("Не удалось обновить: {0}", ex.Message));
        }
    }
}
