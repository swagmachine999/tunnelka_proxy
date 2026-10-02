namespace VpnClient.Services;

public sealed class TrafficTracker
{
    private readonly Settings _settings;
    private readonly List<(double Down, double Up)> _samples = new();
    private TrafficCounters _last = new();
    private int _tick;

    public TrafficTracker(Settings settings)
    {
        _settings = settings;
    }

    public void Reset()
    {
        _last = new TrafficCounters();
        _samples.Clear();
        _tick = 0;
    }

    public void ResetCounters() => _last = new TrafficCounters();

    public void RestartAveraging() => _tick = 0;

    public TrafficCounters Process(TrafficCounters counters)
    {
        var delta = new TrafficCounters
        {
            ProxyDown = Math.Max(0, counters.ProxyDown - _last.ProxyDown),
            ProxyUp = Math.Max(0, counters.ProxyUp - _last.ProxyUp),
            DirectDown = Math.Max(0, counters.DirectDown - _last.DirectDown),
            DirectUp = Math.Max(0, counters.DirectUp - _last.DirectUp)
        };
        _last = counters;

        var interval = _settings.Data.SpeedInterval;
        _samples.Add((delta.ProxyDown + delta.DirectDown, delta.ProxyUp + delta.DirectUp));
        if (_samples.Count > interval)
            _samples.RemoveRange(0, _samples.Count - interval);

        return delta;
    }

    public bool TryGetAverage(out double down, out double up)
    {
        down = up = 0;
        _tick++;
        if (_tick < _settings.Data.SpeedInterval || _samples.Count == 0)
            return false;

        _tick = 0;
        down = _samples.Average(s => s.Down);
        up = _samples.Average(s => s.Up);
        return true;
    }
}
