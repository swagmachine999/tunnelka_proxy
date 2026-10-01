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

    public static Panel Header(string text, Action onBack)
    {
        var header = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = 52 }, () => Theme.Surface);
        var back = new BackButton { Location = new Point(0, 9) };
        back.Click += (_, _) => onBack();
        var title = Title(text);
        title.Dock = DockStyle.Fill;
        title.Padding = new Padding(46, 0, 0, 0);
        header.Controls.Add(back);
        header.Controls.Add(title);
        back.BringToFront();
        return header;
    }

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

public class BackButton : Control
{
    private bool _hover;

    public BackButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(36, 36);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Theme.FillRounded(g, _hover ? Theme.CardHover : Theme.Card, rect, 10);
        Theme.DrawRounded(g, Theme.Border, rect, 10);

        using var pen = new Pen(Theme.Text, 2f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        var cx = Width / 2f + 1;
        var cy = Height / 2f;
        g.DrawLine(pen, cx + 3, cy - 6, cx - 3, cy);
        g.DrawLine(pen, cx - 3, cy, cx + 3, cy + 6);
    }
}
