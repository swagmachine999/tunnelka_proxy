using Avalonia.Media.Imaging;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Tunnelka.UI;

namespace Tunnelka.Next;

public static class ServerLinks
{
    public static void Open(string url)
    {
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }
}

public sealed class AnnounceView : Control
{
    private const double LineHeight = SubscriptionFonts.BodyLine;
    private const double FontSize = SubscriptionFonts.Body;
    private const double SymbolSize = FontSize * 1.15;

    private static readonly Regex UrlPattern = new(@"https?://\S+", RegexOptions.Compiled);
    private static readonly Regex ColorPattern = new(@"^#([0-9A-Fa-f]{6})", RegexOptions.Compiled);
    private static readonly string[] RefreshSymbols = { "\U0001F504", "\U0001F503", "♻" };

    private sealed class Token
    {
        public Token(string text, bool symbol, string? url, double width, bool lineBreak = false, Color? color = null)
        {
            Text = text;
            Symbol = symbol;
            Url = url;
            Width = width;
            LineBreak = lineBreak;
            Color = color;
        }

        public string Text { get; }
        public bool Symbol { get; }
        public string? Url { get; }
        public double Width { get; }
        public bool LineBreak { get; }
        public Color? Color { get; }
    }

    private sealed class Placed
    {
        public Placed(Token token, Rect rect, Rect hit, bool refresh)
        {
            Token = token;
            Rect = rect;
            Hit = hit;
            Refresh = refresh;
        }

        public Token Token { get; }
        public Rect Rect { get; }
        public Rect Hit { get; }
        public bool Refresh { get; }
    }

    private string _text = "";
    private List<Token> _tokens = new();
    private List<Placed> _placed = new();
    private int _lineCount;
    private double _layoutWidth = -1;
    private Rect _hover;

    public AnnounceView()
    {
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    public event Action? RefreshClicked;

    public void SetText(string text)
    {
        if (_text == text)
            return;

        _text = text;
        _tokens = Tokenize(text);
        _layoutWidth = -1;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private static Typeface Face(bool bold) =>
        new(new FontFamily("Segoe UI"), FontStyle.Normal, bold ? FontWeight.SemiBold : FontWeight.Normal);

    private static double MeasureText(string text, bool bold) =>
        new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Face(bold), FontSize, null).Width;

    private static double SpaceWidth() => Math.Max(2, MeasureText("a a", false) - MeasureText("aa", false) - 1);

    private static List<Token> Tokenize(string announce)
    {
        var tokens = new List<Token>();
        if (announce.Length == 0)
            return tokens;

        var paragraphs = announce.Replace("\r", "").Split('\n');
        for (var p = 0; p < paragraphs.Length; p++)
        {
            if (p > 0)
                tokens.Add(new Token("", false, null, 0, true));

            var line = paragraphs[p];
            var position = 0;
            Color? color = null;
            foreach (Match match in UrlPattern.Matches(line))
            {
                AddText(tokens, line.Substring(position, match.Index - position), ref color);
                var url = match.Value.TrimEnd('.', ',', ')', '!');
                tokens.Add(new Token(url, false, url, MeasureText(url, true)));
                position = match.Index + match.Value.Length;
            }

            AddText(tokens, line.Substring(position), ref color);
        }

        return tokens;
    }

    private static void AddText(List<Token> tokens, string text, ref Color? color)
    {
        if (text.Trim().Length == 0)
            return;

        foreach (var part in ServerText.Parts(text, null))
        {
            if (part.IsSymbol)
            {
                tokens.Add(new Token(part.Text, true, null, SymbolSize));
                continue;
            }

            foreach (var raw in part.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var word = raw;
                var match = ColorPattern.Match(word);
                if (match.Success)
                {
                    color = Color.Parse("#" + match.Groups[1].Value);
                    word = word.Substring(match.Length);
                    if (word.Length == 0)
                        continue;
                }

                tokens.Add(new Token(word, false, null, MeasureText(word, color != null), color: color));
            }
        }
    }

    private void Relayout(double width)
    {
        _layoutWidth = width;
        var lines = new List<List<Token>>();
        if (_tokens.Count > 0)
        {
            var space = SpaceWidth();
            var current = new List<Token>();
            var used = 0.0;
            foreach (var token in _tokens)
            {
                if (token.LineBreak)
                {
                    lines.Add(current);
                    current = new List<Token>();
                    used = 0;
                    continue;
                }

                var needed = current.Count == 0 ? token.Width : used + space + token.Width;
                if (current.Count > 0 && needed > width)
                {
                    lines.Add(current);
                    current = new List<Token>();
                    needed = token.Width;
                }

                current.Add(token);
                used = needed;
            }

            lines.Add(current);
        }

        _lineCount = lines.Count;
        _placed = new List<Placed>();
        var gap = SpaceWidth();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineWidth = line.Sum(t => t.Width) + gap * Math.Max(0, line.Count - 1);
            var x = (width - lineWidth) / 2;
            var y = i * LineHeight;
            foreach (var token in line)
            {
                var rect = new Rect(x, y, token.Width, LineHeight);
                var refresh = token.Symbol && RefreshSymbols.Any(s => token.Text.StartsWith(s, StringComparison.Ordinal));
                var hit = refresh
                    ? new Rect(x - 4, y + (LineHeight - SymbolSize) / 2 - 3, SymbolSize + 8, SymbolSize + 6)
                    : rect;
                _placed.Add(new Placed(token, rect, hit, refresh));
                x += token.Width + gap;
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width;
        Relayout(width);
        return new Size(width, _lineCount * LineHeight);
    }

    private Placed? HitAt(Point point)
    {
        foreach (var item in _placed)
        {
            if ((item.Refresh || item.Token.Url != null) && item.Hit.Contains(point))
                return item;
        }

        return null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var item = HitAt(e.GetPosition(this));
        var hover = item?.Hit ?? default;
        Cursor = item == null ? null : new Cursor(StandardCursorType.Hand);
        if (hover == _hover)
            return;

        _hover = hover;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Cursor = null;
        if (_hover == default)
            return;

        _hover = default;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var item = HitAt(e.GetPosition(this));
        if (item == null)
            return;

        e.Handled = true;
        if (item.Refresh)
            RefreshClicked?.Invoke();
        else if (item.Token.Url != null)
            ServerLinks.Open(item.Token.Url);
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        if (Math.Abs(_layoutWidth - Bounds.Width) > 0.5)
            Relayout(Bounds.Width);

        var text = Ui.Brush(this, "TextBrush");
        var strong = Ui.Brush(this, "AccentStrongBrush");
        var accent = Ui.Color(this, "AccentColor");

        foreach (var item in _placed)
        {
            var token = item.Token;
            var rect = item.Rect;
            if (token.Symbol)
            {
                if (item.Refresh)
                {
                    var fill = new SolidColorBrush(ServerRes.Alpha(accent, item.Hit == _hover ? 90.0 / 255 : 45.0 / 255));
                    context.DrawRectangle(fill, null, item.Hit, 6, 6);
                }

                var top = rect.Y + (LineHeight - SymbolSize) / 2;
                SymbolPainter.Draw(context, token.Text, new Rect(rect.X, top, SymbolSize, SymbolSize), text);
                continue;
            }

            if (token.Url != null)
            {
                Draw(context, token.Text, true, strong, rect);
                var line = new Pen(new SolidColorBrush(ServerRes.Alpha(Ui.Color(this, "AccentStrongColor"), 140.0 / 255)), 1);
                context.DrawLine(line, new Point(rect.X, rect.Bottom - 3), new Point(rect.X + token.Width, rect.Bottom - 3));
                continue;
            }

            if (token.Color is { } color)
            {
                Draw(context, token.Text, true, new SolidColorBrush(color), rect);
                continue;
            }

            Draw(context, token.Text, false, text, rect);
        }
    }

    private static void Draw(DrawingContext context, string text, bool bold, IBrush brush, Rect rect)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Face(bold), FontSize, brush);
        context.DrawText(formatted, new Point(rect.X, rect.Y + (rect.Height - formatted.Height) / 2));
    }
}
