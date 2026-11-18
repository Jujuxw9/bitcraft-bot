using System.Globalization;

namespace BitcraftBot.Server.World;

// Requêtes SQL de lecture envoyées au serveur (abonnements). Uniquement des SELECT.
public static class Queries
{
    // Étape 1 : retrouver le personnage du compte (son numéro d'entité) et la région.
    public static IReadOnlyList<string> FindPlayer(string hexIdentity) =>
    [
        $"SELECT * FROM user_state WHERE identity = 0x{hexIdentity}",
        "SELECT * FROM world_region_state",
        "SELECT * FROM world_region_name_state",
    ];

    // Étape 2 : suivre le personnage (position, vie, endurance, faim), et les ressources autour de lui.
    public static IReadOnlyList<string> FollowPlayer(string hexIdentity, ulong entityId, Area? area)
    {
        var e = entityId.ToString(CultureInfo.InvariantCulture);
        var list = new List<string>(FindPlayer(hexIdentity))
        {
            $"SELECT * FROM mobile_entity_state WHERE entity_id = {e}",
            $"SELECT * FROM health_state WHERE entity_id = {e}",
            $"SELECT * FROM stamina_state WHERE entity_id = {e}",
            $"SELECT * FROM satiation_state WHERE entity_id = {e}",
            $"SELECT * FROM signed_in_player_state WHERE entity_id = {e}",
        };
        if (area is { } a)
        {
            list.AddRange(Resources(a));
        }
        return list;
    }

    // Ressources (arbres, rochers, buissons…) dans un carré autour du personnage : leur position et leur type.
    public static IReadOnlyList<string> Resources(Area a)
    {
        var where = string.Create(CultureInfo.InvariantCulture,
            $"l.x >= {a.MinX} AND l.x <= {a.MaxX} AND l.z >= {a.MinZ} AND l.z <= {a.MaxZ}");
        return
        [
            $"SELECT l.* FROM location_state l JOIN resource_state r ON l.entity_id = r.entity_id WHERE {where}",
            $"SELECT r.* FROM resource_state r JOIN location_state l ON r.entity_id = l.entity_id WHERE {where}",
        ];
    }
}

// Carré de cases (petites cases hexagonales du serveur), bornes incluses.
public readonly record struct Area(int MinX, int MaxX, int MinZ, int MaxZ)
{
    public static Area Around(double x, double z, int radius) =>
        new((int)Math.Floor(x) - radius, (int)Math.Ceiling(x) + radius, (int)Math.Floor(z) - radius, (int)Math.Ceiling(z) + radius);

    public double CenterX => (MinX + MaxX) / 2.0;

    public double CenterZ => (MinZ + MaxZ) / 2.0;
}
