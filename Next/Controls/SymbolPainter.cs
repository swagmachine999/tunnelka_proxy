using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace Tunnelka.Next;

public static class SymbolPainter
{
    public static void Draw(DrawingContext context, string symbol, Rect rect, IBrush? fallback)
    {
        var image = EmojiCache.Get(symbol);
        if (image != null)
        {
            context.DrawImage(image, rect);
            return;
        }

        if (symbol.StartsWith("♾", StringComparison.Ordinal))
            SymbolShapes.DrawInfinity(context, rect);
        else if (symbol.StartsWith("⭐", StringComparison.Ordinal) || symbol.StartsWith("★", StringComparison.Ordinal))
            SymbolShapes.DrawStar(context, rect);
        else
            DrawText(context, symbol, rect, fallback);
    }

    private static void DrawText(DrawingContext context, string symbol, Rect rect, IBrush? brush)
    {
        var text = new FormattedText(symbol, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Emoji"), rect.Height * 0.78, brush);
        context.DrawText(text, new Point(rect.X + (rect.Width - text.Width) / 2, rect.Y + (rect.Height - text.Height) / 2));
    }
}
