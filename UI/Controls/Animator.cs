namespace Tunnelka.UI.Controls;

public static class Animator
{
    private static readonly System.Windows.Forms.Timer Timer = new() { Interval = 15 };
    private static readonly HashSet<ThemedControl> Running = new();

    static Animator()
    {
        Timer.Tick += (_, _) => Tick();
    }

    public static void Start(ThemedControl control)
    {
        Running.Add(control);
        Timer.Enabled = true;
    }

    public static bool Approach(ref float value, float target)
    {
        if (Math.Abs(target - value) < 0.01f)
        {
            var changed = value != target;
            value = target;
            return changed;
        }

        value += (target - value) * 0.28f;
        return true;
    }

    private static void Tick()
    {
        foreach (var control in Running.ToList())
        {
            if (control.IsDisposed || !control.StepAnimation())
                Running.Remove(control);
        }

        if (Running.Count == 0)
            Timer.Enabled = false;
    }
}
