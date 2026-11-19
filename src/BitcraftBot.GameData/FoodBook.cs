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

    public required IReadOnlyDictionary<int, string> BuffNames { get; init; }

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
            BuffNames = buffs.ToDictionary(b => b.Key, b => Str(b.Value, "description")),
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
