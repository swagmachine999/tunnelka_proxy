namespace Tunnelka.UI.Controls.ServerList;

public sealed class ListScroller
{
    private const float Rate = 14f;
    private const float Settled = 0.05f;

    private float _target;
    private float _max;

    public float Offset { get; private set; }

    public float Max => _max;

    public bool Moving => Math.Abs(_target - Offset) > Settled;

    public void SetRange(float content, float viewport)
    {
        _max = Math.Max(0, content - viewport);
        _target = Clamp(_target);
        Offset = Clamp(Offset);
    }

    public void Nudge(float delta) => _target = Clamp(_target + delta);

    public void Jump(float value)
    {
        _target = Clamp(value);
        Offset = _target;
    }

    public bool Step(float seconds)
    {
        if (!Moving)
        {
            Offset = _target;
            return false;
        }

        Offset += (_target - Offset) * (1 - (float)Math.Exp(-seconds * Rate));
        if (!Moving)
            Offset = _target;
        return true;
    }

    private float Clamp(float value) => Math.Max(0, Math.Min(_max, value));
}
