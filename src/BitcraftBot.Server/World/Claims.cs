using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Une colonie (claim) de la région (v0.4.13, préparation de la ville de Julien) : lecture seule des tables publiques
// claim_state (nom, propriétaire, neutre) et claim_local_state (position de la tour, nombre de cases, réserves).
// Position : null si le format n'est pas celui attendu (À vérifier sur le journal : « Exemple brut de claim_local_state »).
public sealed record ClaimInfo(ulong EntityId, string Name, ulong OwnerPlayer, bool Neutral, ServerPosition? Position, int Tiles, int Supplies);

public static class Claims
{
    public static IReadOnlyList<ClaimInfo> Of(TableCache cache)
    {
        var locals = cache.Rows("claim_local_state")
            .Where(r => r.U64("entity_id") is not null)
            .ToDictionary(r => r.U64("entity_id")!.Value, r => r, EqualityComparer<ulong>.Default);
        var result = new List<ClaimInfo>();
        foreach (var row in cache.Rows("claim_state"))
        {
            var id = row.U64("entity_id") ?? 0;
            locals.TryGetValue(id, out var local);
            result.Add(new ClaimInfo(id, row.Get("name") is { ValueKind: JsonValueKind.String } n ? n.GetString() ?? "" : "",
                row.U64("owner_player_entity_id") ?? 0, row.Bool("neutral") ?? false,
                local?.Get("location") is { } loc ? Position(loc) : null,
                local?.I32("num_tiles") ?? 0, local?.I32("supplies") ?? 0));
        }
        return result.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // Position : option SpacetimeDB ({"some": {...}} ou [0, {...}]) contenant {x, z} ou [x, z, dimension].
    // Les coordonnées de claim_local_state sont des petites cases hexagonales : même repère que les ressources (À vérifier).
    public static ServerPosition? Position(JsonElement location)
    {
        var e = location;
        if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty("some", out var some))
        {
            e = some;
        }
        else if (e.ValueKind == JsonValueKind.Array && e.GetArrayLength() == 2 && e[0].ValueKind == JsonValueKind.Number && e[1].ValueKind is JsonValueKind.Object or JsonValueKind.Array)
        {
            e = e[1];
        }
        var x = Number(e, 0, "x");
        var z = Number(e, 1, "z");
        return x is null || z is null ? null : new ServerPosition(x.Value, z.Value);
    }

    private static int? Number(JsonElement e, int index, string name)
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
