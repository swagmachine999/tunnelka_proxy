using static Tunnelka.Next.Kitten.KittenMath;

namespace Tunnelka.Next.Kitten;

public sealed class KittenBrain
{
    private const float BlinkPeriod = 4.4f;
    private const float BlinkLength = 0.16f;
    private const float WakeCooldown = 10f;
    private const float TwitchLength = 0.35f;
    private const float FirstIdleMin = 8f;
    private const float IdleMin = 20f;
    private const float IdleMax = 38f;

    private readonly Random _random;
    private readonly WakeAnimation _wake = new();
    private readonly WaitAnimation _wait = new();
    private readonly ErrorAnimation _error = new();
    private readonly HuntAnimation _hunt = new();
    private readonly GroomAnimation _groom = new();

    private IKittenAnimation? _playing;
    private float _playingFrom;
    private bool _connected;
    private bool _connecting;
    private bool _wasAwake;
    private float _nextIdle = float.MaxValue;
    private float _twitchFrom = -100;
    private bool _twitchRight;
    private float _gazeX;
    private float _gazeY;
    private float _wantX;
    private float _wantY;
    private float _heart;
    private float _last;
    private float _lastWake = -100;
    private bool _groomNext;

    public KittenBrain(int seed = 0)
    {
        _random = seed == 0 ? new Random() : new Random(seed);
    }

    public bool IsPlaying => _playing != null;

    public void Press(float time)
    {
        if (_connected || _connecting)
            return;

        _lastWake = time;
        Play(_wake, time);
    }

    public void Fail(float time) => Play(_error, time);

    public void Click(float time)
    {
        if (!IsAwake(time))
            return;

        _twitchFrom = time;
        _twitchRight = !_twitchRight;
    }

    public void Look(float x, float y)
    {
        _wantX = Clamp(x);
        _wantY = Clamp(y);
    }

    public void LookAway()
    {
        _wantX = 0;
        _wantY = 0;
    }

    public KittenPose Evaluate(float time, bool connected, bool connecting)
    {
        var seconds = Math.Max(0, Math.Min(0.1f, time - _last));
        _last = time;
        Track(time, connected, connecting);
        Glide(seconds, connected);

        if (_playing != null && time - _playingFrom >= _playing.Duration)
            _playing = null;

        ScheduleIdle(time);

        var pose = BasePose(time);
        if (_playing != null)
            _playing.Apply(pose, time - _playingFrom);
        else if (_connecting && !_connected)
            _wait.Apply(pose, time);

        ApplyTwitch(pose, time);
        return pose;
    }

    private bool IsAwake(float time) => _connected || _connecting || (_playing != null && _playing != _error);

    private void Track(float time, bool connected, bool connecting)
    {
        if (connected == _connected && connecting == _connecting)
            return;

        _connected = connected;
        _connecting = connecting;

        var awake = connected || connecting;
        if (awake && !_wasAwake && _playing == null && time - _lastWake > WakeCooldown)
        {
            _lastWake = time;
            Play(_wake, time);
        }

        if (!awake && _playing != null && _playing != _error)
            _playing = null;

        if (connected)
            _nextIdle = time + FirstIdleMin + (float)_random.NextDouble() * (IdleMax - FirstIdleMin);
        else
            _nextIdle = float.MaxValue;

        _wasAwake = awake;
    }

    private void Glide(float seconds, bool connected)
    {
        var follow = 1 - (float)Math.Exp(-seconds * 9);
        _gazeX += (_wantX - _gazeX) * follow;
        _gazeY += (_wantY - _gazeY) * follow;

        var target = connected && _playing == null ? 1f : 0f;
        _heart += (target - _heart) * (1 - (float)Math.Exp(-seconds * 4));
    }

    private void ScheduleIdle(float time)
    {
        if (_playing != null || !_connected || time < _nextIdle)
            return;

        _groomNext = !_groomNext;
        Play(_groomNext ? _groom : _hunt, time);
        _nextIdle = time + (_groomNext ? _groom.Duration : _hunt.Duration) + IdleMin + (float)_random.NextDouble() * (IdleMax - IdleMin);
    }

    private void Play(IKittenAnimation animation, float time)
    {
        _playing = animation;
        _playingFrom = time;
    }

    private KittenPose BasePose(float time)
    {
        var awake = _connected || _connecting || (_playing != null && _playing != _error);
        var pose = new KittenPose();

        if (!awake || (_playing == _wake && time - _playingFrom < 1.5f))
        {
            pose.EyeOpen = 0;
            pose.Zzz = 1;
            pose.Breath = Wave(time, 1.6f) * 1.6f;
            pose.TailSway = Wave(time, 1.1f) * 3;
            return pose;
        }

        pose.Heart = _heart;
        pose.TailSway = Wave(time, 3.2f) * 9;
        if (_connected && time % BlinkPeriod < BlinkLength)
            pose.EyeOpen = 0;

        if (_connected && _playing == null)
        {
            pose.PupilX = _gazeX * 3;
            pose.PupilY = _gazeY * 2.5f;
            pose.HeadX = _gazeX * 4;
            pose.HeadY = _gazeY * 2.5f;
            pose.HeadTilt = _gazeX * 5;
        }

        return pose;
    }

    private void ApplyTwitch(KittenPose pose, float time)
    {
        var age = time - _twitchFrom;
        if (age < 0 || age > TwitchLength)
            return;

        var flick = (float)Math.Sin(age / TwitchLength * Math.PI) * 16;
        if (_twitchRight)
            pose.EarRight += flick;
        else
            pose.EarLeft += flick;
    }

    private static float Clamp(float value) => Math.Max(-1, Math.Min(1, value));
}
