namespace Tunnelka.UI.Controls;

public sealed class SliderRange
{
    public SliderRange(int minimum, int maximum, int step)
    {
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
    }

    public int Minimum { get; }

    public int Maximum { get; }

    public int Step { get; }

    public int Snap(int value)
    {
        var clamped = Math.Max(Minimum, Math.Min(Maximum, value));
        var steps = (int)Math.Round((clamped - Minimum) / (double)Step);
        return Math.Min(Maximum, Minimum + steps * Step);
    }

    public float ToFraction(int value) => (Snap(value) - Minimum) / (float)(Maximum - Minimum);

    public int FromFraction(float fraction)
    {
        var clamped = Math.Max(0f, Math.Min(1f, fraction));
        return Snap((int)Math.Round(Minimum + clamped * (Maximum - Minimum)));
    }
}
