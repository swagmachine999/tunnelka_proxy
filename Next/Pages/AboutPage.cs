using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Services;

namespace Tunnelka.Next;

public sealed class AboutPage : UserControl
{
    private readonly AboutUpdateFlow _updates;
    private Button _check = null!;
    private TextBlock _status = null!;
    private bool _working;

    public AboutPage(Session session)
    {
        _updates = new AboutUpdateFlow(session, () => TopLevel.GetTopLevel(this) as Window);
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
        grid.Children.Add(new BrandTile(64));
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
            status.Text = await _updates.CheckAsync();
        }
        finally
        {
            _working = false;
            check.IsEnabled = true;
        }
    }
}
