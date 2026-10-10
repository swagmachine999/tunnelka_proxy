using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Tunnelka.UI;

namespace Tunnelka.Next;

public static class SymbolPainter
{
    private static readonly IBrush StarBrush = new SolidColorBrush(Color.FromRgb(247, 196, 72));
    private static readonly IBrush InfinityBrush = new SolidColorBrush(Color.FromRgb(110, 164, 244));

    public static void Draw(DrawingContext context, string symbol, Rect rect, IBrush? fallback)
    {
        if (symbol == "♾" || symbol == "♾️")
        {
            DrawInfinity(context, rect);
            return;
        }

        var image = EmojiCache.Get(symbol);
        if (image != null)
        {
            context.DrawImage(image, rect);
            return;
        }

        if (symbol.StartsWith("⭐", StringComparison.Ordinal) || symbol.StartsWith("★", StringComparison.Ordinal))
        {
            DrawStar(context, rect);
            return;
        }

        var text = new FormattedText(symbol, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Emoji"), rect.Height * 0.78, fallback);
        context.DrawText(text, new Point(rect.X + (rect.Width - text.Width) / 2, rect.Y + (rect.Height - text.Height) / 2));
    }

    private static void DrawStar(DrawingContext context, Rect rect)
    {
        var cx = rect.X + rect.Width / 2;
        var cy = rect.Y + rect.Height / 2 + rect.Height * 0.04;
        var outer = rect.Width / 2;
        var inner = outer * 0.48;
        var points = new Point[10];
        for (var i = 0; i < 10; i++)
        {
            var radius = i % 2 == 0 ? outer : inner;
            var angle = Math.PI / 5 * i - Math.PI / 2;
            points[i] = new Point(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
        }

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(points[0], true);
            for (var i = 1; i < points.Length; i++)
                stream.LineTo(points[i]);
            stream.EndFigure(true);
        }

        var pen = new Pen(StarBrush, 1.4, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(StarBrush, pen, geometry);
    }

    private static void DrawInfinity(DrawingContext context, Rect rect)
    {
        var cy = rect.Y + rect.Height / 2;
        var w = rect.Width * 1.1;
        var x = rect.X - (w - rect.Width) / 2;
        var h = rect.Height * 0.42;

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(new Point(x + w / 2, cy), false);
            stream.CubicBezierTo(new Point(x + w * 0.75, cy - h), new Point(x + w, cy - h), new Point(x + w, cy));
            stream.CubicBezierTo(new Point(x + w, cy + h), new Point(x + w * 0.75, cy + h), new Point(x + w / 2, cy));
            stream.CubicBezierTo(new Point(x + w * 0.25, cy - h), new Point(x, cy - h), new Point(x, cy));
            stream.CubicBezierTo(new Point(x, cy + h), new Point(x + w * 0.25, cy + h), new Point(x + w / 2, cy));
            stream.EndFigure(false);
        }

        var pen = new Pen(InfinityBrush, Math.Max(1.6, rect.Height / 8), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(null, pen, geometry);
    }
}

public sealed class NameView : Control
{
    private const double Gap = 5;

    public static readonly StyledProperty<double> FontSizeProperty = TextBlock.FontSizeProperty.AddOwner<NameView>();
    public static readonly StyledProperty<FontWeight> FontWeightProperty = TextBlock.FontWeightProperty.AddOwner<NameView>();
    public static readonly StyledProperty<FontFamily> FontFamilyProperty = TextBlock.FontFamilyProperty.AddOwner<NameView>();
    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextBlock.ForegroundProperty.AddOwner<NameView>();

    private IReadOnlyList<NamePart> _parts = Array.Empty<NamePart>();
    private string? _code;

    static NameView()
    {
        AffectsMeasure<NameView>(FontSizeProperty, FontWeightProperty, FontFamilyProperty);
        AffectsRender<NameView>(ForegroundProperty);
    }

    public NameView()
    {
        ClipToBounds = true;
        IsHitTestVisible = false;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double FlagSize { get; set; }

    public void SetParts(IReadOnlyList<NamePart> parts, string? countryCode)
    {
        _parts = parts;
        _code = countryCode;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private double SymbolSize => FontSize * 1.15;

    private double EffectiveFlagSize => FlagSize > 0 ? FlagSize : FontSize * 1.4;

    private Typeface Face => new(FontFamily, FontStyle.Normal, FontWeight);

    private FormattedText Make(string text, IBrush? brush) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Face, FontSize, brush);

    private bool HasFlag => _code != null && FlagCache.Get(_code) != null;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = Make("Ag", null).Height;

        if (HasFlag)
        {
            width += EffectiveFlagSize + Gap;
            height = Math.Max(height, EffectiveFlagSize);
        }

        foreach (var part in _parts)
            width += PartWidth(part) + Gap;

        width = Math.Max(0, width - Gap);
        height = Math.Max(height, _parts.Any(p => p.IsSymbol) ? SymbolSize : 0);
        if (!double.IsInfinity(availableSize.Width))
            width = Math.Min(width, availableSize.Width);

        return new Size(width, height);
    }

    private double PartWidth(NamePart part) =>
        part.IsSymbol ? SymbolSize : Make(part.Text, null).Width + 1;

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var cy = Bounds.Height / 2;
        var brush = Foreground ?? Brushes.Gray;
        var x = 0.0;

        if (_code != null)
        {
            var flag = FlagCache.Get(_code);
            if (flag != null)
            {
                var size = EffectiveFlagSize;
                context.DrawImage(flag, new Rect(x, cy - size / 2, size, size));
                x += size + Gap;
            }
        }

        foreach (var part in _parts)
        {
            if (x >= width)
                break;

            if (part.IsSymbol)
            {
                var size = SymbolSize;
                SymbolPainter.Draw(context, part.Text, new Rect(x, cy - size / 2, size, size), brush);
                x += size + Gap;
                continue;
            }

            var full = Make(part.Text, brush);
            var remaining = width - x;
            var shown = full;
            if (full.Width > remaining)
                shown = Make(Trim(part.Text, remaining, brush), brush);

            context.DrawText(shown, new Point(x, cy - shown.Height / 2));
            x += full.Width + 1 + Gap;
        }
    }

    private string Trim(string text, double limit, IBrush brush)
    {
        var low = 0;
        var high = text.Length;
        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (Make(text.Substring(0, middle).TrimEnd() + "…", brush).Width <= limit)
                low = middle;
            else
                high = middle - 1;
        }

        return text.Substring(0, low).TrimEnd() + "…";
    }
}
