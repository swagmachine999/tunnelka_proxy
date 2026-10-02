namespace Tunnelka.UI.Controls;

public class WelcomeCard : ThemedControl
{
    private const float Pad = 20;
    private const float StepIndent = 44;
    private const float ButtonHeight = 40;

    private readonly (string Title, string Text)[] _steps =
    {
        (L.T("Получите ключ"), L.T("Ключ выдаёт ваш VPN-сервис: обычно его присылает Telegram-бот или он есть в личном кабинете на сайте. Это ссылка вида https://… или vless://…")),
        (L.T("Добавьте его сюда"), L.T("Скопируйте ключ и нажмите «Вставить ключ». Если ключ показан QR-кодом, нажмите «Сканировать QR» выше.")),
        (L.T("Подключитесь"), L.T("Появится список серверов. Выберите любой и нажмите большую круглую кнопку справа."))
    };

    private readonly List<float> _stepHeights = new();
    private RectangleF _pasteRect;
    private RectangleF _manualRect;
    private int _hover;
    private int _layoutWidth = -1;

    public event EventHandler? PasteClicked;
    public event EventHandler? ManualClicked;

    public WelcomeCard()
    {
        Margin = Theme.Px(0, 4, 0, 10);
        Height = Theme.Px(420);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width == _layoutWidth)
            return;

        _layoutWidth = Width;
        _stepHeights.Clear();
        var y = 92f;
        foreach (var (_, text) in _steps)
        {
            var height = Theme.MeasureWrapped(text, Theme.Body, W - Pad * 2 - StepIndent) + 30;
            _stepHeights.Add(height);
            y += height + 10;
        }

        var half = (W - Pad * 2 - 10) / 2;
        _pasteRect = new RectangleF(Pad, y + 6, half, ButtonHeight);
        _manualRect = new RectangleF(Pad + half + 10, y + 6, half, ButtonHeight);
        var device = Theme.Px(_pasteRect.Bottom + Pad);
        if (Height != device)
            Height = device;
    }

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Theme.Card, Theme.Lighten(Theme.CardSelected, Theme.IsDark ? 0f : 0.3f), 90f))
        using (var path = Theme.RoundedRect(rect, 16))
            g.FillPath(brush, path);
        Theme.DrawRounded(g, Theme.Border, rect, 16);

        Theme.DrawText(g, L.T("Как начать"), Theme.CardTitle, Theme.Text, new RectangleF(Pad, 18, W - Pad * 2, 26));
        Theme.DrawText(g, L.T("Три шага — и VPN работает"), Theme.Caption, Theme.TextMuted, new RectangleF(Pad, 46, W - Pad * 2, 20));

        var y = 92f;
        for (var i = 0; i < _steps.Length && i < _stepHeights.Count; i++)
        {
            var badge = new RectangleF(Pad, y, 28, 28);
            using (var fill = new SolidBrush(Theme.Accent))
                g.FillEllipse(fill, badge);
            Theme.DrawText(g, (i + 1).ToString(), Theme.BodyBold, Color.White, badge, StringAlignment.Center);

            var textX = Pad + StepIndent;
            var textWidth = W - Pad * 2 - StepIndent;
            Theme.DrawText(g, _steps[i].Title, Theme.BodyBold, Theme.Text, new RectangleF(textX, y + 2, textWidth, 24));
            Theme.DrawText(g, _steps[i].Text, Theme.Body, Theme.TextMuted, new RectangleF(textX, y + 28, textWidth, _stepHeights[i]),
                StringAlignment.Near, StringAlignment.Near, true);
            y += _stepHeights[i] + 10;
        }

        DrawButton(g, _pasteRect, L.T("Вставить ключ"), true, _hover == 1);
        DrawButton(g, _manualRect, L.T("Ввести вручную"), false, _hover == 2);
    }

    private static void DrawButton(Graphics g, RectangleF r, string text, bool primary, bool hover)
    {
        if (primary)
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(r,
                hover ? Theme.Lighten(Theme.Accent, 0.15f) : Theme.Accent,
                hover ? Theme.Lighten(Theme.Pink, 0.15f) : Theme.Pink, 0f);
            using var path = Theme.RoundedRect(r, r.Height / 2);
            g.FillPath(brush, path);
        }
        else
        {
            Theme.FillRounded(g, hover ? Theme.CardHover : Theme.Card, r, r.Height / 2);
            Theme.DrawRounded(g, Theme.Accent, r, r.Height / 2, 1.4f);
        }

        Theme.DrawText(g, text, Theme.BodyBold, primary ? Color.White : Theme.AccentStrong, r, StringAlignment.Center);
    }

    private int HitTest(Point location)
    {
        var point = Theme.Design(location);
        return _pasteRect.Contains(point) ? 1 : _manualRect.Contains(point) ? 2 : 0;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hover = HitTest(e.Location);
        Cursor = hover == 0 ? Cursors.Default : Cursors.Hand;
        if (hover != _hover)
        {
            _hover = hover;
            Invalidate();
        }
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
                PasteClicked?.Invoke(this, EventArgs.Empty);
                break;
            case 2:
                ManualClicked?.Invoke(this, EventArgs.Empty);
                break;
        }
    }
}
