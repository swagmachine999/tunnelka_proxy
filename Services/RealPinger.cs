using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tunnelka.Models;

namespace Tunnelka.Services;

public static class RealPinger
{
    private const int Parallel = 16;
    private const int TimeoutMs = 5000;

    public static async Task<Dictionary<ProxyServer, int>> PingAsync(IReadOnlyList<ProxyServer> servers, string url)
    {
        var results = await TryBatch(servers, url);
        if (results != null)
            return results;

        results = servers.ToDictionary(s => s, _ => -1);
        if (servers.Count == 1)
            return results;

        using var gate = new SemaphoreSlim(4);
        await Task.WhenAll(servers.Select(async server =>
        {
            await gate.WaitAsync();
            try
            {
                var single = await TryBatch(new[] { server }, url);
                if (single != null)
                    results[server] = single[server];
            }
            finally
            {
                gate.Release();
            }
        }));

        return results;
    }

    private static async Task<Dictionary<ProxyServer, int>?> TryBatch(IReadOnlyList<ProxyServer> servers, string url)
    {
        var results = servers.ToDictionary(s => s, _ => -1);
        if (servers.Count == 0 || !File.Exists(XrayRunner.XrayPath))
            return results;

        var ports = FreePorts(servers.Count);
        Directory.CreateDirectory(XrayRunner.ConfigDir);
        var configPath = Path.Combine(XrayRunner.ConfigDir, $"ping-{Guid.NewGuid():N}.json");
        Process? process = null;

        try
        {
            File.WriteAllText(configPath, BuildConfig(servers, ports));
            process = Process.Start(new ProcessStartInfo(XrayRunner.XrayPath, $"run -c \"{configPath}\"")
            {
                WorkingDirectory = XrayRunner.CoreDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process == null || !await WaitForPort(process, ports[0]))
                return null;

            using var gate = new SemaphoreSlim(Parallel);
            var tasks = servers.Select(async (server, i) =>
            {
                await gate.WaitAsync();
                try
                {
                    results[server] = await Measure(ports[i], url);
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }
        catch (Exception)
        {
            return results;
        }
        finally
        {
            try
            {
                if (process is { HasExited: false })
                    process.Kill(true);
                process?.Dispose();
            }
            catch (Exception)
            {
            }

            try
            {
                File.Delete(configPath);
            }
            catch (Exception)
            {
            }
        }
    }

    private static async Task<int> Measure(int port, string url)
    {
        using var handler = new HttpClientHandler
        {
            Proxy = new WebProxy($"http://127.0.0.1:{port}"),
            UseProxy = true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(TimeoutMs) };

        var best = -1;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var timer = Stopwatch.StartNew();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var ms = (int)timer.ElapsedMilliseconds;
                if ((int)response.StatusCode < 500 && (best < 0 || ms < best))
                    best = ms;
            }
            catch (Exception)
            {
            }
        }

        return best;
    }

    private static string BuildConfig(IReadOnlyList<ProxyServer> servers, IReadOnlyList<int> ports)
    {
        var inbounds = new JsonArray();
        var outbounds = new JsonArray();
        var rules = new JsonArray();

        for (var i = 0; i < servers.Count; i++)
        {
            JsonObject outbound;
            try
            {
                outbound = XrayConfigBuilder.Outbound(servers[i], $"out-{i}");
            }
            catch (Exception)
            {
                continue;
            }

            outbounds.Add(outbound);
            inbounds.Add(new JsonObject
            {
                ["tag"] = $"in-{i}",
                ["listen"] = "127.0.0.1",
                ["port"] = ports[i],
                ["protocol"] = "http",
                ["settings"] = new JsonObject()
            });
            rules.Add(new JsonObject
            {
                ["type"] = "field",
                ["inboundTag"] = new JsonArray(JsonValue.Create($"in-{i}")),
                ["outboundTag"] = $"out-{i}"
            });
        }

        var config = new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = "none" },
            ["inbounds"] = inbounds,
            ["outbounds"] = outbounds,
            ["routing"] = new JsonObject { ["rules"] = rules }
        };

        return config.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static List<int> FreePorts(int count)
    {
        var listeners = new List<TcpListener>();
        try
        {
            for (var i = 0; i < count; i++)
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                listeners.Add(listener);
            }

            return listeners.Select(l => ((IPEndPoint)l.LocalEndpoint).Port).ToList();
        }
        finally
        {
            foreach (var listener in listeners)
                listener.Stop();
        }
    }

    private static async Task<bool> WaitForPort(Process process, int port)
    {
        for (var i = 0; i < 40; i++)
        {
            if (process.HasExited)
                return false;

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                return true;
            }
            catch (SocketException)
            {
                await Task.Delay(100);
            }
        }

        return false;
    }
}
