using System.Text.Json;

namespace BitcraftBot.GameData;

// Une capacité spéciale du jeu (ability_custom_desc), avec le métier qu'elle aide (v0.4.17, D-069).
// SkillId : déduit des effets de ses bonus (ex. « ForestrySpeed » → Forestry), car le champ
// profession_hit_skill_id du jeu vaut 1 (« ANY ») pour toutes les capacités. null = aucun métier (ex. Sprint).
public sealed record AbilityDef(
    int Id,
    string Name,
    double StaminaCost,
    double CooldownSeconds,
    IReadOnlyList<int> BuffIds,
    int BuffSeconds,
    int? SkillId,
    string? SkillName,
    string Effect)
{
    // Allumée par défaut quand Julien n'a encore rien choisi : liée à un métier et pas une capacité
    // « à répéter » (recharge d'au moins 10 s). Break the Roots, Reel In, Let It Run (1 s) restent coupées.
    public bool DefaultOn => SkillId is not null && CooldownSeconds >= 10;
}

// Capacités du jeu et leur métier (ability_custom_desc, buff_desc, skill_desc).
public sealed class AbilityBook
{
    public static IReadOnlyList<string> Tables { get; } = ["ability_custom_desc", "buff_desc", "skill_desc"];

    public required IReadOnlyDictionary<int, AbilityDef> Abilities { get; init; }

    public static bool CanLoad(string folder) => Tables.All(t => File.Exists(Path.Combine(folder, t + ".json")));

    public static AbilityBook Load(string folder)
    {
        var professions = Read(folder, "skill_desc")
            .Where(e => Str(e, "skill_category") == "Profession")
            .ToDictionary(e => Int(e, "id"), e => Str(e, "name"));
        var buffs = Read(folder, "buff_desc").GroupBy(e => Int(e, "id")).ToDictionary(g => g.Key, g => g.First());
        var result = new Dictionary<int, AbilityDef>();
        foreach (var e in Read(folder, "ability_custom_desc"))
        {
            var buffIds = e.TryGetProperty("buffs", out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Select(b => Int(b, "buff_id")).Where(b => b != 0).ToList()
                : [];
            var stats = new List<BuffStat>();
            var seconds = 0;
            foreach (var id in buffIds)
            {
                if (!buffs.TryGetValue(id, out var buff))
                {
                    continue;
                }
                seconds = Math.Max(seconds, Int(buff, "duration"));
                if (buff.TryGetProperty("stats", out var st) && st.ValueKind == JsonValueKind.Array)
                {
                    stats.AddRange(st.EnumerateArray().Select(s => new BuffStat(Str(s, "id"), Dbl(s, "value"), s.TryGetProperty("is_pct", out var p) && p.ValueKind == JsonValueKind.True)));
                }
            }
            var skill = SkillFromStats(stats, professions);
            var def = new AbilityDef(Int(e, "id"), Str(e, "ability_name"), Dbl(e, "stamina_cost"), Dbl(e, "cooldown"), buffIds, seconds,
                skill, skill is { } k ? professions[k] : null, string.Join(", ", stats.Select(s => s.Text)));
            result.TryAdd(def.Id, def);
        }
        return new AbilityBook { Abilities = result };
    }

    // Métier aidé par une capacité : le premier effet positif dont le nom commence par un métier
    // (« ForestrySpeed », « MiningPower », « FishingPower », « HuntingWeaponPower »). null si aucun.
    public static int? SkillFromStats(IEnumerable<BuffStat> stats, IReadOnlyDictionary<int, string> professions)
    {
        foreach (var stat in stats.Where(s => s.Value > 0))
        {
            foreach (var (id, name) in professions)
            {
                if (name.Length > 0 && stat.Stat.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    return id;
                }
            }
        }
        return null;
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
