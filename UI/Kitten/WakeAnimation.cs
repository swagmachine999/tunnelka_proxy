using static Tunnelka.UI.Kitten.KittenMath;

namespace Tunnelka.UI.Kitten;

public sealed class WakeAnimation : IKittenAnimation
{
    public float Duration => 2.7f;

    public void Apply(KittenPose pose, float time)
    {
        var stretch = Pulse(time, 0f, 0.5f, 1.1f);
        var yawn = Pulse(time, 0.5f, 1.0f, 1.55f);
        var open = Span(time, 1.5f, 1.85f);
        var look = Span(time, 1.9f, 2.7f);

        pose.Zzz *= 1 - Span(time, 0f, 0.45f);
        pose.Heart = 0;
        pose.ScaleX = Lerp(1, 0.94f, stretch);
        pose.ScaleY = Lerp(1, 1.1f, stretch);
        pose.HeadY = -5 * stretch;
        pose.HeadTilt = -5 * yawn;
        pose.Yawn = yawn;
        pose.EarFlat = 0.55f * yawn;
        pose.EyeOpen = open;
        pose.EyeWide = 0.15f * open;

        var sweep = Wave(look * 6.28f, 1) * (1 - look * 0.4f);
        pose.PupilX = 3 * sweep * open;
        pose.HeadX = 3.5f * sweep * open;
        pose.HeadTilt += 4 * sweep * open;
        pose.TailSway = Wave(time, 4) * 4 * open;
    }
}
