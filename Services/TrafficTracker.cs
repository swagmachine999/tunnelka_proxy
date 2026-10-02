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

    public (double Down, double Up) Process(TrafficCounters counters)
    {
        var proxyDown = Math.Max(0, counters.ProxyDown - _last.ProxyDown);
        var proxyUp = Math.Max(0, counters.ProxyUp - _last.ProxyUp);
        var down = proxyDown + Math.Max(0, counters.DirectDown - _last.DirectDown);
        var up = proxyUp + Math.Max(0, counters.DirectUp - _last.DirectUp);

        _settings.Data.TotalDownload += proxyDown;
        _settings.Data.TotalUpload += proxyUp;
        _last = counters;

        var interval = _settings.Data.SpeedInterval;
        _samples.Add((down, up));
        if (_samples.Count > interval)
            _samples.RemoveRange(0, _samples.Count - interval);

        return (down, up);
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
