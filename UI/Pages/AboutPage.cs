using System.Diagnostics;
using Tunnelka.Services;
using Tunnelka.Storage;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class AboutPage : Panel
{
    private readonly Func<int?> _proxyPort;
    private readonly Button _updateButton = PageParts.Button(L.T("Проверить"), true);
    private readonly SettingRow _updateRow;
    private UpdateInfo? _update;
    private bool _working;

    public AboutPage(Func<int?> proxyPort)
    {
        _proxyPort = proxyPort;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        _updateButton.Size = new Size(Theme.Px(150), Theme.Px(36));
        _updateButton.Click += async (_, _) => await OnUpdateClick();
        _updateRow = new SettingRow(L.T("Обновления"), L.T("Нажмите, чтобы проверить новую версию"), _updateButton);

        var releases = new SettingRow(L.T("Все версии"), L.T("Страница загрузок на GitHub"), chevron: true);
        releases.Click += (_, _) => Open(UpdateService.ReleasesPage);
        var licenses = new SettingRow(L.T("Лицензии"), L.T("Xray, sing-box, шрифты и иконки"), chevron: true);
        licenses.Click += (_, _) => Open(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md"));
        var data = new SettingRow(L.T("Папка с данными"), AppStorage.Folder, chevron: true);
        data.Click += (_, _) => Open(AppStorage.Folder);

        Controls.Add(data);
        Controls.Add(licenses);
        Controls.Add(releases);
        Controls.Add(_updateRow);
        Controls.Add(new AboutCard());
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(PageParts.Title(L.T("О приложении")));
    }

    private async Task OnUpdateClick()
    {
        if (_working)
            return;

        _working = true;
        _updateButton.Enabled = false;
        try
        {
            if (_update == null)
                await Check();
            else
                await Install(_update);
        }
        finally
        {
            _working = false;
            if (!IsDisposed)
                _updateButton.Enabled = true;
        }
    }

    private async Task Check()
    {
        ShowStatus(L.T("Проверяю…"));
        try
        {
            _update = await UpdateService.CheckAsync(_proxyPort());
            if (_update == null)
            {
                ShowStatus(L.F("У вас последняя версия {0}", UpdateService.Current.ToString(3)));
                return;
            }

            ShowStatus(L.F("Доступна версия {0}", _update.Version.ToString(3)));
            _updateButton.Text = L.T("Обновить");
        }
        catch (Exception ex)
        {
            ShowStatus(L.F("Не удалось проверить: {0}", ex.Message));
        }
    }

    private async Task Install(UpdateInfo update)
    {
        if (update.DownloadUrl.Length == 0)
        {
            Open(update.PageUrl);
            return;
        }

        try
        {
            var progress = new Progress<int>(percent => ShowStatus(L.F("Загрузка {0}%", percent)));
            var path = await UpdateService.DownloadAsync(update, _proxyPort(), progress);
            ShowStatus(L.T("Устанавливаю…"));
            UpdateService.RunInstaller(path);
            Application.Exit();
        }
        catch (Exception ex)
        {
            ShowStatus(L.F("Не удалось обновить: {0}", ex.Message));
        }
    }

    private void ShowStatus(string text)
    {
        _updateRow.Subtitle = text;
        _updateRow.Invalidate();
    }

    private static void Open(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }

    private sealed class AboutCard : ThemedControl
    {
        public AboutCard()
        {
            Dock = DockStyle.Top;
            Height = Theme.Px(128);
            Theme.Bind(this, () => Theme.Surface);
        }

        protected override void Draw(Graphics g)
        {
            var rect = new RectangleF(Theme.ShadowSide, 1, W - Theme.ShadowSide * 2, H - 9);
            Theme.DrawCard(g, rect, 16, Theme.Card, Theme.Border);

            var tile = new RectangleF(rect.X + 20, rect.Y + (rect.Height - 64) / 2, 64, 64);
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(tile, Theme.Pink, Theme.Accent, 45f))
            using (var path = Theme.RoundedRect(tile, 18))
                g.FillPath(brush, path);
            KittenPainter.DrawFace(g, new RectangleF(tile.X + 7, tile.Y + 9, 50, 48));

            var x = tile.Right + 18;
            var width = rect.Right - x - 16;
            Theme.DrawText(g, "Tunnelka", Theme.Big, Theme.Text, new RectangleF(x, tile.Y - 4, width, 32));
            Theme.DrawText(g, L.F("Версия {0}", UpdateService.Current.ToString(3)), Theme.BodyBold, Theme.AccentStrong, new RectangleF(x, tile.Y + 28, width, 20));
            Theme.DrawText(g, L.T("VPN-клиент на Xray и sing-box"), Theme.Caption, Theme.TextMuted, new RectangleF(x, tile.Y + 48, width, 18));
        }
    }
}
