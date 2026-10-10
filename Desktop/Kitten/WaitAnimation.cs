using static Tunnelka.Desktop.Kitten.KittenMath;

namespace Tunnelka.Desktop.Kitten;

public sealed class WaitAnimation : IKittenAnimation
{
    public float Duration => float.MaxValue;

    public void Apply(KittenPose pose, float time)
    {
        var sweep = Wave(time, 1.7f);
        pose.EyeWide = 0.25f;
        pose.PupilX = 3 * sweep;
        pose.HeadX = 4 * sweep;
        pose.HeadTilt = 4 * sweep;
        pose.EarLeft = -4 + 5 * Math.Max(0, Wave(time, 3.1f));
        pose.EarRight = -4 + 5 * Math.Max(0, Wave(time + 1.3f, 2.7f));

        var flick = 0.5f + 0.5f * Wave(time, 0.9f);
        pose.TailSway = Wave(time, 6.5f) * 7 * flick;
        pose.Heart = 0;
    }
}
