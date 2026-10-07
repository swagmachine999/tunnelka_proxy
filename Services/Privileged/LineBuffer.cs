namespace Tunnelka.Services.Privileged;

public sealed class LineBuffer
{
    private readonly int _capacity;
    private readonly Queue<string> _lines = new();
    private readonly object _gate = new();
    private long _total;

    public LineBuffer(int capacity = 500)
    {
        _capacity = capacity;
    }

    public long Cursor
    {
        get { lock (_gate) return _total; }
    }

    public void Add(string line)
    {
        lock (_gate)
        {
            _lines.Enqueue(line);
            _total++;
            while (_lines.Count > _capacity)
                _lines.Dequeue();
        }
    }

    public (long Cursor, List<string> Lines) ReadFrom(long cursor)
    {
        lock (_gate)
        {
            var first = _total - _lines.Count;
            var skip = (int)Math.Clamp(cursor - first, 0, _lines.Count);
            return (_total, _lines.Skip(skip).ToList());
        }
    }
}
