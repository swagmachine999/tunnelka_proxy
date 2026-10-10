namespace Tunnelka.Desktop.Kitten;

public sealed class FramePacer
{
    public const int RippleMs = 15;
    public const int ActiveMs = 25;
    public const int IdleMs = 33;

    public int IntervalMs(bool rippling, bool active) =>
        rippling ? RippleMs : active ? ActiveMs : IdleMs;
}
