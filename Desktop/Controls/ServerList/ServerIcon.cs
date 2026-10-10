using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Tunnelka.Models;

namespace Tunnelka.Desktop;

public enum ServerIconKind
{
    Gauge,
    Refresh,
    Ping,
    Menu,
    Support,
    Chevron,
    Search
}

public sealed class ServerIcon : Control
{
    private const double DoneSeconds = 2.4;

    private readonly DispatcherTimer _frame = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private bool _hover;
    private bool _collapsed;
    private RefreshStatus? _status;

    public ServerIcon(ServerIconKind kind, double width, double height)
    {
        Kind = kind;
        Width = width;
        Height = height;
        _frame.Tick += (_, _) => Tick();
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
        if (kind != ServerIconKind.Chevron && kind != ServerIconKind.Search)
            Cursor = new Cursor(StandardCursorType.Hand);
    }

    public ServerIconKind Kind { get; }

    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            if (_collapsed == value)
                return;

            _collapsed = value;
            InvalidateVisual();
        }
    }

    public RefreshStatus? Status
    {
        get => _status;
        set
        {
            _status = value;
            UpdateTimer();
            InvalidateVisual();
        }
    }

    public event Action? Clicked;

    private bool Failed => _status?.State == RefreshState.Failed;

    private double SinceStatus => _status == null ? double.MaxValue : (DateTime.Now - _status.At).TotalSeconds;

    private bool NeedsFrames =>
        Kind == ServerIconKind.Refresh &&
        (_status?.State == RefreshState.Busy || (_status?.State == RefreshState.Done && SinceStatus < DoneSeconds + 0.3));

    private void UpdateTimer()
    {
        if (NeedsFrames && VisualRoot != null)
            _frame.Start();
        else
            _frame.Stop();
    }

    private void Tick()
    {
        InvalidateVisual();
        if (!NeedsFrames)
            _frame.Stop();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _frame.Stop();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _hover = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hover = false;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Kind == ServerIconKind.Chevron || Kind == ServerIconKind.Search)
            return;

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            Clicked?.Invoke();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Kind != ServerIconKind.Chevron && Kind != ServerIconKind.Search)
            e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Transparent, bounds);
        var cx = bounds.Width / 2;
        var cy = bounds.Height / 2;

        if (_hover && Kind != ServerIconKind.Chevron && Kind != ServerIconKind.Search)
        {
            var key = Kind == ServerIconKind.Gauge ? "SidebarHoverBrush" : "CardHoverBrush";
            var radius = Kind == ServerIconKind.Gauge ? 12 : 9;
            context.DrawRectangle(Ui.Brush(this, key), null, bounds, radius, radius);
        }

        switch (Kind)
        {
            case ServerIconKind.Gauge:
                DrawGauge(context, cx, cy, 10, 5, -6, 2.5, Ui.Brush(this, _hover ? "AccentStrongBrush" : "TextBrush"));
                break;
            case ServerIconKind.Ping:
                DrawGauge(context, cx, cy, 8, 4, -5, 2, Ui.Brush(this, "TextMutedBrush"));
                break;
            case ServerIconKind.Refresh:
                DrawRefreshState(context, cx, cy);
                break;
            case ServerIconKind.Menu:
                DrawMenu(context, cx, cy);
                break;
            case ServerIconKind.Support:
                DrawSupport(context, cx, cy);
                break;
            case ServerIconKind.Chevron:
                DrawChevron(context, cx, cy);
                break;
            case ServerIconKind.Search:
                DrawSearch(context, cx, cy);
                break;
        }
    }

    private static void DrawGauge(DrawingContext context, double cx, double cy, double radius, double needleX, double needleY, double dot, IBrush brush)
    {
        cy += (radius - dot) / 2;
        var pen = ServerRes.IconPen(brush);
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(new Point(cx - radius, cy), false);
            stream.ArcTo(new Point(cx + radius, cy), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            stream.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
        context.DrawLine(pen, new Point(cx, cy), new Point(cx + needleX, cy + needleY));
        context.DrawEllipse(brush, null, new Point(cx, cy), dot, dot);
    }

    private void DrawMenu(DrawingContext context, double cx, double cy)
    {
        var brush = Ui.Brush(this, "TextMutedBrush");
        for (var i = -1; i <= 1; i++)
            context.DrawEllipse(brush, null, new Point(cx + i * 6, cy), 1.8, 1.8);
    }

    private void DrawSupport(DrawingContext context, double cx, double cy)
    {
        cy -= 0.5;
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(new Point(cx - 9, cy - 1), true);
            stream.LineTo(new Point(cx + 9, cy - 8));
            stream.LineTo(new Point(cx + 5, cy + 9));
            stream.LineTo(new Point(cx - 1, cy + 3));
            stream.EndFigure(true);
        }

        context.DrawGeometry(Ui.Brush(this, "AccentBrush"), null, geometry);
        var pen = new Pen(Ui.Brush(this, "CardBrush"), 1.4);
        context.DrawLine(pen, new Point(cx - 1, cy + 3), new Point(cx + 9, cy - 8));
    }

    private void DrawChevron(DrawingContext context, double cx, double cy)
    {
        var pen = ServerRes.IconPen(Ui.Brush(this, "TextMutedBrush"));
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            if (_collapsed)
            {
                var x = cx - 0.5;
                stream.BeginFigure(new Point(x - 2, cy - 5), false);
                stream.LineTo(new Point(x + 3, cy));
                stream.LineTo(new Point(x - 2, cy + 5));
            }
            else
            {
                var y = cy - 0.5;
                stream.BeginFigure(new Point(cx - 5, y - 2), false);
                stream.LineTo(new Point(cx, y + 3));
                stream.LineTo(new Point(cx + 5, y - 2));
            }

            stream.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private void DrawSearch(DrawingContext context, double cx, double cy)
    {
        var pen = ServerRes.IconPen(Ui.Brush(this, "TextMutedBrush"));
        var lensX = cx - 1;
        var lensY = cy - 1;
        context.DrawEllipse(null, pen, new Point(lensX, lensY), 6, 6);
        context.DrawLine(pen, new Point(lensX + 4, lensY + 4), new Point(lensX + 8, lensY + 8));
    }

    private void DrawRefreshState(DrawingContext context, double cx, double cy)
    {
        switch (_status?.State)
        {
            case RefreshState.Busy:
                var rotation = DateTime.Now.TimeOfDay.TotalSeconds * 360 % 360;
                DrawRefresh(context, cx, cy, rotation, Ui.Brush(this, "AccentBrush"));
                return;
            case RefreshState.Done when SinceStatus < DoneSeconds:
                DrawCheck(context, cx, cy, SinceStatus);
                return;
        }

        DrawRefresh(context, cx, cy, 0, Ui.Brush(this, "TextMutedBrush"));
        if (Failed)
            DrawWarning(context, cx + 7, cy + 2, 0.75, Ui.Color(this, "PingMidColor"));
    }

    private static void DrawRefresh(DrawingContext context, double cx, double cy, double rotation, IBrush brush)
    {
        var matrix = Matrix.CreateRotation(rotation * Math.PI / 180) * Matrix.CreateTranslation(cx + 0.7, cy + 1.1);
        using var state = context.PushTransform(matrix);
        var pen = ServerRes.IconPen(brush);
        var start = 40 * Math.PI / 180;
        var end = 320 * Math.PI / 180;
        var from = new Point(8 * Math.Cos(start), 8 * Math.Sin(start));
        var tip = new Point(8 * Math.Cos(end), 8 * Math.Sin(end));

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(from, false);
            stream.ArcTo(tip, new Size(8, 8), 0, true, SweepDirection.Clockwise);
            stream.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
        context.DrawLine(pen, tip, new Point(tip.X - 5, tip.Y - 1));
        context.DrawLine(pen, tip, new Point(tip.X + 0.5, tip.Y - 5));
    }

    private void DrawCheck(DrawingContext context, double cx, double cy, double time)
    {
        var grow = Math.Min(1, time / 0.25);
        var fade = Math.Min(1, Math.Max(0, (DoneSeconds - time) / 0.4));
        var radius = 10 * (0.6 + 0.4 * grow);
        var good = Ui.Color(this, "PingGoodColor");
        context.DrawEllipse(new SolidColorBrush(ServerRes.Alpha(good, fade)), null, new Point(cx, cy), radius, radius);

        var stroke = Math.Min(1, Math.Max(0, (time - 0.15) / 0.3));
        if (stroke <= 0)
            return;

        var a = new Point(cx - 4.75, cy + 0.2);
        var b = new Point(cx - 1.45, cy + 3.5);
        var c = new Point(cx + 4.75, cy - 3.5);
        var pen = new Pen(new SolidColorBrush(ServerRes.Alpha(Colors.White, fade)), 2.2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        if (stroke < 0.4)
        {
            context.DrawLine(pen, a, Lerp(a, b, stroke / 0.4));
            return;
        }

        context.DrawLine(pen, a, b);
        context.DrawLine(pen, b, Lerp(b, c, (stroke - 0.4) / 0.6));
    }

    private static Point Lerp(Point from, Point to, double t) =>
        new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);

    public static void DrawWarning(DrawingContext context, double cx, double top, double scale, Color fill)
    {
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(new Point(cx, top), true);
            stream.LineTo(new Point(cx + 9 * scale, top + 16 * scale));
            stream.LineTo(new Point(cx - 9 * scale, top + 16 * scale));
            stream.EndFigure(true);
        }

        context.DrawGeometry(new SolidColorBrush(fill), null, geometry);
        var mark = new Pen(Brushes.White, 1.8 * scale, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawLine(mark, new Point(cx, top + 5 * scale), new Point(cx, top + 10 * scale));
        context.DrawLine(mark, new Point(cx, top + 13 * scale), new Point(cx, top + 13.2 * scale));
    }
}
