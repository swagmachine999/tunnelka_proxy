using System.Drawing.Drawing2D;
using Tunnelka.UI.Kitten;

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

    public static void Draw(Graphics g, RectangleF bounds, KittenPose pose, float time)
    {
        var state = g.Save();
        var scale = Math.Min(bounds.Width / 200f, bounds.Height / 160f);
        g.TranslateTransform(bounds.X + (bounds.Width - 200 * scale) / 2, bounds.Y + (bounds.Height - 160 * scale) / 2);
        g.ScaleTransform(scale, scale);

        using var line = new Pen(Line, 2.4f) { LineJoin = LineJoin.Round };
        using var fur = new SolidBrush(Fur);

        var lift = Math.Min(1f, Math.Max(0f, -pose.BodyY / 44f));
        var shadowWidth = 104 * (1 - 0.25f * lift) * pose.ScaleX;
        using (var shadow = new SolidBrush(Color.FromArgb((int)(45 * (1 - 0.4f * lift)), Theme.Accent)))
            g.FillEllipse(shadow, 100 + pose.BodyX - shadowWidth / 2, 146, shadowWidth, 11);

        var bodyState = g.Save();
        g.TranslateTransform(100 + pose.BodyX, 152 + pose.BodyY);
        g.ScaleTransform(pose.ScaleX, pose.ScaleY);
        g.TranslateTransform(-100, -152);

        DrawTail(g, pose.TailSway, pose.TailPuff);

        var breath = pose.Breath;
        g.FillEllipse(fur, 62, 86 - breath, 76, 64 + breath);
        g.DrawEllipse(line, 62, 86 - breath, 76, 64 + breath);
        using (var belly = new SolidBrush(FurLight))
            g.FillEllipse(belly, 80, 102 - breath, 40, 40 + breath);

        if (pose.Bristle > 0.01f)
            DrawBristle(g, 100, 118, 38, 32, 195, 345, 7, pose.Bristle, 0.5f);

        foreach (var x in new[] { 74f, 104f })
        {
            if (pose.Lick > 0.5f && x > 100)
                continue;

            g.FillEllipse(fur, x, 138, 22, 14);
            g.DrawEllipse(line, x, 138, 22, 14);
        }

        var headState = g.Save();
        g.TranslateTransform(pose.HeadX, pose.HeadY - breath);
        g.TranslateTransform(100, 108);
        g.RotateTransform(pose.HeadTilt);
        g.TranslateTransform(-100, -108);
        DrawHead(g, pose, line, fur);
        g.Restore(headState);

        if (pose.Lick > 0.01f)
            DrawLickingPaw(g, pose, line, fur);

        g.Restore(bodyState);

        if (pose.Heart > 0.01f)
            DrawHeart(g, 160, 18 + (float)Math.Sin(time * 2.4) * 3f, pose.Heart);
        if (pose.Zzz > 0.01f)
            DrawSleep(g, time, pose.Zzz);

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
        DrawHead(g, new KittenPose(), line, fur);
        g.Restore(state);
    }

    private static void DrawHead(Graphics g, KittenPose pose, Pen line, Brush fur)
    {
        using var inner = new SolidBrush(EarInner);

        var outward = pose.EarFlat * 50;
        DrawEar(g, new PointF(77, 45), -(pose.EarLeft + outward), pose.EarFlat,
            new[] { new PointF(56, 56), new PointF(60, 6), new PointF(98, 34) },
            new[] { new PointF(67, 42), new PointF(66, 18), new PointF(88, 33) }, line, fur, inner);
        DrawEar(g, new PointF(123, 45), pose.EarRight + outward, pose.EarFlat,
            new[] { new PointF(144, 56), new PointF(140, 6), new PointF(102, 34) },
            new[] { new PointF(133, 42), new PointF(134, 18), new PointF(112, 33) }, line, fur, inner);

        g.FillEllipse(fur, 46, 26, 108, 84);
        g.DrawEllipse(line, 46, 26, 108, 84);

        if (pose.Bristle > 0.01f)
            DrawBristle(g, 100, 68, 54, 42, 200, 340, 9, pose.Bristle, 1f);

        DrawEyes(g, pose);

        using (var blush = new SolidBrush(Blush))
        {
            g.FillEllipse(blush, 61, 74, 19, 11);
            g.FillEllipse(blush, 120, 74, 19, 11);
        }

        using (var nose = new SolidBrush(Nose))
            g.FillEllipse(nose, 95, 74, 10, 7);

        DrawMouth(g, pose);

        using var whisker = new Pen(Color.FromArgb(150, Line), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(whisker, 64, 80, 38, 74);
        g.DrawLine(whisker, 64, 85, 38, 88);
        g.DrawLine(whisker, 136, 80, 162, 74);
        g.DrawLine(whisker, 136, 85, 162, 88);
    }

    private static void DrawEar(Graphics g, PointF pivot, float angle, float flat, PointF[] outer, PointF[] inner, Pen line, Brush fur, Brush innerBrush)
    {
        var state = g.Save();
        g.TranslateTransform(pivot.X, pivot.Y);
        g.RotateTransform(angle);
        g.ScaleTransform(1, 1 - 0.18f * flat);
        g.TranslateTransform(-pivot.X, -pivot.Y);
        g.FillPolygon(fur, outer);
        g.DrawPolygon(line, outer);
        g.FillPolygon(innerBrush, inner);
        g.Restore(state);
    }

    private static void DrawEyes(Graphics g, KittenPose pose)
    {
        using var eye = new SolidBrush(Eye);
        using var eyeLine = new Pen(Eye, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        if (pose.EyeOpen < 0.15f)
        {
            g.DrawArc(eyeLine, 73, 58, 17, 12, 20, 140);
            g.DrawArc(eyeLine, 110, 58, 17, 12, 20, 140);
            return;
        }

        var grow = 1 + pose.EyeWide * 0.35f;
        var width = 15 * grow;
        var height = 18 * grow * pose.EyeOpen;
        foreach (var centerX in new[] { 81.5f, 118.5f })
        {
            var cx = centerX + pose.PupilX;
            var cy = 65 + pose.PupilY;
            g.FillEllipse(eye, cx - width / 2, cy - height / 2, width, height);
            if (height < 8)
                continue;

            g.FillEllipse(Brushes.White, cx - width / 2 + 4, cy - height / 2 + 3, 6 * grow, 6 * grow);
            g.FillEllipse(Brushes.White, cx - width / 2 + 10 * grow, cy - height / 2 + 11 * grow, 3 * grow, 3 * grow);
        }
    }

    private static void DrawMouth(Graphics g, KittenPose pose)
    {
        if (pose.Yawn > 0.05f)
        {
            var open = pose.Yawn;
            var mouth = new RectangleF(100 - 8 * open - 2, 80, 16 * open + 4, 18 * open + 2);
            using var dark = new SolidBrush(Eye);
            using var tongue = new SolidBrush(Nose);
            g.FillEllipse(dark, mouth);
            g.FillEllipse(tongue, mouth.X + mouth.Width * 0.2f, mouth.Bottom - mouth.Height * 0.45f, mouth.Width * 0.6f, mouth.Height * 0.4f);
            return;
        }

        using var smile = new Pen(Line, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(smile, 91, 77, 9, 8, 0, 180);
        g.DrawArc(smile, 100, 77, 9, 8, 0, 180);

        if (pose.Tongue > 0.05f)
        {
            using var tongue = new SolidBrush(Nose);
            g.FillEllipse(tongue, 97, 84, 8, 10 * pose.Tongue);
        }
    }

    private static void DrawBristle(Graphics g, float cx, float cy, float rx, float ry, float from, float to, int count, float amount, float width)
    {
        using var pen = new Pen(Line, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        for (var i = 0; i < count; i++)
        {
            var angle = (from + (to - from) * i / (count - 1)) * Math.PI / 180;
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);
            var inner = new PointF(cx + cos * rx, cy + sin * ry);
            var length = (i % 2 == 0 ? 9 : 6) * amount;
            var tip = new PointF(cx + cos * (rx + length), cy + sin * (ry + length));
            g.DrawLine(pen, inner, tip);
        }
    }

    private static void DrawLickingPaw(Graphics g, KittenPose pose, Pen line, Brush fur)
    {
        var lift = pose.Lick;
        var shoulder = new PointF(110, 124);
        var rest = new PointF(115, 144);
        var raised = new PointF(101 + pose.HeadX * 0.5f, 90 + pose.HeadY * 0.9f);
        var tip = new PointF(rest.X + (raised.X - rest.X) * lift, rest.Y + (raised.Y - rest.Y) * lift);

        using var path = new GraphicsPath();
        path.AddBezier(shoulder, new PointF(shoulder.X + 20 * lift, shoulder.Y - 4), new PointF(tip.X + 14 * lift, tip.Y + 18), tip);
        using (var outline = new Pen(Line, 15f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawPath(outline, path);
        using (var fill = new Pen(Fur, 10.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawPath(fill, path);

        var ball = new RectangleF(tip.X - 8, tip.Y - 7, 16, 14);
        g.FillEllipse(fur, ball);
        g.DrawEllipse(line, ball);
        using var toe = new Pen(Color.FromArgb(170, Line), 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(toe, tip.X - 2.5f, tip.Y - 1, tip.X - 2.5f, tip.Y + 4);
        g.DrawLine(toe, tip.X + 2.5f, tip.Y - 1, tip.X + 2.5f, tip.Y + 4);
    }

    private static void DrawTail(Graphics g, float sway, float puff)
    {
        using var path = new GraphicsPath();
        path.AddBezier(128, 134, 172, 142, 182 + sway * 0.4f, 104, 160 + sway, 84);

        using (var outline = new Pen(Line, 15f + 9f * puff) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawPath(outline, path);
        using (var fill = new Pen(Fur, 10.2f + 9f * puff) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawPath(fill, path);
    }

    private static void DrawHeart(Graphics g, float x, float y, float amount)
    {
        using var brush = new SolidBrush(Color.FromArgb((int)(230 * amount), Theme.Pink));
        using var path = new GraphicsPath();
        path.AddBezier(x, y + 4, x - 1, y - 3, x - 11, y - 3, x - 11, y + 4);
        path.AddBezier(x - 11, y + 4, x - 11, y + 10, x - 3, y + 13, x, y + 17);
        path.AddBezier(x, y + 17, x + 3, y + 13, x + 11, y + 10, x + 11, y + 4);
        path.AddBezier(x + 11, y + 4, x + 11, y - 3, x + 1, y - 3, x, y + 4);
        g.FillPath(brush, path);
    }

    private static void DrawSleep(Graphics g, float time, float amount)
    {
        for (var i = 0; i < 3; i++)
        {
            var phase = (float)((time * 0.45 + i / 3.0) % 1.0);
            var alpha = (int)(210 * Math.Sin(phase * Math.PI));
            using var brush = new SolidBrush(Color.FromArgb(Math.Max(0, (int)(alpha * amount)), Theme.AccentStrong));
            g.DrawString("z", SleepFonts[i], brush, 146 + phase * 22, 46 - phase * 46);
        }
    }
}
