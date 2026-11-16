using System.Text.Json;

namespace BitcraftBot.GameData;

// Une ressource du monde (arbre, roche, plante…), telle que décrite dans resource_desc.json.
public sealed record ResourceInfo(int Id, string Name, int Tier, string Tag, int MaxHealth);

// Ce qu'il faut pour récolter une ressource (extraction_recipe_desc.json).
// ToolType : type d'outil (1 = hache, voir tool_type_desc.json). SkillId : métier (2 = Forestry).
public sealed record ExtractionInfo(int ResourceId, double SecondsPerAction, double StaminaPerAction,
    int ToolType, int ToolLevel, int SkillId, int SkillLevel);

// Version des données téléchargées : commit du dépôt BitCraft_GameData et date de mise à jour.
public sealed record GameDataVersion(string Commit, DateTimeOffset UpdatedAt, DateTimeOffset DownloadedAt);

// Données du jeu lues depuis les fichiers JSON de BitCraft_GameData (D-033).
// Lecture seule de données publiques, sans connexion au serveur du jeu.
public sealed class GameDataStore
{
    // Tables utilisées par le bot (dossier static/ du dépôt).
    public static IReadOnlyList<string> Tables { get; } =
    [
        "resource_desc",
        "extraction_recipe_desc",
        "biome_desc",
        "item_desc",
        "cargo_desc",
        "food_desc",
        "tool_type_desc",
        "skill_desc",
    ];

    public required IReadOnlyList<ResourceInfo> Resources { get; init; }
    public required IReadOnlyDictionary<int, ExtractionInfo> Extractions { get; init; }
    public required IReadOnlyList<string> Biomes { get; init; }
    public required IReadOnlyDictionary<int, string> Items { get; init; }
    public required IReadOnlyDictionary<int, string> Cargos { get; init; }
    public required IReadOnlySet<int> FoodItemIds { get; init; }
    public GameDataVersion? Version { get; init; }

    private Dictionary<string, ResourceInfo>? _byName;

    public ResourceInfo? FindResource(string name)
    {
        _byName ??= Resources.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        return _byName.GetValueOrDefault(name);
    }

    // Noms à reconnaître sur l'écran : ressources récoltables du monde (plus les noms vus à la main).
    public IReadOnlyList<string> ResourceNames() =>
        Resources.Select(r => r.Name).Concat(KnownNames.Resources).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    // Lit le dossier. Lève une exception si un fichier manque ou est illisible.
    public static GameDataStore Load(string folder)
    {
        var resources = ReadArray(folder, "resource_desc").Select(e => new ResourceInfo(
            Int(e, "id"), Str(e, "name"), Int(e, "tier"), Str(e, "tag"), Int(e, "max_health"))).ToList();

        var extractions = new Dictionary<int, ExtractionInfo>();
        foreach (var e in ReadArray(folder, "extraction_recipe_desc"))
        {
            var resourceId = Int(e, "resource_id");
            if (extractions.ContainsKey(resourceId))
            {
                continue;
            }
            var tool = FirstOf(e, "tool_requirements");
            var skill = FirstOf(e, "level_requirements");
            extractions[resourceId] = new ExtractionInfo(resourceId,
                Dbl(e, "time_requirement"), Dbl(e, "stamina_requirement"),
                tool is { } t ? Int(t, "tool_type") : 0, tool is { } t2 ? Int(t2, "level") : 0,
                skill is { } s ? Int(s, "skill_id") : 0, skill is { } s2 ? Int(s2, "level") : 0);
        }

        var biomes = ReadArray(folder, "biome_desc").Select(e => Str(e, "name")).Where(n => n.Length > 0).ToList();
        var items = ReadArray(folder, "item_desc").GroupBy(e => Int(e, "id")).ToDictionary(g => g.Key, g => Str(g.First(), "name"));
        var cargos = ReadArray(folder, "cargo_desc").GroupBy(e => Int(e, "id")).ToDictionary(g => g.Key, g => Str(g.First(), "name"));
        var foods = ReadArray(folder, "food_desc").Select(e => Int(e, "item_id")).ToHashSet();

        return new GameDataStore
        {
            Resources = resources,
            Extractions = extractions,
            Biomes = biomes,
            Items = items,
            Cargos = cargos,
            FoodItemIds = foods,
            Version = ReadVersion(folder),
        };
    }

    // Contrôles de vraisemblance : refuse des données vides, tronquées ou incohérentes avec ce qu'on a vu à l'écran.
    // Renvoie la liste des problèmes (vide = données acceptées).
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        if (Resources.Count < 300)
        {
            problems.Add($"trop peu de ressources ({Resources.Count})");
        }
        // Valeurs vérifiées à l'écran le 2026-11-16 (panneau de cible).
        foreach (var (name, health) in ScreenChecks)
        {
            var resource = FindResource(name);
            if (resource is null)
            {
                problems.Add($"« {name} » absent");
            }
            else if (resource.MaxHealth != health)
            {
                problems.Add($"« {name} » : {resource.MaxHealth} points au lieu de {health} vus à l'écran");
            }
        }
        if (!Biomes.Contains("Safe Meadows"))
        {
            problems.Add("biome « Safe Meadows » absent");
        }
        if (Items.Count < 500)
        {
            problems.Add($"trop peu d'objets ({Items.Count})");
        }
        return problems;
    }

    // Ressources dont la santé maximale a été relevée sur l'écran du jeu.
    public static IReadOnlyList<(string Name, int MaxHealth)> ScreenChecks { get; } =
    [
        ("Mature Oak Tree", 12000),
        ("Young Oak Tree", 3000),
    ];

    public const string VersionFile = "version.json";

    public static GameDataVersion? ReadVersion(string folder)
    {
        var path = Path.Combine(folder, VersionFile);
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<GameDataVersion>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void WriteVersion(string folder, GameDataVersion version) =>
        File.WriteAllText(Path.Combine(folder, VersionFile), JsonSerializer.Serialize(version, new JsonSerializerOptions { WriteIndented = true }));

    private static List<JsonElement> ReadArray(string folder, string table)
    {
        using var stream = File.OpenRead(Path.Combine(folder, table + ".json"));
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"{table}.json : liste attendue.");
        }
        return document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static JsonElement? FirstOf(JsonElement e, string property) =>
        e.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array && array.GetArrayLength() > 0
            ? array[0]
            : null;

    private static string Str(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : string.Empty;

    private static int Int(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n)
            ? (int)Math.Clamp(n, int.MinValue, int.MaxValue)
            : 0;

    private static double Dbl(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
}
