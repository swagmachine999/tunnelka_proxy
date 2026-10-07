namespace Tunnelka.Storage;

public static class UiScaleMigration
{
    public const int Current = 1;
    public const int DefaultPercent = 100;

    public static readonly int[] Steps = { 70, 80, 90, 100, 110, 125, 150, 175, 200 };

    private const float LegacyDesignPercent = 90f;

    public static void Apply(AppData data)
    {
        if (data.UiScaleModel >= Current)
            return;

        data.UiScale = Nearest((int)Math.Round(data.UiScale / LegacyDesignPercent * DefaultPercent));
        data.UiScaleModel = Current;
    }

    public static int Nearest(int percent)
    {
        var best = Steps[0];
        foreach (var step in Steps)
        {
            if (Math.Abs(step - percent) < Math.Abs(best - percent))
                best = step;
        }
        return best;
    }
}
