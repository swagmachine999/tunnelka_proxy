using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Tunnelka.UI;

namespace Tunnelka.Desktop;

public sealed class PingPage : UserControl
{
    private static readonly int[] Intervals = { 1, 2, 3, 5, 10, 15, 30, 60, 300, 600, 1800, 3600 };
    private const string RealPingDefault = "https://www.gstatic.com/generate_204";

    private readonly Session _session;
    private readonly TextBox _url;

    public PingPage(Session session)
    {
        _session = session;
        var data = session.Data;
        var panel = new StackPanel();

        _url = new TextBox
        {
            Watermark = RealPingDefault,
            Text = data.PingUrl,
            CornerRadius = new CornerRadius(12),
            MinHeight = 38
        };

        var reset = SettingsParts.Pill(L.T("Сбросить адрес"), false);
        reset.HorizontalAlignment = HorizontalAlignment.Left;
        reset.Margin = new Thickness(0, 10, 0, 0);
        reset.Click += (_, _) => _url.Text = RealPingDefault;

        panel.Children.Add(SettingsParts.Paragraph(L.T("Пинг проверяется запросом через сервер VPN. Нерабочий сервер покажет n/a. Проверка может занять несколько секунд.")));
        var urlCaption = SettingsParts.Caption(L.T("Тестовый адрес для реального пинга"));
        urlCaption.Margin = new Thickness(2, 18, 0, 6);
        panel.Children.Add(urlCaption);
        panel.Children.Add(_url);
        panel.Children.Add(reset);

        var speed = new SettingsStepper(Intervals, ServerText.Duration, data.SpeedInterval, 150);
        speed.ValueChanged += (_, _) =>
        {
            data.SpeedInterval = speed.Value;
            session.RestartSpeedAveraging();
            session.Save();
        };
        var speedRow = Ui.Row(L.T("Отображение скорости"), L.T("Как часто отображать"), speed, out _, out _);
        speedRow.Margin = new Thickness(0, 18, 0, 0);
        panel.Children.Add(speedRow);

        _url.TextChanged += (_, _) => Changed();
        Content = panel;
    }

    private void Changed()
    {
        var data = _session.Data;
        var url = (_url.Text ?? "").Trim();
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            data.PingUrl = url;
        _session.Save();
    }
}
