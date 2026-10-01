using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace VpnClient.UI;

public sealed class Palette
{
    public Color Window;
    public Color Sidebar;
    public Color SidebarHover;
    public Color Surface;
    public Color Card;
    public Color CardHover;
    public Color CardSelected;
    public Color Border;
    public Color Accent;
    public Color AccentStrong;
    public Color Pink;
    public Color Mint;
    public Color Text;
    public Color TextMuted;
    public Color HeroTop;
    public Color HeroBottom;
    public Color PowerOff;
    public Color TrackOff;
    public Color PingGood;
    public Color PingMid;
    public Color PingBad;
}

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
        Text = Color.FromArgb(74, 62, 98),
        TextMuted = Color.FromArgb(150, 139, 174),
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

    private static readonly PrivateFontCollection Fonts = new();
    private static readonly FontFamily Family = LoadFamily();
    private static readonly Dictionary<string, Image?> Flags = new();

    public static readonly Font Title = MakeFont(25, FontStyle.Bold);
    public static readonly Font Body = MakeFont(13);
    public static readonly Font BodyBold = MakeFont(13, FontStyle.Bold);
    public static readonly Font CardTitle = MakeFont(14, FontStyle.Bold);
    public static readonly Font Caption = MakeFont(11);
    public static readonly Font CaptionBold = MakeFont(11, FontStyle.Bold);
    public static readonly Font Status = MakeFont(11, FontStyle.Bold);
    public static readonly Font Timer = MakeFont(17, FontStyle.Bold);
    public static readonly Font ServerName = MakeFont(16, FontStyle.Bold);
    public static readonly Font Big = MakeFont(22, FontStyle.Bold);
    public static readonly Font Log = new("Consolas", 12, FontStyle.Regular, GraphicsUnit.Pixel);

    public static Font MakeFont(float pixels, FontStyle style = FontStyle.Regular) =>
        new(Family, pixels, style, GraphicsUnit.Pixel);

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
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
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
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = horizontal,
            LineAlignment = vertical,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = wrap ? 0 : StringFormatFlags.NoWrap
        };
        g.DrawString(text, font, brush, r, format);
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

    public static Color Lighten(Color c, float amount) => Color.FromArgb(
        c.A,
        (int)(c.R + (255 - c.R) * amount),
        (int)(c.G + (255 - c.G) * amount),
        (int)(c.B + (255 - c.B) * amount));

    public static Color PingColor(int? ms) => ms switch
    {
        null => TextMuted,
        < 0 => PingBad,
        < 120 => PingGood,
        < 300 => PingMid,
        _ => PingBad
    };

    public static string PingLabel(int? ms) => ms switch
    {
        null => "",
        < 0 => "n/a",
        _ => $"{ms} ms"
    };

    private static FontFamily LoadFamily()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
            foreach (var file in new[] { "Inter-Regular.ttf", "Inter-Bold.ttf" })
            {
                var path = Path.Combine(dir, file);
                if (!File.Exists(path))
                    continue;

                Fonts.AddFontFile(path);
                try
                {
                    AddFontResourceEx(path, 0x10, IntPtr.Zero);
                }
                catch (Exception)
                {
                }
            }

            if (Fonts.Families.Length > 0)
                return Fonts.Families[0];
        }
        catch (Exception)
        {
        }

        return new FontFamily("Segoe UI");
    }

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern int AddFontResourceEx(string name, uint flags, IntPtr reserved);
}
