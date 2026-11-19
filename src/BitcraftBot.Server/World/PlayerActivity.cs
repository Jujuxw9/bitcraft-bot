using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Expérience d'un métier (experience_state) et sa progression vers le niveau suivant
// (c'est ce que montre la « jauge de métier » à l'écran).
public sealed record SkillProgress(int SkillId, int Experience)
{
    public int Level => SkillLevels.LevelFor(Experience);

    // Part du niveau en cours déjà faite, de 0 à 1 (1 au niveau maximum).
    public double Progress
    {
        get
        {
            var level = Level;
            if (level >= SkillLevels.MaxLevel)
            {
                return 1;
            }
            var from = SkillLevels.ForLevel(level);
            var to = SkillLevels.ForLevel(level + 1);
            return to > from ? Math.Clamp((Experience - from) / (double)(to - from), 0, 1) : 0;
        }
    }

    public int ExperienceToNextLevel => Level >= SkillLevels.MaxLevel ? 0 : SkillLevels.ForLevel(Level + 1) - Experience;
}

// Formule des niveaux du serveur (code serveur, ExperienceStack) : 125 niveaux, croissance 2^0,158 par niveau.
public static class SkillLevels
{
    public const int MaxLevel = 125;
    private const int Multiplier = 10;
    private const int LevelOneExperience = 52;

    public static int ForLevel(int level)
    {
        if (level > MaxLevel)
        {
            return -1;
        }
        // Calcul en float (32 bits), comme le serveur, pour tomber sur les mêmes valeurs.
        var growth = MathF.Pow(2.0f, 0.158f);
        var value = MathF.Floor(LevelOneExperience * ((MathF.Pow(growth, level) - growth) / (growth * growth - growth)));
        return (int)value * Multiplier;
    }

    public static int LevelFor(int experience)
    {
        var level = 1;
        while (level < MaxLevel && experience >= ForLevel(level + 1))
        {
            level++;
        }
        return level;
    }
}

// Action en cours du personnage (player_action_state) : type, début et durée en millisecondes.
public sealed record ActionInfo(string Type, long StartMs, long DurationMs, ulong? Target, int? RecipeId)
{
    public double Progress(DateTimeOffset now) =>
        DurationMs <= 0 ? 0 : Math.Clamp((now.ToUnixTimeMilliseconds() - StartMs) / (double)DurationMs, 0, 1);

    public string French => ActionNames.French(Type);
}

// Dernier coup de récolte (extract_outcome_state) : ressource visée et dégâts du coup.
public sealed record ExtractInfo(ulong Target, int Damage);

public static class PlayerActivity
{
    // Types d'action du serveur, dans l'ordre (code serveur, PlayerActionType).
    public static readonly string[] ActionTypes =
    [
        "None", "Attack", "DestroyPaving", "StationaryEmote", "Extract", "PaveTile", "SpawnCargo", "Build", "Deconstruct",
        "RepairBuilding", "ResupplyClaim", "CargoPickUp", "Terraform", "DeployDeployable", "StoreDeployable", "Sleep",
        "Teleport", "Death", "Climb", "UseItem", "Craft", "ConvertItems", "PlayerMove", "DeployableMove", "ResupplyEmpireNode",
        "SetHome", "UseElevator", "MobileEmote", "PlacePillarShaping", "DestroyPillarShaping", "AbilityCustom", "Prospect",
        "PlacePlaceable", "InteractPlaceable",
    ];

    public static IReadOnlyList<SkillProgress>? Skills(TableCache cache, ulong player)
    {
        var row = cache.Rows("experience_state").FirstOrDefault(r => r.U64("entity_id") == player);
        return row is null ? null : row.Get("experience_stacks") is { } list ? ParseSkills(list) : [];
    }

    public static IReadOnlyList<SkillProgress> ParseSkills(JsonElement list)
    {
        var result = new List<SkillProgress>();
        if (list.ValueKind != JsonValueKind.Array)
        {
            return result;
        }
        foreach (var stack in list.EnumerateArray())
        {
            if (Number(Field(stack, 0, "skill_id")) is { } id && Number(Field(stack, 1, "quantity")) is { } xp)
            {
                result.Add(new SkillProgress((int)id, (int)Math.Clamp(xp, 0, int.MaxValue)));
            }
        }
        return result;
    }

    // Action de la couche principale (« Base »), sauf « None ».
    public static ActionInfo? CurrentAction(TableCache cache, ulong player)
    {
        foreach (var row in cache.Rows("player_action_state").Where(r => r.U64("entity_id") == player))
        {
            var layer = row.Get("layer") is { } l ? EnumName(l, ["Base", "UpperBody"]) : "Base";
            var type = row.Get("action_type") is { } t ? EnumName(t, ActionTypes) : null;
            if (layer != "Base" || type is null or "None")
            {
                continue;
            }
            return new ActionInfo(type, row.I64("start_time") ?? 0, row.I64("duration") ?? 0,
                row.Get("target") is { } target ? OptionU64(target) : null,
                row.Get("recipe_id") is { } recipe && OptionU64(recipe) is { } r ? (int)r : null);
        }
        return null;
    }

    public static ExtractInfo? LastExtract(TableCache cache, ulong player)
    {
        var row = cache.Rows("extract_outcome_state").FirstOrDefault(r => r.U64("entity_id") == player);
        return row?.U64("target_entity_id") is { } target and > 0 ? new ExtractInfo(target, row.I32("damage") ?? 0) : null;
    }

    // Énumération : [index, []], {"Nom": []}, "Nom" ou un nombre, selon le format.
    public static string? EnumName(JsonElement e, IReadOnlyList<string> names) => e.ValueKind switch
    {
        JsonValueKind.Array when e.GetArrayLength() > 0 && e[0].ValueKind == JsonValueKind.Number && e[0].TryGetInt32(out var i) => i >= 0 && i < names.Count ? names[i] : $"type {i}",
        JsonValueKind.Number when e.TryGetInt32(out var n) => n >= 0 && n < names.Count ? names[n] : $"type {n}",
        JsonValueKind.Object => e.EnumerateObject().Select(p => p.Name).FirstOrDefault(),
        JsonValueKind.String => e.GetString(),
        _ => null,
    };

    // Option : [0, x] ou {"some": x} → x ; [1, []], {"none": …} ou null → rien ; un nombre seul → lui-même.
    private static ulong? OptionU64(JsonElement e)
    {
        JsonElement? inner = e.ValueKind switch
        {
            JsonValueKind.Number => e,
            JsonValueKind.Array when e.GetArrayLength() == 2 && e[0].ValueKind == JsonValueKind.Number => e[0].GetInt32() == 0 ? e[1] : null,
            JsonValueKind.Object when e.TryGetProperty("some", out var s) => s,
            _ => null,
        };
        return inner is { ValueKind: JsonValueKind.Number } v && v.TryGetUInt64(out var u) ? u
            : inner is { ValueKind: JsonValueKind.Number } w && w.TryGetInt64(out var s64) && s64 >= 0 ? (ulong)s64 : null;
    }

    private static JsonElement? Field(JsonElement e, int index, string name) => e.ValueKind switch
    {
        JsonValueKind.Array when e.GetArrayLength() > index => e[index],
        JsonValueKind.Object when e.TryGetProperty(name, out var v) => v,
        _ => null,
    };

    private static long? Number(JsonElement? e) =>
        e is { ValueKind: JsonValueKind.Number } n && n.TryGetInt64(out var v) ? v : null;
}

public static class ActionNames
{
    private static readonly Dictionary<string, string> Names = new()
    {
        ["Extract"] = "récolte",
        ["Craft"] = "fabrication",
        ["Attack"] = "combat",
        ["Build"] = "construction",
        ["Deconstruct"] = "démolition",
        ["RepairBuilding"] = "réparation",
        ["CargoPickUp"] = "ramasse une cargaison",
        ["Terraform"] = "terrassement",
        ["Sleep"] = "sommeil",
        ["Teleport"] = "téléportation",
        ["Death"] = "mort",
        ["Climb"] = "escalade",
        ["UseItem"] = "utilise un objet",
        ["ConvertItems"] = "conversion d'objets",
        ["PlayerMove"] = "déplacement",
        ["DeployableMove"] = "déplacement en véhicule",
        ["Prospect"] = "prospection",
        ["PaveTile"] = "pavage",
        ["DeployDeployable"] = "sortie d'un véhicule",
        ["StoreDeployable"] = "rangement d'un véhicule",
        ["SpawnCargo"] = "dépôt d'une cargaison",
        ["DestroyPaving"] = "retrait d'un pavé",
        ["ResupplyClaim"] = "ravitaillement d'un terrain",
        ["SetHome"] = "choix du foyer",
        ["PlacePlaceable"] = "pose d'un objet",
        ["InteractPlaceable"] = "utilisation d'un objet",
        ["AbilityCustom"] = "capacité",
    };

    public static string French(string type) => Names.GetValueOrDefault(type) ?? type;
}

// Métier en cours : celui dont l'expérience vient d'augmenter (le serveur ne le dit pas directement).
public sealed class SkillTracker
{
    private readonly Dictionary<int, int> _last = [];

    public int? ActiveSkill { get; private set; }

    public DateTime ActiveSince { get; private set; }

    public DateTime LastGain { get; private set; } = DateTime.MinValue;

    public void Update(IReadOnlyList<SkillProgress>? skills, DateTime now)
    {
        if (skills is null)
        {
            return;
        }
        var first = _last.Count == 0;
        foreach (var s in skills)
        {
            if (!first && _last.TryGetValue(s.SkillId, out var before) && s.Experience > before)
            {
                if (ActiveSkill != s.SkillId)
                {
                    ActiveSince = now;
                }
                ActiveSkill = s.SkillId;
                LastGain = now;
            }
            _last[s.SkillId] = s.Experience;
        }
    }

    // Le métier est « en cours » s'il a gagné de l'expérience récemment.
    public bool IsActive(DateTime now, TimeSpan window) => ActiveSkill is not null && now - LastGain <= window;
}
