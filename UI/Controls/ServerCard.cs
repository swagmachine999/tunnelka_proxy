using Tunnelka.Models;

namespace Tunnelka.UI.Controls;

public class ServerCard : ThemedControl
{
    private static readonly HashSet<ServerCard> BusyCards = new();
    private static readonly System.Windows.Forms.Timer BusyTimer = CreateBusyTimer();
    private static float _busyTime;

    private bool _busy;
    private bool _selected;
    private bool _active;
    private float _selection;

    public ServerCard(ProxyServer server)
    {
        Server = server;
        Parts = ServerText.Parts(server);
        DisplayName = ServerText.CleanName(server);
        Code = ServerText.CountryCode(server.Name);
        Description = ServerText.Describe(server);
        BackColor = Theme.Surface;
        Height = Theme.Px(66 + Theme.ShadowBottom);
        Margin = Theme.Px(0, 0, 0, 3);
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
        set { if (_selected != value) { _selected = value; Animate(); } }
    }

    public bool IsBusy
    {
        get => _busy;
        set
        {
            if (_busy == value)
                return;

            _busy = value;
            if (value)
                BusyCards.Add(this);
            else
                BusyCards.Remove(this);

            BusyTimer.Enabled = BusyCards.Count > 0;
            Invalidate();
        }
    }

    private static System.Windows.Forms.Timer CreateBusyTimer()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 40 };
        timer.Tick += (_, _) =>
        {
            _busyTime += 0.04f;
            foreach (var card in BusyCards.ToList())
            {
                if (card.IsDisposed)
                    BusyCards.Remove(card);
                else if (card.Visible)
                    card.Invalidate();
            }
        };
        return timer;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            IsBusy = false;
        base.Dispose(disposing);
    }

    public bool IsActive
    {
        get => _active;
        set { if (_active != value) { _active = value; Invalidate(); } }
    }

    protected override bool AnimateMore() => Animator.Approach(ref _selection, _selected ? 1 : 0);

    protected override void Draw(Graphics g)
    {
        var rect = Theme.CardRect(W, H);
        var fill = Theme.Blend(Theme.Blend(Theme.Card, Theme.CardHover, Hover), Theme.CardSelected, _selection);
        Theme.DrawCard(g, rect, 14, fill, Theme.Blend(Theme.Border, Color.FromArgb(140, Theme.Accent), _selection));

        if (_selection > 0.01f)
            Theme.FillRounded(g, Color.FromArgb((int)(255 * _selection), Theme.Accent), new RectangleF(rect.X + 1, rect.Y + 15, 4, rect.Height - 30), 2);

        var badge = new RectangleF(rect.X + 14, rect.Y + (rect.Height - 34) / 2f, 34, 34);
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

        NamePainter.Draw(g, Parts, Theme.CardTitle, Theme.Text, new RectangleF(textLeft, rect.Y + 10, textWidth, 24));
        Theme.DrawText(g, Description, Theme.Caption, Theme.TextMuted, new RectangleF(textLeft, rect.Y + 34, textWidth, 18));

        if (_busy)
        {
            Theme.DrawBusyDots(g, rect.Right - 50, rect.Y + rect.Height / 2, _busyTime, 6);
            return;
        }

        var ping = Theme.PingLabel(Server.PingMs);
        if (ping.Length > 0)
        {
            var pingRect = new RectangleF(rect.Right - pingWidth - 12, rect.Y, pingWidth, rect.Height);
            Theme.DrawText(g, ping, Theme.CaptionBold, Theme.PingColor(Server.PingMs), pingRect, StringAlignment.Far);
        }
    }
}
