using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace VpnClient.UI;

public static class Theme
{
    public static readonly Palette Light = new()
    {
        Window = Color.FromArgb(246, 241, 251),
        Sidebar = Color.FromArgb(238, 230, 248),
        SidebarHover = Color.FromArgb(226, 213, 244),
        Surface = Color.FromArgb(251, 248, 254),
        Card = Color.White,
        CardHover = Color.FromArgb(249, 244, 254),
        CardSelected = Color.FromArgb(242, 233, 253),
        Border = Color.FromArgb(233, 224, 245),
        Accent = Color.FromArgb(178, 144, 240),
        AccentStrong = Color.FromArgb(140, 106, 214),
        Pink = Color.FromArgb(246, 166, 196),
        Mint = Color.FromArgb(110, 206, 164),
        Text = Color.FromArgb(44, 36, 64),
        TextMuted = Color.FromArgb(108, 98, 134),
        HeroTop = Color.FromArgb(253, 241, 248),
        HeroBottom = Color.FromArgb(239, 231, 251),
        PowerOff = Color.White,
        TrackOff = Color.FromArgb(221, 212, 234),
        PingGood = Color.FromArgb(58, 176, 133),
        PingMid = Color.FromArgb(214, 150, 60),
        PingBad = Color.FromArgb(212, 108, 146)
    };

    public static readonly Palette Dark = new()
    {
        Window = Color.FromArgb(24, 20, 32),
        Sidebar = Color.FromArgb(20, 17, 27),
        SidebarHover = Color.FromArgb(44, 37, 58),
        Surface = Color.FromArgb(29, 25, 38),
        Card = Color.FromArgb(38, 33, 49),
        CardHover = Color.FromArgb(45, 39, 58),
        CardSelected = Color.FromArgb(55, 45, 76),
        Border = Color.FromArgb(54, 47, 68),
        Accent = Color.FromArgb(184, 154, 245),
        AccentStrong = Color.FromArgb(208, 186, 255),
        Pink = Color.FromArgb(240, 160, 194),
        Mint = Color.FromArgb(110, 214, 168),
        Text = Color.FromArgb(237, 231, 247),
        TextMuted = Color.FromArgb(150, 140, 172),
        HeroTop = Color.FromArgb(40, 30, 50),
        HeroBottom = Color.FromArgb(27, 23, 39),
        PowerOff = Color.FromArgb(44, 38, 58),
        TrackOff = Color.FromArgb(70, 62, 88),
        PingGood = Color.FromArgb(96, 212, 162),
        PingMid = Color.FromArgb(236, 182, 92),
        PingBad = Color.FromArgb(240, 134, 174)
    };

    private static Palette _palette = Light;
    private static readonly List<(Control Control, Func<Color>? Back, Func<Color>? Fore)> Bindings = new();

    public static bool IsDark => _palette == Dark;

    public static Color Window => _palette.Window;
    public static Color Sidebar => _palette.Sidebar;
    public static Color SidebarHover => _palette.SidebarHover;
    public static Color Surface => _palette.Surface;
    public static Color Card => _palette.Card;
    public static Color CardHover => _palette.CardHover;
    public static Color CardSelected => _palette.CardSelected;
    public static Color Border => _palette.Border;
    public static Color Accent => _palette.Accent;
    public static Color AccentStrong => _palette.AccentStrong;
    public static Color Pink => _palette.Pink;
    public static Color Mint => _palette.Mint;
    public static Color Text => _palette.Text;
    public static Color TextMuted => _palette.TextMuted;
    public static Color HeroTop => _palette.HeroTop;
    public static Color HeroBottom => _palette.HeroBottom;
    public static Color PowerOff => _palette.PowerOff;
    public static Color TrackOff => _palette.TrackOff;
    public static Color PingGood => _palette.PingGood;
    public static Color PingMid => _palette.PingMid;
    public static Color PingBad => _palette.PingBad;

    public static readonly Color Star = Color.FromArgb(247, 196, 72);
    public static readonly Color Infinity = Color.FromArgb(110, 164, 244);

    private static readonly Dictionary<string, Image?> Flags = new();

    public static readonly Font Title = MakeFont(26, FontStyle.Bold);
    public static readonly Font Body = MakeFont(14);
    public static readonly Font BodyBold = MakeFont(14, FontStyle.Bold);
    public static readonly Font CardTitle = MakeFont(15, FontStyle.Bold);
    public static readonly Font Caption = MakeFont(12);
    public static readonly Font CaptionBold = MakeFont(12, FontStyle.Bold);
    public static readonly Font Status = Fonts.Make(Fonts.SemiBold, 11.5f);
    public static readonly Font Timer = Fonts.Make(Fonts.SemiBold, 19);
    public static readonly Font ServerName = MakeFont(17, FontStyle.Bold);
    public static readonly Font Big = MakeFont(23, FontStyle.Bold);
    public static readonly Font Log = new("Consolas", 12, FontStyle.Regular, GraphicsUnit.Pixel);

    public static Font MakeFont(float pixels, FontStyle style = FontStyle.Regular)
    {
        var strong = (style & FontStyle.Bold) != 0;
        return Fonts.Make(strong ? Fonts.SemiBold : Fonts.Regular, pixels, style & ~FontStyle.Bold);
    }

    public static float S { get; private set; } = 0.9f;

    public static void SetScale(float scale) => S = Math.Max(0.5f, Math.Min(1.5f, scale));

    public static int Px(float value) => (int)Math.Round(value * S);

    public static Padding Px(int left, int top, int right, int bottom) => new(Px(left), Px(top), Px(right), Px(bottom));

    public static PointF Design(Point point) => new(point.X / S, point.Y / S);

    public static void Begin(Graphics g, Color background)
    {
        Smooth(g);
        g.Clear(background);
        g.ScaleTransform(S, S);
    }

    public static Font Scaled(Font font) => ScaledFont(font, S);

    private static readonly Dictionary<(Font Font, int Scale), Font> ScaledFonts = new();

    private static Font ScaledFont(Font font, float scale)
    {
        var key = (font, (int)Math.Round(scale * 1000));
        if (Math.Abs(scale - 1) < 0.001f)
            return font;
        if (!ScaledFonts.TryGetValue(key, out var scaled))
        {
            scaled = new Font(font.FontFamily, font.Size * scale, font.Style, font.Unit);
            ScaledFonts[key] = scaled;
        }
        return scaled;
    }

    public static Size Measure(string text, Font font)
    {
        var size = TextRenderer.MeasureText(text, ScaledFont(font, S), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        return new Size((int)Math.Ceiling(size.Width / S), (int)Math.Ceiling(size.Height / S));
    }

    public static T Bind<T>(T control, Func<Color>? back = null, Func<Color>? fore = null) where T : Control
    {
        Bindings.Add((control, back, fore));
        control.Disposed += (_, _) => Bindings.RemoveAll(b => b.Control == control);
        ApplyBinding(control, back, fore);
        return control;
    }

    public static void Use(bool dark)
    {
        _palette = dark ? Dark : Light;
        foreach (var (control, back, fore) in Bindings.ToList())
            ApplyBinding(control, back, fore);
    }

    private static void ApplyBinding(Control control, Func<Color>? back, Func<Color>? fore)
    {
        if (back != null)
            control.BackColor = back();
        if (fore != null)
            control.ForeColor = fore();
        control.Invalidate();
    }

    public static void Smooth(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = IsDark ? TextRenderingHint.AntiAlias : TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0)
        {
            path.AddRectangle(r);
            return path;
        }

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(Graphics g, Color color, RectangleF r, float radius)
    {
        using var path = RoundedRect(r, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    public static void DrawRounded(Graphics g, Color color, RectangleF r, float radius, float width = 1)
    {
        using var path = RoundedRect(r, radius);
        using var pen = new Pen(color, width);
        g.DrawPath(pen, path);
    }

    public static void DrawText(Graphics g, string text, Font font, Color color, RectangleF r,
        StringAlignment horizontal = StringAlignment.Near, StringAlignment vertical = StringAlignment.Center, bool wrap = false)
    {
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        flags |= wrap ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        flags |= horizontal switch
        {
            StringAlignment.Center => TextFormatFlags.HorizontalCenter,
            StringAlignment.Far => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };
        flags |= vertical switch
        {
            StringAlignment.Center => TextFormatFlags.VerticalCenter,
            StringAlignment.Far => TextFormatFlags.Bottom,
            _ => TextFormatFlags.Top
        };

        using var matrix = g.Transform;
        var e = matrix.Elements;
        var device = new RectangleF(r.X * e[0] + e[4], r.Y * e[3] + e[5], r.Width * e[0], r.Height * e[3]);
        var state = g.Save();
        g.ResetTransform();
        TextRenderer.DrawText(g, text, ScaledFont(font, e[0]), Rectangle.Round(device), color, flags);
        g.Restore(state);
    }

    public static void DrawBadge(Graphics g, RectangleF r, string? code)
    {
        var flag = code == null ? null : GetFlag(code);
        if (flag != null)
        {
            g.DrawImage(flag, r);
            return;
        }

        using (var brush = new LinearGradientBrush(r, Lighten(Accent, 0.3f), Accent, 45f))
            g.FillEllipse(brush, r);

        using var pen = new Pen(Color.White, Math.Max(1.2f, r.Width / 22f));
        var inner = new RectangleF(r.X + r.Width * 0.22f, r.Y + r.Height * 0.22f, r.Width * 0.56f, r.Height * 0.56f);
        g.DrawEllipse(pen, inner);
        g.DrawEllipse(pen, inner.X + inner.Width * 0.28f, inner.Y, inner.Width * 0.44f, inner.Height);
        g.DrawLine(pen, inner.X, inner.Y + inner.Height / 2, inner.Right, inner.Y + inner.Height / 2);
    }

    public static Image? GetFlag(string code)
    {
        code = code.ToLowerInvariant();
        if (Flags.TryGetValue(code, out var cached))
            return cached;

        Image? image = null;
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Flags", code + ".png");
        try
        {
            if (File.Exists(path))
                image = Image.FromFile(path);
        }
        catch (Exception)
        {
        }

        Flags[code] = image;
        return image;
    }

    public static void DrawArrow(Graphics g, float x, float cy, bool down, Color color)
    {
        using var pen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var top = cy - 6;
        var bottom = cy + 6;
        g.DrawLine(pen, x + 5, top, x + 5, bottom);
        var tip = down ? bottom : top;
        var back = down ? -4.5f : 4.5f;
        g.DrawLine(pen, x + 5, tip, x + 1, tip + back);
        g.DrawLine(pen, x + 5, tip, x + 9, tip + back);
    }

        public static void DrawBusyDots(Graphics g, float left, float cy, float time, float size)
    {
        var step = size * 1.85f;
        for (var i = 0; i < 3; i++)
        {
            var phase = (float)Math.Max(0, Math.Sin(time * 6 - i * 0.9));
            using var brush = new SolidBrush(Color.FromArgb((int)(170 + 85 * phase), i == 1 ? Pink : Accent));
            g.FillEllipse(brush, left + i * step, cy - size / 2 - phase * size * 0.55f, size, size);
        }
    }

    public static Color Lighten(Color c, float amount) => Color.FromArgb(
        c.A,
        (int)(c.R + (255 - c.R) * amount),
        (int)(c.G + (255 - c.G) * amount),
        (int)(c.B + (255 - c.B) * amount));

    public static Color PingColor(int? ms) => ms switch
    {
        null => TextMuted,
        < 0 => PingBad,
        < 150 => PingGood,
        < 400 => PingMid,
        _ => PingBad
    };

    public static string PingLabel(int? ms) => ms switch
    {
        null => "",
        < 0 => "n/a",
        _ => $"{ms} ms"
    };
}
