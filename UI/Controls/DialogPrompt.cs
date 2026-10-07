using System.Drawing.Drawing2D;

namespace Tunnelka.UI.Controls;

public sealed class DialogPrompt : ThemedControl
{
    private readonly string _title;
    private readonly string _text;
    private readonly string _yesLabel;
    private readonly string _noLabel;
    private RectangleF _yes;
    private RectangleF _no;
    private int _hover;
    private int? _progress;
    private string? _error;

    public DialogPrompt(string title, string text, string yesLabel, string noLabel)
    {
        _title = title;
        _text = text;
        _yesLabel = yesLabel;
        _noLabel = noLabel;
    }

    public event EventHandler? Accepted;
    public event EventHandler? Declined;

    protected override Color Background => Theme.Window;

    public void ShowProgress(int percent)
    {
        _progress = percent;
        _error = null;
        Invalidate();
    }

    public void ShowError(string text)
    {
        _progress = null;
        _error = text;
        Invalidate();
    }

    protected override void Draw(Graphics g)
    {
        using (var brush = new LinearGradientBrush(new RectangleF(0, 0, W, H), Theme.HeroTop, Theme.HeroBottom, 90f))
            g.FillRectangle(brush, 0, 0, W, H);

        var tile = new RectangleF(28, 30, 64, 64);
        using (var brush = new LinearGradientBrush(tile, Theme.Pink, Theme.Accent, 45f))
        using (var path = Theme.RoundedRect(tile, 18))
            g.FillPath(brush, path);
        KittenPainter.DrawFace(g, new RectangleF(tile.X + 7, tile.Y + 9, 50, 48));

        var x = tile.Right + 20;
        var width = W - x - 24;
        Theme.DrawText(g, _title, Theme.CardTitle, Theme.Text, new RectangleF(x, 32, width, 26));
        Theme.DrawText(g, _text, Theme.Body, Theme.TextMuted, new RectangleF(x, 60, width, H - 60 - 24 - 44 - 36),
            StringAlignment.Near, StringAlignment.Near, true);

        var buttonsTop = H - 24 - 44;
        if (_progress is { } percent)
        {
            DrawProgress(g, new RectangleF(28, buttonsTop + 14, W - 56, 16), percent);
            return;
        }

        if (_error != null)
            Theme.DrawText(g, _error, Theme.Caption, Theme.PingBad, new RectangleF(28, buttonsTop - 26, W - 56, 20));

        var half = (W - 56 - 12) / 2;
        _no = new RectangleF(28, buttonsTop, half, 44);
        _yes = new RectangleF(28 + half + 12, buttonsTop, half, 44);

        Theme.FillRounded(g, _hover == 2 ? Theme.CardHover : Theme.Card, _no, 22);
        Theme.DrawRounded(g, _hover == 2 ? Theme.Accent : Theme.Border, _no, 22, 1.4f);
        Theme.DrawText(g, _noLabel, Theme.BodyBold, Theme.AccentStrong, _no, StringAlignment.Center);

        using (var brush = new LinearGradientBrush(_yes,
                   _hover == 1 ? Theme.Lighten(Theme.Accent, 0.15f) : Theme.Accent,
                   _hover == 1 ? Theme.Lighten(Theme.Pink, 0.15f) : Theme.Pink, 0f))
        using (var path = Theme.RoundedRect(_yes, 22))
            g.FillPath(brush, path);
        Theme.DrawText(g, _yesLabel, Theme.BodyBold, Color.White, _yes, StringAlignment.Center);
    }

    private static void DrawProgress(Graphics g, RectangleF r, int percent)
    {
        Theme.FillRounded(g, Theme.TrackOff, r, r.Height / 2);
        var done = new RectangleF(r.X, r.Y, Math.Max(r.Height, r.Width * percent / 100f), r.Height);
        using (var brush = new LinearGradientBrush(r, Theme.Accent, Theme.Pink, 0f))
        using (var path = Theme.RoundedRect(done, r.Height / 2))
            g.FillPath(brush, path);
        Theme.DrawText(g, L.F("Загрузка {0}%", percent), Theme.CaptionBold, Theme.TextMuted,
            new RectangleF(r.X, r.Y - 26, r.Width, 20), StringAlignment.Center);
    }

    private int HitTest(Point location)
    {
        if (_progress != null)
            return 0;
        var point = Theme.Design(location);
        return _yes.Contains(point) ? 1 : _no.Contains(point) ? 2 : 0;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hover = HitTest(e.Location);
        Cursor = hover == 0 ? Cursors.Default : Cursors.Hand;
        if (hover == _hover)
            return;

        _hover = hover;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = 0;
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        switch (HitTest(e.Location))
        {
            case 1:
                Accepted?.Invoke(this, EventArgs.Empty);
                break;
            case 2:
                Declined?.Invoke(this, EventArgs.Empty);
                break;
        }
    }
}
