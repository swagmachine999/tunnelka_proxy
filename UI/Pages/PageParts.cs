namespace Tunnelka.UI.Pages;

public static class PageParts
{
    public static Label Title(string text) => Theme.Bind(new Label
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = Theme.Px(52),
        Font = Theme.Scaled(Theme.Title),
        TextAlign = ContentAlignment.MiddleLeft
    }, () => Theme.Surface, () => Theme.Text);

    public static Panel Header(string text, Action onBack)
    {
        var header = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(52) }, () => Theme.Surface);
        var back = new BackButton { Location = new Point(0, Theme.Px(9)) };
        back.Click += (_, _) => onBack();
        var title = Title(text);
        title.Dock = DockStyle.Fill;
        title.Padding = Theme.Px(46, 0, 0, 0);
        header.Controls.Add(back);
        header.Controls.Add(title);
        back.BringToFront();
        return header;
    }

    public static Label Caption(string text, int height) => Theme.Bind(new Label
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = Theme.Px(height),
        Font = Theme.Scaled(Theme.Caption),
        TextAlign = ContentAlignment.MiddleLeft
    }, () => Theme.Surface, () => Theme.TextMuted);

    public static Button Button(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.Scaled(Theme.BodyBold),
            Cursor = Cursors.Hand
        };

        Theme.Bind(button, () => primary ? Theme.Accent : Theme.Card, () => primary ? Color.White : Theme.AccentStrong);
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Theme.Accent;
        return button;
    }
}
