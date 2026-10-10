using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Tunnelka.Next;

public sealed class WindowScaler
{
    private const double DesignWidth = 1233;
    private const double DesignHeight = 760;
    private const double MinDesignWidth = 920;
    private const double MinDesignHeight = 735;
    private const double ReferenceHeight = 1080;
    private const double ScreenMargin = 24;

    private readonly Window _window;
    private readonly LayoutTransformControl _target;
    private readonly Session _session;
    private double _current;

    public WindowScaler(Window window, LayoutTransformControl target, Session session)
    {
        _window = window;
        _target = target;
        _session = session;
        _window.ScalingChanged += (_, _) => Apply(false);
    }

    public void Apply(bool initial)
    {
        var area = WorkingArea();
        var wanted = _session.Data.UiScale / 100.0 * ResolutionFactor();
        var fit = Math.Min(wanted, Math.Min(area.Width / MinDesignWidth, area.Height / MinDesignHeight));
        var previous = _current;
        _current = fit;

        _target.LayoutTransform = new ScaleTransform(fit, fit);
        _window.MinWidth = MinDesignWidth * fit;
        _window.MinHeight = MinDesignHeight * fit;

        if (initial || previous <= 0)
        {
            _window.Width = Math.Min(DesignWidth * fit, area.Width);
            _window.Height = Math.Min(DesignHeight * fit, area.Height);
            return;
        }

        if (Math.Abs(previous - fit) < 0.001)
            return;

        var ratio = fit / previous;
        _window.Width = Math.Min(_window.Width * ratio, area.Width);
        _window.Height = Math.Min(_window.Height * ratio, area.Height);
    }

    private Size WorkingArea()
    {
        var screen = CurrentScreen();
        if (screen == null)
            return new Size(DesignWidth, DesignHeight);

        return new Size(
            Math.Max(MinDesignWidth * 0.5, screen.WorkingArea.Width / screen.Scaling - ScreenMargin),
            Math.Max(MinDesignHeight * 0.5, screen.WorkingArea.Height / screen.Scaling - ScreenMargin));
    }

    private double ResolutionFactor()
    {
        var screen = CurrentScreen();
        if (screen == null)
            return 1;

        return Math.Max(1, screen.Bounds.Height / screen.Scaling / ReferenceHeight);
    }

    private Avalonia.Platform.Screen? CurrentScreen() =>
        _window.Screens.ScreenFromVisual(_window) ?? _window.Screens.Primary;
}
