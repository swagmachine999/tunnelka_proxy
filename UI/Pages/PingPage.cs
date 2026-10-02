using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class PingPage : Panel
{
    public event EventHandler? Changed;

    public PingPage(bool real, string url, Action onBack)
    {
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);

        Mode = new Segmented(L.T("Реальный (via proxy)"), L.T("Быстрый (TCP)")) { Dock = DockStyle.Top, Height = Theme.Px(40) };
        Mode.SelectedIndex = real ? 0 : 1;
        Mode.SelectedIndexChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

        Url = new SearchBox(RealPingDefault, false) { Dock = DockStyle.Top };
        Url.SetText(url);
        Url.QueryChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

        var reset = PageParts.Button(L.T("Сбросить адрес"), false);
        reset.Dock = DockStyle.Left;
        reset.Width = Theme.Px(150);
        reset.Click += (_, _) => Url.SetText(RealPingDefault);
        var resetRow = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(44), Padding = Theme.Px(0, 8, 0, 2) }, () => Theme.Surface);
        resetRow.Controls.Add(reset);

        Controls.Add(resetRow);
        Controls.Add(Url);
        Controls.Add(PageParts.Caption(L.T("Тестовый адрес для реального пинга"), 30));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(Wrapped(L.T("Быстрый: проверяет только, открыт ли порт сервера. Мгновенно, но может показать пинг у сервера, через который VPN не работает."), 70));
        Controls.Add(Wrapped(L.T("Реальный: запрос идёт через сам сервер, как при работе VPN. Делается два запроса, берётся лучший. Нерабочий сервер покажет n/a."), 70));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(8) }, () => Theme.Surface));
        Controls.Add(Mode);
        Controls.Add(PageParts.Caption(L.T("Тип пинга"), 30));
        Controls.Add(PageParts.Header(L.T("Пинг"), onBack));
    }

    public const string RealPingDefault = "https://www.gstatic.com/generate_204";

    public Segmented Mode { get; }
    public SearchBox Url { get; }

    public bool IsReal => Mode.SelectedIndex == 0;

    private static Label Wrapped(string text, int height) => Theme.Bind(new Label
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = Theme.Px(height),
        Font = Theme.Scaled(Theme.Body),
        TextAlign = ContentAlignment.TopLeft,
        Padding = Theme.Px(0, 4, 0, 0)
    }, () => Theme.Surface, () => Theme.Text);
}
