namespace VpnClient.UI.Pages;

public class LogPage : Panel
{
    private readonly TextBox _log = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        WordWrap = true,
        BorderStyle = BorderStyle.None,
        ScrollBars = ScrollBars.Vertical,
        Font = Theme.Scaled(Theme.Log)
    };

    public LogPage(Action onBack)
    {
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);
        Theme.Bind(_log, () => Theme.Card, () => Theme.Text);

        var frame = new Panel { Dock = DockStyle.Fill, Padding = Theme.Px(14, 12, 6, 12) };
        Theme.Bind(frame, () => Theme.Card);
        frame.Controls.Add(_log);

        var clear = PageParts.Button("Очистить", false);
        clear.Dock = DockStyle.Right;
        clear.Width = Theme.Px(110);
        clear.Click += (_, _) => _log.Clear();

        var header = PageParts.Header("Журнал", onBack);
        var buttonHolder = new Panel { Dock = DockStyle.Right, Width = Theme.Px(110), Padding = Theme.Px(0, 10, 0, 8) };
        Theme.Bind(buttonHolder, () => Theme.Surface);
        buttonHolder.Controls.Add(clear);
        header.Controls.Add(buttonHolder);

        var gap = new Panel { Dock = DockStyle.Top, Height = Theme.Px(8) };
        Theme.Bind(gap, () => Theme.Surface);

        Controls.Add(frame);
        Controls.Add(gap);
        Controls.Add(header);
    }

    public TextBox Box => _log;

    public void Append(string line) => _log.AppendText(line + Environment.NewLine);
}
