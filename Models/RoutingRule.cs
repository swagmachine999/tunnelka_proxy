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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string IconPath { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Action { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonIgnore]
    public bool IsProcess => Value.StartsWith(ProcessPrefix, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string Target => IsProcess ? Value.Substring(ProcessPrefix.Length).Trim() : Value;

    [JsonIgnore]
    public string DisplayName => Value.StartsWith("domain:", StringComparison.OrdinalIgnoreCase) ? Value.Substring("domain:".Length) : Target;

    public static RoutingRule ForProcess(string nameOrPath)
    {
        var path = nameOrPath.Trim().Trim('"');
        return new RoutingRule
        {
            Value = ProcessPrefix + ProcessName(path),
            IconPath = path.Contains('\\') || path.Contains('/') ? path : ""
        };
    }

    public static string ProcessName(string nameOrPath)
    {
        var name = nameOrPath.Trim().Trim('"').Split('/', '\\').Last();
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";
    }
}
