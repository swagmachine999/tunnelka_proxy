namespace VpnClient.UI.Controls;

public class SearchBox : Control
{
    private readonly TextBox _box = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Card,
        ForeColor = Theme.Text,
        Font = Theme.Body
    };

    private readonly bool _icon;

    private readonly Label _placeholder = new()
    {
        BackColor = Theme.Card,
        ForeColor = Theme.TextMuted,
        Font = Theme.Body,
        AutoSize = false,
        Cursor = Cursors.IBeam
    };

    public event EventHandler? QueryChanged;

    public SearchBox(string placeholder = "Поиск сервера", bool icon = true)
    {
        _icon = icon;
        _placeholder.Text = placeholder;
        Theme.Bind(_box, () => Theme.Card, () => Theme.Text);
        Theme.Bind(_placeholder, () => Theme.Card, () => Theme.TextMuted);
        Theme.Bind(this, () => Theme.Surface);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
        Height = 42;

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

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var height = _box.PreferredHeight;
        var top = (Height - height) / 2;
        var left = _icon ? 40 : 14;
        _box.SetBounds(left, top, Width - left - 14, height);
        _placeholder.SetBounds(left, top, Width - left - 14, height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Theme.FillRounded(g, Theme.Card, rect, 12);
        Theme.DrawRounded(g, _box.Focused ? Theme.Accent : Theme.Border, rect, 12, _box.Focused ? 1.6f : 1f);

        if (!_icon)
            return;

        using var pen = new Pen(Theme.TextMuted, 1.8f) { EndCap = System.Drawing.Drawing2D.LineCap.Round };
        var cy = Height / 2f;
        g.DrawEllipse(pen, 15, cy - 8, 12, 12);
        g.DrawLine(pen, 25, cy + 2, 29, cy + 6);
    }
}
