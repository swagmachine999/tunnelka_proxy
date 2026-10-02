using Tunnelka.Services;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class StatsPage : Panel
{
    private static readonly int[] Periods = { 3, 10, 30, 60, 180, 300, 1440, TrafficHistory.AllTime };

    private readonly TrafficHistory _history;
    private readonly StatsView _view = new() { Dock = DockStyle.Fill };
    private readonly Segmented _periodSelector = new(L.T("3 мин"), L.T("10 мин"), L.T("30 мин"), L.T("1 ч"), L.T("3 ч"), L.T("5 ч"), L.T("24 ч"), L.T("Всё"))
    {
        Dock = DockStyle.Top
    };

    private double _down;
    private double _up;
    private bool _connected;

    public event Action<int>? PeriodChanged;

    public StatsPage(TrafficHistory history, int period)
    {
        _history = history;
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);

        _periodSelector.SelectedIndex = Math.Max(0, Array.IndexOf(Periods, period));
        _periodSelector.SelectedIndexChanged += (_, _) =>
        {
            PeriodChanged?.Invoke(Period);
            UpdateData();
        };

        var reset = PageParts.Button(L.T("Сбросить"), false);
        reset.Dock = DockStyle.Right;
        reset.Width = Theme.Px(120);
        reset.Click += (_, _) => Reset();

        var header = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(52), Padding = Theme.Px(0, 10, 6, 8) }, () => Theme.Surface);
        var title = PageParts.Title(L.T("Статистика"));
        title.Dock = DockStyle.Fill;
        header.Controls.Add(title);
        header.Controls.Add(reset);

        Controls.Add(_view);
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(_periodSelector);
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(12) }, () => Theme.Surface));
        Controls.Add(header);

        VisibleChanged += (_, _) => UpdateData();
    }

    private int Period => Periods[_periodSelector.SelectedIndex];

    public void SetSpeed(double down, double up, bool connected)
    {
        _down = down;
        _up = up;
        _connected = connected;
        UpdateData();
    }

    private void UpdateData()
    {
        if (!Visible)
            return;

        var minutes = Period;
        var period = minutes == TrafficHistory.AllTime ? L.T("всё время") : ServerText.Duration(minutes * 60);
        var graphPeriod = minutes == TrafficHistory.AllTime ? ServerText.Duration(24 * 3600) : period;
        _view.SetData(period, graphPeriod, _history.Sum(minutes), _history.Speeds(minutes, 120), _down, _up, _connected);
    }

    private void Reset()
    {
        var answer = MessageBox.Show(FindForm(), L.T("Сбросить всю статистику трафика?"), "Tunnelka", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        _history.Reset();
        UpdateData();
    }
}
