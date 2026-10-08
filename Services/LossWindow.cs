namespace Tunnelka.Services;

public sealed class LossWindow
{
    public const int Capacity = 50;
    public const int MinSamples = 10;

    private readonly Queue<bool> _results = new();
    private int _lost;

    public int Count => _results.Count;

    public int Percent => _results.Count < MinSamples ? 0 : (int)Math.Round(100.0 * _lost / _results.Count);

    public void Add(bool delivered)
    {
        _results.Enqueue(delivered);
        if (!delivered)
            _lost++;

        while (_results.Count > Capacity)
        {
            if (!_results.Dequeue())
                _lost--;
        }
    }

    public void Clear()
    {
        _results.Clear();
        _lost = 0;
    }
}
