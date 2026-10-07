using System.Diagnostics;
using Tunnelka.Models;
using Tunnelka.Services;

namespace Tunnelka.UI.Controls.ServerList;

public class ServerListView : Control
{
    private const float Gutter = 12;
    private const float PanelRadius = 14;
    private const float WheelStep = 96;
    private const float ThumbWidth = 5;
    private const float ThumbMinimum = 36;

    private readonly SubscriptionService _subscriptions;
    private readonly ListScroller _scroller = new();
    private readonly List<ListRow> _rows = new();
    private readonly List<ServerRow> _servers = new();
    private readonly ContextMenuStrip _serverMenu = new();
    private readonly ContextMenuStrip _subscriptionMenu = new();
    private readonly System.Windows.Forms.Timer _frame = new() { Interval = 8 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly WelcomeCard _welcome = new() { Visible = false };
    private ServerRow? _menuServer;
    private SubscriptionRow? _menuSubscription;
    private ListRow? _hoverRow;
    private string _query = "";
    private float _lastFrame;
    private float _contentHeight;
    private bool _periodRaised;
    private bool _thumbHover;
    private bool _layoutDirty;
    private float _dragGrab = -1;

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
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Theme.Bind(this, () => Theme.Surface);

        _frame.Tick += (_, _) => Frame();
        _subscriptions.StatusChanged += ShowStatus;

        _welcome.PasteClicked += (_, _) => PasteRequested?.Invoke(this, EventArgs.Empty);
        _welcome.ManualClicked += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(_welcome);

        _serverMenu.Items.Add(L.T("Подключиться"), null, (_, _) =>
        {
            if (_menuServer != null)
                ServerConnectRequested?.Invoke(_menuServer.Server);
        });
        _serverMenu.Items.Add(L.T("Удалить"), null, (_, _) =>
        {
            if (_menuServer != null)
                ServerDeleteRequested?.Invoke(_menuServer.Server);
        });

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
    }

    private float ViewWidth => Width / Theme.S;

    private float ViewHeight => Height / Theme.S;

    private float RowWidth => ViewWidth - Gutter;

    public void Rebuild(IReadOnlyList<ProxyServer> servers, Func<string?, ProxyServer> autoFor, ProxyServer? selected, ProxyServer? active)
    {
        ClearRows();

        var empty = servers.Count == 0 && _subscriptions.Profiles.Count == 0;
        _welcome.Visible = empty;
        PlaceWelcome();

        foreach (var info in _subscriptions.Profiles)
        {
            var url = info.Url;
            var subscriptionServers = _subscriptions.Servers(url);
            var header = new SubscriptionRow(info)
            {
                ServerCount = subscriptionServers.Count,
                Status = _subscriptions.StatusOf(url)
            };
            header.RefreshClicked += () => SubscriptionRefreshRequested?.Invoke(url);
            header.PingClicked += () => SubscriptionPingRequested?.Invoke(url);
            header.MenuClicked += local => ShowSubscriptionMenu(header, local);
            header.CollapseClicked += () =>
            {
                _subscriptions.ToggleCollapsed(info);
                header.Refresh();
                ApplyFilter();
            };
            Add(header);
            if (subscriptionServers.Count > 0)
                Add(CreateRow(autoFor(url)));
            foreach (var server in subscriptionServers)
                Add(CreateRow(server));
        }

        var loose = servers.Where(s => s.SubscriptionUrl == null || !_subscriptions.IsKnown(s.SubscriptionUrl)).ToList();
        if (loose.Count > 1)
            Add(CreateRow(autoFor(null)));
        foreach (var server in loose)
            Add(CreateRow(server));

        Mark(selected, active);
        ApplyFilter();
    }

    public void Filter(string query)
    {
        _query = query;
        _scroller.Jump(0);
        ApplyFilter();
    }

    public void Mark(ProxyServer? selected, ProxyServer? active)
    {
        foreach (var row in _servers)
        {
            row.IsSelected = row.Server == selected;
            row.IsActive = row.Server == active;
        }
    }

    public void SetBusy(IEnumerable<ProxyServer> servers, bool busy)
    {
        var set = new HashSet<ProxyServer>(servers);
        foreach (var row in _servers)
        {
            if (set.Contains(row.Server))
                row.IsBusy = busy;
        }
    }

    public void RefreshSubscriptionCards()
    {
        foreach (var header in _rows.OfType<SubscriptionRow>())
            header.Refresh();
    }

    public bool ScrollWheel(int delta)
    {
        if (_scroller.Max <= 0)
            return false;

        _scroller.Nudge(-delta / 120f * WheelStep);
        Kick();
        return true;
    }

    public bool ContainsCursor() =>
        IsHandleCreated && Visible && RectangleToScreen(ClientRectangle).Contains(Cursor.Position);

    private void ClearRows()
    {
        foreach (var row in _rows)
            row.Changed -= OnRowChanged;
        _rows.Clear();
        _servers.Clear();
        _hoverRow = null;
    }

    private void Add(ListRow row)
    {
        row.Changed += OnRowChanged;
        _rows.Add(row);
    }

    private ServerRow CreateRow(ProxyServer server)
    {
        var row = new ServerRow(server);
        row.Selected += r => ServerSelected?.Invoke(r.Server);
        row.ConnectRequested += r => ServerConnectRequested?.Invoke(r.Server);
        row.MenuRequested += (r, local) => ShowServerMenu(r, local);
        _servers.Add(row);
        return row;
    }

    private void OnRowChanged()
    {
        _layoutDirty = true;
        Kick();
    }

    private void ShowStatus(string url)
    {
        foreach (var header in _rows.OfType<SubscriptionRow>().Where(h => h.Info.Url == url))
            header.Status = _subscriptions.StatusOf(url);
    }

    private void ApplyFilter()
    {
        foreach (var row in _rows)
        {
            row.Shown = row switch
            {
                SubscriptionRow => _query.Length == 0,
                ServerRow server => _query.Length == 0
                    ? !_subscriptions.IsCollapsed(server.Server.SubscriptionUrl)
                    : server.DisplayName.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0
                      || server.Server.Address.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0,
                _ => true
            };
        }

        Relayout();
        Kick();
    }

    private void Relayout()
    {
        _layoutDirty = false;
        var top = 0f;
        foreach (var row in _rows)
        {
            if (!row.Shown)
                continue;

            row.Top = top;
            row.Extent = row.Measure(RowWidth);
            top += row.Extent;
        }

        _contentHeight = top;
        _scroller.SetRange(_contentHeight, ViewHeight);
    }

    private void PlaceWelcome()
    {
        _welcome.SetBounds(0, 0, Math.Max(1, Width - Theme.Px(Gutter)), Math.Min(Height, Theme.Px(420)));
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Kick();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PlaceWelcome();
        Relayout();
    }

    private void ShowServerMenu(ServerRow row, PointF local)
    {
        _menuServer = row;
        _serverMenu.Show(this, ToClient(row, local));
    }

    private void ShowSubscriptionMenu(SubscriptionRow row, PointF local)
    {
        _menuSubscription = row;
        _subscriptionMenu.Show(this, ToClient(row, local));
    }

    private Point ToClient(ListRow row, PointF local) =>
        new((int)(local.X * Theme.S), (int)((row.Top - _scroller.Offset + local.Y) * Theme.S));

    private ListRow? RowAt(float y)
    {
        var content = y + _scroller.Offset;
        foreach (var row in _rows)
        {
            if (row.Shown && content >= row.Top && content < row.Top + row.Extent)
                return row;
        }

        return null;
    }

    private PointF Local(ListRow row, PointF design) => new(design.X, design.Y + _scroller.Offset - row.Top);

    private RectangleF Thumb()
    {
        if (_scroller.Max <= 0)
            return RectangleF.Empty;

        var view = ViewHeight;
        var length = Math.Max(ThumbMinimum, view * view / _contentHeight);
        var travel = view - length - 8;
        var y = 4 + travel * (_scroller.Offset / _scroller.Max);
        return new RectangleF(ViewWidth - ThumbWidth - 3, y, ThumbWidth, length);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_layoutDirty)
            Relayout();

        var g = e.Graphics;
        Theme.Begin(g, Theme.Surface);

        var panel = new RectangleF(1, 1, ViewWidth - 2, ViewHeight - 2);
        Theme.FillRounded(g, Theme.Card, panel, PanelRadius);
        g.SetClip(new RectangleF(panel.X + 1, panel.Y + 3, panel.Width - 2, panel.Height - 6), System.Drawing.Drawing2D.CombineMode.Replace);

        var offset = _scroller.Offset;
        var width = RowWidth;
        var time = (float)_clock.Elapsed.TotalSeconds;
        foreach (var row in _rows)
        {
            if (!row.Shown)
                continue;

            var top = row.Top - offset;
            if (top + row.Extent < 0)
                continue;
            if (top > ViewHeight)
                break;

            var state = g.Save();
            g.TranslateTransform(0, top);
            g.SetClip(new RectangleF(0, 0, width, row.Extent), System.Drawing.Drawing2D.CombineMode.Intersect);
            row.Draw(g, width, time);
            g.Restore(state);
        }

        g.ResetClip();
        Theme.DrawRounded(g, Theme.Border, panel, PanelRadius);
        DrawThumb(g);
    }

    private void DrawThumb(Graphics g)
    {
        var thumb = Thumb();
        if (thumb.IsEmpty)
            return;

        var alpha = _dragGrab >= 0 ? 190 : _thumbHover ? 150 : 80;
        Theme.FillRounded(g, Color.FromArgb(alpha, Theme.TextMuted), thumb, ThumbWidth / 2);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = Theme.Design(e.Location);

        if (_dragGrab >= 0)
        {
            DragThumb(point.Y);
            return;
        }

        var thumb = Thumb();
        var overThumb = !thumb.IsEmpty && point.X >= ViewWidth - Gutter && point.Y >= thumb.Top - 4 && point.Y <= thumb.Bottom + 4;
        if (overThumb != _thumbHover)
        {
            _thumbHover = overThumb;
            Invalidate();
        }

        var row = overThumb ? null : RowAt(point.Y);
        if (!ReferenceEquals(row, _hoverRow))
        {
            _hoverRow?.PointerLeft();
            _hoverRow = row;
        }

        if (row == null)
        {
            Cursor = Cursors.Default;
            return;
        }

        var local = Local(row, point);
        row.PointerMoved(local);
        Cursor = row.Hits(local) ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverRow?.PointerLeft();
        _hoverRow = null;
        if (_thumbHover)
        {
            _thumbHover = false;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button != MouseButtons.Left)
            return;

        var point = Theme.Design(e.Location);
        var thumb = Thumb();
        if (thumb.IsEmpty || point.X < ViewWidth - Gutter)
            return;

        if (point.Y >= thumb.Top && point.Y <= thumb.Bottom)
        {
            _dragGrab = point.Y - thumb.Top;
        }
        else
        {
            _dragGrab = thumb.Height / 2;
            DragThumb(point.Y);
        }

        Capture = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragGrab >= 0)
        {
            _dragGrab = -1;
            Capture = false;
            Invalidate();
            return;
        }

        if (e.Button != MouseButtons.Right)
            return;

        var point = Theme.Design(e.Location);
        var row = RowAt(point.Y);
        row?.RightClick(Local(row, point));
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left)
            return;

        var point = Theme.Design(e.Location);
        if (point.X >= ViewWidth - Gutter && !Thumb().IsEmpty)
            return;

        var row = RowAt(point.Y);
        row?.Click(Local(row, point));
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button != MouseButtons.Left)
            return;

        var point = Theme.Design(e.Location);
        var row = RowAt(point.Y);
        row?.DoubleClick(Local(row, point));
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (ScrollWheel(e.Delta) && e is HandledMouseEventArgs handled)
            handled.Handled = true;
    }

    private void DragThumb(float pointerY)
    {
        var thumb = Thumb();
        var travel = ViewHeight - thumb.Height - 8;
        if (travel <= 0)
            return;

        var fraction = Math.Max(0, Math.Min(1, (pointerY - _dragGrab - 4) / travel));
        _scroller.Jump(fraction * _scroller.Max);
        Invalidate();
    }

    private void Kick()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        Invalidate();
        if (_frame.Enabled)
            return;

        _lastFrame = (float)_clock.Elapsed.TotalSeconds;
        if (!_periodRaised)
            _periodRaised = TryPeriod(TimeBeginPeriod);

        _frame.Start();
    }

    private void Frame()
    {
        var now = (float)_clock.Elapsed.TotalSeconds;
        var seconds = Math.Min(0.1f, now - _lastFrame);
        _lastFrame = now;

        if (_layoutDirty)
            Relayout();

        var moving = _scroller.Step(seconds);
        foreach (var row in _rows)
        {
            if (row.Shown)
                moving |= row.Step() | row.NeedsFrames;
        }

        Invalidate();
        if (!moving)
            StopFrames();
    }

    private void StopFrames()
    {
        _frame.Stop();
        if (!_periodRaised)
            return;

        TryPeriod(TimeEndPeriod);
        _periodRaised = false;
    }

    private static bool TryPeriod(Func<uint, uint> change)
    {
        try
        {
            change(1);
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint milliseconds);

    [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint milliseconds);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscriptions.StatusChanged -= ShowStatus;
            StopFrames();
            _frame.Dispose();
            _serverMenu.Dispose();
            _subscriptionMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
