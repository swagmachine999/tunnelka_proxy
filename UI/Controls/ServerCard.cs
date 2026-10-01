using VpnClient.Models;

namespace VpnClient.UI.Controls;

public class ServerCard : Control
{
    private float W => Width / Theme.S;
    private float H => Height / Theme.S;

    private bool _hover;
    private bool _selected;
    private bool _active;

    public ServerCard(ProxyServer server)
    {
        Server = server;
        Parts = ServerText.Parts(server);
        DisplayName = ServerText.CleanName(server);
        Code = ServerText.CountryCode(server.Name);
        Description = ServerText.Describe(server);

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
        Height = Theme.Px(66);
        Margin = Theme.Px(0, 0, 0, 8);
        Cursor = Cursors.Hand;
    }

    public ProxyServer Server { get; }
    public IReadOnlyList<NamePart> Parts { get; }
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
        Theme.Begin(g, Theme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        var fill = _selected ? Theme.CardSelected : _hover ? Theme.CardHover : Theme.Card;
        Theme.FillRounded(g, fill, rect, 14);
        Theme.DrawRounded(g, _selected ? Color.FromArgb(140, Theme.Accent) : Theme.Border, rect, 14);

        if (_selected)
            Theme.FillRounded(g, Theme.Accent, new RectangleF(rect.X + 1, 16, 4, H - 32), 2);

        var badge = new RectangleF(16, (H - 34) / 2f, 34, 34);
        Theme.DrawBadge(g, badge, Code);

        if (_active)
        {
            using var ring = new SolidBrush(fill);
            using var dot = new SolidBrush(Theme.Mint);
            g.FillEllipse(ring, badge.Right - 11, badge.Bottom - 11, 13, 13);
            g.FillEllipse(dot, badge.Right - 9, badge.Bottom - 9, 9, 9);
        }

        var textLeft = badge.Right + 14;
        var pingWidth = 64f;
        var textWidth = W - textLeft - pingWidth - 8;

        NamePainter.Draw(g, Parts, Theme.CardTitle, Theme.Text, new RectangleF(textLeft, 11, textWidth, 24));
        Theme.DrawText(g, Description, Theme.Caption, Theme.TextMuted, new RectangleF(textLeft, 35, textWidth, 18));

        var ping = Theme.PingLabel(Server.PingMs);
        if (ping.Length > 0)
        {
            var pingRect = new RectangleF(W - pingWidth - 14, 0, pingWidth, H);
            Theme.DrawText(g, ping, Theme.CaptionBold, Theme.PingColor(Server.PingMs), pingRect, StringAlignment.Far);
        }
    }
}
