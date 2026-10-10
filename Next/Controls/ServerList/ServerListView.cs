using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Tunnelka.Models;
using Tunnelka.Services;

namespace Tunnelka.Next;

public sealed class ServerListView : UserControl
{
    private sealed class Item
    {
        public Item(Control view, ServerRowView? server, SubscriptionRowView? header)
        {
            View = view;
            Server = server;
            Header = header;
        }

        public Control View { get; }
        public ServerRowView? Server { get; }
        public SubscriptionRowView? Header { get; }
    }

    private readonly Session _session;
    private readonly ScrollViewer _scroll;
    private readonly StackPanel _stack;
    private readonly Border _frame;
    private readonly ScrollViewer _welcomeScroll;
    private readonly WelcomeCard _welcome;
    private readonly List<Item> _items = new();
    private string _query = "";

    public ServerListView(Session session)
    {
        _session = session;
        Focusable = true;

        _stack = new StackPanel { Margin = new Thickness(0, 3, 12, 3) };
        _scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = _stack
        };

        _frame = new Border
        {
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Child = _scroll
        };
        ServerRes.Bind(_frame, Border.BackgroundProperty, "CardBrush");
        ServerRes.Bind(_frame, Border.BorderBrushProperty, "BorderBrush2");

        _welcome = new WelcomeCard();
        _welcome.PasteClicked += () => PasteRequested?.Invoke();
        _welcome.ManualClicked += () => ManualRequested?.Invoke();
        _welcomeScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Padding = new Thickness(0, 0, 12, 0),
            Content = _welcome,
            IsVisible = false
        };

        var grid = new Grid();
        grid.Children.Add(_frame);
        grid.Children.Add(_welcomeScroll);
        Content = grid;
    }

    public event Action? PasteRequested;

    public event Action? ManualRequested;

    public event Action<SubscriptionInfo>? ShowKeyRequested;

    public void Localize()
    {
        _welcome.Localize();
        Rebuild();
    }

    public void Rebuild()
    {
        var offset = _scroll.Offset.Y;
        _stack.Children.Clear();
        _items.Clear();

        var data = _session.Data;
        var subscriptions = _session.Subscriptions;
        var empty = data.Servers.Count == 0 && subscriptions.Profiles.Count == 0;
        _welcomeScroll.IsVisible = empty;
        _frame.IsVisible = !empty;

        foreach (var info in subscriptions.Profiles)
        {
            var url = info.Url;
            var subscriptionServers = subscriptions.Servers(url);
            var header = new SubscriptionRowView(_session, info);
            header.CollapseToggled += ApplyFilter;
            header.ShowKeyRequested += row => ShowKeyRequested?.Invoke(row.Info);
            header.DeleteRequested += async row => await ConfirmDelete(row.Info);
            Add(new Item(header, null, header));
            if (subscriptionServers.Count > 0)
                AddServer(_session.Autos.For(url));
            foreach (var server in subscriptionServers)
                AddServer(server);
        }

        var loose = data.Servers.Where(s => s.SubscriptionUrl == null || !subscriptions.IsKnown(s.SubscriptionUrl)).ToList();
        if (loose.Count > 1)
            AddServer(_session.Autos.For(null));
        foreach (var server in loose)
            AddServer(server);

        Mark();
        ApplyFilter();
        Dispatcher.UIThread.Post(() => _scroll.Offset = new Vector(0, offset), DispatcherPriority.Loaded);
    }

    private void Add(Item item)
    {
        _items.Add(item);
        _stack.Children.Add(item.View);
    }

    private void AddServer(ProxyServer server)
    {
        var row = new ServerRowView(server);
        row.Selected += r =>
        {
            Focus();
            _session.Select(r.Server);
        };
        row.ConnectRequested += r =>
        {
            _session.Select(r.Server);
            _ = _session.Connect();
        };
        row.DeleteRequested += r => _session.Delete(r.Server);
        Add(new Item(row, row, null));
    }

    private async Task ConfirmDelete(SubscriptionInfo info)
    {
        var dialogs = _session.Dialogs;
        if (dialogs != null && !await dialogs.AskYesNo(L.F("Удалить ключ «{0}» и все его серверы?", info.Title)))
            return;

        _session.DeleteSubscription(info.Url);
    }

    public void Filter(string query)
    {
        _query = query;
        _scroll.Offset = new Vector(0, 0);
        ApplyFilter();
    }

    public void Mark()
    {
        foreach (var item in _items)
        {
            if (item.Server is { } row)
                row.SetState(row.Server == _session.Selected, row.Server == _session.Active, _session.IsPinging(row.Server));
        }
    }

    public void UpdateCards()
    {
        Mark();
        foreach (var item in _items)
            item.Header?.Update();
    }

    private void ApplyFilter()
    {
        foreach (var item in _items)
        {
            if (item.Header != null)
            {
                item.View.IsVisible = _query.Length == 0;
                continue;
            }

            var row = item.Server!;
            item.View.IsVisible = _query.Length == 0
                ? !_session.Subscriptions.IsCollapsed(row.Server.SubscriptionUrl)
                : row.DisplayName.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0
                  || row.Server.Address.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var selected = _session.Selected;
        if (e.Key == Key.Delete && selected != null && !AutoServers.IsAuto(selected))
        {
            e.Handled = true;
            _session.Delete(selected);
        }
    }
}
