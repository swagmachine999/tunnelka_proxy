using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Tunnelka.Next.Kitten;

public static class KittenPainter
{
    private static readonly Color Fur = Color.FromRgb(255, 229, 208);
    private static readonly Color FurLight = Color.FromRgb(255, 245, 235);
    private static readonly Color Line = Color.FromRgb(124, 106, 142);
    private static readonly Color EarInner = Color.FromRgb(249, 186, 208);
    private static readonly Color Blush = Color.FromArgb(150, 249, 168, 196);
    private static readonly Color Nose = Color.FromRgb(240, 138, 176);
    private static readonly Color Eye = Color.FromRgb(74, 62, 98);

    private static readonly IBrush FurBrush = new ImmutableSolidColorBrush(Fur);
    private static readonly IBrush FurLightBrush = new ImmutableSolidColorBrush(FurLight);
    private static readonly IBrush LineBrush = new ImmutableSolidColorBrush(Line);
    private static readonly IBrush EarInnerBrush = new ImmutableSolidColorBrush(EarInner);
    private static readonly IBrush BlushBrush = new ImmutableSolidColorBrush(Blush);
    private static readonly IBrush NoseBrush = new ImmutableSolidColorBrush(Nose);
    private static readonly IBrush EyeBrush = new ImmutableSolidColorBrush(Eye);
    private static readonly IBrush WhiteBrush = new ImmutableSolidColorBrush(Colors.White);

    private static readonly Pen LinePen = new(LineBrush, 2.4, lineJoin: PenLineJoin.Round);
    private static readonly Pen FacePen = new(LineBrush, 3.2, lineJoin: PenLineJoin.Round);
    private static readonly Pen EyeLinePen = new(EyeBrush, 2.8, lineCap: PenLineCap.Round);
    private static readonly Pen SmilePen = new(LineBrush, 2, lineCap: PenLineCap.Round);
    private static readonly Pen BristlePen = new(LineBrush, 2.2, lineCap: PenLineCap.Round);
    private static readonly Pen ToePen = new(new ImmutableSolidColorBrush(Color.FromArgb(170, Line.R, Line.G, Line.B)), 1.4, lineCap: PenLineCap.Round);
    private static readonly Pen WhiskerPen = new(new ImmutableSolidColorBrush(Color.FromArgb(150, Line.R, Line.G, Line.B)), 1.6, lineCap: PenLineCap.Round);

    private static readonly Typeface SleepFace = new(new FontFamily("Segoe UI"), FontStyle.Normal, FontWeight.SemiBold);
    private static readonly double[] SleepSizes = { 14.3, 17.6, 22 };

    public static void Draw(DrawingContext g, Rect bounds, KittenPose pose, double time, Color accent, Color accentStrong, Color pink)
    {
        var scale = Math.Min(bounds.Width / 200.0, bounds.Height / 160.0);
        using var place = g.PushTransform(Matrix.CreateTranslation(bounds.X + (bounds.Width - 200 * scale) / 2, bounds.Y + (bounds.Height - 160 * scale) / 2));
        using var size = g.PushTransform(Matrix.CreateScale(scale, scale));

        var lift = Math.Min(1.0, Math.Max(0.0, -pose.BodyY / 44.0));
        var shadowWidth = 104 * (1 - 0.25 * lift) * pose.ScaleX;
        var shadowBrush = new ImmutableSolidColorBrush(Color.FromArgb((byte)(45 * (1 - 0.4 * lift)), accent.R, accent.G, accent.B));
        Ellipse(g, shadowBrush, null, 100 + pose.BodyX - shadowWidth / 2, 146, shadowWidth, 11);

        using (g.PushTransform(Matrix.CreateTranslation(100 + pose.BodyX, 152 + pose.BodyY)))
        using (g.PushTransform(Matrix.CreateScale(pose.ScaleX, pose.ScaleY)))
        using (g.PushTransform(Matrix.CreateTranslation(-100, -152)))
        {
            DrawTail(g, pose.TailSway, pose.TailPuff);

            var breath = (double)pose.Breath;
            Ellipse(g, FurBrush, LinePen, 62, 86 - breath, 76, 64 + breath);
            Ellipse(g, FurLightBrush, null, 80, 102 - breath, 40, 40 + breath);

            if (pose.Bristle > 0.01f)
                DrawBristle(g, 100, 118, 38, 32, 195, 345, 7, pose.Bristle);

            foreach (var x in new[] { 74.0, 104.0 })
            {
                if (pose.Lick > 0.5f && x > 100)
                    continue;

                Ellipse(g, FurBrush, LinePen, x, 138, 22, 14);
            }

            using (g.PushTransform(Matrix.CreateTranslation(pose.HeadX, pose.HeadY - breath)))
            using (g.PushTransform(Matrix.CreateTranslation(100, 108)))
            using (g.PushTransform(Matrix.CreateRotation(pose.HeadTilt * Math.PI / 180.0)))
            using (g.PushTransform(Matrix.CreateTranslation(-100, -108)))
            {
                DrawHead(g, pose, LinePen);
            }

            if (pose.Lick > 0.01f)
                DrawLickingPaw(g, pose);
        }

        if (pose.Heart > 0.01f)
            DrawHeart(g, 160, 18 + Math.Sin(time * 2.4) * 3.0, pose.Heart, pink);
        if (pose.Zzz > 0.01f)
            DrawSleep(g, time, pose.Zzz, accentStrong);
    }

    public static void DrawFace(DrawingContext g, Rect bounds)
    {
        var scale = Math.Min(bounds.Width / 124.0, bounds.Height / 112.0);
        using var place = g.PushTransform(Matrix.CreateTranslation(bounds.X + (bounds.Width - 124 * scale) / 2, bounds.Y + (bounds.Height - 112 * scale) / 2));
        using var size = g.PushTransform(Matrix.CreateScale(scale, scale));
        using var shift = g.PushTransform(Matrix.CreateTranslation(-38, -2));
        DrawHead(g, new KittenPose(), FacePen);
    }

    private static void Ellipse(DrawingContext g, IBrush? brush, IPen? pen, double x, double y, double width, double height) =>
        g.DrawEllipse(brush, pen, new Point(x + width / 2, y + height / 2), width / 2, height / 2);

    private static void DrawHead(DrawingContext g, KittenPose pose, Pen linePen)
    {
        var outward = pose.EarFlat * 50;
        DrawEar(g, new Point(77, 45), -(pose.EarLeft + outward), pose.EarFlat, linePen,
            new[] { new Point(56, 56), new Point(60, 6), new Point(98, 34) },
            new[] { new Point(67, 42), new Point(66, 18), new Point(88, 33) });
        DrawEar(g, new Point(123, 45), pose.EarRight + outward, pose.EarFlat, linePen,
            new[] { new Point(144, 56), new Point(140, 6), new Point(102, 34) },
            new[] { new Point(133, 42), new Point(134, 18), new Point(112, 33) });

        Ellipse(g, FurBrush, linePen, 46, 26, 108, 84);

        if (pose.Bristle > 0.01f)
            DrawBristle(g, 100, 68, 54, 42, 200, 340, 9, pose.Bristle);

        DrawEyes(g, pose);

        Ellipse(g, BlushBrush, null, 61, 74, 19, 11);
        Ellipse(g, BlushBrush, null, 120, 74, 19, 11);
        Ellipse(g, NoseBrush, null, 95, 74, 10, 7);

        DrawMouth(g, pose);

        g.DrawLine(WhiskerPen, new Point(64, 80), new Point(38, 74));
        g.DrawLine(WhiskerPen, new Point(64, 85), new Point(38, 88));
        g.DrawLine(WhiskerPen, new Point(136, 80), new Point(162, 74));
        g.DrawLine(WhiskerPen, new Point(136, 85), new Point(162, 88));
    }

    private static StreamGeometry Polygon(Point[] points)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(points[0], true);
            for (var i = 1; i < points.Length; i++)
                context.LineTo(points[i]);
            context.EndFigure(true);
        }

        return geometry;
    }

    private static void DrawEar(DrawingContext g, Point pivot, double angle, double flat, Pen linePen, Point[] outer, Point[] inner)
    {
        using (g.PushTransform(Matrix.CreateTranslation(pivot.X, pivot.Y)))
        using (g.PushTransform(Matrix.CreateRotation(angle * Math.PI / 180.0)))
        using (g.PushTransform(Matrix.CreateScale(1, 1 - 0.18 * flat)))
        using (g.PushTransform(Matrix.CreateTranslation(-pivot.X, -pivot.Y)))
        {
            g.DrawGeometry(FurBrush, linePen, Polygon(outer));
            g.DrawGeometry(EarInnerBrush, null, Polygon(inner));
        }
    }

    private static StreamGeometry Arc(double x, double y, double width, double height, double startDegrees, double sweepDegrees)
    {
        var rx = width / 2;
        var ry = height / 2;
        var cx = x + rx;
        var cy = y + ry;
        var start = startDegrees * Math.PI / 180.0;
        var end = (startDegrees + sweepDegrees) * Math.PI / 180.0;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(cx + Math.Cos(start) * rx, cy + Math.Sin(start) * ry), false);
            context.ArcTo(
                new Point(cx + Math.Cos(end) * rx, cy + Math.Sin(end) * ry),
                new Size(rx, ry),
                0,
                Math.Abs(sweepDegrees) > 180,
                sweepDegrees >= 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
            context.EndFigure(false);
        }

        return geometry;
    }

    private static void DrawEyes(DrawingContext g, KittenPose pose)
    {
        if (pose.EyeOpen < 0.15f)
        {
            g.DrawGeometry(null, EyeLinePen, Arc(73, 58, 17, 12, 20, 140));
            g.DrawGeometry(null, EyeLinePen, Arc(110, 58, 17, 12, 20, 140));
            return;
        }

        var grow = 1 + pose.EyeWide * 0.35;
        var width = 15 * grow;
        var height = 18 * grow * pose.EyeOpen;
        foreach (var centerX in new[] { 81.5, 118.5 })
        {
            var cx = centerX + pose.PupilX;
            var cy = 65 + pose.PupilY;
            Ellipse(g, EyeBrush, null, cx - width / 2, cy - height / 2, width, height);
            if (height < 8)
                continue;

            Ellipse(g, WhiteBrush, null, cx - width / 2 + 4, cy - height / 2 + 3, 6 * grow, 6 * grow);
            Ellipse(g, WhiteBrush, null, cx - width / 2 + 10 * grow, cy - height / 2 + 11 * grow, 3 * grow, 3 * grow);
        }
    }

    private static void DrawMouth(DrawingContext g, KittenPose pose)
    {
        if (pose.Yawn > 0.05f)
        {
            var open = (double)pose.Yawn;
            var mx = 100 - 8 * open - 2;
            var my = 80.0;
            var mw = 16 * open + 4;
            var mh = 18 * open + 2;
            Ellipse(g, EyeBrush, null, mx, my, mw, mh);
            Ellipse(g, NoseBrush, null, mx + mw * 0.2, my + mh - mh * 0.45, mw * 0.6, mh * 0.4);
            return;
        }

        g.DrawGeometry(null, SmilePen, Arc(91, 77, 9, 8, 0, 180));
        g.DrawGeometry(null, SmilePen, Arc(100, 77, 9, 8, 0, 180));

        if (pose.Tongue > 0.05f)
            Ellipse(g, NoseBrush, null, 97, 84, 8, 10 * pose.Tongue);
    }

    private static void DrawBristle(DrawingContext g, double cx, double cy, double rx, double ry, double from, double to, int count, double amount)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = (from + (to - from) * i / (count - 1)) * Math.PI / 180;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var inner = new Point(cx + cos * rx, cy + sin * ry);
            var length = (i % 2 == 0 ? 9 : 6) * amount;
            var tip = new Point(cx + cos * (rx + length), cy + sin * (ry + length));
            g.DrawLine(BristlePen, inner, tip);
        }
    }

    private static StreamGeometry Curve(Point start, Point first, Point second, Point end)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false);
            context.CubicBezierTo(first, second, end);
            context.EndFigure(false);
        }

        return geometry;
    }

    private static void DrawLickingPaw(DrawingContext g, KittenPose pose)
    {
        double lift = pose.Lick;
        var shoulder = new Point(110, 124);
        var rest = new Point(115, 144);
        var raised = new Point(101 + pose.HeadX * 0.5, 90 + pose.HeadY * 0.9);
        var tip = new Point(rest.X + (raised.X - rest.X) * lift, rest.Y + (raised.Y - rest.Y) * lift);

        var path = Curve(shoulder, new Point(shoulder.X + 20 * lift, shoulder.Y - 4), new Point(tip.X + 14 * lift, tip.Y + 18), tip);
        g.DrawGeometry(null, new Pen(LineBrush, 15, lineCap: PenLineCap.Round), path);
        g.DrawGeometry(null, new Pen(FurBrush, 10.2, lineCap: PenLineCap.Round), path);

        Ellipse(g, FurBrush, LinePen, tip.X - 8, tip.Y - 7, 16, 14);
        g.DrawLine(ToePen, new Point(tip.X - 2.5, tip.Y - 1), new Point(tip.X - 2.5, tip.Y + 4));
        g.DrawLine(ToePen, new Point(tip.X + 2.5, tip.Y - 1), new Point(tip.X + 2.5, tip.Y + 4));
    }

    private static void DrawTail(DrawingContext g, double sway, double puff)
    {
        var path = Curve(new Point(128, 134), new Point(172, 142), new Point(182 + sway * 0.4, 104), new Point(160 + sway, 84));
        g.DrawGeometry(null, new Pen(LineBrush, 15 + 9 * puff, lineCap: PenLineCap.Round), path);
        g.DrawGeometry(null, new Pen(FurBrush, 10.2 + 9 * puff, lineCap: PenLineCap.Round), path);
    }

    private static void DrawHeart(DrawingContext g, double x, double y, double amount, Color pink)
    {
        var brush = new ImmutableSolidColorBrush(Color.FromArgb((byte)Math.Min(255, 230 * amount), pink.R, pink.G, pink.B));
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(x, y + 4), true);
            context.CubicBezierTo(new Point(x - 1, y - 3), new Point(x - 11, y - 3), new Point(x - 11, y + 4));
            context.CubicBezierTo(new Point(x - 11, y + 10), new Point(x - 3, y + 13), new Point(x, y + 17));
            context.CubicBezierTo(new Point(x + 3, y + 13), new Point(x + 11, y + 10), new Point(x + 11, y + 4));
            context.CubicBezierTo(new Point(x + 11, y - 3), new Point(x + 1, y - 3), new Point(x, y + 4));
            context.EndFigure(true);
        }

        g.DrawGeometry(brush, null, geometry);
    }

    private static void DrawSleep(DrawingContext g, double time, double amount, Color accentStrong)
    {
        for (var i = 0; i < 3; i++)
        {
            var phase = (time * 0.45 + i / 3.0) % 1.0;
            var alpha = 210 * Math.Sin(phase * Math.PI);
            var shown = Math.Max(0, Math.Min(255, alpha * amount));
            var brush = new ImmutableSolidColorBrush(Color.FromArgb((byte)shown, accentStrong.R, accentStrong.G, accentStrong.B));
            var text = new FormattedText("z", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, SleepFace, SleepSizes[i], brush);
            g.DrawText(text, new Point(146 + phase * 22, 46 - phase * 46));
        }
    }
}
