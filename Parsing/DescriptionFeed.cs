using System.Text.Json;
using Tunnelka.Models;

namespace Tunnelka.Parsing;

public static class DescriptionFeed
{
    public const string UserAgent = "Happ/2.0";

    public static Dictionary<string, string> Read(string json)
    {
        var result = new Dictionary<string, string>();
        var text = json.Trim();
        if (!text.StartsWith('['))
            return result;

        try
        {
            using var document = JsonDocument.Parse(text);
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object
                    || !item.TryGetProperty("remarks", out var remarks) || remarks.ValueKind != JsonValueKind.String
                    || !item.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object
                    || !meta.TryGetProperty("serverDescription", out var description) || description.ValueKind != JsonValueKind.String)
                    continue;

                var name = remarks.GetString()?.Trim() ?? "";
                var note = description.GetString()?.Trim() ?? "";
                if (name.Length > 0 && note.Length > 0)
                    result[name] = note;
            }
        }
        catch (JsonException)
        {
            result.Clear();
        }

        return result;
    }

    public static void Apply(IEnumerable<ProxyServer> servers, string json)
    {
        var notes = Read(json);
        if (notes.Count == 0)
            return;

        foreach (var server in servers)
        {
            if (notes.TryGetValue(server.Name.Trim(), out var note))
                server.Description = note;
        }
    }
}
