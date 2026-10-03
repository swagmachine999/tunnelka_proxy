namespace Tunnelka.UI.Controls;

public class SearchBox : ThemedControl
{
    private readonly TextBox _box = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Card,
        ForeColor = Theme.Text,
        Font = Theme.Scaled(Theme.Body)
    };

    private readonly bool _icon;

    private readonly Label _placeholder = new()
    {
        BackColor = Theme.Card,
        ForeColor = Theme.TextMuted,
        Font = Theme.Scaled(Theme.Body),
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Cursor = Cursors.IBeam
    };

    public event EventHandler? QueryChanged;

    public SearchBox(string? placeholder = null, bool icon = true)
    {
        _icon = icon;
        _placeholder.Text = placeholder ?? L.T("Поиск сервера");
        Theme.Bind(_box, () => Theme.Card, () => Theme.Text);
        Theme.Bind(_placeholder, () => Theme.Card, () => Theme.TextMuted);
        Theme.Bind(this, () => Theme.Surface);
        BackColor = Theme.Surface;
        Height = Theme.Px(42);

        Controls.Add(_box);
        Controls.Add(_placeholder);
        _placeholder.BringToFront();

        _placeholder.Click += (_, _) => _box.Focus();
        _box.TextChanged += (_, _) =>
        {
            _placeholder.Visible = _box.Text.Length == 0;
            QueryChanged?.Invoke(this, EventArgs.Empty);
        };
        _box.GotFocus += (_, _) => Invalidate();
        _box.LostFocus += (_, _) => Invalidate();
    }

    public string Query => _box.Text.Trim();

    public void Clear() => _box.Clear();

    public void SetText(string text) => _box.Text = text;

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var height = _box.PreferredHeight;
        var top = (Height - height) / 2;
        var left = Theme.Px(_icon ? 40 : 14);
        _box.SetBounds(left, top, Width - left - Theme.Px(14), height);
        _placeholder.SetBounds(left, Theme.Px(3), Width - left - Theme.Px(14), Height - Theme.Px(6));
    }

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, Theme.Card, rect, 12);
        Theme.DrawRounded(g, _box.Focused ? Theme.Accent : Theme.Border, rect, 12, _box.Focused ? 1.6f : 1f);

        if (!_icon)
            return;

        using var pen = Theme.IconPen(Theme.TextMuted);
        var cy = H / 2f;
        g.DrawEllipse(pen, 15, cy - 8, 12, 12);
        g.DrawLine(pen, 25, cy + 2, 29, cy + 6);
    }
}
