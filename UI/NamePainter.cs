using System.Drawing.Drawing2D;

namespace VpnClient.UI;

public static class NamePainter
{
    private const float Gap = 5;

    public static float Measure(Graphics g, IReadOnlyList<NamePart> parts, Font font)
    {
        var width = 0f;
        foreach (var part in parts)
            width += PartWidth(g, part, font) + Gap;
        return Math.Max(0, width - Gap);
    }

    public static void Draw(Graphics g, IReadOnlyList<NamePart> parts, Font font, Color color, RectangleF bounds)
    {
        var state = g.Save();
        g.SetClip(bounds);

        var x = bounds.X;
        var cy = bounds.Y + bounds.Height / 2;
        var symbolSize = font.Size * 1.15f;

        foreach (var part in parts)
        {
            if (x >= bounds.Right)
                break;

            if (part.IsSymbol)
            {
                DrawSymbol(g, part.Text, new RectangleF(x, cy - symbolSize / 2, symbolSize, symbolSize), color);
                x += symbolSize + Gap;
            }
            else
            {
                var measured = g.MeasureString(part.Text, font).Width;
                var width = Math.Min(bounds.Right - x, measured + 2);
                Theme.DrawText(g, part.Text, font, color, new RectangleF(x, bounds.Y, width, bounds.Height));
                x += measured + 1 + Gap;
            }
        }

        g.Restore(state);
    }

    private static float PartWidth(Graphics g, NamePart part, Font font) =>
        part.IsSymbol ? font.Size * 1.15f : g.MeasureString(part.Text, font).Width + 1;

    private static void DrawSymbol(Graphics g, string symbol, RectangleF r, Color textColor)
    {
        if (symbol == "\u267E" || symbol == "\u267E\uFE0F")
        {
            DrawInfinity(g, r);
            return;
        }

        var image = Emoji.Get(symbol);
        if (image != null)
        {
            g.DrawImage(image, r);
            return;
        }

        if (symbol.StartsWith("\u2B50") || symbol.StartsWith("\u2605"))
        {
            DrawStar(g, r);
            return;
        }

        using var font = new Font("Segoe UI Emoji", r.Height * 0.78f, FontStyle.Regular, GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, symbol, font, Rectangle.Round(new RectangleF(r.X - 4, r.Y - 2, r.Width + 8, r.Height + 4)),
            textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private static void DrawStar(Graphics g, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2 + r.Height * 0.04f;
        var outer = r.Width / 2;
        var inner = outer * 0.48f;
        var points = new PointF[10];
        for (var i = 0; i < 10; i++)
        {
            var radius = i % 2 == 0 ? outer : inner;
            var angle = Math.PI / 5 * i - Math.PI / 2;
            points[i] = new PointF(cx + radius * (float)Math.Cos(angle), cy + radius * (float)Math.Sin(angle));
        }

        using var brush = new SolidBrush(Theme.Star);
        using var pen = new Pen(Theme.Star, 1.4f) { LineJoin = LineJoin.Round };
        g.FillPolygon(brush, points);
        g.DrawPolygon(pen, points);
    }

    private static void DrawInfinity(Graphics g, RectangleF r)
    {
        var cy = r.Y + r.Height / 2;
        var w = r.Width * 1.1f;
        var x = r.X - (w - r.Width) / 2;
        var h = r.Height * 0.42f;

        using var path = new GraphicsPath();
        path.AddBezier(x + w / 2, cy, x + w * 0.75f, cy - h, x + w, cy - h, x + w, cy);
        path.AddBezier(x + w, cy, x + w, cy + h, x + w * 0.75f, cy + h, x + w / 2, cy);
        path.AddBezier(x + w / 2, cy, x + w * 0.25f, cy - h, x, cy - h, x, cy);
        path.AddBezier(x, cy, x, cy + h, x + w * 0.25f, cy + h, x + w / 2, cy);

        using var pen = new Pen(Theme.Infinity, Math.Max(1.6f, r.Height / 8f)) { LineJoin = LineJoin.Round };
        g.DrawPath(pen, path);
    }
}
