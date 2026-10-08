namespace Tunnelka.UI.Animation;

public static class FrameRate
{
    public const int FallbackHertz = 60;
    private const int MinHertz = 30;
    private const int MinIntervalMs = 4;
    private const int MaxIntervalMs = 33;

    public static int IntervalFor(int hertz)
    {
        var safe = hertz < MinHertz ? FallbackHertz : hertz;
        return Math.Max(MinIntervalMs, Math.Min(MaxIntervalMs, (int)Math.Round(1000.0 / safe)));
    }
}
