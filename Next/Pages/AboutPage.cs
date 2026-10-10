using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Services;

namespace Tunnelka.Next;

public sealed class AboutPage : UserControl
{
    private readonly Session _session;
    private Button _check = null!;
    private TextBlock _status = null!;
    private bool _working;

    public AboutPage(Session session)
    {
        _session = session;
        Build();
    }

    public void Localize() => Build();

    private void Build()
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        panel.Children.Add(Ui.Title(L.T("О приложении")));
        panel.Children.Add(BuildCard());

        _check = SettingsParts.Pill(L.T("Проверить"), true);
        _check.IsEnabled = !_working;
        _check.Click += async (_, _) => await CheckForUpdate();
        panel.Children.Add(Ui.Row(L.T("Обновления"), L.T("Нажмите, чтобы проверить новую версию"), _check, out _, out _status));

        panel.Children.Add(Ui.ClickRow(L.T("Все версии"), L.T("Страница загрузок на GitHub"), () => UpdateService.OpenPage(UpdateService.ReleasesPage), out _, out _));

        Content = new ScrollViewer { Content = panel };
    }

    private static Border BuildCard()
    {
        var mark = new TextBlock
        {
            Text = "T",
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var tile = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(18),
            Child = mark
        };
        SettingsTheme.Paint(tile, Border.BackgroundProperty, "AccentGradient");

        var name = new TextBlock { Text = "Tunnelka", FontSize = 25, FontWeight = FontWeight.Bold };
        var version = new TextBlock
        {
            Text = L.F("Версия {0}", UpdateService.Current.ToString(3)),
            FontWeight = FontWeight.SemiBold
        };
        SettingsTheme.Paint(version, TextBlock.ForegroundProperty, "AccentStrongBrush");
        var texts = new StackPanel
        {
            Margin = new Thickness(18, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children = { name, version }
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(texts, 1);
        grid.Children.Add(tile);
        grid.Children.Add(texts);

        var card = Ui.Card(grid);
        card.Padding = new Thickness(20);
        card.CornerRadius = new CornerRadius(16);
        return card;
    }

    private async Task CheckForUpdate()
    {
        if (_working)
            return;

        _working = true;
        var check = _check;
        var status = _status;
        check.IsEnabled = false;
        status.Text = L.T("Проверяю…");
        try
        {
            var update = await UpdateService.CheckAsync(_session.ProxyPort);
            if (update == null)
            {
                status.Text = L.F("У вас последняя версия {0}", UpdateService.Current.ToString(3));
                return;
            }

            status.Text = L.F("Доступна версия {0}", update.Version.ToString(3));
            ShowUpdate(update);
        }
        catch (Exception ex)
        {
            status.Text = L.F("Не удалось проверить: {0}", ex.Message);
        }
        finally
        {
            _working = false;
            check.IsEnabled = true;
        }
    }

    private void ShowUpdate(UpdateInfo update)
    {
        var dialog = new PromptDialog(
            L.T("Доступна новая версия"),
            L.F("Tunnelka {0}. Скачать и установить сейчас?", update.Version.ToString(3)),
            L.T("Да"),
            L.T("Нет"),
            _session.Data.UiScale);
        dialog.Declined += () => dialog.Close();
        dialog.Accepted += async () => await Install(dialog, update);
        if (TopLevel.GetTopLevel(this) is Window owner)
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
