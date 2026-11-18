using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Ce que le bot sait du personnage à un instant donné, d'après le serveur.
public sealed record PlayerSnapshot
{
    public ulong? EntityId { get; init; }
    public bool? SignedIn { get; init; }

    // Position en petites cases hexagonales du serveur (valeur brute / 1000).
    public ServerPosition? Position { get; init; }
    public ServerPosition? Destination { get; init; }
    public bool? Walking { get; init; }

    public double? Health { get; init; }
    public double? Stamina { get; init; }
    public double? Satiation { get; init; }

    public string? RegionName { get; init; }
    public int? RegionIndex { get; init; }

    public IReadOnlyList<NearbyResource> Resources { get; init; } = [];
    public IReadOnlyDictionary<string, int> TableCounts { get; init; } = new Dictionary<string, int>();
}

public readonly record struct ServerPosition(double X, double Z)
{
    // Distance approchée en cases : les rangées d'hexagones sont espacées de √3/2.
    public double DistanceTo(ServerPosition other)
    {
        var dx = X - other.X;
        var dz = (Z - other.Z) * 0.8660254;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    public override string ToString() => $"x {X:0.0}, z {Z:0.0}";
}

public sealed record NearbyResource(ulong EntityId, int ResourceId, string Name, int? Tier, string? Tag, ServerPosition Position, double Distance);

// Description d'une ressource d'après les données du jeu (BitCraft_GameData).
public sealed record ResourceLabel(string Name, int? Tier, string? Tag);

public static class SnapshotBuilder
{
    public static PlayerSnapshot Build(TableCache cache, string? hexIdentity, Func<int, ResourceLabel?> describe)
    {
        var id = Row.Normalize(hexIdentity);
        var user = cache.Rows("user_state").FirstOrDefault(r => id is not null && Row.Normalize(r.Identity("identity")) == id);
        var entity = user?.U64("entity_id");
        if (entity is null)
        {
            return new PlayerSnapshot { TableCounts = cache.Counts() };
        }

        Row? Of(string table) => cache.Rows(table).FirstOrDefault(r => r.U64("entity_id") == entity);
        var mobile = Of("mobile_entity_state");
        ServerPosition? position = mobile is null ? null : FromMilli(mobile.I64("location_x"), mobile.I64("location_z"));
        ServerPosition? destination = mobile is null ? null : FromMilli(mobile.I64("destination_x"), mobile.I64("destination_z"));

        var region = cache.Rows("world_region_state").FirstOrDefault();
        var regionIndex = region?.I32("region_index");
        var regionName = cache.Rows("world_region_name_state").FirstOrDefault(r => regionIndex is not null && r.I32("id") == regionIndex)
                         ?? cache.Rows("world_region_name_state").FirstOrDefault();

        return new PlayerSnapshot
        {
            EntityId = entity,
            SignedIn = cache.Rows("signed_in_player_state").Any(r => r.U64("entity_id") == entity),
            Position = position,
            Destination = destination,
            Walking = mobile?.Bool("is_walking"),
            Health = Of("health_state")?.F64("health"),
            Stamina = Of("stamina_state")?.F64("stamina"),
            Satiation = Of("satiation_state")?.F64("satiation"),
            RegionIndex = regionIndex,
            RegionName = regionName?.Get("player_facing_name")?.ToString(),
            Resources = position is { } p ? NearbyResources(cache, p, describe) : [],
            TableCounts = cache.Counts(),
        };
    }

    // Ressources connues autour du personnage, de la plus proche à la plus lointaine.
    public static IReadOnlyList<NearbyResource> NearbyResources(TableCache cache, ServerPosition from, Func<int, ResourceLabel?> describe)
    {
        var locations = new Dictionary<ulong, ServerPosition>();
        foreach (var row in cache.Rows("location_state"))
        {
            if (row.U64("entity_id") is { } e && row.I32("x") is { } x && row.I32("z") is { } z)
            {
                locations[e] = new ServerPosition(x, z);
            }
        }
        var result = new List<NearbyResource>();
        foreach (var row in cache.Rows("resource_state"))
        {
            if (row.U64("entity_id") is not { } e || row.I32("resource_id") is not { } rid || !locations.TryGetValue(e, out var at))
            {
                continue;
            }
            var label = describe(rid);
            result.Add(new NearbyResource(e, rid, label?.Name ?? $"ressource n° {rid}", label?.Tier, label?.Tag, at, at.DistanceTo(from)));
        }
        result.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return result;
    }

    private static ServerPosition? FromMilli(long? x, long? z) =>
        x is null || z is null ? null : new ServerPosition(x.Value / 1000.0, z.Value / 1000.0);
}
