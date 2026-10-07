using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Tunnelka.Models;

namespace Tunnelka.UI;

public sealed class OverlayState
{
    public bool Connected { get; set; }
    public int? PingMs { get; set; }
    public int Loss { get; set; }
    public long Down { get; set; }
    public long Up { get; set; }
}

public sealed class OverlayWindow : Form
{
    private const int Margin = 16;

    private static readonly Color Back = Color.FromArgb(215, 24, 21, 36);
    private static readonly Color Edge = Color.FromArgb(60, 255, 255, 255);
    private static readonly Color Text = Color.FromArgb(240, 240, 248);
    private static readonly Color Muted = Color.FromArgb(160, 156, 180);
    private static readonly Color Good = Color.FromArgb(96, 214, 140);
    private static readonly Color Mid = Color.FromArgb(246, 198, 80);
    private static readonly Color Bad = Color.FromArgb(242, 98, 112);
    private static readonly Color Down = Color.FromArgb(150, 130, 255);

    public OverlayWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int layered = 0x80000;
            const int transparent = 0x20;
            const int toolWindow = 0x80;
            const int noActivate = 0x8000000;
            const int topmost = 0x8;
            var parameters = base.CreateParams;
            parameters.ExStyle |= layered | transparent | toolWindow | noActivate | topmost;
            return parameters;
        }
    }

    public void ShowState(OverlayState state, OverlayOptions options)
    {
        using var bitmap = Render(state, options);
        var area = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        var margin = (int)Math.Round(Margin * Theme.Base);
        var left = options.Corner is OverlayCorner.TopLeft or OverlayCorner.BottomLeft;
        var top = options.Corner is OverlayCorner.TopLeft or OverlayCorner.TopRight;
        var location = new Point(
            left ? area.Left + margin : area.Right - margin - bitmap.Width,
            top ? area.Top + margin : area.Bottom - margin - bitmap.Height);

        if (!Visible)
            Show();
        Push(bitmap, location);
    }

    public static Bitmap Render(OverlayState state, OverlayOptions options)
    {
        var k = options.Scale / 100f * Theme.Base;
        using var font = Fonts.Make(Fonts.SemiBold, 14 * k);
        using var label = Fonts.Make(Fonts.Regular, 12 * k);

        var parts = new List<Part>();
        if (!state.Connected)
            parts.Add(new Part(L.T("VPN выключен"), Muted, label));
        if (options.ShowPing)
            parts.Add(new Part(state.PingMs is { } ms ? L.F("{0} мс", ms) : "— " + L.T("мс"), Text, font, PingColor(state.PingMs)));
        if (options.ShowSpeed)
        {
            parts.Add(new Part("↓ " + ServerText.Bytes(state.Down) + L.T("/с"), Text, font, Joined: true));
            parts.Add(new Part("↑ " + ServerText.Bytes(state.Up) + L.T("/с"), Text, font));
        }
        if (options.ShowLoss)
            parts.Add(new Part(L.F("потери {0}%", state.Loss), state.Loss == 0 ? Muted : state.Loss < 10 ? Mid : Bad, font));

        var pad = 12 * k;
        var gap = 14 * k;
        var dot = 8 * k;
        var height = (int)Math.Ceiling(34 * k);

        using var measure = new Bitmap(1, 1);
        using var mg = Graphics.FromImage(measure);
        var dotWidth = dot + 6 * k;
        var widths = parts.Select(p => mg.MeasureString(p.Text, p.Font, PointF.Empty, StringFormat.GenericTypographic).Width + (p.Dot != null ? dotWidth : 0)).ToList();
        var width = (int)Math.Ceiling(pad * 2 + widths.Sum() + gap * Math.Max(0, parts.Count - 1)) + 2;

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        var rect = new RectangleF(0.5f, 0.5f, width - 1, height - 1);
        using (var path = Theme.RoundedRect(rect, rect.Height / 2))
        {
            using (var brush = new SolidBrush(Back))
                g.FillPath(brush, path);
            using (var pen = new Pen(Edge))
                g.DrawPath(pen, path);
        }

        var x = pad;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var textX = x;
            if (part.Dot is { } dotColor)
            {
                using var brush = new SolidBrush(dotColor);
                g.FillEllipse(brush, x, height / 2f - dot / 2, dot, dot);
                textX += dotWidth;
            }

            using (var brush = new SolidBrush(part.Color))
            {
                var size = g.MeasureString(part.Text, part.Font, PointF.Empty, StringFormat.GenericTypographic);
                g.DrawString(part.Text, part.Font, brush, textX, (height - size.Height) / 2, StringFormat.GenericTypographic);
            }

            x += widths[i] + gap;
            if (i < parts.Count - 1 && !part.Joined)
            {
                using var pen = new Pen(Color.FromArgb(50, 255, 255, 255));
                g.DrawLine(pen, x - gap / 2, height * 0.3f, x - gap / 2, height * 0.7f);
            }
        }

        return bitmap;
    }

    private sealed record Part(string Text, Color Color, Font Font, Color? Dot = null, bool Joined = false);

    private static Color PingColor(int? ms) => ms switch
    {
        null => Bad,
        < 150 => Good,
        < 400 => Mid,
        _ => Bad
    };

    private void Push(Bitmap bitmap, Point location)
    {
        var screen = GetDC(IntPtr.Zero);
        var memory = CreateCompatibleDC(screen);
        var hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
        var old = SelectObject(memory, hBitmap);
        try
        {
            var size = new NativeSize { Width = bitmap.Width, Height = bitmap.Height };
            var source = new NativePoint();
            var target = new NativePoint { X = location.X, Y = location.Y };
            var blend = new BlendFunction { Op = 0, Flags = 0, Alpha = 255, Format = 1 };
            UpdateLayeredWindow(Handle, screen, ref target, ref size, memory, ref source, 0, ref blend, 2);
        }
        finally
        {
            SelectObject(memory, old);
            DeleteObject(hBitmap);
            DeleteDC(memory);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BlendFunction
    {
        public byte Op;
        public byte Flags;
        public byte Alpha;
        public byte Format;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref NativePoint pptDst, ref NativeSize psize,
        IntPtr hdcSrc, ref NativePoint pprSrc, int crKey, ref BlendFunction pblend, int dwFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
