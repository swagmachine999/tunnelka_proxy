namespace VpnClient.UI.Pages;

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

public class BackButton : Control
{
    private float W => Width / Theme.S;
    private float H => Height / Theme.S;

    private bool _hover;

    public BackButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(Theme.Px(36), Theme.Px(36));
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
        Theme.Begin(g, Theme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, _hover ? Theme.CardHover : Theme.Card, rect, 10);
        Theme.DrawRounded(g, Theme.Border, rect, 10);

        using var pen = new Pen(Theme.Text, 2f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        var cx = W / 2f + 1;
        var cy = H / 2f;
        g.DrawLine(pen, cx + 3, cy - 6, cx - 3, cy);
        g.DrawLine(pen, cx - 3, cy, cx + 3, cy + 6);
    }
}
