using BitcraftBot.GameData;

namespace BitcraftBot.Server.World;

// Capacités automatiques (v0.4.17, D-069, demande de Julien du 25/11) : pendant que Julien récolte lui-même,
// appuyer sur la touche de la capacité de la barre d'action qui aide ce métier, dès que sa recharge est finie.
// Logique pure (aucune fenêtre, aucune touche) : décide seulement « quelle touche maintenant », ou rien.
public sealed record AbilitySituation
{
    public bool Enabled { get; init; }

    // Capacités allumées par Julien (id de ability_custom_desc).
    public IReadOnlySet<int> Allowed { get; init; } = new HashSet<int>();

    // Action en cours (player_action_state) et métier de la ressource visée (null si pas une récolte ou inconnu).
    public string? ActionType { get; init; }

    public int? ActionSkill { get; init; }

    public IReadOnlyList<ActionBarSlot>? Bar { get; init; }

    // null = bonus pas lus : prudence, aucune capacité.
    public IReadOnlyList<ActiveBuffInfo>? Buffs { get; init; }

    public double? Stamina { get; init; }

    public double? MaxStamina { get; init; }

    public DateTimeOffset Now { get; init; }

    public DateTimeOffset LastPress { get; init; } = DateTimeOffset.MinValue;

    // Capacités mises de côté un moment (ex. deux appuis sans effet) : id → jusqu'à quand.
    public IReadOnlyDictionary<int, DateTimeOffset> BlockedUntil { get; init; } = new Dictionary<int, DateTimeOffset>();
}

// État d'une case de la barre (pour l'affichage) : Ready = toutes les conditions sont réunies pour cette case.
public sealed record AbilitySlotState(ActionBarSlot Slot, AbilityDef Def, bool Ready, string Text);

public sealed record AbilityDecision(AbilitySlotState? Press, string Reason, IReadOnlyList<AbilitySlotState> Slots);

public static class AbilityPlanner
{
    // Réserve d'endurance (Julien, 25/11) : jamais de capacité qui fait passer l'endurance sous 30 %.
    public const double StaminaReserve = 0.30;

    // Au plus une touche toutes les 2 s.
    public static readonly TimeSpan MinGap = TimeSpan.FromSeconds(2);

    // Seule la première barre (barre 0) est affichée par le jeu ; touches 1 à 9, 0, -, =.
    public const int MaxSlot = 11;

    // Capacités de la première barre que le bot sait utiliser : capacités spéciales connues des données du jeu.
    public static IReadOnlyList<(ActionBarSlot Slot, AbilityDef Def)> Usable(IReadOnlyList<ActionBarSlot>? bar, IReadOnlyDictionary<int, AbilityDef> book) =>
        bar is null
            ? []
            : bar.Where(s => s.Bar == 0 && s.Slot is >= 0 and <= MaxSlot && s.Ability is { Kind: AbilityKind.Speciale })
                .Select(s => (Slot: s, Def: book.GetValueOrDefault(s.Ability!.Value)))
                .Where(x => x.Def is not null)
                .Select(x => (x.Slot, x.Def!))
                .ToList();

    public static AbilityDecision Decide(AbilitySituation s, IReadOnlyDictionary<int, AbilityDef> book)
    {
        var slots = new List<AbilitySlotState>();
        var usable = Usable(s.Bar, book);
        var harvesting = s.ActionType == "Extract" && s.ActionSkill is not null;
        foreach (var (slot, def) in usable)
        {
            var why = WhyNot(slot, def, s, harvesting);
            slots.Add(new AbilitySlotState(slot, def, why is null, why ?? "prête"));
        }
        if (!s.Enabled)
        {
            return new AbilityDecision(null, "coupé", slots);
        }
        if (s.Bar is null)
        {
            return new AbilityDecision(null, "barre d'action pas lue", slots);
        }
        if (usable.Count == 0)
        {
            return new AbilityDecision(null, "aucune capacité connue dans la barre", slots);
        }
        if (!harvesting)
        {
            return new AbilityDecision(null, s.ActionType == "Extract" ? "récolte en cours, métier inconnu" : "attend une récolte", slots);
        }
        if (s.Now - s.LastPress < MinGap)
        {
            return new AbilityDecision(null, "pause entre deux touches", slots);
        }
        var ready = slots.FirstOrDefault(x => x.Ready);
        return ready is not null
            ? new AbilityDecision(ready, $"{ready.Def.Name} prête", slots)
            : new AbilityDecision(null, "rien à lancer", slots);
    }

    // Raison pour laquelle cette capacité ne part pas maintenant (null = elle peut partir).
    private static string? WhyNot(ActionBarSlot slot, AbilityDef def, AbilitySituation s, bool harvesting)
    {
        if (def.SkillId is null)
        {
            return "pas liée à une récolte";
        }
        if (!s.Allowed.Contains(def.Id))
        {
            return "coupée";
        }
        if (s.BlockedUntil.TryGetValue(def.Id, out var until) && s.Now < until)
        {
            return $"en pause jusqu'à {until.ToLocalTime():HH:mm}";
        }
        if (harvesting && s.ActionSkill != def.SkillId)
        {
            return $"pour {def.SkillName}";
        }
        if (slot.Ability is { } ability && ability.RemainingSeconds(s.Now) is > 0 and var left)
        {
            return $"recharge {Math.Ceiling(left):0} s";
        }
        if (s.Buffs is null)
        {
            return "bonus pas lus";
        }
        var active = s.Buffs.Where(b => def.BuffIds.Contains(b.BuffId)).Select(b => b.RemainingSeconds(s.Now)).FirstOrDefault(r => r != 0);
        if (active != 0)
        {
            return active < 0 ? "bonus actif" : $"bonus actif ({active} s)";
        }
        if (def.StaminaCost > 0)
        {
            if (s.Stamina is not { } st || s.MaxStamina is not { } max || max <= 0)
            {
                return "endurance pas lue";
            }
            if (st - def.StaminaCost < max * StaminaReserve)
            {
                return $"endurance (réserve {StaminaReserve * 100:0} %)";
            }
        }
        return harvesting ? null : "attend une récolte";
    }

    // Nom de l'activité pour le journal : « Capacité : Hewing Strikes (touche 7), coupe de bois ».
    public static string Activity(string? skillName) => skillName switch
    {
        "Forestry" => "coupe de bois",
        "Mining" => "minage",
        "Fishing" => "pêche",
        "Foraging" => "cueillette",
        "Hunting" => "chasse",
        "Farming" => "culture",
        null or "" => "récolte",
        _ => $"récolte ({skillName})",
    };
}
