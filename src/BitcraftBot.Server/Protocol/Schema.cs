using System.Text.Json;

namespace BitcraftBot.Server.Protocol;

// Schéma public d'un module (liste des tables et de leurs colonnes), lu sur
// https://{hôte}/v1/database/{module}/schema?version=9. Il sert à donner un nom à chaque valeur d'une ligne.
public sealed class Schema
{
    private readonly Dictionary<string, TableSchema> _tables;

    private Schema(Dictionary<string, TableSchema> tables) => _tables = tables;

    public int TableCount => _tables.Count;

    public TableSchema? Find(string table) => _tables.GetValueOrDefault(table);

    public static Schema Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var types = root.GetProperty("typespace").GetProperty("types");
        var tables = new Dictionary<string, TableSchema>(StringComparer.Ordinal);
        foreach (var table in root.GetProperty("tables").EnumerateArray())
        {
            var name = table.GetProperty("name").GetString()!;
            var typeRef = table.GetProperty("product_type_ref").GetInt32();
            var columns = new List<string>();
            if (typeRef < types.GetArrayLength()
                && types[typeRef].TryGetProperty("Product", out var product))
            {
                foreach (var element in product.GetProperty("elements").EnumerateArray())
                {
                    columns.Add(element.GetProperty("name").TryGetProperty("some", out var n) ? n.GetString() ?? "?" : "?");
                }
            }
            var primaryKey = new List<int>();
            if (table.TryGetProperty("primary_key", out var pk) && pk.ValueKind == JsonValueKind.Array)
            {
                primaryKey.AddRange(pk.EnumerateArray().Select(e => e.GetInt32()));
            }
            var isPublic = table.TryGetProperty("table_access", out var access) && access.TryGetProperty("Public", out _);
            tables[name] = new TableSchema(name, columns, primaryKey, isPublic);
        }
        return new Schema(tables);
    }
}

public sealed record TableSchema(string Name, IReadOnlyList<string> Columns, IReadOnlyList<int> PrimaryKey, bool IsPublic);
