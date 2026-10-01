using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using VpnClient.Models;

namespace VpnClient.UI.Controls;

public class SubscriptionCard : ThemedControl
{
    private const float Pad = 16;
    private const float LineHeight = 21;
    private const float SymbolSize = 16;

    private static readonly Regex UrlPattern = new(@"https?://\S+", RegexOptions.Compiled);
    private static readonly string[] RefreshSymbols = { "\U0001F504", "\U0001F503", "♻" };

    private readonly List<(RectangleF Rect, Action Action)> _hits = new();
    private List<List<Token>> _lines = new();
    private RectangleF _hoverRect = RectangleF.Empty;
    private int _layoutWidth = -1;

    public event EventHandler? RefreshClicked;

    public SubscriptionCard(SubscriptionInfo info)
    {
        Info = info;
        Margin = Theme.Px(0, 4, 0, 10);
        Height = Theme.Px(120);
    }

    public SubscriptionInfo Info { get; }

    private sealed class Token
    {
        public Token(string text, bool symbol, string? url, float width, bool lineBreak = false)
        {
            Text = text;
            Symbol = symbol;
            Url = url;
            Width = width;
            LineBreak = lineBreak;
        }

        public string Text { get; }
        public bool Symbol { get; }
        public string? Url { get; }
        public float Width { get; }
        public bool LineBreak { get; }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width == _layoutWidth)
            return;

        _layoutWidth = Width;
        _lines = Wrap(Tokenize(Info.Announce), W - Pad * 2);

        var height = (int)(InfoTop + InfoRows * 22 + 8);
        if (_lines.Count > 0)
            height += (int)(14 + _lines.Count * LineHeight + 6);
        var device = Theme.Px(height);
        if (Height != device)
            Height = device;
    }

    private const float InfoTop = 66;
    private int InfoRows => Info.Total > 0 ? 2 : 1;

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
            foreach (Match match in UrlPattern.Matches(line))
            {
                AddText(tokens, line.Substring(position, match.Index - position));
                var url = match.Value.TrimEnd('.', ',', ')', '!');
                tokens.Add(new Token(url, false, url, Theme.Measure(url, Theme.CaptionBold).Width));
                position = match.Index + match.Value.Length;
            }
            AddText(tokens, line.Substring(position));
        }

        return tokens;
    }

    private static void AddText(List<Token> tokens, string text)
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

            foreach (var word in part.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                tokens.Add(new Token(word, false, null, Theme.Measure(word, Theme.Caption).Width));
        }
    }

    private static List<List<Token>> Wrap(List<Token> tokens, float maxWidth)
    {
        var lines = new List<List<Token>>();
        if (tokens.Count == 0)
            return lines;

        var space = Theme.Measure(" ", Theme.Caption).Width + 1;
        var current = new List<Token>();
        var width = 0f;

        foreach (var token in tokens)
        {
            if (token.LineBreak)
            {
                lines.Add(current);
                current = new List<Token>();
                width = 0;
                continue;
            }

            var needed = current.Count == 0 ? token.Width : width + space + token.Width;
            if (current.Count > 0 && needed > maxWidth)
            {
                lines.Add(current);
                current = new List<Token>();
                needed = token.Width;
            }

            current.Add(token);
            width = needed;
        }

        lines.Add(current);
        return lines;
    }

    protected override void Draw(Graphics g)
    {
        _hits.Clear();

        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        using (var brush = new LinearGradientBrush(rect, Theme.Card, Theme.Lighten(Theme.CardSelected, Theme.IsDark ? 0f : 0.3f), 90f))
        using (var path = Theme.RoundedRect(rect, 16))
            g.FillPath(brush, path);
        Theme.DrawRounded(g, Theme.Border, rect, 16);

        var refreshRect = new RectangleF(W - Pad - 32, 14, 32, 32);
        DrawRefresh(g, refreshRect);
        _hits.Add((refreshRect, () => RefreshClicked?.Invoke(this, EventArgs.Empty)));

        var titleRight = refreshRect.X - 8;
        if (Info.SupportUrl.Length > 0)
        {
            var supportRect = new RectangleF(refreshRect.X - 38, 14, 32, 32);
            DrawSupport(g, supportRect);
            _hits.Add((supportRect, () => Open(Info.SupportUrl)));
            titleRight = supportRect.X - 8;
        }

        var titleParts = ServerText.Parts(Info.Title, Info.Title);
        NamePainter.Draw(g, titleParts, Theme.CardTitle, Theme.Text, new RectangleF(Pad, 13, titleRight - Pad, 24));

        var updated = Info.UpdatedAt.Date == DateTime.Today ? $"{Info.UpdatedAt:HH:mm}" : $"{Info.UpdatedAt:dd.MM HH:mm}";
        Theme.DrawText(g, $"Обновлено в {updated} · раз в {Info.UpdateIntervalHours} ч",
            Theme.Caption, Theme.TextMuted, new RectangleF(Pad, 37, titleRight - Pad, 18));

        var y = InfoTop;
        DrawInfoRow(g, y, ExpireText(), ExpireColor());
        if (Info.Total > 0)
        {
            y += 22;
            DrawInfoRow(g, y, $"Трафик: {ServerText.Bytes(Info.Upload + Info.Download)} из {ServerText.Bytes(Info.Total)}", Theme.Text);
        }

        if (_lines.Count == 0)
            return;

        y += 30;
        using (var pen = new Pen(Theme.Border))
            g.DrawLine(pen, Pad, y - 8, W - Pad, y - 8);

        DrawAnnounce(g, y);
    }

    private void DrawInfoRow(Graphics g, float y, string text, Color color)
    {
        var icon = new RectangleF(Pad, y + 1, 16, 16);
        using (var pen = new Pen(Theme.Accent, 1.6f))
            g.DrawEllipse(pen, icon);
        using (var brush = new SolidBrush(Theme.Accent))
        {
            g.FillEllipse(brush, icon.X + 7, icon.Y + 3.5f, 2.2f, 2.2f);
            g.FillRectangle(brush, icon.X + 7.1f, icon.Y + 7, 2f, 5.5f);
        }

        Theme.DrawText(g, text, Theme.CaptionBold, color, new RectangleF(Pad + 24, y, W - Pad * 2 - 24, 18));
    }

    private void DrawAnnounce(Graphics g, float top)
    {
        var space = Theme.Measure(" ", Theme.Caption).Width + 1;

        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            var lineWidth = line.Sum(t => t.Width) + space * Math.Max(0, line.Count - 1);
            var x = (W - lineWidth) / 2;
            var y = top + i * LineHeight;

            foreach (var token in line)
            {
                var r = new RectangleF(x, y, token.Width, LineHeight);
                if (token.Symbol)
                {
                    var symbolRect = new RectangleF(x, y + (LineHeight - SymbolSize) / 2, SymbolSize, SymbolSize);
                    if (RefreshSymbols.Any(s => token.Text.StartsWith(s, StringComparison.Ordinal)))
                    {
                        var hit = RectangleF.Inflate(symbolRect, 4, 3);
                        Theme.FillRounded(g, Color.FromArgb(hit == _hoverRect ? 90 : 45, Theme.Accent), hit, 6);
                        _hits.Add((hit, () => RefreshClicked?.Invoke(this, EventArgs.Empty)));
                    }
                    NamePainter.Draw(g, new[] { new NamePart(token.Text, true) }, Theme.Caption, Theme.Text,
                        new RectangleF(x, y, SymbolSize + 2, LineHeight));
                }
                else if (token.Url != null)
                {
                    Theme.DrawText(g, token.Text, Theme.CaptionBold, Theme.AccentStrong, new RectangleF(x, y, token.Width + 2, LineHeight));
                    using var pen = new Pen(Color.FromArgb(140, Theme.AccentStrong));
                    g.DrawLine(pen, x, y + LineHeight - 3, x + token.Width, y + LineHeight - 3);
                    var url = token.Url;
                    _hits.Add((r, () => Open(url)));
                }
                else
                {
                    Theme.DrawText(g, token.Text, Theme.Caption, Theme.Text, new RectangleF(x, y, token.Width + 2, LineHeight));
                }

                x += token.Width + space;
            }
        }
    }

    private void DrawRefresh(Graphics g, RectangleF r)
    {
        Theme.FillRounded(g, r == _hoverRect ? Theme.CardHover : Color.FromArgb(0, Theme.Card), r, 10);
        using var pen = new Pen(Theme.TextMuted, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        g.DrawArc(pen, cx - 8, cy - 8, 16, 16, 40, 280);
        var angle = (40 + 280) * Math.PI / 180;
        var tip = new PointF(cx + 8 * (float)Math.Cos(angle), cy + 8 * (float)Math.Sin(angle));
        g.DrawLine(pen, tip.X, tip.Y, tip.X - 5, tip.Y - 1);
        g.DrawLine(pen, tip.X, tip.Y, tip.X + 0.5f, tip.Y - 5);
    }

    private void DrawSupport(Graphics g, RectangleF r)
    {
        Theme.FillRounded(g, r == _hoverRect ? Theme.CardHover : Color.FromArgb(0, Theme.Card), r, 10);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var plane = new[]
        {
            new PointF(cx - 9, cy - 1), new PointF(cx + 9, cy - 8), new PointF(cx + 5, cy + 9), new PointF(cx - 1, cy + 3)
        };
        using var brush = new SolidBrush(Theme.Accent);
        g.FillPolygon(brush, plane);
        using var pen = new Pen(Theme.Card, 1.4f);
        g.DrawLine(pen, cx - 1, cy + 3, cx + 9, cy - 8);
    }

    private string ExpireText()
    {
        if (Info.Expire == null)
            return "Подписка без срока";

        var expire = Info.Expire.Value;
        var left = expire - DateTime.Now;
        if (left <= TimeSpan.Zero)
            return $"Подписка истекла {expire:dd.MM.yyyy}";

        var tail = left.TotalDays >= 1
            ? ServerText.Plural((int)left.TotalDays, "день", "дня", "дней")
            : ServerText.Plural(Math.Max(1, (int)left.TotalHours), "час", "часа", "часов");
        return $"Истекает {expire:dd.MM.yyyy} · осталось {tail}";
    }

    private Color ExpireColor()
    {
        if (Info.Expire == null)
            return Theme.Text;

        var left = Info.Expire.Value - DateTime.Now;
        return left <= TimeSpan.Zero ? Theme.PingBad : left.TotalDays < 3 ? Theme.PingMid : Theme.Text;
    }

    private static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hover = RectangleF.Empty;
        foreach (var (rect, _) in _hits)
        {
            if (rect.Contains(Theme.Design(e.Location)))
                hover = rect;
        }

        Cursor = hover.IsEmpty ? Cursors.Default : Cursors.Hand;
        if (hover != _hoverRect)
        {
            _hoverRect = hover;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverRect = RectangleF.Empty;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left)
            return;

        foreach (var (rect, action) in _hits)
        {
            if (rect.Contains(Theme.Design(e.Location)))
            {
                action();
                return;
            }
        }
    }
}
