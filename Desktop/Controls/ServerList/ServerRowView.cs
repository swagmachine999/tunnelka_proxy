using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Models;
using Tunnelka.Services;
using Tunnelka.UI;

namespace Tunnelka.Desktop;

public sealed class ServerRowView : Border
{
    private readonly bool _auto;
    private readonly Border _hoverFill;
    private readonly Border _selectedFill;
    private readonly Border _selectedOutline;
    private readonly Border _bar;
    private readonly Ellipse _ring;
    private readonly Ellipse _dot;
    private readonly NameView _name;
    private readonly TextBlock _ping;
    private readonly ServerBusyDots _busyDots;
    private IDisposable? _ringBinding;
    private IDisposable? _pingBinding;
    private string _pingKey = "";
    private bool _hovered;
    private bool _selected;
    private bool _active;
    private bool _busy;

    public ServerRowView(ProxyServer server)
    {
        Server = server;
        _auto = AutoServers.IsAuto(server);
        DisplayName = _auto ? L.T("Авто") : ServerText.CleanName(server);
        Height = 42;
        Margin = new Thickness(4, 2, 4, 2);
        CornerRadius = new CornerRadius(12);
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);

        _hoverFill = Layer();
        ServerRes.Bind(_hoverFill, BackgroundProperty, "CardHoverBrush");
        _selectedFill = Layer();
        ServerRes.Bind(_selectedFill, BackgroundProperty, "CardSelectedBrush");
        _selectedOutline = Layer();
        _selectedOutline.BorderThickness = new Thickness(1);
        ServerRes.Bind(_selectedOutline, BorderBrushProperty, "AccentBrush");
        _bar = new Border
        {
            Width = 4,
            Margin = new Thickness(1, 11, 0, 11),
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(2),
            IsHitTestVisible = false,
            Opacity = 0,
            Transitions = Fade()
        };
        ServerRes.Bind(_bar, BackgroundProperty, "AccentBrush");

        var badge = new Canvas { Width = 28, Height = 28, Margin = new Thickness(14, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        var flag = new ServerFlagBadge { Code = _auto ? null : ServerText.CountryCode(server.Name) };
        _ring = new Ellipse { Width = 12, Height = 12, IsVisible = false };
        _dot = new Ellipse { Width = 8, Height = 8, IsVisible = false };
        ServerRes.BindColor(_dot, Shape.FillProperty, "MintColor");
        Canvas.SetLeft(flag, 0);
        Canvas.SetTop(flag, 0);
        Canvas.SetLeft(_ring, 18);
        Canvas.SetTop(_ring, 18);
        Canvas.SetLeft(_dot, 20);
        Canvas.SetTop(_dot, 20);
        badge.Children.Add(flag);
        badge.Children.Add(_ring);
        badge.Children.Add(_dot);

        _name = new NameView
        {
            FontSize = 15 * 1.1,
            FontWeight = FontWeight.SemiBold,
            FontFamily = new FontFamily("Segoe UI"),
            VerticalAlignment = VerticalAlignment.Center
        };
        ServerRes.Bind(_name, NameView.ForegroundProperty, "TextBrush");
        IReadOnlyList<NamePart> parts = _auto
            ? new List<NamePart> { new NamePart("⚡", true), new NamePart(DisplayName, false) }
            : ServerText.Parts(server);
        _name.SetParts(parts, null);

        _ping = new TextBlock
        {
            FontSize = 13 * 1.1,
            FontWeight = FontWeight.SemiBold,
            FontFamily = new FontFamily("Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        _busyDots = new ServerBusyDots
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, -2, 0),
            IsVisible = false
        };

        var pingHost = new Grid { Width = 64, Margin = new Thickness(2, 0, 14, 0) };
        pingHost.Children.Add(_ping);
        pingHost.Children.Add(_busyDots);

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            IsHitTestVisible = false
        };
        Grid.SetColumn(badge, 0);
        Grid.SetColumn(_name, 1);
        Grid.SetColumn(pingHost, 2);
        content.Children.Add(badge);
        content.Children.Add(_name);
        content.Children.Add(pingHost);

        var layers = new Grid();
        layers.Children.Add(_hoverFill);
        layers.Children.Add(_selectedFill);
        layers.Children.Add(_selectedOutline);
        layers.Children.Add(_bar);
        layers.Children.Add(content);
        Child = layers;

        var connectItem = new MenuItem { Header = L.T("Подключиться") };
        connectItem.Click += (_, _) => ConnectRequested?.Invoke(this);
        var deleteItem = new MenuItem { Header = L.T("Удалить") };
        deleteItem.Click += (_, _) => DeleteRequested?.Invoke(this);
        var menu = new ContextMenu();
        menu.Items.Add(connectItem);
        if (!_auto)
            menu.Items.Add(deleteItem);
        ContextMenu = menu;

        ApplyRing();
        RefreshPing();
    }

    public ProxyServer Server { get; }

    public string DisplayName { get; }

    public event Action<ServerRowView>? Selected;

    public event Action<ServerRowView>? ConnectRequested;

    public event Action<ServerRowView>? DeleteRequested;

    private static Transitions Fade() => new()
    {
        new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(140) }
    };

    private static Border Layer() => new()
    {
        CornerRadius = new CornerRadius(12),
        IsHitTestVisible = false,
        Opacity = 0,
        Transitions = Fade()
    };

    public void SetState(bool selected, bool active, bool busy)
    {
        _selected = selected;
        _active = active;
        if (_busy != busy)
        {
            _busy = busy;
            _busyDots.IsVisible = busy;
            _busyDots.Running = busy;
        }

        ApplyLayers();
        RefreshPing();
    }

    private void RefreshPing()
    {
        _ping.IsVisible = !_busy;
        var text = Server.PingMs switch
        {
            null => "",
            < 0 => "n/a",
            var ms => $"{ms} ms"
        };
        _ping.Text = text;
        var key = Ui.ToneBrush(Session.PingTone(Server.PingMs));
        if (key == _pingKey)
            return;

        _pingKey = key;
        _pingBinding?.Dispose();
        _pingBinding = ServerRes.Bind(_ping, TextBlock.ForegroundProperty, key);
    }

    private void ApplyLayers()
    {
        _hoverFill.Opacity = _hovered && !_selected ? 1 : 0;
        _selectedFill.Opacity = _selected ? 1 : 0;
        _selectedOutline.Opacity = _selected ? 150.0 / 255 : 0;
        _bar.Opacity = _selected ? 1 : 0;
        _ring.IsVisible = _active;
        _dot.IsVisible = _active;
        ApplyRing();
    }

    private void ApplyRing()
    {
        var key = _selected ? "CardSelectedBrush" : _hovered ? "CardHoverBrush" : "CardBrush";
        _ringBinding?.Dispose();
        _ringBinding = ServerRes.Bind(_ring, Shape.FillProperty, key);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _hovered = true;
        ApplyLayers();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        ApplyLayers();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (e.ClickCount >= 2)
        {
            ConnectRequested?.Invoke(this);
            return;
        }

        Selected?.Invoke(this);
    }
}
