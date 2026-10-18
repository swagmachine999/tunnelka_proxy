namespace Tunnelka.Services;

public static class PingOrder
{
    public const int StepMs = 40;
    public const int MaxMs = 6000;

    public static int Delay(int index) => Math.Min(index * StepMs, MaxMs);
}
