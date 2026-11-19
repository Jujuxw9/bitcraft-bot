using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Une pile d'objets dans une poche d'inventaire. IsCargo : cargaison (cargo_desc) au lieu d'objet (item_desc).
public sealed record ItemStackInfo(int ItemId, int Quantity, bool IsCargo);

// Un inventaire lu sur le serveur (table inventory_state, publique, lecture seule).
// Owner : propriétaire (le personnage pour son sac, le bâtiment pour une banque, 0 pour une banque régionale).
// PlayerOwner : le joueur à qui l'inventaire appartient (banques : un inventaire par joueur).
public sealed record InventoryInfo(ulong EntityId, ulong Owner, ulong PlayerOwner, int Index, int CargoIndex, IReadOnlyList<ItemStackInfo> Stacks)
{
    // Banque régionale : pas de bâtiment propriétaire (code serveur, InventoryState::is_regional_bank).
    public bool IsRegionalBank => Owner == 0 && PlayerOwner != 0;
}

public static class Inventories
{
    // Inventaires du personnage : son sac et sa ceinture (owner = lui), ses coffres de banque (player_owner = lui).
    public static IReadOnlyList<InventoryInfo> Of(TableCache cache, ulong player)
    {
        var result = new List<InventoryInfo>();
        foreach (var row in cache.Rows("inventory_state"))
        {
            var owner = row.U64("owner_entity_id") ?? 0;
            var playerOwner = row.U64("player_owner_entity_id") ?? 0;
            if (owner != player && playerOwner != player)
            {
                continue;
            }
            result.Add(new InventoryInfo(row.U64("entity_id") ?? 0, owner, playerOwner,
                row.I32("inventory_index") ?? 0, row.I32("cargo_index") ?? 0,
                row.Get("pockets") is { } pockets ? ParsePockets(pockets) : []));
        }
        return result.OrderBy(i => i.Owner == player ? 0 : 1).ThenBy(i => i.Owner).ThenBy(i => i.Index).ToList();
    }

    // Les poches arrivent en JSON, sous forme de tableaux ([volume, contenu, verrou]) ou d'objets ({"volume":…}).
    // Contenu : option SpacetimeDB ({"some": pile} / {"none": []}) ou null. Pile : [item_id, quantité, type, durabilité].
    public static IReadOnlyList<ItemStackInfo> ParsePockets(JsonElement pockets)
    {
        var stacks = new List<ItemStackInfo>();
        if (pockets.ValueKind != JsonValueKind.Array)
        {
            return stacks;
        }
        foreach (var pocket in pockets.EnumerateArray())
        {
            var contents = Field(pocket, 1, "contents");
            if (contents is { } c && Unwrap(c) is { } stack && ParseStack(stack) is { } s && s.Quantity > 0)
            {
                stacks.Add(s);
            }
        }
        return stacks;
    }

    private static ItemStackInfo? ParseStack(JsonElement stack)
    {
        var id = Number(Field(stack, 0, "item_id"));
        var quantity = Number(Field(stack, 1, "quantity"));
        if (id is null || quantity is null)
        {
            return null;
        }
        return new ItemStackInfo(id.Value, quantity.Value, IsCargo(Field(stack, 2, "item_type")));
    }

    // Type d'objet : {"Cargo": []}, "Cargo", 1, ou {"1": []} selon le format.
    private static bool IsCargo(JsonElement? type) => type switch
    {
        { ValueKind: JsonValueKind.Object } o => o.EnumerateObject().Any(p => p.Name is "Cargo" or "1"),
        { ValueKind: JsonValueKind.String } s => s.GetString() == "Cargo",
        { ValueKind: JsonValueKind.Number } n => n.TryGetInt32(out var v) && v == 1,
        { ValueKind: JsonValueKind.Array } a => a.GetArrayLength() > 0 && (a[0].ValueKind == JsonValueKind.Number ? a[0].GetInt32() == 1 : a[0].GetString() == "Cargo"),
        _ => false,
    };

    // Option SpacetimeDB : {"some": x} → x ; {"none": …} ou null → rien ; [0, x] (somme en tableau) → x.
    private static JsonElement? Unwrap(JsonElement option)
    {
        switch (option.ValueKind)
        {
            case JsonValueKind.Null or JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Object:
                foreach (var p in option.EnumerateObject())
                {
                    if (p.Name is "some" or "0") return p.Value;
                    if (p.Name is "none" or "1") return null;
                }
                // Pas une option : c'est déjà la pile (forme objet).
                return option.TryGetProperty("item_id", out _) ? option : null;
            case JsonValueKind.Array when option.GetArrayLength() == 2 && option[0].ValueKind == JsonValueKind.Number && option[1].ValueKind != JsonValueKind.Number:
                return option[0].GetInt32() == 0 ? option[1] : null;
            case JsonValueKind.Array:
                return option; // la pile elle-même, en tableau
            default:
                return null;
        }
    }

    private static JsonElement? Field(JsonElement e, int index, string name) => e.ValueKind switch
    {
        JsonValueKind.Array when e.GetArrayLength() > index => e[index],
        JsonValueKind.Object when e.TryGetProperty(name, out var v) => v,
        _ => null,
    };

    private static int? Number(JsonElement? e) =>
        e is { ValueKind: JsonValueKind.Number } n && n.TryGetInt64(out var v) ? (int)Math.Clamp(v, int.MinValue, int.MaxValue) : null;
}

public static class Banks
{
    // Position des banques de la région (bank_state.coordinates : case hexagonale « axiale » x, z).
    // Conversion en position du serveur (petites cases « décalées ») : x + z / 2 (code serveur, OffsetCoordinates).
    public static IReadOnlyDictionary<ulong, ServerPosition> Positions(TableCache cache)
    {
        var result = new Dictionary<ulong, ServerPosition>();
        foreach (var row in cache.Rows("bank_state"))
        {
            if (row.U64("building_entity_id") is not { } id || row.Get("coordinates") is not { } c)
            {
                continue;
            }
            var x = Coordinate(c, 0, "x");
            var z = Coordinate(c, 1, "z");
            if (x is not null && z is not null)
            {
                result[id] = new ServerPosition(x.Value + z.Value / 2, z.Value);
            }
        }
        return result;
    }

    private static int? Coordinate(JsonElement e, int index, string name)
    {
        var v = e.ValueKind switch
        {
            JsonValueKind.Array when e.GetArrayLength() > index => e[index],
            JsonValueKind.Object when e.TryGetProperty(name, out var p) => p,
            _ => default,
        };
        return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : null;
    }
}
