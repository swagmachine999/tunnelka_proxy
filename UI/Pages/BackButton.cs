using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class BackButton : ThemedControl
{

    public BackButton()
    {
        Size = new Size(Theme.Px(36), Theme.Px(36));
        Cursor = Cursors.Hand;
    }

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, IsHovered ? Theme.CardHover : Theme.Card, rect, 10);
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
