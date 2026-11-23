using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Type de capacité (AbilityType du code serveur, même ordre).
public enum AbilityKind
{
    Inconnue = 0,
    Manger = 1,
    Attaque = 2,
    AttaqueAuto = 3,
    Speciale = 4,
    Prospection = 5,
    Equiper = 6,
    Deployer = 7,
    Ceinture = 8,
    Deployable = 9,
    Emote = 10,
    Preset = 11,
    Placer = 12,
}

// Une capacité du personnage (table ability_state, publique, lecture seule).
// Value : l'id qui va avec (objet à manger, attaque, capacité spéciale…). Cooldown : début en ms Unix et durée en secondes.
public sealed record AbilityInfo(ulong EntityId, AbilityKind Kind, int Value, long CooldownStartMs, double CooldownSeconds)
{
    // Secondes avant de pouvoir la relancer (0 = prête). Code serveur : AbilityState::is_under_cooldown.
    public double RemainingSeconds(DateTimeOffset now)
    {
        var ready = CooldownStartMs + (long)(CooldownSeconds * 1000);
        var left = (ready - now.ToUnixTimeMilliseconds()) / 1000.0;
        return left > 0 ? left : 0;
    }
}

// Une case de la barre d'action (table action_bar_state). Bar : numéro de barre (0 = la première) ; Slot : case (0 = touche 1).
public sealed record ActionBarSlot(int Bar, int Slot, AbilityInfo? Ability)
{
    // Touche du clavier de la case : 1 à 9, 0, -, = (barre d'action du jeu, D-032).
    public string Key => Slot switch
    {
        >= 0 and <= 8 => (Slot + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
        9 => "0",
        10 => "-",
        11 => "=",
        _ => $"case {Slot + 1}",
    };
}

public static class ActionBars
{
    // Capacité « Sprint » (ability_custom_desc 594765058, recharge 120 s) et son bonus (buff_desc 1845851314, 15 s, +40 % de vitesse).
    public const int SprintAbilityId = 594765058;
    public const int SprintBuffId = 1845851314;

    public static IReadOnlyList<AbilityInfo> Abilities(TableCache cache, ulong player) =>
        cache.Rows("ability_state").Where(r => r.U64("owner_entity_id") == player).Select(Parse).OfType<AbilityInfo>().ToList();

    // null : table pas lue (abonnement refusé ou pas encore reçu).
    public static IReadOnlyList<ActionBarSlot>? Of(TableCache cache, ulong player)
    {
        var rows = cache.Rows("action_bar_state").Where(r => r.U64("player_entity_id") == player).ToList();
        if (rows.Count == 0 && !cache.Counts().ContainsKey("action_bar_state"))
        {
            return null;
        }
        var abilities = Abilities(cache, player).ToDictionary(a => a.EntityId);
        return rows
            .Select(r => new ActionBarSlot(r.I32("action_bar_index") ?? 0, r.I32("local_ability_index") ?? 0,
                r.U64("ability_entity_id") is { } id && abilities.TryGetValue(id, out var a) ? a : null))
            .OrderBy(s => s.Bar).ThenBy(s => s.Slot).ToList();
    }

    public static AbilityInfo? Parse(Row row)
    {
        if (row.U64("entity_id") is not { } id || row.Get("ability") is not { } ability)
        {
            return null;
        }
        var (kind, value) = Variant(ability);
        long start = 0;
        double seconds = 0;
        if (row.Get("cooldown") is { } c)
        {
            start = Field(c, 0, "timestamp") is { ValueKind: JsonValueKind.Number } t && t.TryGetInt64(out var ts) ? ts : 0;
            seconds = Field(c, 1, "cooldown") is { ValueKind: JsonValueKind.Number } d ? d.GetDouble() : 0;
        }
        return new AbilityInfo(id, Enum.IsDefined(typeof(AbilityKind), kind) ? (AbilityKind)kind : AbilityKind.Inconnue, value, start, seconds);
    }

    // Somme SpacetimeDB : [n° de variante, valeur] (vu en réel, ex. action_type: [0,{}]) ou {"Custom": 123}.
    public static (int Kind, int Value) Variant(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.Array && e.GetArrayLength() >= 1 && e[0].ValueKind == JsonValueKind.Number)
        {
            var value = e.GetArrayLength() > 1 && e[1].ValueKind == JsonValueKind.Number && e[1].TryGetInt32(out var v) ? v : 0;
            return (e[0].GetInt32(), value);
        }
        if (e.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in e.EnumerateObject())
            {
                var value = p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var v) ? v : 0;
                return (NameToKind(p.Name) ?? (int.TryParse(p.Name, out var n) ? n : 0), value);
            }
        }
        return (0, 0);
    }

    private static int? NameToKind(string name) => name switch
    {
        "Eat" => 1,
        "CombatAction" => 2,
        "AutoAttack" => 3,
        "Custom" => 4,
        "Prospecting" => 5,
        "Equip" => 6,
        "DeployableDeploy" => 7,
        "AddToToolbelt" => 8,
        "DeployableToggle" => 9,
        "Emote" => 10,
        "EquipPreset" => 11,
        "PlaceablePlace" => 12,
        _ => null,
    };

    private static JsonElement? Field(JsonElement e, int index, string name) => e.ValueKind switch
    {
        JsonValueKind.Array when e.GetArrayLength() > index => e[index],
        JsonValueKind.Object when e.TryGetProperty(name, out var v) => v,
        _ => null,
    };
}

// Où en est le sprint : bonus actif (secondes restantes), capacité en recharge (secondes), case de la barre.
public sealed record SprintStatus(double ActiveSeconds, double CooldownSeconds, ActionBarSlot? Slot, bool AbilityKnown)
{
    public bool Active => ActiveSeconds > 0;

    public bool Ready => AbilityKnown && CooldownSeconds <= 0 && !Active;

    public string French => !AbilityKnown
        ? "capacité « Sprint » pas trouvée"
        : Active ? $"actif ({ActiveSeconds:0} s)"
        : CooldownSeconds > 0 ? $"en recharge ({CooldownSeconds:0} s)"
        : "prêt";

    public static SprintStatus Of(IReadOnlyList<ActionBarSlot>? bar, IReadOnlyList<AbilityInfo> abilities, IReadOnlyList<ActiveBuffInfo>? buffs, DateTimeOffset now)
    {
        var ability = abilities.FirstOrDefault(a => a.Kind == AbilityKind.Speciale && a.Value == ActionBars.SprintAbilityId);
        var slot = bar?.FirstOrDefault(s => s.Ability is { Kind: AbilityKind.Speciale, Value: ActionBars.SprintAbilityId });
        var buff = buffs?.FirstOrDefault(b => b.BuffId == ActionBars.SprintBuffId);
        return new SprintStatus(buff?.RemainingSeconds(now) ?? 0, ability?.RemainingSeconds(now) ?? 0, slot, ability is not null);
    }
}
