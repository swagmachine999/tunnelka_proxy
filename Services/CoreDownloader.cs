using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;

namespace Tunnelka.Services;

public sealed class CoreDownloader
{
    private readonly int? _proxyPort;

    public CoreDownloader(int? proxyPort = null)
    {
        _proxyPort = proxyPort;
    }

    public async Task DownloadAsync(CorePackage package, IProgress<int> progress, CancellationToken cancel = default)
    {
        Directory.CreateDirectory(CoreLocator.DownloadedDir);
        var archive = Path.Combine(CoreLocator.DownloadedDir, package.Name + ".download");

        try
        {
            var hash = await FetchAsync(package, archive, progress, cancel);
            if (!string.Equals(hash, package.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(L.T("Контрольная сумма не совпала, файл не использован"));

            Extract(package, archive);
        }
        finally
        {
            TryDelete(archive);
        }
    }

    private async Task<string> FetchAsync(CorePackage package, string path, IProgress<int> progress, CancellationToken cancel)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(package.Url, HttpCompletionOption.ResponseHeadersRead, cancel);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? package.Size;
        using var sha = SHA256.Create();
        await using var source = await response.Content.ReadAsStreamAsync(cancel);
        await using var target = File.Create(path);

        var buffer = new byte[81920];
        long done = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancel)) > 0)
        {
            sha.TransformBlock(buffer, 0, read, null, 0);
            await target.WriteAsync(buffer.AsMemory(0, read), cancel);
            done += read;
            if (total > 0)
                progress.Report((int)Math.Min(100, done * 100 / total));
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }

    private static void Extract(CorePackage package, string archivePath)
    {
        var staging = Path.Combine(CoreLocator.DownloadedDir, "staging-" + package.Name);
        TryDeleteFolder(staging);
        Directory.CreateDirectory(staging);

        try
        {
            using (var zip = ZipFile.OpenRead(archivePath))
            {
                foreach (var (entryName, target) in package.Files)
                {
                    var entry = zip.Entries.FirstOrDefault(e => string.Equals(e.Name, entryName, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidDataException(L.F("В архиве нет файла {0}", entryName));
                    entry.ExtractToFile(Path.Combine(staging, target), true);
                }
            }

            foreach (var (_, target) in package.Files)
                File.Move(Path.Combine(staging, target), Path.Combine(CoreLocator.DownloadedDir, target), true);
        }
        finally
        {
            TryDeleteFolder(staging);
        }
    }

    private HttpClient CreateClient()
    {
        var handler = new HttpClientHandler();
        if (_proxyPort != null)
        {
            handler.Proxy = new WebProxy($"http://127.0.0.1:{_proxyPort}");
            handler.UseProxy = true;
        }

        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Tunnelka/" + UpdateService.Current);
        return client;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
        }
    }

    private static void TryDeleteFolder(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (Exception)
        {
        }
    }
}
