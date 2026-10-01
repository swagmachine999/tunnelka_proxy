using VpnClient.Models;

namespace VpnClient.UI.Controls;

public class ServerCard : Control
{
    private bool _hover;
    private bool _selected;
    private bool _active;

    public ServerCard(ProxyServer server)
    {
        Server = server;
        DisplayName = ServerText.CleanName(server);
        Code = ServerText.CountryCode(server.Name);
        Description = ServerText.Describe(server);

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
        Height = 66;
        Margin = new Padding(0, 0, 0, 8);
        Cursor = Cursors.Hand;
    }

    public ProxyServer Server { get; }
    public string DisplayName { get; }
    public string? Code { get; }
    public string Description { get; }

    public bool IsSelected
    {
        get => _selected;
        set { if (_selected != value) { _selected = value; Invalidate(); } }
    }

    public bool IsActive
    {
        get => _active;
        set { if (_active != value) { _active = value; Invalidate(); } }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        var fill = _selected ? Theme.CardSelected : _hover ? Theme.CardHover : Theme.Card;
        Theme.FillRounded(g, fill, rect, 14);
        Theme.DrawRounded(g, _selected ? Color.FromArgb(140, Theme.Accent) : Theme.Border, rect, 14);

        if (_selected)
            Theme.FillRounded(g, Theme.Accent, new RectangleF(rect.X + 1, 16, 4, Height - 32), 2);

        var badge = new RectangleF(16, (Height - 34) / 2f, 34, 34);
        Theme.DrawBadge(g, badge, Code);

        if (_active)
        {
            using var ring = new SolidBrush(Color.White);
            using var dot = new SolidBrush(Theme.Mint);
            g.FillEllipse(ring, badge.Right - 11, badge.Bottom - 11, 13, 13);
            g.FillEllipse(dot, badge.Right - 9, badge.Bottom - 9, 9, 9);
        }

        var textLeft = badge.Right + 14;
        var pingWidth = 64f;
        var textWidth = Width - textLeft - pingWidth - 8;

        Theme.DrawText(g, DisplayName, Theme.CardTitle, Theme.Text, new RectangleF(textLeft, 12, textWidth, 22));
        Theme.DrawText(g, Description, Theme.Caption, Theme.TextMuted, new RectangleF(textLeft, 35, textWidth, 18));

        var ping = Theme.PingLabel(Server.PingMs);
        if (ping.Length > 0)
        {
            var pingRect = new RectangleF(Width - pingWidth - 14, 0, pingWidth, Height);
            Theme.DrawText(g, ping, Theme.CaptionBold, Theme.PingColor(Server.PingMs), pingRect, StringAlignment.Far);
        }
    }
}
