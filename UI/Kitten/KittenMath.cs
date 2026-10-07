namespace Tunnelka.UI.Kitten;

public static class KittenMath
{
    public static float Clamp01(float value) => Math.Max(0, Math.Min(1, value));

    public static float Lerp(float from, float to, float amount) => from + (to - from) * amount;

    public static float Smooth(float value)
    {
        var t = Clamp01(value);
        return t * t * (3 - 2 * t);
    }

    public static float Span(float time, float from, float to) => Smooth((time - from) / (to - from));

    public static float Pulse(float time, float from, float peak, float to) =>
        Span(time, from, peak) * (1 - Span(time, peak, to));

    public static float Wave(float time, float speed) => (float)Math.Sin(time * speed);
}
