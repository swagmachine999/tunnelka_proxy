using Tunnelka.Models;

namespace Tunnelka.UI.Controls.ServerList;

public sealed class ServerRow : ListRow
{
    public const float RowHeight = 46;

    private bool _selected;
    private bool _active;
    private bool _busy;
    private bool _hovered;
    private float _hover;
    private float _selection;

    public ServerRow(ProxyServer server)
    {
        Server = server;
        Parts = ServerText.Parts(server);
        DisplayName = ServerText.CleanName(server);
        Code = ServerText.CountryCode(server.Name);
    }

    public ProxyServer Server { get; }

    public IReadOnlyList<NamePart> Parts { get; }

    public string DisplayName { get; }

    public string? Code { get; }

    public bool IsSelected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;

            _selected = value;
            RaiseChanged();
        }
    }

    public bool IsActive
    {
        get => _active;
        set
        {
            if (_active == value)
                return;

            _active = value;
            RaiseChanged();
        }
    }

    public bool IsBusy
    {
        get => _busy;
        set
        {
            if (_busy == value)
                return;

            _busy = value;
            RaiseChanged();
        }
    }

    public override bool NeedsFrames => _busy;

    public override float Measure(float width) => RowHeight;

    public override bool Step()
    {
        var moving = Animator.Approach(ref _hover, _hovered ? 1 : 0);
        moving |= Animator.Approach(ref _selection, _selected ? 1 : 0);
        return moving;
    }

    public override void PointerMoved(PointF local)
    {
        if (_hovered)
            return;

        _hovered = true;
        RaiseChanged();
    }

    public override void PointerLeft()
    {
        if (!_hovered)
            return;

        _hovered = false;
        RaiseChanged();
    }

    public event Action<ServerRow>? Selected;

    public event Action<ServerRow>? ConnectRequested;

    public event Action<ServerRow, PointF>? MenuRequested;

    public override bool Hits(PointF local) => true;

    public override void Click(PointF local) => Selected?.Invoke(this);

    public override void DoubleClick(PointF local) => ConnectRequested?.Invoke(this);

    public override void RightClick(PointF local) => MenuRequested?.Invoke(this, local);

    public override void Draw(Graphics g, float width, float time)
    {
        var rect = new RectangleF(4, 2, width - 8, Extent - 4);
        var fill = Theme.Blend(Theme.Blend(Theme.Surface, Theme.CardHover, _hover), Theme.CardSelected, _selection);
        if (_hover > 0.01f || _selection > 0.01f)
        {
            Theme.FillRounded(g, fill, rect, 12);
            Theme.DrawRounded(g, Color.FromArgb((int)(150 * _selection), Theme.Accent), rect, 12);
        }

        if (_selection > 0.01f)
            Theme.FillRounded(g, Color.FromArgb((int)(255 * _selection), Theme.Accent), new RectangleF(rect.X + 1, rect.Y + 11, 4, rect.Height - 22), 2);

        var badge = new RectangleF(rect.X + 14, rect.Y + (rect.Height - 28) / 2f, 28, 28);
        Theme.DrawBadge(g, badge, Code);

        if (_active)
        {
            using var ring = new SolidBrush(Theme.Blend(Theme.Surface, fill, Math.Max(_hover, _selection)));
            using var dot = new SolidBrush(Theme.Mint);
            g.FillEllipse(ring, badge.Right - 10, badge.Bottom - 10, 12, 12);
            g.FillEllipse(dot, badge.Right - 8, badge.Bottom - 8, 8, 8);
        }

        const float pingWidth = 64;
        var textLeft = badge.Right + 12;
        var textWidth = rect.Right - textLeft - pingWidth - 16;
        NamePainter.Draw(g, Parts, Theme.CardTitle, Theme.Text, new RectangleF(textLeft, rect.Y, textWidth, rect.Height));

        if (_busy)
        {
            Theme.DrawBusyDots(g, rect.Right - 40, rect.Y + rect.Height / 2, time, 6);
            return;
        }

        var ping = Theme.PingLabel(Server.PingMs);
        if (ping.Length > 0)
            Theme.DrawText(g, ping, Theme.CaptionBold, Theme.PingColor(Server.PingMs),
                new RectangleF(rect.Right - pingWidth - 14, rect.Y, pingWidth, rect.Height), StringAlignment.Far);
    }
}
