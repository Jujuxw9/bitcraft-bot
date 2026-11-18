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
    // Coordonnées affichées par le jeu (HUD) : E = arrondi(x ÷ 3), N = arrondi(z ÷ 3).
    // Vérifié en réel le 2026-11-18 (essais v0.3.7 à v0.3.9, écart de 0,3 à 0,4 à l'arrêt).
    public double ScreenEast => X / 3.0;

    public double ScreenNorth => Z / 3.0;

    public int RoundedEast => (int)Math.Round(ScreenEast, MidpointRounding.AwayFromZero);

    public int RoundedNorth => (int)Math.Round(ScreenNorth, MidpointRounding.AwayFromZero);

    // Distance dans les unités de l'écran (celles des coordonnées N et E du HUD).
    public double ScreenDistanceTo(ServerPosition other)
    {
        var de = ScreenEast - other.ScreenEast;
        var dn = ScreenNorth - other.ScreenNorth;
        return Math.Sqrt(de * de + dn * dn);
    }

    public string ScreenText => $"N {RoundedNorth}, E {RoundedEast}";

    // Distance approchée en cases : les rangées d'hexagones sont espacées de √3/2.
    public double DistanceTo(ServerPosition other)
    {
        var dx = X - other.X;
        var dz = (Z - other.Z) * 0.8660254;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    public override string ToString() => $"x {X:0.0}, z {Z:0.0}";
}

// Distance : en petites cases du serveur. ScreenDistance : en unités de l'écran (N, E).
// Health : santé actuelle lue sur le serveur (table resource_health_state) ; MaxHealth : données du jeu.
public sealed record NearbyResource(ulong EntityId, int ResourceId, string Name, int? Tier, string? Tag, ServerPosition Position, double Distance)
{
    public int? Health { get; init; }

    public int? MaxHealth { get; init; }

    public double ScreenDistance { get; init; }

    // Direction vue du personnage (N, NE, E, SE, S, SO, O, NO).
    public string Direction { get; init; } = "";

    public bool IsTree => string.Equals(Tag, "Tree", StringComparison.OrdinalIgnoreCase);

    public string Age => ResourceNames.Age(Name);

    public string Family => ResourceFamilies.Of(Tag);
}

// Description d'une ressource d'après les données du jeu (BitCraft_GameData).
public sealed record ResourceLabel(string Name, int? Tier, string? Tag, int? MaxHealth = null);

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
        var health = new Dictionary<ulong, int>();
        foreach (var row in cache.Rows("resource_health_state"))
        {
            if (row.U64("entity_id") is { } e && row.I32("health") is { } h)
            {
                health[e] = h;
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
            result.Add(new NearbyResource(e, rid, label?.Name ?? $"ressource n° {rid}", label?.Tier, label?.Tag, at, at.DistanceTo(from))
            {
                Health = health.TryGetValue(e, out var h) ? h : null,
                MaxHealth = label?.MaxHealth,
                ScreenDistance = at.ScreenDistanceTo(from),
                Direction = ResourceNames.Direction(at.ScreenEast - from.ScreenEast, at.ScreenNorth - from.ScreenNorth),
            });
        }
        result.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return result;
    }

    private static ServerPosition? FromMilli(long? x, long? z) =>
        x is null || z is null ? null : new ServerPosition(x.Value / 1000.0, z.Value / 1000.0);
}
