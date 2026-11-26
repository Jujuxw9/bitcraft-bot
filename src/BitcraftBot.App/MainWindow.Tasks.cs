using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Journal des niveaux et des tâches (v0.3.19, demande de Julien) : d'après le serveur seulement (D-044).
// « [Métier] Forestry passe niveau 30 », « [Tâche] Début : récolte « Mature Oak Tree »… », « [Tâche] Fin : … ».
public partial class MainWindow
{
    private readonly TaskJournal _taskJournal = new();
    private PlayerSnapshot? _taskSnapshot;

    private void InitTaskJournal()
    {
        _taskJournal.SkillName = id => _gameData.Current?.Skills.GetValueOrDefault(id) ?? $"métier {id}";
        _taskJournal.ItemName = ItemName;
        _taskJournal.Subject = action => TargetResource(action)?.Name;
        _taskJournal.SkillOf = ActionSkill;
    }

    // Métier d'une récolte : celui de la ressource visée (extraction_recipe_desc). null si pas une récolte ou inconnu.
    private int? ActionSkill(ActionInfo action) => TargetResource(action) is { } r
        && _gameData.Current?.Extractions.GetValueOrDefault(r.ResourceId) is { } extraction ? extraction.SkillId : null;

    // Ressource visée par une récolte : d'après la cible de l'action, sinon le dernier coup donné.
    // Seulement pour une récolte (essai v0.3.19 : « DeployDeployable « Young Beech Tree » », faux).
    private NearbyResource? TargetResource(ActionInfo action)
    {
        var s = _taskSnapshot;
        if (action.Type != "Extract")
        {
            return null;
        }
        var target = action.Target ?? s?.LastExtract?.Target;
        return target is { } id && s is not null ? s.Resources.FirstOrDefault(r => r.EntityId == id) : null;
    }

    // Appelé toutes les 500 ms quand le serveur est lu. inventories : déjà calculés pour ce passage.
    private void TrackTasks(PlayerSnapshot s, IReadOnlyList<InventoryInfo> inventories, bool inventoriesKnown)
    {
        _taskSnapshot = s;
        var items = inventoriesKnown && s.EntityId is { } me
            ? inventories.Where(i => i.Owner == me).SelectMany(i => i.Stacks).ToList()
            : null;
        WriteJournalEvents(_taskJournal.Update(new TaskInput(DateTime.Now, s.Skills, s.Action, items, s.Position)));
    }

    // Liaison coupée : la tâche en cours est écrite telle quelle (sans le bilan des objets).
    private void FlushTasks()
    {
        if (_taskJournal.HasRunningTask)
        {
            WriteJournalEvents(_taskJournal.Flush(new TaskInput(DateTime.Now, null, null, null, null)));
        }
    }

    private void WriteJournalEvents(IReadOnlyList<JournalEvent> events)
    {
        foreach (var e in events)
        {
            _log.Info(e.Category, e.Message);
            if (e.Category == "Métier")
            {
                // Le bandeau « niveau gagné » du jeu recouvre la cible à l'écran quelques secondes.
                _hudLogger.NotifyLevelUp(DateTime.Now);
            }
        }
    }
}
