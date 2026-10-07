using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Tunnelka.Services.Privileged;

public sealed class ServiceClient : IServiceChannel
{
    private const int ConnectTimeoutMs = 400;
    private const int ReplyTimeoutMs = 8000;
    private static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(10);

    private readonly string _pipeName;
    private readonly Func<string?> _ownPath;
    private readonly object _gate = new();
    private DateTime _checkedAt = DateTime.MinValue;
    private bool _available;

    public ServiceClient(string pipeName, Func<string?> ownPath)
    {
        _pipeName = pipeName;
        _ownPath = ownPath;
    }

    public static ServiceClient Default { get; } = new(ServiceConstants.PipeName, () => Environment.ProcessPath);

    public bool IsAvailable
    {
        get
        {
            lock (_gate)
            {
                if (DateTime.UtcNow - _checkedAt < CacheTime)
                    return _available;
            }

            var available = Probe();
            lock (_gate)
            {
                _available = available;
                _checkedAt = DateTime.UtcNow;
            }

            return available;
        }
    }

    public void Forget()
    {
        lock (_gate)
            _checkedAt = DateTime.MinValue;
    }

    public ServiceResponse? Send(ServiceRequest request)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.None);
            pipe.Connect(ConnectTimeoutMs);

            var encoding = new UTF8Encoding(false);
            var payload = encoding.GetBytes(JsonSerializer.Serialize(request) + "\n");
            pipe.Write(payload, 0, payload.Length);
            pipe.Flush();

            using var cancel = new CancellationTokenSource(ReplyTimeoutMs);
            var line = ReadLine(pipe, cancel.Token);
            return line == null ? null : JsonSerializer.Deserialize<ServiceResponse>(line);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or JsonException or UnauthorizedAccessException or OperationCanceledException or InvalidOperationException)
        {
            return null;
        }
    }

    private bool Probe()
    {
        var reply = Send(new ServiceRequest { Command = ServiceCommand.Ping });
        if (reply is not { Ok: true } || reply.Protocol != ServiceConstants.ProtocolVersion)
            return false;

        var own = _ownPath();
        return own != null && reply.ServiceExe != null
            && string.Equals(Path.GetFullPath(own), Path.GetFullPath(reply.ServiceExe), StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadLine(Stream stream, CancellationToken token)
    {
        var bytes = new List<byte>();
        var buffer = new byte[1];
        while (bytes.Count < PipeServer.MaxMessageBytes)
        {
            var read = stream.ReadAsync(buffer, 0, 1, token).GetAwaiter().GetResult();
            if (read == 0)
                break;
            if (buffer[0] == (byte)'\n')
                return Encoding.UTF8.GetString(bytes.ToArray());
            bytes.Add(buffer[0]);
        }

        return bytes.Count > 0 && bytes.Count < PipeServer.MaxMessageBytes ? Encoding.UTF8.GetString(bytes.ToArray()) : null;
    }
}
