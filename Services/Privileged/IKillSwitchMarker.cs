using Microsoft.Win32;

namespace Tunnelka.Services.Privileged;

public interface IKillSwitchMarker
{
    string? Read();

    void Write(string value);

    void Clear();
}

public sealed class RegistryKillSwitchMarker : IKillSwitchMarker
{
    private const string Path = @"Software\Tunnelka";
    private const string Name = "FirewallPolicy";

    private readonly RegistryKey _root;

    public RegistryKillSwitchMarker(RegistryKey root)
    {
        _root = root;
    }

    public static RegistryKillSwitchMarker CurrentUser => new(Registry.CurrentUser);

    public static RegistryKillSwitchMarker LocalMachine => new(Registry.LocalMachine);

    public string? Read()
    {
        try
        {
            using var key = _root.OpenSubKey(Path);
            return key?.GetValue(Name) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Write(string value)
    {
        using var key = _root.CreateSubKey(Path);
        key.SetValue(Name, value);
    }

    public void Clear()
    {
        using var key = _root.OpenSubKey(Path, true);
        key?.DeleteValue(Name, false);
    }
}
