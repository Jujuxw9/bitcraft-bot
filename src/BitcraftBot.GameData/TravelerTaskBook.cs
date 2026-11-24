using System.Text.Json;

namespace BitcraftBot.GameData;

// Un objet demandé par une tâche de voyageur. IsCargo : cargaison (cargo_desc) au lieu d'objet (item_desc).
public sealed record TaskItem(int ItemId, int Quantity, bool IsCargo);

// Une tâche de voyageur (traveler_task_desc.json, v0.4.11) : objets demandés, métier, récompense en expérience.
public sealed record TravelerTaskDesc(int Id, int SkillId, int MinLevel, int MaxLevel,
    IReadOnlyList<TaskItem> Required, string Description, int RewardSkillId, double RewardExperience);

// Tâches de voyageur et noms des voyageurs, lus depuis BitCraft_GameData (D-033).
public static class TravelerTaskBook
{
    // traveler_task_desc.json : une entrée par tâche possible. Fichier absent : liste vide.
    public static IReadOnlyDictionary<int, TravelerTaskDesc> LoadTasks(string folder)
    {
        var result = new Dictionary<int, TravelerTaskDesc>();
        foreach (var e in ReadArray(folder, "traveler_task_desc"))
        {
            var id = Int(e, "id");
            if (result.ContainsKey(id))
            {
                continue;
            }
            var level = e.TryGetProperty("level_requirement", out var l) ? l : default;
            var xp = e.TryGetProperty("rewarded_experience", out var x) ? x : default;
            var items = new List<TaskItem>();
            if (e.TryGetProperty("required_items", out var req) && req.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in req.EnumerateArray())
                {
                    var type = it.TryGetProperty("item_type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : "Item";
                    items.Add(new TaskItem(Int(it, "item_id"), Int(it, "quantity"), type == "Cargo"));
                }
            }
            result[id] = new TravelerTaskDesc(id, Int(level, "skill_id"), Int(level, "min_level"), Int(level, "max_level"),
                items, Str(e, "description"), Int(xp, "skill_id"), Dbl(xp, "quantity"));
        }
        return result;
    }

    // npc_desc.json : numéro du voyageur (npc_type) → nom (Rumbagh, Svim…). Fichier absent : liste vide.
    public static IReadOnlyDictionary<int, string> LoadTravelers(string folder) =>
        ReadArray(folder, "npc_desc").GroupBy(e => Int(e, "npc_type")).ToDictionary(g => g.Key, g => Str(g.First(), "name"));

    private static List<JsonElement> ReadArray(string folder, string table)
    {
        var path = Path.Combine(folder, table + ".json");
        if (!File.Exists(path))
        {
            return [];
        }
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList()
            : [];
    }

    private static string Str(JsonElement e, string property) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : string.Empty;

    private static int Int(JsonElement e, string property) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n)
            ? (int)Math.Clamp(n, int.MinValue, int.MaxValue)
            : 0;

    private static double Dbl(JsonElement e, string property) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
}
