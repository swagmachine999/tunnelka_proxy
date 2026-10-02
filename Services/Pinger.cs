using System.Diagnostics;
using System.Net.Sockets;

namespace Tunnelka.Services;

public static class Pinger
{
    public static async Task<int?> TcpPingAsync(string host, int port, int timeoutMs = 3000)
    {
        using var client = new TcpClient();
        using var cts = new CancellationTokenSource(timeoutMs);
        var timer = Stopwatch.StartNew();

        try
        {
            await client.ConnectAsync(host, port, cts.Token);
            return (int)timer.ElapsedMilliseconds;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
