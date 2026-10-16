using System.Net;
using System.Security.Cryptography;

namespace Tunnelka.Services;

public static class LocalAccess
{
    public static string User { get; private set; } = "";
    public static string Password { get; private set; } = "";
    public static bool HttpProtected { get; private set; }

    public static bool Enabled => User.Length > 0 && Password.Length > 0;

    public static void Renew(bool protectHttp)
    {
        User = Token(8);
        Password = Token(24);
        HttpProtected = protectHttp;
    }

    public static WebProxy HttpProxy(int port)
    {
        var proxy = new WebProxy($"http://127.0.0.1:{port}");
        if (HttpProtected && Enabled && port == XrayConfigBuilder.HttpPort)
            proxy.Credentials = new NetworkCredential(User, Password);
        return proxy;
    }

    public static string CacheKey(int port) => HttpProtected ? $"{port}:{User}" : port.ToString();

    private static string Token(int bytes) => Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();
}
