using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

public enum BagOwner
{
    // Le sac est à Julien : lui seul peut le ramasser jusqu'à PublicAt.
    AMoi,
    // Le sac est à un autre joueur : verrouillé jusqu'à PublicAt.
    AutreJoueur,
    // Le sac est à tout le monde (propriétaire 0) : il disparaîtra à DespawnAt.
    Libre,
}

// Un sac d'objets tombé au sol (table dropped_inventory_state, publique, lecture seule) : objets qui ne rentraient pas
// dans le sac (butin d'un animal, tronc coupé avec le sac plein…), ou affaires d'un joueur mort.
// Code serveur (dropped_inventory_state.rs) : un sac a un propriétaire pendant un temps (minuterie « ownership »),
// puis devient à tout le monde (propriétaire 0) avec une minuterie de disparition (« despawn »).
public sealed record GroundBag(ulong EntityId, ServerPosition Position, double ScreenDistance, string Direction, ulong OwnerEntityId,
    BagOwner Owner, DateTimeOffset? PublicAt, DateTimeOffset? DespawnAt, IReadOnlyList<ItemStackInfo> Stacks)
{
    // Ramassable par Julien maintenant ?
    public bool Mine => Owner is BagOwner.AMoi or BagOwner.Libre;
}

public static class GroundBags
{
    public static IReadOnlyList<GroundBag> Near(TableCache cache, ulong player, ServerPosition from)
    {
        var locations = new Dictionary<ulong, ServerPosition>();
        foreach (var row in cache.Rows("location_state"))
        {
            if (row.U64("entity_id") is { } e && row.I32("x") is { } x && row.I32("z") is { } z)
            {
                locations[e] = new ServerPosition(x, z);
            }
        }
        // Contenu : inventaire dont le propriétaire est le sac (code serveur, DroppedInventoryState::new_inventory).
        var inventories = cache.Rows("inventory_state").Where(r => r.U64("owner_entity_id") is not null)
            .GroupBy(r => r.U64("owner_entity_id")!.Value).ToDictionary(g => g.Key, g => g.First());
        var ownership = Timers(cache, "dropped_inventory_ownership_timer");
        var despawn = Timers(cache, "dropped_inventory_despawn_timer");
        var result = new List<GroundBag>();
        foreach (var row in cache.Rows("dropped_inventory_state"))
        {
            if (row.U64("entity_id") is not { } id || !locations.TryGetValue(id, out var at))
            {
                continue;
            }
            var owner = row.U64("owner_entity_id") ?? 0;
            var stacks = inventories.TryGetValue(id, out var inv) && inv.Get("pockets") is { } p ? Inventories.ParsePockets(p) : [];
            result.Add(new GroundBag(id, at, at.ScreenDistanceTo(from),
                ResourceNames.Direction(at.ScreenEast - from.ScreenEast, at.ScreenNorth - from.ScreenNorth), owner,
                owner == 0 ? BagOwner.Libre : owner == player ? BagOwner.AMoi : BagOwner.AutreJoueur,
                ownership.GetValueOrDefault(id), despawn.GetValueOrDefault(id), stacks));
        }
        return result.OrderBy(b => b.ScreenDistance).ToList();
    }

    // Minuteries : entity_id → heure prévue (scheduled_at, ScheduleAt::Time).
    private static Dictionary<ulong, DateTimeOffset> Timers(TableCache cache, string table)
    {
        var result = new Dictionary<ulong, DateTimeOffset>();
        foreach (var row in cache.Rows(table))
        {
            if (row.U64("entity_id") is { } e && row.Get("scheduled_at") is { } s && Micros(s) is { } us)
            {
                result[e] = DateTimeOffset.FromUnixTimeMilliseconds(us / 1000);
            }
        }
        return result;
    }

    // Heure SpacetimeDB en microsecondes, où qu'elle soit dans le JSON : {"__timestamp_micros_since_unix_epoch__": n},
    // [1, {...}] (variante « Time » de ScheduleAt), ou un grand nombre seul.
    public static long? Micros(JsonElement e)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Number when e.TryGetInt64(out var n) && n is > 100_000_000_000_000 and < 100_000_000_000_000_000:
                return n;
            case JsonValueKind.Object:
                foreach (var p in e.EnumerateObject())
                {
                    if (Micros(p.Value) is { } v)
                    {
                        return v;
                    }
                }
                return null;
            case JsonValueKind.Array:
                foreach (var item in e.EnumerateArray())
                {
                    if (Micros(item) is { } v)
                    {
                        return v;
                    }
                }
                return null;
            default:
                return null;
        }
    }
}
