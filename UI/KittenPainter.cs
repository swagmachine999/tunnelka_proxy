using System.Drawing.Drawing2D;

namespace Tunnelka.UI;

public static class KittenPainter
{
    private static readonly Color Fur = Color.FromArgb(255, 229, 208);
    private static readonly Color FurLight = Color.FromArgb(255, 245, 235);
    private static readonly Color Line = Color.FromArgb(124, 106, 142);
    private static readonly Color EarInner = Color.FromArgb(249, 186, 208);
    private static readonly Color Blush = Color.FromArgb(150, 249, 168, 196);
    private static readonly Color Nose = Color.FromArgb(240, 138, 176);
    private static readonly Color Eye = Color.FromArgb(74, 62, 98);

    private static readonly Font[] SleepFonts =
    {
        Theme.MakeFont(13, FontStyle.Bold),
        Theme.MakeFont(16, FontStyle.Bold),
        Theme.MakeFont(20, FontStyle.Bold)
    };

    public static void Draw(Graphics g, RectangleF bounds, bool awake, float time, bool blink)
    {
        var state = g.Save();
        var scale = Math.Min(bounds.Width / 200f, bounds.Height / 160f);
        g.TranslateTransform(bounds.X + (bounds.Width - 200 * scale) / 2, bounds.Y + (bounds.Height - 160 * scale) / 2);
        g.ScaleTransform(scale, scale);

        var breath = awake ? 0f : (float)Math.Sin(time * 1.6) * 1.6f;
        var sway = awake ? (float)Math.Sin(time * 3.2) * 9f : (float)Math.Sin(time * 1.1) * 3f;

        using var line = new Pen(Line, 2.4f) { LineJoin = LineJoin.Round };
        using var fur = new SolidBrush(Fur);

        using (var shadow = new SolidBrush(Color.FromArgb(45, Theme.Accent)))
            g.FillEllipse(shadow, 50, 146, 104, 11);

        DrawTail(g, sway);

        g.FillEllipse(fur, 62, 86 - breath, 76, 64 + breath);
        g.DrawEllipse(line, 62, 86 - breath, 76, 64 + breath);
        using (var belly = new SolidBrush(FurLight))
            g.FillEllipse(belly, 80, 102 - breath, 40, 40 + breath);

        foreach (var x in new[] { 74f, 104f })
        {
            g.FillEllipse(fur, x, 138, 22, 14);
            g.DrawEllipse(line, x, 138, 22, 14);
        }

        var headState = g.Save();
        g.TranslateTransform(0, -breath);
        DrawHead(g, awake && !blink, line, fur);
        g.Restore(headState);

        if (awake)
            DrawHeart(g, 160, 18 + (float)Math.Sin(time * 2.4) * 3f);
        else
            DrawSleep(g, time);

        g.Restore(state);
    }

    public static void DrawFace(Graphics g, RectangleF bounds)
    {
        var state = g.Save();
        var scale = Math.Min(bounds.Width / 124f, bounds.Height / 112f);
        g.TranslateTransform(bounds.X + (bounds.Width - 124 * scale) / 2, bounds.Y + (bounds.Height - 112 * scale) / 2);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(-38, -2);

        using var line = new Pen(Line, 3.2f) { LineJoin = LineJoin.Round };
        using var fur = new SolidBrush(Fur);
        DrawHead(g, true, line, fur);
        g.Restore(state);
    }

    private static void DrawHead(Graphics g, bool eyesOpen, Pen line, Brush fur)
    {
        using var inner = new SolidBrush(EarInner);

        var leftEar = new[] { new PointF(56, 56), new PointF(60, 6), new PointF(98, 34) };
        var rightEar = new[] { new PointF(144, 56), new PointF(140, 6), new PointF(102, 34) };
        g.FillPolygon(fur, leftEar);
        g.DrawPolygon(line, leftEar);
        g.FillPolygon(fur, rightEar);
        g.DrawPolygon(line, rightEar);
        g.FillPolygon(inner, new[] { new PointF(67, 42), new PointF(66, 18), new PointF(88, 33) });
        g.FillPolygon(inner, new[] { new PointF(133, 42), new PointF(134, 18), new PointF(112, 33) });

        g.FillEllipse(fur, 46, 26, 108, 84);
        g.DrawEllipse(line, 46, 26, 108, 84);

        using (var eye = new SolidBrush(Eye))
        using (var eyeLine = new Pen(Eye, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            if (eyesOpen)
            {
                g.FillEllipse(eye, 74, 56, 15, 18);
                g.FillEllipse(eye, 111, 56, 15, 18);
                g.FillEllipse(Brushes.White, 78, 59, 6, 6);
                g.FillEllipse(Brushes.White, 115, 59, 6, 6);
                g.FillEllipse(Brushes.White, 84, 67, 3, 3);
                g.FillEllipse(Brushes.White, 121, 67, 3, 3);
            }
            else
            {
                g.DrawArc(eyeLine, 73, 58, 17, 12, 20, 140);
                g.DrawArc(eyeLine, 110, 58, 17, 12, 20, 140);
            }
        }

        using (var blush = new SolidBrush(Blush))
        {
            g.FillEllipse(blush, 61, 74, 19, 11);
            g.FillEllipse(blush, 120, 74, 19, 11);
        }

        using (var nose = new SolidBrush(Nose))
            g.FillEllipse(nose, 95, 74, 10, 7);

        using (var mouth = new Pen(Line, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawArc(mouth, 91, 77, 9, 8, 0, 180);
            g.DrawArc(mouth, 100, 77, 9, 8, 0, 180);
        }

        using var whisker = new Pen(Color.FromArgb(150, Line), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(whisker, 64, 80, 38, 74);
        g.DrawLine(whisker, 64, 85, 38, 88);
        g.DrawLine(whisker, 136, 80, 162, 74);
        g.DrawLine(whisker, 136, 85, 162, 88);
    }

    private static void DrawTail(Graphics g, float sway)
    {
        using var path = new GraphicsPath();
        path.AddBezier(128, 134, 172, 142, 182 + sway * 0.4f, 104, 160 + sway, 84);

        using (var outline = new Pen(Line, 15f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawPath(outline, path);
        using (var fill = new Pen(Fur, 10.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawPath(fill, path);
    }

    private static void DrawHeart(Graphics g, float x, float y)
    {
        using var brush = new SolidBrush(Color.FromArgb(230, Theme.Pink));
        using var path = new GraphicsPath();
        path.AddBezier(x, y + 4, x - 1, y - 3, x - 11, y - 3, x - 11, y + 4);
        path.AddBezier(x - 11, y + 4, x - 11, y + 10, x - 3, y + 13, x, y + 17);
        path.AddBezier(x, y + 17, x + 3, y + 13, x + 11, y + 10, x + 11, y + 4);
        path.AddBezier(x + 11, y + 4, x + 11, y - 3, x + 1, y - 3, x, y + 4);
        g.FillPath(brush, path);
    }

    private static void DrawSleep(Graphics g, float time)
    {
        for (var i = 0; i < 3; i++)
        {
            var phase = (float)((time * 0.45 + i / 3.0) % 1.0);
            var alpha = (int)(210 * Math.Sin(phase * Math.PI));
            using var brush = new SolidBrush(Color.FromArgb(Math.Max(0, alpha), Theme.AccentStrong));
            g.DrawString("z", SleepFonts[i], brush, 146 + phase * 22, 46 - phase * 46);
        }
    }
}
