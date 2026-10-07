using Tunnelka.Services;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI;

public sealed class CoreDownloadDialog : Form
{
    private readonly DialogPrompt _prompt;
    private readonly IReadOnlyList<CorePackage> _packages;
    private readonly CancellationTokenSource _cancel = new();

    private CoreDownloadDialog(IReadOnlyList<CorePackage> packages)
    {
        _packages = packages;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Text = "Tunnelka";
        Icon = LogoView.CreateAppIcon() ?? Icon;
        ClientSize = new Size(Theme.Px(460), Theme.Px(270));

        var names = string.Join(", ", packages.Select(p => p.Name));
        var megabytes = Math.Max(1, (int)(packages.Sum(p => p.Size) / (1024 * 1024)));
        _prompt = new DialogPrompt(L.T("Скачать файлы VPN?"),
            L.F("Не хватает файлов для работы VPN: {0}. Скачать их с официальных страниц разработчиков на GitHub (около {1} МБ)? Подлинность проверяется контрольной суммой.", names, megabytes),
            L.T("Скачать"), L.T("Не сейчас")) { Dock = DockStyle.Fill };
        _prompt.Declined += (_, _) => Close();
        _prompt.Accepted += async (_, _) => await Download();
        Controls.Add(_prompt);
        FormClosing += (_, _) => _cancel.Cancel();
        FormClosed += (_, _) => _cancel.Dispose();
    }

    public static bool Ask(IWin32Window owner, IReadOnlyList<CorePackage> packages)
    {
        using var dialog = new CoreDownloadDialog(packages);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeTheme.TitleBar(this, Theme.IsDark);
    }

    private async Task Download()
    {
        try
        {
            var downloader = new CoreDownloader();
            for (var i = 0; i < _packages.Count; i++)
            {
                var index = i;
                var progress = new Progress<int>(percent => _prompt.ShowProgress((index * 100 + percent) / _packages.Count));
                _prompt.ShowProgress(index * 100 / _packages.Count);
                await downloader.DownloadAsync(_packages[i], progress, _cancel.Token);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _prompt.ShowError(L.F("Не удалось скачать: {0}", ex.Message));
        }
    }
}
