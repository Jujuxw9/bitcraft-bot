using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Voyageurs » (v0.4.11, demande de Julien) : pour chaque tâche de voyageur du personnage,
// les objets demandés, le stock (sac, ceinture, banques) et un verdict. Lecture seule du serveur.
public partial class MainWindow
{
    private static readonly TimeSpan TravelerInterval = TimeSpan.FromSeconds(2);

    private DateTime _travelerShownAt = DateTime.MinValue;
    private string _travelerSignature = "";

    // Une ligne par objet demandé ; le voyageur, la tâche et le verdict ne sont écrits qu'à la première ligne de la tâche.
    private sealed record TravelerRow(string Traveler, string Task, string Verdict, string Item, string Tier,
        string Wanted, string InBag, string Elsewhere, string Missing);

    // Bot pas relié au serveur : rien à comparer.
    private void ShowTravelersOffline()
    {
        _travelerSignature = "";
        TravelerSummary.Text = "Serveur non lu : connectez le bot (avant le jeu), onglet PARAMÈTRES.";
        TravelerList.ItemsSource = null;
    }

    private void ShowTravelers(ServerReader reader, PlayerSnapshot s, IReadOnlyList<InventoryInfo> inventories)
    {
        if (DateTime.Now - _travelerShownAt < TravelerInterval)
        {
            return;
        }
        _travelerShownAt = DateTime.Now;
        if (reader.TasksRefused)
        {
            TravelerSummary.Text = "Le serveur a refusé la lecture des tâches de voyageur (journal).";
            TravelerList.ItemsSource = null;
            return;
        }
        if (reader.InventoryRefused || s.EntityId is null)
        {
            TravelerSummary.Text = reader.InventoryRefused ? "Stock non lu : le serveur a refusé la lecture du sac et des banques (journal)." : "Personnage pas encore trouvé.";
            TravelerList.ItemsSource = null;
            return;
        }
        var store = _gameData.Current;
        if (store is null || store.TravelerTasks.Count == 0)
        {
            TravelerSummary.Text = "Données du jeu sur les tâches pas encore téléchargées (redémarrer le bot avec Internet).";
            TravelerList.ItemsSource = null;
            return;
        }

        var banks = reader.BankPositions();
        var stock = inventories
            .SelectMany(inv =>
            {
                var where = Describe(inv, s, banks);
                var inBag = inv.Owner == s.EntityId && inv.Index == 0;
                return inv.Stacks.Select(st => new StockStack(where, inBag, st));
            })
            .ToList();
        var tasks = reader.TravelerTasks();
        var signature = string.Join("|", tasks.Select(t => $"{t.EntityId}:{t.TaskId}:{t.Completed}"))
                        + "#" + string.Join("|", stock.Select(x => $"{x.Where}:{x.Stack.ItemId}:{x.Stack.IsCargo}:{x.Stack.Quantity}"));
        if (signature == _travelerSignature)
        {
            return;
        }
        _travelerSignature = signature;

        if (tasks.Count == 0)
        {
            TravelerSummary.Text = $"Aucune tâche de voyageur reçue du serveur. Mis à jour à {DateTime.Now:HH:mm:ss}.";
            TravelerList.ItemsSource = null;
            return;
        }

        var checks = TravelerTaskPlanner.Check(tasks, store.TravelerTasks, stock);
        var rows = new List<TravelerRow>();
        foreach (var c in checks)
        {
            var traveler = store.Travelers.GetValueOrDefault(c.Task.TravelerId) ?? $"voyageur n° {c.Task.TravelerId}";
            var task = c.Desc?.Description is { Length: > 0 } d ? d : $"tâche n° {c.Task.TaskId}";
            var verdict = TravelerTaskPlanner.Text(c.Verdict);
            if (c.Items.Count == 0)
            {
                rows.Add(new TravelerRow(traveler, task, verdict, "", "", "", "", "", ""));
                continue;
            }
            var first = true;
            foreach (var i in c.Items)
            {
                var key = (i.Item.IsCargo ? "C" : "I") + i.Item.ItemId;
                var tier = _recipes?.Entries.TryGetValue(key, out var e) == true ? e.Tier.ToString() : "?";
                var elsewhere = string.Join(", ", i.Elsewhere.Select(x => $"{x.Quantity:N0} : {x.Where}"));
                rows.Add(new TravelerRow(first ? traveler : "", first ? task : "", first ? verdict : "",
                    ItemName(i.Item.ItemId, i.Item.IsCargo) + (i.Shared ? " (aussi demandé par une autre tâche)" : ""),
                    tier, i.Item.Quantity.ToString("N0"), i.InBag.ToString("N0"), elsewhere,
                    i.Missing > 0 ? i.Missing.ToString("N0") : "—"));
                first = false;
            }
        }
        TravelerList.ItemsSource = rows;

        var active = checks.Where(c => c.Verdict != TaskVerdict.DejaRendue).ToList();
        int Count(TaskVerdict v) => active.Count(c => c.Verdict == v);
        TravelerSummary.Text = active.Count == 0
            ? $"Toutes les tâches sont déjà rendues. Mis à jour à {DateTime.Now:HH:mm:ss}."
            : $"{active.Count} tâches actives : {Count(TaskVerdict.FaisableMaintenant)} faisables maintenant, " +
              $"{Count(TaskVerdict.FaisableApresBanque)} à aller chercher, {Count(TaskVerdict.Partiel)} partielles, " +
              $"{Count(TaskVerdict.Impossible)} impossibles. Mis à jour à {DateTime.Now:HH:mm:ss}.";
    }
}
