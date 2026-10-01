namespace VpnClient.UI.Controls;

public class TipBubble : Control
{
    public TipBubble()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Visible = false;
    }

    public void ShowNear(Control anchor, string text)
    {
        Text = text;
        using (var g = CreateGraphics())
        {
            var size = g.MeasureString(text, Theme.BodyBold);
            Size = new Size((int)size.Width + 30, 34);
        }

        var form = FindForm();
        if (form == null)
            return;

        var point = form.PointToClient(anchor.PointToScreen(Point.Empty));
        Location = new Point(point.X + anchor.Width + 8, point.Y + (anchor.Height - Height) / 2);
        BringToFront();
        Visible = true;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Surface);

        var back = Theme.IsDark ? Theme.Lighten(Theme.Card, 0.12f) : Theme.Text;
        var fore = Theme.IsDark ? Theme.Text : Color.White;
        var rect = new RectangleF(6, 0, Width - 7, Height - 1);
        Theme.FillRounded(g, back, rect, 9);

        using (var brush = new SolidBrush(back))
            g.FillPolygon(brush, new[] { new PointF(0.5f, Height / 2f), new PointF(7, Height / 2f - 6), new PointF(7, Height / 2f + 6) });

        Theme.DrawText(g, Text, Theme.BodyBold, fore, rect, StringAlignment.Center);
    }
}
