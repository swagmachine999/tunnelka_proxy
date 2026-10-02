using VpnClient.Models;

namespace VpnClient.Services;

public sealed class TrafficHistory
{
    public const int AllTime = 0;

    private const int SecondsKept = 30 * 60;
    private const int MinutesKept = 24 * 60;

    private readonly Settings _settings;
    private readonly List<(long Second, TrafficCounters Delta)> _seconds = new();

    public TrafficHistory(Settings settings)
    {
        _settings = settings;
    }

    private List<TrafficMinute> Minutes => _settings.Data.TrafficMinutes;

    private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public void Add(TrafficCounters delta)
    {
        var now = Now;
        _seconds.Add((now, delta));
        _seconds.RemoveAll(s => s.Second <= now - SecondsKept);

        var minute = now / 60;
        var bucket = Minutes.Count > 0 && Minutes[Minutes.Count - 1].Minute == minute ? Minutes[Minutes.Count - 1] : null;
        if (bucket == null)
        {
            bucket = new TrafficMinute { Minute = minute };
            Minutes.Add(bucket);
            Minutes.RemoveAll(m => m.Minute <= minute - MinutesKept);
        }

        bucket.ProxyDown += delta.ProxyDown;
        bucket.ProxyUp += delta.ProxyUp;
        bucket.DirectDown += delta.DirectDown;
        bucket.DirectUp += delta.DirectUp;

        var data = _settings.Data;
        data.TotalDownload += delta.ProxyDown;
        data.TotalUpload += delta.ProxyUp;
        data.TotalDirectDownload += delta.DirectDown;
        data.TotalDirectUpload += delta.DirectUp;
    }

    public TrafficCounters Sum(int minutes)
    {
        var data = _settings.Data;
        if (minutes == AllTime)
        {
            return new TrafficCounters
            {
                ProxyDown = data.TotalDownload,
                ProxyUp = data.TotalUpload,
                DirectDown = data.TotalDirectDownload,
                DirectUp = data.TotalDirectUpload
            };
        }

        var result = new TrafficCounters();
        if (minutes * 60 <= SecondsKept)
        {
            var from = Now - minutes * 60;
            foreach (var (second, delta) in _seconds)
            {
                if (second > from)
                    Add(result, delta.ProxyDown, delta.ProxyUp, delta.DirectDown, delta.DirectUp);
            }
            return result;
        }

        var fromMinute = Now / 60 - minutes;
        foreach (var m in Minutes)
        {
            if (m.Minute > fromMinute)
                Add(result, m.ProxyDown, m.ProxyUp, m.DirectDown, m.DirectUp);
        }
        return result;
    }

    public List<(double Down, double Up)> Speeds(int minutes, int points)
    {
        if (minutes == AllTime)
            minutes = MinutesKept;

        var now = Now;
        var perSecond = minutes * 60 <= SecondsKept;
        var length = perSecond ? minutes * 60 : minutes;
        var down = new double[length];
        var up = new double[length];

        if (perSecond)
        {
            foreach (var (second, delta) in _seconds)
            {
                var index = (int)(second - (now - length)) - 1;
                if (index < 0 || index >= length)
                    continue;
                down[index] += delta.ProxyDown + delta.DirectDown;
                up[index] += delta.ProxyUp + delta.DirectUp;
            }
        }
        else
        {
            var last = now / 60;
            foreach (var m in Minutes)
            {
                var index = (int)(m.Minute - (last - length)) - 1;
                if (index < 0 || index >= length)
                    continue;
                down[index] += (m.ProxyDown + m.DirectDown) / 60.0;
                up[index] += (m.ProxyUp + m.DirectUp) / 60.0;
            }
        }

        var size = Math.Max(1, (int)Math.Ceiling(length / (double)points));
        var result = new List<(double Down, double Up)>();
        for (var start = 0; start < length; start += size)
        {
            var count = Math.Min(size, length - start);
            result.Add((down.Skip(start).Take(count).Average(), up.Skip(start).Take(count).Average()));
        }
        return result;
    }

    public void Reset()
    {
        _seconds.Clear();
        Minutes.Clear();
        var data = _settings.Data;
        data.TotalDownload = data.TotalUpload = data.TotalDirectDownload = data.TotalDirectUpload = 0;
        _settings.Save();
    }

    private static void Add(TrafficCounters target, long proxyDown, long proxyUp, long directDown, long directUp)
    {
        target.ProxyDown += proxyDown;
        target.ProxyUp += proxyUp;
        target.DirectDown += directDown;
        target.DirectUp += directUp;
    }
}
