using System.Diagnostics;
using System.Drawing.Drawing2D;
using Tunnelka.Models;

namespace Tunnelka.UI.Controls.ServerList;

public sealed class SubscriptionRow : ListRow
{
    private const float Pad = 16;
    private const float LineHeight = 21;
    private const float SymbolSize = AnnounceLayout.SymbolSize;
    private const float IconSize = 28;
    private const float HeaderHeight = 50;
    private const float WarningIndent = 40;
    private const float DoneSeconds = 2.4f;
    private static readonly PointF Origin = new(4, 6);

    private static readonly string[] RefreshSymbols = { "\U0001F504", "\U0001F503", "♻" };

    private readonly List<(RectangleF Rect, Action Action)> _hits = new();
    private List<List<AnnounceToken>> _lines = new();
    private RectangleF _hoverRect = RectangleF.Empty;
    private RefreshStatus? _status;
    private float _width;
    private float _cardHeight = HeaderHeight;
    private float _warningHeight;
    private string? _warning;
    private int _layoutWidth = -1;

    public SubscriptionRow(SubscriptionInfo info)
    {
        Info = info;
    }

    public event Action? RefreshClicked;

    public event Action? PingClicked;

    public event Action? CollapseClicked;

    public event Action<PointF>? MenuClicked;

    public SubscriptionInfo Info { get; }

    public int ServerCount { get; set; }

    public RefreshStatus? Status
    {
        get => _status;
        set
        {
            _status = value;
            _layoutWidth = -1;
            RaiseChanged();
        }
    }

    public override bool NeedsFrames =>
        _status?.State == RefreshState.Busy || (_status?.State == RefreshState.Done && SinceStatus < DoneSeconds + 0.3f);

    private bool Failed => _status?.State == RefreshState.Failed;

    private float SinceStatus => _status == null ? float.MaxValue : (float)(DateTime.Now - _status.At).TotalSeconds;

    private bool HasDetails => !Info.Collapsed && (Failed || _warning != null || _lines.Count > 0);

    public void Refresh()
    {
        _layoutWidth = -1;
        RaiseChanged();
    }

    public override float Measure(float width)
    {
        _width = width - Origin.X * 2;
        var warning = WarningText();
        if (_layoutWidth != (int)width || warning != _warning)
        {
            _layoutWidth = (int)width;
            _warning = warning;
            _lines = AnnounceLayout.Build(Info.Announce, _width - Pad * 2);
            _warningHeight = warning == null ? 0
                : Theme.MeasureWrapped(warning, Theme.CaptionBold, _width - Pad * 2 - WarningIndent - 12) + 20 + (Info.SupportUrl.Length > 0 ? 22 : 0);
        }

        _cardHeight = HeaderHeight;
        if (HasDetails)
        {
            _cardHeight += 6;
            if (Failed)
                _cardHeight += 24;
            if (_warningHeight > 0)
                _cardHeight += _warningHeight + 8;
            if (_lines.Count > 0)
                _cardHeight += 14 + _lines.Count * LineHeight;
            _cardHeight += 8;
        }

        return Origin.Y + _cardHeight + 4;
    }

    public override void PointerMoved(PointF local)
    {
        var point = new PointF(local.X - Origin.X, local.Y - Origin.Y);
        var hover = RectangleF.Empty;
        foreach (var (rect, _) in _hits)
        {
            if (rect.Contains(point))
                hover = rect;
        }

        if (hover == _hoverRect)
            return;

        _hoverRect = hover;
        RaiseChanged();
    }

    public override void PointerLeft()
    {
        if (_hoverRect.IsEmpty)
            return;

        _hoverRect = RectangleF.Empty;
        RaiseChanged();
    }

    public override bool Hits(PointF local) => ActionAt(local) != null;

    public override void Click(PointF local) => ActionAt(local)?.Invoke();

    public override void RightClick(PointF local) => MenuClicked?.Invoke(local);

    private Action? ActionAt(PointF local)
    {
        var point = new PointF(local.X - Origin.X, local.Y - Origin.Y);
        foreach (var (rect, action) in _hits)
        {
            if (rect.Contains(point))
                return action;
        }

        return null;
    }

    public override void Draw(Graphics g, float width, float time)
    {
        _hits.Clear();
        _width = width - Origin.X * 2;

        var card = new RectangleF(Origin.X, Origin.Y, _width, _cardHeight);
        using (var brush = new LinearGradientBrush(card, Theme.Card, Theme.Lighten(Theme.CardSelected, Theme.IsDark ? 0f : 0.3f), 90f))
        using (var path = Theme.RoundedRect(card, 14))
            g.FillPath(brush, path);
        Theme.DrawRounded(g, Theme.Border, card, 14);

        var state = g.Save();
        g.TranslateTransform(Origin.X, Origin.Y);
        DrawContent(g);
        g.Restore(state);
    }

    private void DrawContent(Graphics g)
    {
        var iconTop = (HeaderHeight - IconSize) / 2;
        var x = _width - Pad - IconSize + 4;
        AddIcon(g, new RectangleF(x, iconTop, IconSize, IconSize), DrawMenu, r => MenuClicked?.Invoke(new PointF(r.Left + Origin.X, r.Bottom + Origin.Y)));
        x -= IconSize + 2;
        AddIcon(g, new RectangleF(x, iconTop, IconSize, IconSize), DrawPing, _ => PingClicked?.Invoke());
        x -= IconSize + 2;
        AddIcon(g, new RectangleF(x, iconTop, IconSize, IconSize), DrawRefreshState, _ => RefreshClicked?.Invoke());
        if (Info.SupportUrl.Length > 0)
        {
            x -= IconSize + 2;
            AddIcon(g, new RectangleF(x, iconTop, IconSize, IconSize), DrawSupport, _ => Open(Info.SupportUrl));
        }

        var titleRect = new RectangleF(Pad - 4, 4, x - Pad, HeaderHeight - 8);
        _hits.Insert(0, (titleRect, () => CollapseClicked?.Invoke()));
        DrawChevron(g, Pad + 4, 17, Info.Collapsed);

        var titleParts = ServerText.Parts(Info.Title, Info.Title);
        NamePainter.Draw(g, titleParts, Theme.ProviderTitle, Theme.Text, new RectangleF(Pad + 16, 5, x - Pad - 20, 24));
        DrawSubline(g, new RectangleF(Pad + 16, 28, x - Pad - 20, 16));

        if (!HasDetails)
            return;

        var y = HeaderHeight + 6;
        if (Failed)
        {
            DrawWarningMark(g, Pad + 8, y + 1, 1f);
            Theme.DrawText(g, L.F("Не удалось обновить: {0}", _status!.Message), Theme.CaptionBold, Theme.PingMid,
                new RectangleF(Pad + 24, y, _width - Pad * 2 - 24, 18));
            y += 24;
        }

        if (_warning != null)
        {
            DrawWarning(g, new RectangleF(Pad, y, _width - Pad * 2, _warningHeight), _warning);
            y += _warningHeight + 8;
        }

        if (_lines.Count == 0)
            return;

        using (var pen = new Pen(Theme.Border))
            g.DrawLine(pen, Pad, y + 2, _width - Pad, y + 2);

        DrawAnnounce(g, y + 14);
    }

    private void DrawSubline(Graphics g, RectangleF area)
    {
        var segments = new List<(string Text, Color Color, Font Font)>
        {
            (ServerText.Plural(ServerCount, L.T("сервер"), L.T("сервера"), L.T("серверов")), Theme.TextMuted, Theme.Caption)
        };

        if (Info.Expire != null)
            segments.Add((ExpireShort(), ExpireColor(), ExpireColor() == Theme.Text ? Theme.Caption : Theme.CaptionBold));

        if (Info.Total > 0)
            segments.Add((L.F("{0} из {1}", ServerText.Bytes(Info.Upload + Info.Download), ServerText.Bytes(Info.Total)), Theme.TextMuted, Theme.Caption));

        var x = area.X;
        for (var i = 0; i < segments.Count; i++)
        {
            var text = i == 0 ? segments[i].Text : "· " + segments[i].Text;
            var width = Theme.Measure(text, segments[i].Font).Width + 2;
            if (x + width > area.Right)
                break;

            var color = segments[i].Color == Theme.Text ? Theme.TextMuted : segments[i].Color;
            Theme.DrawText(g, text, segments[i].Font, color, new RectangleF(x, area.Y, width, area.Height));
            x += width + 3;
        }
    }

    private string ExpireShort()
    {
        var left = Info.Expire!.Value - DateTime.Now;
        if (left <= TimeSpan.Zero)
            return L.T("Подписка истекла");

        var tail = left.TotalDays >= 1
            ? ServerText.Plural((int)left.TotalDays, L.T("день"), L.T("дня"), L.T("дней"))
            : ServerText.Plural(Math.Max(1, (int)left.TotalHours), L.T("час"), L.T("часа"), L.T("часов"));
        return L.F("осталось {0}", tail);
    }

    private void AddIcon(Graphics g, RectangleF r, Action<Graphics, RectangleF> draw, Action<RectangleF> click)
    {
        if (r == _hoverRect)
            Theme.FillRounded(g, Theme.CardHover, r, 9);
        draw(g, r);
        _hits.Add((r, () => click(r)));
    }

    private void DrawAnnounce(Graphics g, float top)
    {
        var space = AnnounceLayout.SpaceWidth();

        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            var lineWidth = line.Sum(t => t.Width) + space * Math.Max(0, line.Count - 1);
            var x = (_width - lineWidth) / 2;
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
                        _hits.Add((hit, () => RefreshClicked?.Invoke()));
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
}
