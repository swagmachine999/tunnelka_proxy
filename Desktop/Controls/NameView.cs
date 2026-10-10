using Avalonia.Media.Imaging;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Tunnelka.UI;

namespace Tunnelka.Desktop;

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

    public void SetParts(IReadOnlyList<NamePart> parts, string? countryCode)
    {
        _parts = parts;
        _code = countryCode;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private double SymbolSize => FontSize * 1.15;

    private double FlagSize => FontSize * 1.4;

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
            width += FlagSize + Gap;
            height = Math.Max(height, FlagSize);
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
                var size = FlagSize;
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
