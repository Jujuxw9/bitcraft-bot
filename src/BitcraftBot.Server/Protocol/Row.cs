using System.Text.Json;

namespace BitcraftBot.Server.Protocol;

// Une ligne d'une table, valeurs nommées grâce au schéma.
// En JSON, SpacetimeDB envoie chaque ligne comme un tableau de valeurs, dans l'ordre des colonnes.
public sealed class Row
{
    private readonly Dictionary<string, JsonElement> _values;

    private Row(string key, Dictionary<string, JsonElement> values)
    {
        Key = key;
        _values = values;
    }

    // Clé de la ligne (valeur de la clé primaire, ou la ligne entière s'il n'y en a pas).
    public string Key { get; }

    public IEnumerable<string> Columns => _values.Keys;

    public static Row Parse(string rowJson, TableSchema? table)
    {
        using var doc = JsonDocument.Parse(rowJson);
        var root = doc.RootElement;
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (root.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var value in root.EnumerateArray())
            {
                var name = table is not null && i < table.Columns.Count ? table.Columns[i] : $"col{i}";
                values[name] = value.Clone();
                i++;
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                values[property.Name] = property.Value.Clone();
            }
        }
        var key = rowJson;
        if (table is { PrimaryKey.Count: > 0 })
        {
            var parts = table.PrimaryKey
                .Select(i => i < table.Columns.Count && values.TryGetValue(table.Columns[i], out var v) ? v.GetRawText() : "?");
            key = string.Join("|", parts);
        }
        return new Row(key, values);
    }

    public bool Has(string column) => _values.ContainsKey(column);

    public JsonElement? Get(string column) => _values.TryGetValue(column, out var v) ? v : null;

    public ulong? U64(string column) =>
        Get(column) is { ValueKind: JsonValueKind.Number } v && v.TryGetUInt64(out var n) ? n : null;

    public long? I64(string column) =>
        Get(column) is { ValueKind: JsonValueKind.Number } v && v.TryGetInt64(out var n) ? n : null;

    public int? I32(string column) =>
        Get(column) is { ValueKind: JsonValueKind.Number } v && v.TryGetInt32(out var n) ? n : null;

    public double? F64(string column) =>
        Get(column) is { ValueKind: JsonValueKind.Number } v && v.TryGetDouble(out var n) ? n : null;

    public bool? Bool(string column) => Get(column) switch
    {
        { ValueKind: JsonValueKind.True } => true,
        { ValueKind: JsonValueKind.False } => false,
        _ => null,
    };

    // Une identité peut arriver sous plusieurs formes : {"__identity__":"0x…"}, "0x…" ou ["0x…"].
    public string? Identity(string column) => Get(column) is { } v ? ReadIdentity(v) : null;

    public static string? ReadIdentity(JsonElement v)
    {
        switch (v.ValueKind)
        {
            case JsonValueKind.String:
                return Normalize(v.GetString());
            case JsonValueKind.Object when v.TryGetProperty("__identity__", out var inner):
                return ReadIdentity(inner);
            case JsonValueKind.Array when v.GetArrayLength() == 1:
                return ReadIdentity(v[0]);
            default:
                return null;
        }
    }

    // Identités comparées en minuscules, sans « 0x ».
    public static string? Normalize(string? hex) =>
        hex is null ? null : (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex).ToLowerInvariant();

    public override string ToString() =>
        "{" + string.Join(", ", _values.Select(kv => $"{kv.Key}: {kv.Value.GetRawText()}")) + "}";
}
