using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Un bonus (buff) actif du personnage (table active_buff_state, publique, lecture seule).
// StartUnix : début, en secondes Unix (heure du serveur). Duration : durée en secondes ; négative = permanent.
public sealed record ActiveBuffInfo(int BuffId, long StartUnix, int Duration)
{
    // Bonus « en pause » : compté seulement en jeu (online_timestamp) et personnage hors ligne.
    // Le serveur garde alors le temps déjà écoulé (petit nombre) au lieu de l'heure de début (code serveur, OnlineTimestamp::pause).
    public bool IsPaused => StartUnix == -1 || StartUnix is > 0 and < 1_000_000_000;

    // Secondes restantes (code serveur, ActiveBuffState::buff_remaining_time) : -1 si permanent, 0 si fini.
    public int RemainingSeconds(DateTimeOffset now)
    {
        if (Duration < 0)
        {
            return -1;
        }
        if (StartUnix == 0)
        {
            return 0;
        }
        if (IsPaused)
        {
            var frozen = Duration - Math.Max(0, StartUnix);
            return frozen > 0 ? (int)frozen : 0;
        }
        var left = StartUnix + Duration - now.ToUnixTimeSeconds();
        return left > 0 ? (int)Math.Min(left, int.MaxValue) : 0;
    }
}

public static class PlayerBuffs
{
    // Index de la faim maximale dans character_stats_state.values (CharacterStatType::MaxSatiation, code serveur).
    public const int MaxSatiationStat = 19;

    // null : table pas (encore) lue pour ce personnage.
    public static IReadOnlyList<ActiveBuffInfo>? Of(TableCache cache, ulong player)
    {
        var row = cache.Rows("active_buff_state").FirstOrDefault(r => r.U64("entity_id") == player);
        return row is null ? null : row.Get("active_buffs") is { } list ? Parse(list) : [];
    }

    // Liste d'ActiveBuff { buff_id, buff_start_timestamp { value }, buff_duration, values }, en tableaux ou en objets.
    public static IReadOnlyList<ActiveBuffInfo> Parse(JsonElement list)
    {
        var result = new List<ActiveBuffInfo>();
        if (list.ValueKind != JsonValueKind.Array)
        {
            return result;
        }
        foreach (var buff in list.EnumerateArray())
        {
            var id = Number(Field(buff, 0, "buff_id"));
            var start = Field(buff, 1, "buff_start_timestamp") is { } s ? Number(s.ValueKind == JsonValueKind.Number ? s : Field(s, 0, "value")) : null;
            var duration = Number(Field(buff, 2, "buff_duration"));
            if (id is not null && start is not null && duration is not null)
            {
                result.Add(new ActiveBuffInfo((int)id.Value, start.Value, (int)duration.Value));
            }
        }
        return result;
    }

    public static double? MaxSatiation(TableCache cache, ulong player)
    {
        var row = cache.Rows("character_stats_state").FirstOrDefault(r => r.U64("entity_id") == player);
        if (row?.Get("values") is not { ValueKind: JsonValueKind.Array } values || values.GetArrayLength() <= MaxSatiationStat)
        {
            return null;
        }
        var v = values[MaxSatiationStat];
        return v.ValueKind == JsonValueKind.Number && v.GetDouble() > 0 ? v.GetDouble() : null;
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
