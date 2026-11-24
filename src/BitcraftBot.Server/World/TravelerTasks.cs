using BitcraftBot.GameData;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Une tâche de voyageur du personnage (table traveler_task_state, publique, lecture seule, v0.4.11).
// TravelerId : numéro du voyageur (npc_desc.npc_type). TaskId : la tâche (traveler_task_desc.id).
// Completed : déjà rendue (le serveur la remplace ensuite par une nouvelle, s'il en trouve une).
public sealed record TravelerTaskInfo(ulong EntityId, int TravelerId, int TaskId, bool Completed);

public static class TravelerTasks
{
    public static IReadOnlyList<TravelerTaskInfo> Of(TableCache cache, ulong player) =>
        cache.Rows("traveler_task_state")
            .Where(r => r.U64("player_entity_id") == player)
            .Select(r => new TravelerTaskInfo(r.U64("entity_id") ?? 0, r.I32("traveler_id") ?? 0, r.I32("task_id") ?? 0, r.Bool("completed") ?? false))
            .OrderBy(t => t.TravelerId).ThenBy(t => t.TaskId)
            .ToList();
}

// Verdict d'une tâche, comparée au stock.
public enum TaskVerdict
{
    // Tout est dans le sac : le serveur ne prend les objets que dans le sac (code serveur, player_complete_task).
    FaisableMaintenant,
    // Assez au total, mais une partie est dans la ceinture ou une banque : à aller chercher.
    FaisableApresBanque,
    // Une partie seulement.
    Partiel,
    // Aucun des objets demandés.
    Impossible,
    // Déjà rendue.
    DejaRendue,
    // Tâche absente des données du jeu (données à mettre à jour).
    Inconnue,
}

// Une pile du stock et l'endroit où elle est. InBag : dans le sac (seul endroit pris par le serveur).
public sealed record StockStack(string Where, bool InBag, ItemStackInfo Stack);

// Un objet demandé : combien dans le sac, combien ailleurs (et où), combien il manque.
// Shared : une autre tâche active demande le même objet (le stock n'est compté qu'une fois).
public sealed record TaskItemCheck(TaskItem Item, int InBag, IReadOnlyList<(string Where, int Quantity)> Elsewhere, int Missing, bool Shared)
{
    public int Found => InBag + Elsewhere.Sum(e => e.Quantity);
}

public sealed record TaskCheck(TravelerTaskInfo Task, TravelerTaskDesc? Desc, TaskVerdict Verdict, IReadOnlyList<TaskItemCheck> Items);

// Compare les tâches au stock (sac, ceinture, banques). Le même objet de palier différent a un autre numéro :
// on compare par numéro d'objet et par type (objet ou cargaison), jamais par nom.
// Plusieurs tâches demandent le même objet : le stock est réparti dans l'ordre des tâches (sac d'abord).
public static class TravelerTaskPlanner
{
    public static IReadOnlyList<TaskCheck> Check(IReadOnlyList<TravelerTaskInfo> tasks,
        IReadOnlyDictionary<int, TravelerTaskDesc> descs, IReadOnlyList<StockStack> stock)
    {
        // Stock restant, par objet : le sac d'abord, puis les autres endroits dans l'ordre alphabétique.
        var pool = stock
            .GroupBy(s => (s.Stack.ItemId, s.Stack.IsCargo))
            .ToDictionary(g => g.Key, g => g
                .GroupBy(s => (s.Where, s.InBag))
                .Select(p => new Place(p.Key.Where, p.Key.InBag, p.Sum(s => s.Stack.Quantity)))
                .OrderBy(p => p.InBag ? 0 : 1).ThenBy(p => p.Where, StringComparer.OrdinalIgnoreCase)
                .ToList());

        // Objets demandés par plus d'une tâche active.
        var demand = tasks.Where(t => !t.Completed)
            .SelectMany(t => descs.GetValueOrDefault(t.TaskId)?.Required.Select(i => (i.ItemId, i.IsCargo)).Distinct() ?? [])
            .GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

        var result = new List<TaskCheck>();
        foreach (var task in tasks)
        {
            var desc = descs.GetValueOrDefault(task.TaskId);
            if (task.Completed)
            {
                result.Add(new TaskCheck(task, desc, TaskVerdict.DejaRendue, []));
                continue;
            }
            if (desc is null)
            {
                result.Add(new TaskCheck(task, null, TaskVerdict.Inconnue, []));
                continue;
            }
            var items = new List<TaskItemCheck>();
            foreach (var item in desc.Required)
            {
                var key = (item.ItemId, item.IsCargo);
                var need = item.Quantity;
                var inBag = 0;
                var elsewhere = new List<(string, int)>();
                foreach (var place in pool.GetValueOrDefault(key) ?? [])
                {
                    if (need <= 0)
                    {
                        break;
                    }
                    var take = Math.Min(need, place.Quantity);
                    if (take <= 0)
                    {
                        continue;
                    }
                    place.Quantity -= take;
                    need -= take;
                    if (place.InBag)
                    {
                        inBag += take;
                    }
                    else
                    {
                        elsewhere.Add((place.Where, take));
                    }
                }
                items.Add(new TaskItemCheck(item, inBag, elsewhere, need, demand.Contains(key)));
            }
            result.Add(new TaskCheck(task, desc, VerdictOf(items), items));
        }
        return result;
    }

    private static TaskVerdict VerdictOf(IReadOnlyList<TaskItemCheck> items)
    {
        if (items.All(i => i.Missing == 0))
        {
            return items.All(i => i.Elsewhere.Count == 0) ? TaskVerdict.FaisableMaintenant : TaskVerdict.FaisableApresBanque;
        }
        return items.Any(i => i.Found > 0) ? TaskVerdict.Partiel : TaskVerdict.Impossible;
    }

    public static string Text(TaskVerdict verdict) => verdict switch
    {
        TaskVerdict.FaisableMaintenant => "Faisable maintenant",
        TaskVerdict.FaisableApresBanque => "Faisable (à aller chercher)",
        TaskVerdict.Partiel => "Partiel",
        TaskVerdict.Impossible => "Impossible",
        TaskVerdict.DejaRendue => "Déjà rendue",
        _ => "Inconnue (données à mettre à jour)",
    };

    private sealed class Place(string where, bool inBag, int quantity)
    {
        public string Where { get; } = where;
        public bool InBag { get; } = inBag;
        public int Quantity { get; set; } = quantity;
    }
}
