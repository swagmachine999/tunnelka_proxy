namespace VpnClient.UI.Controls;

public class LogoView : Control
{
    public LogoView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Sidebar;
        Size = new Size(44, 44);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);
        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Theme.Pink, Theme.Accent, 45f))
        using (var path = Theme.RoundedRect(rect, 13))
            g.FillPath(brush, path);

        KittenPainter.DrawFace(g, new RectangleF(5, 6, Width - 10, Height - 11));
    }

    public static Icon? CreateAppIcon()
    {
        try
        {
            using var bitmap = new Bitmap(64, 64);
            using (var g = Graphics.FromImage(bitmap))
            {
                Theme.Smooth(g);
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, 64, 64), Theme.Pink, Theme.Accent, 45f))
                using (var path = Theme.RoundedRect(new RectangleF(0, 0, 63, 63), 16))
                    g.FillPath(brush, path);
                KittenPainter.DrawFace(g, new RectangleF(6, 8, 52, 50));
            }

            return Icon.FromHandle(bitmap.GetHicon());
        }
        catch (Exception)
        {
            return null;
        }
    }
}
