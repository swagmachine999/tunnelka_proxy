namespace Tunnelka.Next.Kitten;

public interface IKittenAnimation
{
    float Duration { get; }

    void Apply(KittenPose pose, float time);
}
