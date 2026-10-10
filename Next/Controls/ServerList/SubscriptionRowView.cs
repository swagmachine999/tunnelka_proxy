using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Models;
using Tunnelka.UI;

namespace Tunnelka.Next;

public sealed class SubscriptionWarningMark : Control
{
    private readonly string _colorKey;
    private readonly double _cx;
    private readonly double _top;
    private readonly double _scale;

    public SubscriptionWarningMark(string colorKey, double cx, double top, double scale, double width, double height)
    {
        _colorKey = colorKey;
        _cx = cx;
        _top = top;
        _scale = scale;
        Width = width;
        Height = height;
        IsHitTestVisible = false;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public override void Render(DrawingContext context) =>
        ServerIcon.DrawWarning(context, _cx, _top, _scale, Ui.Color(this, _colorKey));
}

public sealed class SubscriptionRowView : Border
{
    private const double Pad = 16;

    private readonly Session _session;
    private readonly ServerIcon _chevron;
    private readonly ServerIcon _refresh;
    private readonly StackPanel _subline;
    private readonly StackPanel _details;
    private readonly NameView _title;
    private readonly MenuItem _showItem;
    private readonly MenuItem _deleteItem;
    private readonly ContextMenu _menu;
    private string _detailsKey = "\u0001";
    private int _serverCount;

    public SubscriptionRowView(Session session, SubscriptionInfo info)
    {
        _session = session;
        Info = info;
        Margin = new Thickness(4, 6, 4, 4);
        CornerRadius = new CornerRadius(14);
        BorderThickness = new Thickness(1);
        Background = ServerRes.CardGradient(this);
        ServerRes.Bind(this, BorderBrushProperty, "BorderBrush2");

        _chevron = new ServerIcon(ServerIconKind.Chevron, 32, 50) { Collapsed = info.Collapsed };

        _title = new NameView
        {
            FontSize = 15 * 1.1,
            FontWeight = FontWeight.Bold,
            FontFamily = new FontFamily("Segoe UI"),
            Height = 24,
            VerticalAlignment = VerticalAlignment.Top
        };
        ServerRes.Bind(_title, NameView.ForegroundProperty, "TextBrush");
        _title.SetParts(ServerText.Parts(info.Title, info.Title), null);

        _subline = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            Height = 16,
            ClipToBounds = true
        };

        var texts = new StackPanel { Margin = new Thickness(0, 5, 0, 0), ClipToBounds = true };
        texts.Children.Add(_title);
        texts.Children.Add(_subline);

        var icons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 12, 0)
        };

        if (info.SupportUrl.Length > 0)
        {
            var support = new ServerIcon(ServerIconKind.Support, 28, 28);
            support.Clicked += () => ServerLinks.Open(Info.SupportUrl);
            icons.Children.Add(support);
        }

        _refresh = new ServerIcon(ServerIconKind.Refresh, 28, 28);
        _refresh.Clicked += () => _ = _session.RefreshSubscription(Info.Url);
        icons.Children.Add(_refresh);

        var ping = new ServerIcon(ServerIconKind.Ping, 28, 28);
        ping.Clicked += () => _ = _session.PingSubscription(Info.Url);
        icons.Children.Add(ping);

        var more = new ServerIcon(ServerIconKind.Menu, 28, 28);
        icons.Children.Add(more);

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Height = 50,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        Grid.SetColumn(_chevron, 0);
        Grid.SetColumn(texts, 1);
        Grid.SetColumn(icons, 2);
        header.Children.Add(_chevron);
        header.Children.Add(texts);
        header.Children.Add(icons);
        header.PointerPressed += (_, e) =>
        {
            if (e.Handled || !e.GetCurrentPoint(header).Properties.IsLeftButtonPressed)
                return;

            e.Handled = true;
            _session.Subscriptions.ToggleCollapsed(Info);
            _chevron.Collapsed = Info.Collapsed;
            BuildDetails();
            CollapseToggled?.Invoke();
        };

        _details = new StackPanel { Margin = new Thickness(Pad, 6, Pad, 8) };

        var root = new StackPanel();
        root.Children.Add(header);
        root.Children.Add(_details);
        Child = root;

        _showItem = new MenuItem { Header = L.T("Показать ключ") };
        _showItem.Click += (_, _) => ShowKeyRequested?.Invoke(this);
        _deleteItem = new MenuItem { Header = L.T("Удалить ключ") };
        _deleteItem.Click += (_, _) => DeleteRequested?.Invoke(this);
        _menu = new ContextMenu();
        _menu.Items.Add(_showItem);
        _menu.Items.Add(_deleteItem);
        ContextMenu = _menu;
        more.Clicked += () => _menu.Open(more);

        Update();
    }

    public SubscriptionInfo Info { get; }

    public event Action? CollapseToggled;

    public event Action<SubscriptionRowView>? ShowKeyRequested;

    public event Action<SubscriptionRowView>? DeleteRequested;

    public void Localize()
    {
        _showItem.Header = L.T("Показать ключ");
        _deleteItem.Header = L.T("Удалить ключ");
        _detailsKey = "\u0001";
        Update();
    }

    public void Update()
    {
        _serverCount = _session.Subscriptions.Servers(Info.Url).Count;
        var status = _session.Subscriptions.StatusOf(Info.Url);
        _refresh.Status = status;
        _chevron.Collapsed = Info.Collapsed;
        _title.SetParts(ServerText.Parts(Info.Title, Info.Title), null);
        BuildSubline();

        var key = $"{status?.State}|{status?.Message}|{WarningText()}|{Info.Announce}|{Info.SupportUrl}|{Info.Collapsed}|{Info.Expire}|{L.English}";
        if (key == _detailsKey)
            return;

        _detailsKey = key;
        BuildDetails();
    }

    private void BuildSubline()
    {
        _subline.Children.Clear();
        AddSegment(ServerText.Plural(_serverCount, L.T("сервер"), L.T("сервера"), L.T("серверов")), "TextMutedBrush", false);

        if (Info.Expire != null)
        {
            var key = ExpireKey();
            AddSegment("· " + ExpireShort(), key, key != "TextMutedBrush");
        }

        if (Info.Total > 0)
            AddSegment("· " + L.F("{0} из {1}", ServerText.Bytes(Info.Upload + Info.Download), ServerText.Bytes(Info.Total)), "TextMutedBrush", false);
    }

    private void AddSegment(string text, string brushKey, bool bold)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = 13 * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
            VerticalAlignment = VerticalAlignment.Center
        };
        ServerRes.Bind(block, TextBlock.ForegroundProperty, brushKey);
        _subline.Children.Add(block);
    }

    private string ExpireShort()
    {
        var left = Info.Expire!.Value - DateTime.Now;
        if (left <= TimeSpan.Zero)
            return L.T("Подписка истекла");

        var tail = left.TotalDays >= 1
            ? ServerText.Plural((int)left.TotalDays, L.T("день"), L.T("дня"), L.T("дней"))
            : ServerText.Plural(Math.Max(1, (int)left.TotalHours), L.T("час"), L.T("часа"), L.T("часов"));
        return L.F("осталось {0}", tail);
    }

    private string ExpireKey()
    {
        if (Info.Expire == null)
            return "TextMutedBrush";

        var left = Info.Expire.Value - DateTime.Now;
        return left <= TimeSpan.Zero ? "PingBadBrush" : left.TotalDays < 3 ? "PingMidBrush" : "TextMutedBrush";
    }

    private string? WarningText()
    {
        if (!Info.ExpiresSoon)
            return null;

        return Info.Expire <= DateTime.Now
            ? L.T("Подписка закончилась. Продлите её, чтобы VPN снова заработал.")
            : L.T("До конца подписки меньше 3 дней. Продлите её, иначе доступ будет приостановлен.");
    }

    private void BuildDetails()
    {
        _details.Children.Clear();
        var status = _session.Subscriptions.StatusOf(Info.Url);
        var failed = status?.State == RefreshState.Failed;
        var warning = WarningText();
        var hasAnnounce = Info.Announce.Length > 0;
        if (Info.Collapsed || (!failed && warning == null && !hasAnnounce))
        {
            _details.IsVisible = false;
            return;
        }

        _details.IsVisible = true;

        if (failed)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Height = 24 };
            row.Children.Add(new SubscriptionWarningMark("PingMidColor", 9, 1, 1, 24, 20) { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 0, 0) });
            var message = new TextBlock
            {
                Text = L.F("Не удалось обновить: {0}", status!.Message),
                FontSize = 13 * 1.1,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0)
            };
            ServerRes.Bind(message, TextBlock.ForegroundProperty, "PingMidBrush");
            row.Children.Add(message);
            _details.Children.Add(row);
        }

        if (warning != null)
            _details.Children.Add(BuildWarning(warning));

        if (!hasAnnounce)
            return;

        var line = new Border { Height = 1, Margin = new Thickness(0, 2, 0, 11) };
        ServerRes.Bind(line, BackgroundProperty, "BorderBrush2");
        _details.Children.Add(line);

        var announce = new AnnounceView();
        announce.SetText(Info.Announce);
        announce.RefreshClicked += () => _ = _session.RefreshSubscription(Info.Url);
        _details.Children.Add(announce);
    }

    private Control BuildWarning(string text)
    {
        var expired = Info.Expire <= DateTime.Now;
        var brushKey = expired ? "PingBadBrush" : "PingMidBrush";
        var colorKey = expired ? "PingBadColor" : "PingMidColor";

        var fill = new Border { CornerRadius = new CornerRadius(12), Opacity = 0.14, IsHitTestVisible = false };
        ServerRes.Bind(fill, BackgroundProperty, brushKey);
        var outline = new Border { CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1.2), Opacity = 150.0 / 255, IsHitTestVisible = false };
        ServerRes.Bind(outline, BorderBrushProperty, brushKey);

        var message = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13 * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeight.SemiBold
        };
        ServerRes.Bind(message, TextBlock.ForegroundProperty, "TextBrush");

        var column = new StackPanel { Margin = new Thickness(40, 10, 12, 10) };
        column.Children.Add(message);

        if (Info.SupportUrl.Length > 0)
        {
            var link = new TextBlock
            {
                Text = L.T("Продлить подписку →"),
                FontSize = 13 * 1.1,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeight.SemiBold,
                TextDecorations = TextDecorations.Underline,
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ServerRes.Bind(link, TextBlock.ForegroundProperty, "AccentStrongBrush");
            link.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(link).Properties.IsLeftButtonPressed)
                    ServerLinks.Open(Info.SupportUrl);
            };
            column.Children.Add(link);
        }

        var mark = new SubscriptionWarningMark(colorKey, 20, 12, 1, 40, 30)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.Children.Add(fill);
        grid.Children.Add(outline);
        grid.Children.Add(mark);
        grid.Children.Add(column);
        return grid;
    }
}
