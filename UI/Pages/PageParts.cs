namespace VpnClient.UI.Pages;

public static class PageParts
{
    public static Label Title(string text) => Theme.Bind(new Label
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = 52,
        Font = Theme.Title,
        TextAlign = ContentAlignment.MiddleLeft
    }, () => Theme.Surface, () => Theme.Text);

    public static Label Caption(string text, int height) => Theme.Bind(new Label
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = height,
        Font = Theme.Caption,
        TextAlign = ContentAlignment.MiddleLeft
    }, () => Theme.Surface, () => Theme.TextMuted);

    public static Button Button(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyBold,
            Cursor = Cursors.Hand
        };

        Theme.Bind(button, () => primary ? Theme.Accent : Theme.Card, () => primary ? Color.White : Theme.AccentStrong);
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Theme.Accent;
        return button;
    }
}
