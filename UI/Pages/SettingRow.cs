using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class SettingRow : ThemedControl
{
    private readonly Control? _accessory;
    private readonly bool _chevron;

    public SettingRow(string title, string subtitle, Control? accessory = null, bool chevron = false)
    {
        Text = title;
        Subtitle = subtitle;
        _accessory = accessory;
        _chevron = chevron;
        Dock = DockStyle.Top;
        Height = Theme.Px(76);
        Theme.Bind(this, () => Theme.Card);

        if (chevron)
            Cursor = Cursors.Hand;

        if (accessory != null)
        {
            Controls.Add(accessory);
            Resize += (_, _) => accessory.Location = new Point(Width - accessory.Width - Theme.Px(18), (Height - Theme.Px(8) - accessory.Height) / 2);
        }
    }

    public string Subtitle { get; set; }

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 9.5f);
        Theme.FillRounded(g, IsHovered && _chevron ? Theme.CardHover : Theme.Card, rect, 14);
        Theme.DrawRounded(g, Theme.Border, rect, 14);

        var right = _accessory != null ? _accessory.Width / Theme.S + 16 : _chevron ? 30 : 0;
        var textWidth = W - 32 - right;
        Theme.DrawText(g, Text, Theme.BodyBold, Theme.Text, new RectangleF(16, 12, textWidth, 22));
        Theme.DrawText(g, Subtitle, Theme.Caption, Theme.TextMuted, new RectangleF(16, 36, textWidth, 20));

        if (_chevron)
        {
            using var pen = new Pen(Theme.TextMuted, 2f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            var cx = W - 24f;
            var cy = rect.Height / 2;
            g.DrawLine(pen, cx - 3, cy - 6, cx + 3, cy);
            g.DrawLine(pen, cx + 3, cy, cx - 3, cy + 6);
        }
    }
}
