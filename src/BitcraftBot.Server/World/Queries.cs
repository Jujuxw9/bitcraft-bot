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
    public static IReadOnlyList<string> FollowPlayer(string hexIdentity, ulong entityId, Area? area, bool withHealth = true, bool withInventory = true, bool withExtras = true, bool withTasks = true, bool withTerrain = false, bool withBags = true, IReadOnlyList<ulong>? terrainExtra = null)
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
            // v0.4.22 : progression des fabrications du personnage (table publique, index sur le propriétaire).
            list.Add($"SELECT * FROM progressive_action_state WHERE owner_entity_id = {e}");
            list.Add($"SELECT * FROM extract_outcome_state WHERE entity_id = {e}");
            // v0.4.24 : cible sélectionnée dans le jeu (table publique), pour la reprise automatique des récoltes.
            list.Add($"SELECT * FROM target_state WHERE entity_id = {e}");
            list.Add($"SELECT * FROM player_state WHERE entity_id = {e}");
            list.Add($"SELECT * FROM teleportation_energy_state WHERE entity_id = {e}");
            if (withTasks)
            {
                // Onglet Voyageurs (v0.4.11) : tâches de voyageur du personnage (table publique, lecture seule).
                list.Add($"SELECT * FROM traveler_task_state WHERE player_entity_id = {e}");
            }
            if (withExtras)
            {
                // Colonies de la région (v0.4.13, préparation de la ville) : tables publiques, lecture seule.
                list.Add("SELECT * FROM claim_state");
                list.Add("SELECT * FROM claim_local_state");
                // Barre d'action et capacités avec leur temps de recharge (v0.4.4 : sprint).
                list.Add($"SELECT * FROM action_bar_state WHERE player_entity_id = {e}");
                list.Add($"SELECT * FROM ability_state WHERE owner_entity_id = {e}");
                // Module « @GPT » (v0.4.5, D-054 ; v0.4.7 : Local, Région, Looking For Group, Trade) : chat récent (table publique, lecture seule).
                list.AddRange(ChatMessages.AnsweredQueriesSince(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300));
            }
        }
        if (area is { } a)
        {
            list.AddRange(Resources(a, withHealth));
            if (withExtras && withBags)
            {
                list.AddRange(GroundBags(a));
            }
            if (withTerrain)
            {
                list.AddRange(Terrain(a, entityId, terrainExtra));
            }
        }
        return list;
    }

    // Rayon couvert par le fond de carte, en cases du jeu (la carte montre 30 cases depuis la v0.4.22, un peu de marge).
    public const double TerrainRadius = 34; // v0.4.21 : zone lue de 25 cases ; v0.4.22 : 30 cases

    // Fond de carte (v0.4.18) : seulement les morceaux de terrain autour de la zone lue (4 à 9 morceaux, jamais toute
    // la région), un par requête sur la clé primaire ; et les zones découvertes du personnage.
    // v0.4.23 : extra = morceaux déjà découverts à garder en mémoire (lus une seule fois, en plus de ceux autour).
    public static IReadOnlyList<string> Terrain(Area a, ulong entityId, IReadOnlyList<ulong>? extra = null)
    {
        var list = TerrainChunk.ChunksAround(a.CenterX / 3.0, a.CenterZ / 3.0, TerrainRadius)
            .Select(c => TerrainChunk.IndexOf(c.X, c.Z))
            .Concat(extra ?? [])
            .Distinct()
            .Select(i => string.Create(CultureInfo.InvariantCulture, $"SELECT * FROM terrain_chunk_state WHERE chunk_index = {i}"))
            .ToList();
        list.Add(string.Create(CultureInfo.InvariantCulture, $"SELECT * FROM exploration_chunks_state_v2 WHERE entity_id = {entityId}"));
        return list;
    }

    // Sacs tombés au sol dans la zone (v0.4.4) : le sac, sa position, son contenu, ses minuteries (propriétaire, disparition).
    public static IReadOnlyList<string> GroundBags(Area a)
    {
        var list = new List<string>();
        foreach (var where in AreaFilters(a))
        {
            list.Add($"SELECT d.* FROM dropped_inventory_state d JOIN location_state l ON d.entity_id = l.entity_id WHERE {where}");
            list.Add($"SELECT l.* FROM location_state l JOIN dropped_inventory_state d ON l.entity_id = d.entity_id WHERE {where}");
            list.Add($"SELECT t.* FROM dropped_inventory_ownership_timer t JOIN location_state l ON t.entity_id = l.entity_id WHERE {where}");
            list.Add($"SELECT t.* FROM dropped_inventory_despawn_timer t JOIN location_state l ON t.entity_id = l.entity_id WHERE {where}");
        }
        list.Add("SELECT i.* FROM inventory_state i JOIN dropped_inventory_state d ON i.owner_entity_id = d.entity_id");
        return list;
    }

    // Ressources (arbres, rochers, buissons…) dans un carré autour du personnage : leur position, leur type
    // et, si demandé, leur santé actuelle (resource_health_state, une ligne par ressource dès son apparition).
    public static IReadOnlyList<string> Resources(Area a, bool withHealth = true)
    {
        var list = new List<string>();
        foreach (var where in AreaFilters(a))
        {
            list.Add($"SELECT l.* FROM location_state l JOIN resource_state r ON l.entity_id = r.entity_id WHERE {where}");
            list.Add($"SELECT r.* FROM resource_state r JOIN location_state l ON r.entity_id = l.entity_id WHERE {where}");
            if (withHealth)
            {
                list.Add($"SELECT h.* FROM resource_health_state h JOIN location_state l ON h.entity_id = l.entity_id WHERE {where}");
            }
        }
        return list;
    }

    // v0.4.19 : depuis le 26/11 au soir, le serveur refuse un carré de coordonnées seul (« Estimated cardinality …
    // exceeds limit »). On ajoute le numéro du morceau de carte (colonne indexée) : une requête par morceau touché
    // par le carré (1 à 4 en général). Mêmes lignes reçues qu'avant, estimation acceptée.
    public static IReadOnlyList<string> AreaFilters(Area a) =>
        a.ChunkIndexes()
            .Select(c => string.Create(CultureInfo.InvariantCulture,
                $"l.chunk_index = {c} AND l.x >= {a.MinX} AND l.x <= {a.MaxX} AND l.z >= {a.MinZ} AND l.z <= {a.MaxZ}"))
            .ToList();
}

// Carré de cases (petites cases hexagonales du serveur), bornes incluses.
public readonly record struct Area(int MinX, int MaxX, int MinZ, int MaxZ)
{
    public static Area Around(double x, double z, int radius) =>
        new((int)Math.Floor(x) - radius, (int)Math.Ceiling(x) + radius, (int)Math.Floor(z) - radius, (int)Math.Ceiling(z) + radius);

    public double CenterX => (MinX + MaxX) / 2.0;

    public double CenterZ => (MinZ + MaxZ) / 2.0;

    // Petites cases par côté d'un morceau de carte (32 grandes cases × 3).
    public const int ChunkSmallCells = TerrainChunk.Size * 3;

    // Numéros des morceaux de carte (chunk_index du serveur) touchés par le carré.
    public IReadOnlyList<ulong> ChunkIndexes()
    {
        var list = new List<ulong>();
        for (var z = FloorDiv(MinZ); z <= FloorDiv(MaxZ); z++)
        {
            for (var x = FloorDiv(MinX); x <= FloorDiv(MaxX); x++)
            {
                list.Add(TerrainChunk.IndexOf(x, z));
            }
        }
        return list;
    }

    private static int FloorDiv(int value) => (int)Math.Floor(value / (double)ChunkSmallCells);
}
