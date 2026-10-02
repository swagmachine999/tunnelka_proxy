namespace VpnClient.UI.Controls;

public class TipBubble : ThemedControl
{
    public TipBubble()
    {
        Visible = false;
    }

    public void ShowNear(Control anchor, string text)
    {
        Text = text;
        Size = new Size(Theme.Px(Theme.Measure(text, Theme.BodyBold).Width + 32), Theme.Px(34));

        var form = FindForm();
        if (form == null)
            return;

        var point = form.PointToClient(anchor.PointToScreen(Point.Empty));
        Location = new Point(point.X + anchor.Width + Theme.Px(8), point.Y + (anchor.Height - Height) / 2);
        BringToFront();
        Visible = true;
        Invalidate();
    }

    protected override void Draw(Graphics g)
    {
        var back = Theme.IsDark ? Theme.Lighten(Theme.Card, 0.12f) : Theme.Text;
        var fore = Theme.IsDark ? Theme.Text : Color.White;
        var rect = new RectangleF(6, 0, W - 7, H - 1);
        Theme.FillRounded(g, back, rect, 9);

        using (var brush = new SolidBrush(back))
            g.FillPolygon(brush, new[] { new PointF(0.5f, H / 2f), new PointF(7, H / 2f - 6), new PointF(7, H / 2f + 6) });

        Theme.DrawText(g, Text, Theme.BodyBold, fore, rect, StringAlignment.Center);
    }
}
