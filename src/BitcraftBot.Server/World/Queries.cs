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
    public static IReadOnlyList<string> FollowPlayer(string hexIdentity, ulong entityId, Area? area, bool withHealth = true, bool withInventory = true, bool withExtras = true)
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
        if (withInventory)
        {
            // Sac, ceinture et coffres de banque du personnage (inventory_state est publique), et les banques de la région.
            list.Add($"SELECT * FROM inventory_state WHERE owner_entity_id = {e}");
            list.Add($"SELECT * FROM inventory_state WHERE player_owner_entity_id = {e}");
            list.Add("SELECT * FROM bank_state");
            // Repas automatique (v0.3.15) : bonus actifs (bonus de nourriture) et faim maximale.
            list.Add($"SELECT * FROM active_buff_state WHERE entity_id = {e}");
            list.Add($"SELECT * FROM character_stats_state WHERE entity_id = {e}");
            // Tableau de bord (v0.3.17) : expérience des métiers, action en cours, dernier coup de récolte, temps de jeu, énergie de téléportation.
            list.Add($"SELECT * FROM experience_state WHERE entity_id = {e}");
            list.Add($"SELECT * FROM player_action_state WHERE entity_id = {e}");
            list.Add($"SELECT * FROM extract_outcome_state WHERE entity_id = {e}");
            list.Add($"SELECT * FROM player_state WHERE entity_id = {e}");
            list.Add($"SELECT * FROM teleportation_energy_state WHERE entity_id = {e}");
            if (withExtras)
            {
                // Barre d'action et capacités avec leur temps de recharge (v0.4.4 : sprint).
                list.Add($"SELECT * FROM action_bar_state WHERE player_entity_id = {e}");
                list.Add($"SELECT * FROM ability_state WHERE owner_entity_id = {e}");
            }
        }
        if (area is { } a)
        {
            list.AddRange(Resources(a, withHealth));
            if (withExtras)
            {
                list.AddRange(GroundBags(a));
            }
        }
        return list;
    }

    // Sacs tombés au sol dans la zone (v0.4.4) : le sac, sa position, son contenu, ses minuteries (propriétaire, disparition).
    public static IReadOnlyList<string> GroundBags(Area a)
    {
        var where = string.Create(CultureInfo.InvariantCulture,
            $"l.x >= {a.MinX} AND l.x <= {a.MaxX} AND l.z >= {a.MinZ} AND l.z <= {a.MaxZ}");
        return
        [
            $"SELECT d.* FROM dropped_inventory_state d JOIN location_state l ON d.entity_id = l.entity_id WHERE {where}",
            $"SELECT l.* FROM location_state l JOIN dropped_inventory_state d ON l.entity_id = d.entity_id WHERE {where}",
            $"SELECT i.* FROM inventory_state i JOIN dropped_inventory_state d ON i.owner_entity_id = d.entity_id",
            $"SELECT t.* FROM dropped_inventory_ownership_timer t JOIN location_state l ON t.entity_id = l.entity_id WHERE {where}",
            $"SELECT t.* FROM dropped_inventory_despawn_timer t JOIN location_state l ON t.entity_id = l.entity_id WHERE {where}",
        ];
    }

    // Ressources (arbres, rochers, buissons…) dans un carré autour du personnage : leur position, leur type
    // et, si demandé, leur santé actuelle (resource_health_state, une ligne par ressource dès son apparition).
    public static IReadOnlyList<string> Resources(Area a, bool withHealth = true)
    {
        var where = string.Create(CultureInfo.InvariantCulture,
            $"l.x >= {a.MinX} AND l.x <= {a.MaxX} AND l.z >= {a.MinZ} AND l.z <= {a.MaxZ}");
        var list = new List<string>
        {
            $"SELECT l.* FROM location_state l JOIN resource_state r ON l.entity_id = r.entity_id WHERE {where}",
            $"SELECT r.* FROM resource_state r JOIN location_state l ON r.entity_id = l.entity_id WHERE {where}",
        };
        if (withHealth)
        {
            list.Add($"SELECT h.* FROM resource_health_state h JOIN location_state l ON h.entity_id = l.entity_id WHERE {where}");
        }
        return list;
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
