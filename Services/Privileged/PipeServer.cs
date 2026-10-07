using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Tunnelka.Services.Privileged;

public interface IPipeFactory
{
    NamedPipeServerStream Create(bool first);
}

public interface IPeerVerifier
{
    bool TryGetTrustedProcess(NamedPipeServerStream pipe, out int processId);
}

public sealed class PipeServer : IDisposable
{
    public const int MaxMessageBytes = 256 * 1024;
    private const int ReadTimeoutMs = 5000;

    private readonly string _name;
    private readonly IPipeFactory _factory;
    private readonly IPeerVerifier _verifier;
    private readonly IServiceHandler _handler;
    private readonly Action<string> _log;
    private readonly CancellationTokenSource _cancel = new();
    private Thread? _thread;

    public PipeServer(string name, IPipeFactory factory, IPeerVerifier verifier, IServiceHandler handler, Action<string> log)
    {
        _name = name;
        _factory = factory;
        _verifier = verifier;
        _handler = handler;
        _log = log;
    }

    public void Start()
    {
        var first = _factory.Create(true);
        _thread = new Thread(() => Loop(first)) { IsBackground = true, Name = "TunnelkaPipe" };
        _thread.Start();
    }

    public void Dispose()
    {
        _cancel.Cancel();
        try
        {
            using var wake = new NamedPipeClientStream(".", _name, PipeDirection.InOut);
            wake.Connect(100);
        }
        catch (Exception)
        {
        }

        _thread?.Join(2000);
    }

    private void Loop(NamedPipeServerStream pipe)
    {
        while (!_cancel.IsCancellationRequested)
        {
            try
            {
                pipe.WaitForConnection();
                var connected = pipe;
                pipe = _factory.Create(false);
                _ = Task.Run(() => Serve(connected));
            }
            catch (Exception ex)
            {
                pipe.Dispose();
                if (_cancel.IsCancellationRequested)
                    return;

                _log($"pipe error: {ex.Message}");
                Thread.Sleep(500);
                try
                {
                    pipe = _factory.Create(false);
                }
                catch (Exception again)
                {
                    _log($"pipe cannot be recreated: {again.Message}");
                    return;
                }
            }
        }

        pipe.Dispose();
    }

    private void Serve(NamedPipeServerStream pipe)
    {
        using (pipe)
        {
            try
            {
                if (!_verifier.TryGetTrustedProcess(pipe, out var processId))
                {
                    _log("connection from an untrusted process rejected");
                    return;
                }

                using var timeout = new CancellationTokenSource(ReadTimeoutMs);
                var line = ReadLine(pipe, timeout.Token);
                var response = Answer(line, processId);
                var bytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(response) + "\n");
                pipe.Write(bytes, 0, bytes.Length);
                pipe.Flush();
                if (!OperatingSystem.IsWindows())
                    return;
                pipe.WaitForPipeDrain();
            }
            catch (Exception ex)
            {
                _log($"request failed: {ex.Message}");
            }
        }
    }

    private ServiceResponse Answer(string? line, int processId)
    {
        if (line == null)
            return ServiceResponse.Fail("bad request");

        ServiceRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ServiceRequest>(line);
        }
        catch (JsonException)
        {
            return ServiceResponse.Fail("bad request");
        }

        return request == null ? ServiceResponse.Fail("bad request") : _handler.Handle(request, processId);
    }

    private static string? ReadLine(Stream stream, CancellationToken token)
    {
        var bytes = new List<byte>();
        var buffer = new byte[1];
        while (bytes.Count < MaxMessageBytes)
        {
            var read = stream.ReadAsync(buffer, 0, 1, token).GetAwaiter().GetResult();
            if (read == 0)
                return bytes.Count > 0 ? Encoding.UTF8.GetString(bytes.ToArray()) : null;
            if (buffer[0] == (byte)'\n')
                return Encoding.UTF8.GetString(bytes.ToArray());
            bytes.Add(buffer[0]);
        }

        return null;
    }
}
