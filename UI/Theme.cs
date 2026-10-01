using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace VpnClient.UI;

public static class Theme
{
    public static readonly Color Window = Color.FromArgb(246, 241, 251);
    public static readonly Color Sidebar = Color.FromArgb(238, 230, 248);
    public static readonly Color SidebarHover = Color.FromArgb(226, 213, 244);
    public static readonly Color Surface = Color.FromArgb(251, 248, 254);
    public static readonly Color Card = Color.White;
    public static readonly Color CardHover = Color.FromArgb(249, 244, 254);
    public static readonly Color CardSelected = Color.FromArgb(242, 233, 253);
    public static readonly Color Border = Color.FromArgb(233, 224, 245);
    public static readonly Color Accent = Color.FromArgb(178, 144, 240);
    public static readonly Color AccentDark = Color.FromArgb(140, 106, 214);
    public static readonly Color Pink = Color.FromArgb(246, 166, 196);
    public static readonly Color Mint = Color.FromArgb(110, 206, 164);
    public static readonly Color Text = Color.FromArgb(74, 62, 98);
    public static readonly Color TextMuted = Color.FromArgb(156, 145, 178);
    public static readonly Color HeroTop = Color.FromArgb(253, 241, 248);
    public static readonly Color HeroBottom = Color.FromArgb(239, 231, 251);
    public static readonly Color LogBack = Color.FromArgb(250, 246, 253);
    public static readonly Color PingGood = Color.FromArgb(58, 176, 133);
    public static readonly Color PingMid = Color.FromArgb(214, 150, 60);
    public static readonly Color PingBad = Color.FromArgb(212, 108, 146);

    private static readonly Color[] BadgeColors =
    {
        Color.FromArgb(178, 144, 240),
        Color.FromArgb(246, 150, 186),
        Color.FromArgb(120, 196, 226),
        Color.FromArgb(110, 200, 160),
        Color.FromArgb(244, 176, 120),
        Color.FromArgb(150, 160, 236),
        Color.FromArgb(226, 140, 210)
    };

    public const string FontName = "Segoe UI";

    public static readonly Font Title = MakeFont(26, FontStyle.Bold);
    public static readonly Font Body = MakeFont(13);
    public static readonly Font BodyBold = MakeFont(13, FontStyle.Bold);
    public static readonly Font CardTitle = MakeFont(14, FontStyle.Bold);
    public static readonly Font Caption = MakeFont(11);
    public static readonly Font CaptionBold = MakeFont(11, FontStyle.Bold);
    public static readonly Font Status = MakeFont(12, FontStyle.Bold);
    public static readonly Font Timer = MakeFont(19, FontStyle.Bold);
    public static readonly Font ServerName = MakeFont(16, FontStyle.Bold);
    public static readonly Font Log = new("Consolas", 12, FontStyle.Regular, GraphicsUnit.Pixel);

    public static Font MakeFont(float pixels, FontStyle style = FontStyle.Regular) =>
        new(FontName, pixels, style, GraphicsUnit.Pixel);

    public static void Smooth(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    }

    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
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
        StringAlignment horizontal = StringAlignment.Near, StringAlignment vertical = StringAlignment.Center)
    {
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = horizontal,
            LineAlignment = vertical,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(text, font, brush, r, format);
    }

    public static void DrawBadge(Graphics g, RectangleF r, string? code)
    {
        var color = code == null ? Accent : BadgeColors[(code[0] * 31 + code[1]) % BadgeColors.Length];
        using (var brush = new LinearGradientBrush(r, Lighten(color, 0.25f), color, 45f))
            g.FillEllipse(brush, r);

        var text = code ?? "•";
        using var font = MakeFont(r.Height * 0.36f, FontStyle.Bold);
        DrawText(g, text, font, Color.White, r, StringAlignment.Center);
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
}
