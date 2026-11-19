using System.Text.Json;

namespace BitcraftBot.GameData;

// Un aliment : ce qu'il rend (faim, vie, endurance) et ses bonus.
// GivesFoodRegen : donne le bonus de nourriture « Food Regen » (vie et endurance qui remontent plus vite).
public sealed record FoodInfo(int ItemId, string Name, double Hunger, double Health, double Stamina, IReadOnlyList<int> BuffIds, bool GivesFoodRegen)
{
    // Aliment « normal » pour le repas automatique : rend de la faim ou donne le bonus de nourriture.
    // Les potions, thés et élixirs n'en font pas partie.
    public bool IsMeal => GivesFoodRegen || (Hunger > 0 && BuffIds.Count == 0);
}

// Aliments et bonus du jeu (food_desc, buff_desc, buff_type_desc ; BitCraft_GameData, D-033).
public sealed class FoodBook
{
    public const string FoodRegenType = "Food Regen";

    public static IReadOnlyList<string> Tables { get; } = ["food_desc", "item_desc", "buff_desc", "buff_type_desc"];

    public required IReadOnlyDictionary<int, FoodInfo> Foods { get; init; }

    // Bonus de type « Food Regen » (un par niveau d'aliment).
    public required IReadOnlySet<int> FoodRegenBuffIds { get; init; }

    // Tous les bonus du jeu (nom, type, effets), pour afficher les bonus actifs du personnage.
    public required IReadOnlyDictionary<int, BuffDescInfo> Buffs { get; init; }

    public static bool CanLoad(string folder) => Tables.All(t => File.Exists(Path.Combine(folder, t + ".json")));

    public static FoodBook Load(string folder)
    {
        var types = Read(folder, "buff_type_desc").ToDictionary(e => Int(e, "id"), e => Str(e, "name"));
        var buffs = Read(folder, "buff_desc").GroupBy(e => Int(e, "id")).ToDictionary(g => g.Key, g => g.First());
        var regen = buffs.Where(b => types.GetValueOrDefault(Int(b.Value, "buff_type_id")) == FoodRegenType).Select(b => b.Key).ToHashSet();
        var names = Read(folder, "item_desc").GroupBy(e => Int(e, "id")).ToDictionary(g => g.Key, g => Str(g.First(), "name"));
        var foods = new Dictionary<int, FoodInfo>();
        foreach (var e in Read(folder, "food_desc"))
        {
            var id = Int(e, "item_id");
            var buffIds = e.TryGetProperty("buffs", out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Select(b => Int(b, "buff_id")).ToList()
                : [];
            foods.TryAdd(id, new FoodInfo(id, names.GetValueOrDefault(id) ?? $"aliment n° {id}",
                Dbl(e, "hunger"), Dbl(e, "hp"), Dbl(e, "stamina"), buffIds, buffIds.Any(regen.Contains)));
        }
        return new FoodBook
        {
            Foods = foods,
            FoodRegenBuffIds = regen,
            Buffs = buffs.ToDictionary(b => b.Key, b => new BuffDescInfo(b.Key, Str(b.Value, "description"),
                types.GetValueOrDefault(Int(b.Value, "buff_type_id")) ?? "", b.Value.TryGetProperty("beneficial", out var good) && good.ValueKind == JsonValueKind.True,
                b.Value.TryGetProperty("stats", out var stats) && stats.ValueKind == JsonValueKind.Array
                    ? stats.EnumerateArray().Select(s => new BuffStat(Str(s, "id"), Dbl(s, "value"), s.TryGetProperty("is_pct", out var p) && p.ValueKind == JsonValueKind.True)).ToList()
                    : [])),
        };
    }

    private static List<JsonElement> Read(string folder, string table)
    {
        using var stream = File.OpenRead(Path.Combine(folder, table + ".json"));
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList()
            : throw new InvalidDataException($"{table}.json : liste attendue.");
    }

    private static string Str(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : string.Empty;

    private static int Int(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n)
            ? (int)Math.Clamp(n, int.MinValue, int.MaxValue)
            : 0;

    private static double Dbl(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
}

// Un effet d'un bonus : statistique du jeu (ex. « GatheringSpeed »), valeur, en pourcentage ou non.
public sealed record BuffStat(string Stat, double Value, bool IsPercent)
{
    public string Text
    {
        get
        {
            var value = IsPercent ? $"{Value * 100:+0.#;-0.#} %" : $"{Value:+0.##;-0.##}";
            return $"{StatNames.French(Stat)} {value}";
        }
    }
}

// Un bonus du jeu (buff_desc). Type : nom du type (« Food Regen », « Food Buffs », « Elixir »…).
public sealed record BuffDescInfo(int Id, string Name, string Type, bool Beneficial, IReadOnlyList<BuffStat> Stats);

// Noms français des statistiques les plus courantes ; les autres gardent leur nom anglais, en mots séparés.
public static class StatNames
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MaxHealth"] = "vie max",
        ["MaxStamina"] = "endurance max",
        ["PassiveHealthRegenRate"] = "vie regagnée",
        ["PassiveStaminaRegenRate"] = "endurance regagnée",
        ["MovementMultiplier"] = "vitesse de marche",
        ["Armor"] = "armure",
        ["CooldownMultiplier"] = "temps de recharge",
        ["Strength"] = "force",
        ["ColdProtection"] = "protection du froid",
        ["HeatProtection"] = "protection de la chaleur",
        ["CraftingSpeed"] = "vitesse de fabrication",
        ["GatheringSpeed"] = "vitesse de récolte",
        ["BuildingSpeed"] = "vitesse de construction",
        ["SatiationRegenRate"] = "faim",
        ["MaxSatiation"] = "faim max",
        ["ForestrySpeed"] = "vitesse en forestry",
        ["MiningSpeed"] = "vitesse en mining",
        ["ActionSpeed"] = "vitesse d'action",
    };

    public static string French(string stat)
    {
        if (Names.TryGetValue(stat, out var name))
        {
            return name;
        }
        var words = new System.Text.StringBuilder();
        foreach (var c in stat)
        {
            if (char.IsUpper(c) && words.Length > 0) words.Append(' ');
            words.Append(char.ToLowerInvariant(c));
        }
        return words.ToString();
    }
}
