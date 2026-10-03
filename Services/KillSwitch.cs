using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Tunnelka.Services;

public sealed class KillSwitch
{
    private const string RuleName = "Tunnelka kill switch";
    private const string MarkerPath = @"Software\Tunnelka";
    private const string MarkerName = "FirewallPolicy";

    private static readonly string[] Profiles = { "domain", "private", "public" };
    private static readonly Regex PolicyPattern = new(@"(\w*Inbound\w*),(\w*Outbound)", RegexOptions.Compiled);

    private readonly AppLog _log;

    public KillSwitch(AppLog log)
    {
        _log = log;
    }

    public bool IsEngaged { get; private set; }

    public bool Engage()
    {
        if (IsEngaged)
            return true;

        try
        {
            var inbound = ReadInbound();
            if (inbound == null)
            {
                _log.Write(L.T("Kill switch: не удалось прочитать настройки брандмауэра Windows"));
                return false;
            }

            using (var marker = Registry.CurrentUser.CreateSubKey(MarkerPath))
                marker.SetValue(MarkerName, string.Join(";", inbound));

            DeleteRules();
            foreach (var program in new[] { XrayRunner.XrayPath, XrayRunner.SingBoxPath, Environment.ProcessPath ?? "" })
            {
                if (program.Length > 0)
                    AllowRule($"program=\"{program}\"");
            }
            AllowRule($"localip={TunConfigBuilder.Address}");
            AllowRule("remoteip=LocalSubnet,127.0.0.0/8,10.0.0.0/8,172.16.0.0/12,192.168.0.0/16");

            for (var i = 0; i < Profiles.Length; i++)
                Netsh($"advfirewall set {Profiles[i]}profile firewallpolicy {inbound[i]},blockoutbound");

            IsEngaged = true;
            _log.Write(L.T("Kill switch включён: без VPN интернет заблокирован"));
            return true;
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Kill switch не включился: {0}", ex.Message));
            Release();
            return false;
        }
    }

    public void Release()
    {
        try
        {
            string? saved;
            using (var marker = Registry.CurrentUser.OpenSubKey(MarkerPath))
                saved = marker?.GetValue(MarkerName) as string;

            if (saved != null)
            {
                var inbound = saved.Split(';');
                for (var i = 0; i < Profiles.Length && i < inbound.Length; i++)
                    Netsh($"advfirewall set {Profiles[i]}profile firewallpolicy {inbound[i]},allowoutbound");

                using var marker = Registry.CurrentUser.OpenSubKey(MarkerPath, true);
                marker?.DeleteValue(MarkerName, false);
            }

            DeleteRules();
            if (IsEngaged || saved != null)
                _log.Write(L.T("Kill switch выключен: интернет снова работает без VPN"));
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Не удалось снять kill switch: {0}", ex.Message));
        }

        IsEngaged = false;
    }

    public static bool WasLeftOn()
    {
        try
        {
            using var marker = Registry.CurrentUser.OpenSubKey(MarkerPath);
            return marker?.GetValue(MarkerName) != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string[]? ReadInbound()
    {
        var output = Netsh("advfirewall show allprofiles firewallpolicy");
        var matches = PolicyPattern.Matches(output);
        return matches.Count >= Profiles.Length
            ? matches.Cast<Match>().Take(Profiles.Length).Select(m => m.Groups[1].Value.ToLowerInvariant()).ToArray()
            : null;
    }

    private static void DeleteRules()
    {
        try
        {
            Netsh($"advfirewall firewall delete rule name=\"{RuleName}\"");
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void AllowRule(string condition) =>
        Netsh($"advfirewall firewall add rule name=\"{RuleName}\" dir=out action=allow enable=yes {condition}");

    private static string Netsh(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("netsh", arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("netsh");

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(10000);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(output.Trim());
        return output;
    }
}
