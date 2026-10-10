using static Tunnelka.Next.Kitten.KittenMath;

namespace Tunnelka.Next.Kitten;

public sealed class GroomAnimation : IKittenAnimation
{
    public float Duration => 4.4f;

    public void Apply(KittenPose pose, float time)
    {
        var raise = Span(time, 0f, 0.6f) * (1 - Span(time, 3.7f, 4.3f));
        var licking = Span(time, 0.6f, 0.9f) * (1 - Span(time, 3.4f, 3.7f));
        var beat = Wave(time, 11);

        pose.Heart = 0;
        pose.Lick = raise * (1 - 0.12f * licking * (0.5f + 0.5f * beat));
        pose.Tongue = licking * Math.Max(0, beat);
        pose.EyeOpen = Lerp(1, 0.3f, raise);
        pose.HeadY = 7 * raise;
        pose.HeadTilt = -6 * raise + 1.5f * beat * licking;
        pose.HeadX = -3 * raise;
        pose.ScaleY = Lerp(1, 1.03f, raise);
        pose.TailSway *= 0.3f;
    }
}
