using System.Text.Json.Serialization;

namespace Tunnelka.Models;

public class RoutingRule
{
    public const string Proxy = "proxy";
    public const string Direct = "direct";
    public const string Block = "block";
    public const string ProcessPrefix = "process:";

    [JsonPropertyName("Values")]
    public string Value { get; set; } = "";

    public string Action { get; set; } = Direct;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonIgnore]
    public bool IsProcess => Value.StartsWith(ProcessPrefix, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string Target => IsProcess ? Value.Substring(ProcessPrefix.Length).Trim() : Value;

    [JsonIgnore]
    public string DisplayName => Value.StartsWith("domain:", StringComparison.OrdinalIgnoreCase) ? Value.Substring("domain:".Length) : Target;

    public static RoutingRule ForProcess(string nameOrPath, string action) =>
        new() { Value = ProcessPrefix + ProcessName(nameOrPath), Action = action };

    public static string ProcessName(string nameOrPath)
    {
        var name = nameOrPath.Trim().Trim('"').Split('/', '\\').Last();
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";
    }
}
