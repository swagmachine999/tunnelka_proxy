using static Tunnelka.Next.Kitten.KittenMath;

namespace Tunnelka.Next.Kitten;

public sealed class ErrorAnimation : IKittenAnimation
{
    public float Duration => 3.2f;

    public void Apply(KittenPose pose, float time)
    {
        var k = Span(time, 0f, 0.18f) * (1 - Span(time, Duration - 0.6f, Duration));

        pose.Heart = 0;
        pose.Zzz = 0;
        pose.EyeOpen = Math.Max(pose.EyeOpen, k);
        pose.EyeWide = k;
        pose.PupilX = 0;
        pose.PupilY = 0;
        pose.HeadX = 0;
        pose.HeadTilt = 0;
        pose.EarFlat = k;
        pose.Bristle = k;
        pose.TailPuff = k;
        pose.TailSway = Wave(time, 14) * 3 * k;
        pose.ScaleX = Lerp(1, 0.95f, k);
        pose.ScaleY = Lerp(1, 1.07f, k);
        pose.BodyX = Wave(time, 45) * 0.9f * k;
        pose.HeadY = -2 * k;
    }
}
