using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Tunnelka.Services;
using Tunnelka.UI;

namespace Tunnelka.Next;

public sealed class StatsPage : UserControl
{
    private static readonly int[] Periods = { 3, 10, 30, 60, 180, 300, 1440, TrafficHistory.AllTime };

    private readonly Session _session;
    private readonly StatsView _view = new();
    private readonly TextBlock _title = new() { Classes = { "title" }, VerticalAlignment = VerticalAlignment.Center };
    private readonly Button _reset = new() { Classes = { "soft" }, Width = 120, HorizontalContentAlignment = HorizontalAlignment.Center };
    private readonly List<ToggleButton> _segments = new();
    private int _index;
    private double _down;
    private double _up;
    private bool _connected;

    public StatsPage(Session session)
    {
        _session = session;
        _index = Math.Max(0, Array.IndexOf(Periods, session.Data.StatsPeriod));

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Height = 52, Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(_title);
        Grid.SetColumn(_reset, 1);
        header.Children.Add(_reset);

        var selector = BuildSelector();
        selector.Margin = new Thickness(0, 0, 0, 10);

        var panel = new StackPanel();
        panel.Children.Add(header);
        panel.Children.Add(selector);
        panel.Children.Add(_view);
        Content = new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };

        _reset.Click += async (_, _) => await Reset();
        session.Traffic += OnTraffic;
        PropertyChanged += (_, e) =>
        {
            if (e.Property == IsVisibleProperty && IsVisible)
                UpdateData();
        };

        Localize();
    }

    private int Period => Periods[_index];

    public void Localize()
    {
        _title.Text = L.T("Статистика");
        _reset.Content = L.T("Сбросить");
        string[] labels = { L.T("3 мин"), L.T("10 мин"), L.T("30 мин"), L.T("1 ч"), L.T("3 ч"), L.T("5 ч"), L.T("24 ч"), L.T("Всё") };
        for (var i = 0; i < _segments.Count; i++)
            _segments[i].Content = labels[i];
        UpdateData();
    }

    private Border BuildSelector()
    {
        var panel = new SegmentPanel();
        for (var i = 0; i < Periods.Length; i++)
        {
            var index = i;
            var button = new ToggleButton
            {
                Classes = { "seg" },
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(8, 0),
                FontSize = 14.3,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsChecked = i == _index
            };
            button.Click += (_, _) => Select(index);
            panel.Children.Add(button);
            _segments.Add(button);
        }

        return new Border
        {
            Classes = { "card" },
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(4),
            Margin = new Thickness(0),
            Child = panel
        };
    }

    private void Select(int index)
    {
        _index = index;
        for (var i = 0; i < _segments.Count; i++)
            _segments[i].IsChecked = i == index;

        _session.Data.StatsPeriod = Period;
        _session.Save();
        UpdateData();
    }

    private void OnTraffic(long down, long up, bool connected)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnTraffic(down, up, connected));
            return;
        }

        _down = down;
        _up = up;
        _connected = connected;
        UpdateData();
    }

    private void UpdateData()
    {
        if (!IsEffectivelyVisible)
            return;

        var minutes = Period;
        var period = minutes == TrafficHistory.AllTime ? L.T("всё время") : ServerText.Duration(minutes * 60);
        var graphPeriod = minutes == TrafficHistory.AllTime ? ServerText.Duration(24 * 3600) : period;
        var history = _session.History;
        _view.SetData(period, graphPeriod, history.Sum(minutes), history.Speeds(minutes, 120), _down, _up, _connected);
    }

    private async Task Reset()
    {
        var dialogs = _session.Dialogs;
        if (dialogs != null && !await dialogs.AskYesNo(L.T("Сбросить всю статистику трафика?")))
            return;

        _session.History.Reset();
        UpdateData();
    }
}
