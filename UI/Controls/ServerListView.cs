using Tunnelka.Models;
using Tunnelka.Services;

namespace Tunnelka.UI.Controls;

public class ServerListView : FlowLayoutPanel
{
    private readonly SubscriptionService _subscriptions;
    private readonly List<ServerCard> _cards = new();
    private readonly ContextMenuStrip _cardMenu = new();
    private readonly ContextMenuStrip _subscriptionMenu = new();
    private ServerCard? _menuCard;
    private SubscriptionCard? _menuSubscription;
    private string _query = "";
    private readonly System.Windows.Forms.Timer _scrollTimer = new() { Interval = 15 };
    private float _scrollTarget;
    private float _scrollCurrent;

    public event Action<ProxyServer>? ServerSelected;
    public event Action<ProxyServer>? ServerConnectRequested;
    public event Action<ProxyServer>? ServerDeleteRequested;
    public event Action<string>? SubscriptionRefreshRequested;
    public event Action<string>? SubscriptionPingRequested;
    public event Action<string>? SubscriptionDeleteRequested;
    public event EventHandler? PasteRequested;
    public event EventHandler? AddRequested;

    public ServerListView(SubscriptionService subscriptions)
    {
        _subscriptions = subscriptions;
        Dock = DockStyle.Fill;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);
        Resize += (_, _) => ResizeCards();
        DoubleBuffered = true;
        _scrollTimer.Tick += (_, _) => ScrollStep();

        _cardMenu.Items.Add(L.T("Подключиться"), null, (_, _) =>
        {
            if (_menuCard != null)
                ServerConnectRequested?.Invoke(_menuCard.Server);
        });
        _cardMenu.Items.Add(L.T("Удалить"), null, (_, _) =>
        {
            if (_menuCard != null)
                ServerDeleteRequested?.Invoke(_menuCard.Server);
        });
        _cardMenu.Opening += (_, _) => _menuCard = _cardMenu.SourceControl as ServerCard;

        _subscriptionMenu.Items.Add(L.T("Показать ключ"), null, (_, _) =>
        {
            if (_menuSubscription != null)
                LinkDialog.Show(FindForm()!, _menuSubscription.Info.Title, new[] { (_menuSubscription.Info.Title, _menuSubscription.Info.Url) }, false);
        });
        _subscriptionMenu.Items.Add(L.T("Удалить ключ"), null, (_, _) =>
        {
            if (_menuSubscription == null)
                return;

            var answer = MessageBox.Show(FindForm(), L.F("Удалить ключ «{0}» и все его серверы?", _menuSubscription.Info.Title),
                "Tunnelka", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
                SubscriptionDeleteRequested?.Invoke(_menuSubscription.Info.Url);
        });
        _subscriptionMenu.Opening += (_, _) => _menuSubscription = _subscriptionMenu.SourceControl as SubscriptionCard;
    }

    public void Rebuild(IReadOnlyList<ProxyServer> servers, Func<string?, ProxyServer> autoFor, ProxyServer? selected, ProxyServer? active)
    {
        SuspendLayout();
        foreach (Control control in Controls.Cast<Control>().ToList())
            control.Dispose();
        Controls.Clear();
        _cards.Clear();

        var controls = new List<Control>();
        if (servers.Count == 0 && _subscriptions.Profiles.Count == 0)
        {
            var welcome = new WelcomeCard();
            welcome.PasteClicked += (_, _) => PasteRequested?.Invoke(this, EventArgs.Empty);
            welcome.ManualClicked += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
            controls.Add(welcome);
        }

        foreach (var info in _subscriptions.Profiles)
        {
            var url = info.Url;
            var subscriptionServers = _subscriptions.Servers(url);
            var header = new SubscriptionCard(info) { ContextMenuStrip = _subscriptionMenu, ServerCount = subscriptionServers.Count };
            header.RefreshClicked += (_, _) => SubscriptionRefreshRequested?.Invoke(url);
            header.PingClicked += (_, _) => SubscriptionPingRequested?.Invoke(url);
            header.MenuClicked += (_, point) => _subscriptionMenu.Show(header, point);
            header.CollapseClicked += (_, _) =>
            {
                _subscriptions.ToggleCollapsed(info);
                header.Invalidate();
                ApplyFilter();
            };
            controls.Add(header);
            if (subscriptionServers.Count > 0)
                controls.Add(CreateCard(autoFor(url)));
            controls.AddRange(subscriptionServers.Select(CreateCard));
        }

        var loose = servers.Where(s => s.SubscriptionUrl == null || !_subscriptions.IsKnown(s.SubscriptionUrl)).ToList();
        if (loose.Count > 1)
            controls.Add(CreateCard(autoFor(null)));
        controls.AddRange(loose.Select(CreateCard));

        Controls.AddRange(controls.ToArray());
        Mark(selected, active);
        ApplyFilter();
        ResizeCards();
        ResumeLayout();
    }

    public void Filter(string query)
    {
        _query = query;
        ApplyFilter();
    }

    public void Mark(ProxyServer? selected, ProxyServer? active)
    {
        foreach (var card in _cards)
        {
            card.IsSelected = card.Server == selected;
            card.IsActive = card.Server == active;
            card.Invalidate();
        }
    }

    public void SetBusy(IEnumerable<ProxyServer> servers, bool busy)
    {
        var set = new HashSet<ProxyServer>(servers);
        foreach (var card in _cards)
        {
            if (set.Contains(card.Server))
                card.IsBusy = busy;
        }
    }

    public void RefreshSubscriptionCards()
    {
        foreach (var header in Controls.OfType<SubscriptionCard>())
        {
            header.UpdateLayout();
            header.Invalidate();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        var max = Math.Max(0, DisplayRectangle.Height - ClientSize.Height);
        if (!_scrollTimer.Enabled)
            _scrollCurrent = _scrollTarget = -AutoScrollPosition.Y;

        _scrollTarget = Math.Max(0, Math.Min(max, _scrollTarget - e.Delta / 120f * Theme.Px(110)));
        _scrollTimer.Start();
        if (e is HandledMouseEventArgs handled)
            handled.Handled = true;
    }

    private void ScrollStep()
    {
        _scrollCurrent += (_scrollTarget - _scrollCurrent) * 0.25f;
        if (Math.Abs(_scrollTarget - _scrollCurrent) < 0.5f)
        {
            _scrollCurrent = _scrollTarget;
            _scrollTimer.Stop();
        }

        AutoScrollPosition = new Point(0, (int)Math.Round(_scrollCurrent));
    }

    private ServerCard CreateCard(ProxyServer server)
    {
        var card = new ServerCard(server) { ContextMenuStrip = _cardMenu };
        card.Click += (_, _) => ServerSelected?.Invoke(server);
        card.DoubleClick += (_, _) => ServerConnectRequested?.Invoke(server);
        _cards.Add(card);
        return card;
    }

    private void ResizeCards()
    {
        var width = Width - SystemInformation.VerticalScrollBarWidth - Theme.Px(6);
        if (width <= 0)
            return;

        foreach (Control control in Controls)
            control.Width = width;
    }

    private void ApplyFilter()
    {
        foreach (var header in Controls.OfType<SubscriptionCard>())
            header.Visible = _query.Length == 0;

        foreach (var card in _cards)
        {
            card.Visible = _query.Length == 0
                ? !_subscriptions.IsCollapsed(card.Server.SubscriptionUrl)
                : card.DisplayName.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0
                  || card.Server.Address.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cardMenu.Dispose();
            _subscriptionMenu.Dispose();
            _scrollTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
