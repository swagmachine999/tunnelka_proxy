namespace Tunnelka.Models;

public enum RefreshState
{
    Busy,
    Done,
    Failed
}

public sealed record RefreshStatus(RefreshState State, string Message, DateTime At);
