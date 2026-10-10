using Avalonia.Controls;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class CoreDownloadDialog : DialogBase
{
    private readonly ConfirmView _view;
    private readonly IReadOnlyList<CorePackage> _packages;
    private readonly CancellationTokenSource _cancel = new();
    private bool _result;

    private CoreDownloadDialog(IReadOnlyList<CorePackage> packages) : base("Tunnelka", 460)
    {
        _packages = packages;
        var names = string.Join(", ", packages.Select(p => p.Name));
        var megabytes = Math.Max(1, (int)(packages.Sum(p => p.Size) / (1024 * 1024)));
        _view = new ConfirmView(L.T("Скачать файлы VPN?"),
            L.F("Не хватает файлов для работы VPN: {0}. Скачать их с официальных страниц разработчиков на GitHub (около {1} МБ)? Подлинность проверяется контрольной суммой.", names, megabytes),
            L.T("Скачать"), L.T("Не сейчас"));
        this.Paint(BackgroundProperty, "HeroBrush");
        Content = _view;
        _view.Declined += (_, _) => Close();
        _view.Accepted += async (_, _) => await Download();
        Closed += (_, _) =>
        {
            _cancel.Cancel();
            _cancel.Dispose();
        };
    }

    public static async Task<bool> Ask(Window? owner, IReadOnlyList<CorePackage> missing)
    {
        var dialog = new CoreDownloadDialog(missing);
        await dialog.Present(owner);
        return dialog._result;
    }

    private async Task Download()
    {
        try
        {
            var downloader = new CoreDownloader();
            for (var i = 0; i < _packages.Count; i++)
            {
                var index = i;
                var progress = new Progress<int>(percent => ShowPercent((index * 100 + percent) / _packages.Count));
                ShowPercent(index * 100 / _packages.Count);
                await downloader.DownloadAsync(_packages[i], progress, _cancel.Token);
            }

            _result = true;
            Close();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _view.ShowError(L.F("Не удалось скачать: {0}", ex.Message));
        }
    }

    private void ShowPercent(int percent) => _view.ShowProgress(percent, L.F("Загрузка {0}%", percent));
}
