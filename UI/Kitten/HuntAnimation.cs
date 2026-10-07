using static Tunnelka.UI.Kitten.KittenMath;

namespace Tunnelka.UI.Kitten;

public sealed class HuntAnimation : IKittenAnimation
{
    public float Duration => 5.2f;

    public void Apply(KittenPose pose, float time)
    {
        var crouch = Span(time, 0f, 0.8f) * (1 - Span(time, 3.9f, 4.7f));
        var wiggle = Span(time, 0.8f, 1.0f) * (1 - Span(time, 2.5f, 2.65f));
        var air = Pulse(time, 2.65f, 3.05f, 3.45f);
        var land = Pulse(time, 3.4f, 3.6f, 3.9f);
        var confused = Span(time, 4.2f, 4.6f) * (1 - Span(time, 4.8f, 5.2f));

        pose.Heart = 0;
        pose.ScaleX = Lerp(1, 1.08f, crouch) - 0.1f * air + 0.12f * land;
        pose.ScaleY = Lerp(1, 0.86f, crouch) + 0.2f * air - 0.14f * land;
        pose.BodyY = Lerp(0, 3, crouch) - 44 * air;
        pose.BodyX = Wave(time, 26) * 3 * wiggle + 14 * air;
        pose.HeadY = 10 * crouch - 6 * air;
        pose.EyeWide = 0.7f * crouch * (1 - land);
        pose.PupilX = 3 * crouch;
        pose.PupilY = -2 * crouch;
        pose.EarLeft = -6 * crouch;
        pose.EarRight = -6 * crouch;
        pose.EarFlat = 0.45f * air;
        pose.TailSway = Wave(time, 18) * 9 * wiggle + Wave(time, 3) * 5 * crouch;
        pose.HeadTilt = 9 * confused;
        pose.PupilX += -2 * confused;
    }
}
