using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Tunnelka.Next;

public sealed class PingPage : UserControl
{
    public const string RealPingDefault = "https://www.gstatic.com/generate_204";

    private readonly Session _session;
    private readonly SettingsSegmented _mode;
    private readonly TextBox _url;

    public PingPage(Session session)
    {
        _session = session;
        var data = session.Data;
        var panel = new StackPanel();

        _mode = new SettingsSegmented(double.NaN, L.T("Реальный (via proxy)"), L.T("Быстрый (TCP)"));
        _mode.Select(data.RealPing ? 0 : 1);
        _mode.Height = 40;

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

        panel.Children.Add(SettingsParts.Caption(L.T("Тип пинга")));
        panel.Children.Add(_mode);
        panel.Children.Add(new Border { Height = 8 });
        panel.Children.Add(SettingsParts.Paragraph(L.T("Быстрый: проверяет только, открыт ли порт сервера. Мгновенно, но может показать пинг у сервера, через который VPN не работает.")));
        panel.Children.Add(SettingsParts.Paragraph(L.T("Реальный: запрос идёт через сам сервер, как при работе VPN. Делается два запроса, берётся лучший. Нерабочий сервер покажет n/a.")));
        panel.Children.Add(new Border { Height = 10 });
        panel.Children.Add(SettingsParts.Caption(L.T("Тестовый адрес для реального пинга")));
        panel.Children.Add(_url);
        panel.Children.Add(reset);

        _mode.SelectedIndexChanged += (_, _) => Changed();
        _url.TextChanged += (_, _) => Changed();
        Content = panel;
    }

    private void Changed()
    {
        var data = _session.Data;
        data.RealPing = _mode.SelectedIndex == 0;
        var url = (_url.Text ?? "").Trim();
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            data.PingUrl = url;
        _session.Save();
    }
}
