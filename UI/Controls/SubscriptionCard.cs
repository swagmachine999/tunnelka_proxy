using System.Diagnostics;
using System.Drawing.Drawing2D;
using Tunnelka.Models;

namespace Tunnelka.UI.Controls;

public class SubscriptionCard : ThemedControl
{
    private const float Pad = 16;
    private const float LineHeight = 21;
    private const float SymbolSize = AnnounceLayout.SymbolSize;

    private static readonly string[] RefreshSymbols = { "\U0001F504", "\U0001F503", "♻" };

    private readonly List<(RectangleF Rect, Action Action)> _hits = new();
    private List<List<AnnounceToken>> _lines = new();
    private RectangleF _hoverRect = RectangleF.Empty;
    private int _layoutWidth = -1;

    public event EventHandler? RefreshClicked;
    public event EventHandler? PingClicked;
    public event EventHandler? CollapseClicked;
    public event EventHandler<Point>? MenuClicked;

    public SubscriptionCard(SubscriptionInfo info)
    {
        Info = info;
        Margin = Theme.Px(0, 4, 0, 6);
        Height = Theme.Px(120);
    }

    public SubscriptionInfo Info { get; }

    public int ServerCount { get; set; }

    private const float DoneSeconds = 2.4f;
    private RefreshStatus? _status;

    public RefreshStatus? Status
    {
        get => _status;
        set
        {
            _status = value;
            UpdateLayout();
            Invalidate();
            Animate();
        }
    }

    private bool Failed => _status?.State == RefreshState.Failed;

    private float SinceStatus => _status == null ? float.MaxValue : (float)(DateTime.Now - _status.At).TotalSeconds;

    protected override bool AnimateMore() =>
        _status?.State == RefreshState.Busy || (_status?.State == RefreshState.Done && SinceStatus < DoneSeconds + 0.3f);

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        var warning = WarningText();
        if (Width == _layoutWidth && warning == _layoutWarning && Failed == _layoutFailed)
            return;

        _layoutWidth = Width;
        _layoutWarning = warning;
        _layoutFailed = Failed;
        _lines = AnnounceLayout.Build(Info.Announce, W - Pad * 2);
        _warningHeight = warning == null ? 0
            : Theme.MeasureWrapped(warning, Theme.CaptionBold, W - Pad * 2 - WarningIndent - 12) + 20 + (Info.SupportUrl.Length > 0 ? 22 : 0);

        var height = (int)(InfoTop + InfoRows * 22 + 8 + (_warningHeight > 0 ? _warningHeight + 10 : 0));
        if (_lines.Count > 0)
            height += (int)(14 + _lines.Count * LineHeight + 6);
        var device = Theme.Px(height + Theme.ShadowBottom);
        if (Height != device)
            Height = device;
    }

    private const float InfoTop = 70;
    private const float WarningIndent = 40;
    private string? _layoutWarning;
    private float _warningHeight;
    private bool _layoutFailed;
    private int InfoRows => (Info.Total > 0 ? 2 : 1) + (Failed ? 1 : 0);

    protected override bool CachePaint => true;

    protected override void Draw(Graphics g)
    {
        _hits.Clear();

        var rect = Theme.CardRect(W, H);
        Theme.DrawShadow(g, rect, 16);
        using (var brush = new LinearGradientBrush(rect, Theme.Card, Theme.Lighten(Theme.CardSelected, Theme.IsDark ? 0f : 0.3f), 90f))
        using (var path = Theme.RoundedRect(rect, 16))
            g.FillPath(brush, path);
        Theme.DrawRounded(g, Theme.Border, rect, 16);

        var x = W - Pad - IconSize;
        AddIcon(g, new RectangleF(x, 12, IconSize, IconSize), DrawMenu, r =>
            MenuClicked?.Invoke(this, new Point((int)(r.Left * Theme.S), (int)(r.Bottom * Theme.S))));
        x -= IconSize + 2;
        AddIcon(g, new RectangleF(x, 12, IconSize, IconSize), DrawPing, _ => PingClicked?.Invoke(this, EventArgs.Empty));
        x -= IconSize + 2;
        AddIcon(g, new RectangleF(x, 12, IconSize, IconSize), DrawRefreshState, _ => RefreshClicked?.Invoke(this, EventArgs.Empty));
        if (Info.SupportUrl.Length > 0)
        {
            x -= IconSize + 2;
            AddIcon(g, new RectangleF(x, 12, IconSize, IconSize), DrawSupport, _ => Open(Info.SupportUrl));
        }

        var titleRect = new RectangleF(Pad - 4, 10, x - Pad, 32);
        _hits.Add((titleRect, () => CollapseClicked?.Invoke(this, EventArgs.Empty)));
        DrawChevron(g, Pad + 4, 26, Info.Collapsed);

        var titleParts = ServerText.Parts(Info.Title, Info.Title);
        NamePainter.Draw(g, titleParts, Theme.CardTitle, Theme.Text, new RectangleF(Pad + 16, 14, x - Pad - 20, 24));

        var updated = Info.UpdatedAt == default ? L.T("никогда")
            : Info.UpdatedAt.Date == DateTime.Today ? L.F("в {0:HH:mm}", Info.UpdatedAt) : $"{Info.UpdatedAt:dd.MM HH:mm}";
        var count = ServerText.Plural(ServerCount, L.T("сервер"), L.T("сервера"), L.T("серверов"));
        Theme.DrawText(g, L.F("{0} · обновлено {1} · раз в {2} ч", count, updated, Info.UpdateIntervalHours),
            Theme.Caption, Theme.TextMuted, new RectangleF(Pad + 16, 42, W - Pad * 2 - 16, 18));

        var y = InfoTop;
        DrawInfoRow(g, y, ExpireText(), ExpireColor());
        if (Info.Total > 0)
        {
            y += 22;
            DrawInfoRow(g, y, L.F("Трафик: {0} из {1}", ServerText.Bytes(Info.Upload + Info.Download), ServerText.Bytes(Info.Total)), Theme.Text);
        }

        if (Failed)
        {
            y += 22;
            DrawWarningMark(g, Pad + 8, y + 1, 1f);
            Theme.DrawText(g, L.F("Не удалось обновить: {0}", _status!.Message), Theme.CaptionBold, Theme.PingMid,
                new RectangleF(Pad + 24, y, W - Pad * 2 - 24, 18));
        }

        if (_layoutWarning != null)
        {
            DrawWarning(g, new RectangleF(Pad, y + 28, W - Pad * 2, _warningHeight), _layoutWarning);
            y += _warningHeight + 10;
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
        using (var pen = Theme.IconPen(Theme.Accent))
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
                else if (token.Color is { } color)
                {
                    Theme.DrawText(g, token.Text, Theme.CaptionBold, color, new RectangleF(x, y, token.Width + 2, LineHeight));
                }
                else
                {
                    Theme.DrawText(g, token.Text, Theme.Caption, Theme.Text, new RectangleF(x, y, token.Width + 2, LineHeight));
                }

                x += token.Width + space;
            }
        }
    }

    private const float IconSize = 28;

    private void AddIcon(Graphics g, RectangleF r, Action<Graphics, RectangleF> draw, Action<RectangleF> click)
    {
        if (r == _hoverRect)
            Theme.FillRounded(g, Theme.CardHover, r, 9);
        draw(g, r);
        _hits.Add((r, () => click(r)));
    }

    private void DrawRefreshState(Graphics g, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        switch (_status?.State)
        {
            case RefreshState.Busy:
                DrawRefresh(g, cx, cy, (float)(DateTime.Now.TimeOfDay.TotalSeconds * 360 % 360), Theme.Accent);
                return;
            case RefreshState.Done when SinceStatus < DoneSeconds:
                DrawCheck(g, cx, cy, SinceStatus);
                return;
        }

        DrawRefresh(g, cx, cy, 0, Theme.TextMuted);
        if (Failed)
            DrawWarningMark(g, cx + 7, cy + 2, 0.75f);
    }

    private static void DrawRefresh(Graphics g, float cx, float cy, float rotation, Color color)
    {
        var state = g.Save();
        g.TranslateTransform(cx, cy);
        g.RotateTransform(rotation);
        using var pen = Theme.IconPen(color);
        g.DrawArc(pen, -8, -8, 16, 16, 40, 280);
        var angle = (40 + 280) * Math.PI / 180;
        var tip = new PointF(8 * (float)Math.Cos(angle), 8 * (float)Math.Sin(angle));
        g.DrawLine(pen, tip.X, tip.Y, tip.X - 5, tip.Y - 1);
        g.DrawLine(pen, tip.X, tip.Y, tip.X + 0.5f, tip.Y - 5);
        g.Restore(state);
    }

    private static void DrawCheck(Graphics g, float cx, float cy, float time)
    {
        var grow = Math.Min(1f, time / 0.25f);
        var fade = Math.Min(1f, Math.Max(0f, (DoneSeconds - time) / 0.4f));
        var radius = 10 * (0.6f + 0.4f * grow);
        using (var brush = new SolidBrush(Color.FromArgb((int)(255 * fade), Theme.PingGood)))
            g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);

        var stroke = Math.Min(1f, Math.Max(0f, (time - 0.15f) / 0.3f));
        if (stroke <= 0)
            return;

        var a = new PointF(cx - 4.5f, cy + 0.2f);
        var b = new PointF(cx - 1.2f, cy + 3.5f);
        var c = new PointF(cx + 5f, cy - 3.5f);
        using var pen = new Pen(Color.FromArgb((int)(255 * fade), Color.White), 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        if (stroke < 0.4f)
        {
            var t = stroke / 0.4f;
            g.DrawLine(pen, a, Lerp(a, b, t));
            return;
        }

        var t2 = (stroke - 0.4f) / 0.6f;
        g.DrawLines(pen, new[] { a, b, Lerp(b, c, t2) });
    }

    private static PointF Lerp(PointF from, PointF to, float t) =>
        new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);

    private static void DrawWarningMark(Graphics g, float cx, float top, float scale)
    {
        var triangle = new[] { new PointF(cx, top), new PointF(cx + 9 * scale, top + 16 * scale), new PointF(cx - 9 * scale, top + 16 * scale) };
        using (var fill = new SolidBrush(Theme.PingMid))
            g.FillPolygon(fill, triangle);
        using var mark = new Pen(Color.White, 1.8f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(mark, cx, top + 5 * scale, cx, top + 10 * scale);
        g.DrawLine(mark, cx, top + 13 * scale, cx, top + 13.2f * scale);
    }

    private static void DrawSupport(Graphics g, RectangleF r)
    {
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

    private static void DrawChevron(Graphics g, float cx, float cy, bool collapsed)
    {
        using var pen = Theme.IconPen(Theme.TextMuted);
        var points = collapsed
            ? new[] { new PointF(cx - 2, cy - 5), new PointF(cx + 3, cy), new PointF(cx - 2, cy + 5) }
            : new[] { new PointF(cx - 5, cy - 2), new PointF(cx, cy + 3), new PointF(cx + 5, cy - 2) };
        g.DrawLines(pen, points);
    }

    private static void DrawPing(Graphics g, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2 + 2;
        using var pen = Theme.IconPen(Theme.TextMuted);
        using var brush = new SolidBrush(Theme.TextMuted);
        g.DrawArc(pen, cx - 8, cy - 8, 16, 16, 180, 180);
        g.DrawLine(pen, cx, cy, cx + 4, cy - 5);
        g.FillEllipse(brush, cx - 2, cy - 2, 4, 4);
    }

    private static void DrawMenu(Graphics g, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        using var brush = new SolidBrush(Theme.TextMuted);
        for (var i = -1; i <= 1; i++)
            g.FillEllipse(brush, cx + i * 6 - 1.8f, cy - 1.8f, 3.6f, 3.6f);
    }

    private string ExpireText()
    {
        if (Info.Expire == null)
            return L.T("Подписка без срока");

        var expire = Info.Expire.Value;
        var left = expire - DateTime.Now;
        if (left <= TimeSpan.Zero)
            return L.F("Подписка истекла {0:dd.MM.yyyy}", expire);

        var tail = left.TotalDays >= 1
            ? ServerText.Plural((int)left.TotalDays, L.T("день"), L.T("дня"), L.T("дней"))
            : ServerText.Plural(Math.Max(1, (int)left.TotalHours), L.T("час"), L.T("часа"), L.T("часов"));
        return L.F("Истекает {0:dd.MM.yyyy} · осталось {1}", expire, tail);
    }

    private string? WarningText()
    {
        if (!Info.ExpiresSoon)
            return null;

        return Info.Expire <= DateTime.Now
            ? L.T("Подписка закончилась. Продлите её, чтобы VPN снова заработал.")
            : L.T("До конца подписки меньше 3 дней. Продлите её, иначе доступ будет приостановлен.");
    }

    private void DrawWarning(Graphics g, RectangleF r, string text)
    {
        var expired = Info.Expire <= DateTime.Now;
        var color = expired ? Theme.PingBad : Theme.PingMid;
        Theme.FillRounded(g, Color.FromArgb(Theme.IsDark ? 40 : 34, color), r, 12);
        Theme.DrawRounded(g, Color.FromArgb(150, color), r, 12, 1.2f);

        var cx = r.X + 20;
        var top = r.Y + 12;
        var triangle = new[] { new PointF(cx, top), new PointF(cx + 9, top + 16), new PointF(cx - 9, top + 16) };
        using (var fill = new SolidBrush(color))
            g.FillPolygon(fill, triangle);
        using (var mark = Theme.IconPen(Color.White))
        {
            g.DrawLine(mark, cx, top + 5, cx, top + 10);
            g.DrawLine(mark, cx, top + 13, cx, top + 13.2f);
        }

        var textRect = new RectangleF(r.X + WarningIndent, r.Y + 10, r.Width - WarningIndent - 12, r.Height - 20);
        Theme.DrawText(g, text, Theme.CaptionBold, Theme.Text, textRect, StringAlignment.Near, StringAlignment.Near, true);

        if (Info.SupportUrl.Length == 0)
            return;

        var label = L.T("Продлить подписку →");
        var width = Theme.Measure(label, Theme.CaptionBold).Width;
        var link = new RectangleF(r.X + WarningIndent, r.Bottom - 28, width + 2, 20);
        Theme.DrawText(g, label, Theme.CaptionBold, Theme.AccentStrong, link);
        using (var pen = new Pen(Color.FromArgb(140, Theme.AccentStrong)))
            g.DrawLine(pen, link.X, link.Bottom - 2, link.X + width, link.Bottom - 2);
        _hits.Add((link, () => Open(Info.SupportUrl)));
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
