namespace BitcraftBot.Server.World;

// Événement à écrire dans le journal : catégorie (« Métier », « Tâche ») et texte.
public sealed record JournalEvent(string Category, string Message);

// Ce que le serveur dit à un instant donné, utile au journal des tâches.
// Items : tous les objets du personnage (sac, ceinture…), null s'ils ne sont pas lus.
public sealed record TaskInput(
    DateTime Now,
    IReadOnlyList<SkillProgress>? Skills,
    ActionInfo? Action,
    IReadOnlyList<ItemStackInfo>? Items,
    ServerPosition? Position);

// Journal des niveaux et des tâches (v0.3.19, demande de Julien), d'après le serveur seulement (D-044).
// Une ligne par événement : passage de niveau, début de tâche, fin de tâche. Jamais une ligne par seconde.
// Une tâche = une suite d'actions du même type sur la même cible (ou la même recette) : couper un arbre,
// miner une roche, fabriquer… Elle finit quand le personnage ne fait plus rien pendant EndGrace, ou change de cible.
public sealed class TaskJournal
{
    // Actions qui ne sont pas des tâches (déplacements, gestes, repas déjà écrits par le repas automatique).
    private static readonly HashSet<string> Ignored =
        ["None", "PlayerMove", "DeployableMove", "StationaryEmote", "MobileEmote", "Climb", "UseElevator", "Teleport", "UseItem", "Death"];

    private readonly Dictionary<int, int> _levels = [];
    private readonly Dictionary<int, int> _xp = [];
    private Running? _task;

    // Entre deux coups, le serveur laisse un court vide : la tâche n'est finie qu'après ce délai sans action.
    public TimeSpan EndGrace { get; init; } = TimeSpan.FromSeconds(3);

    public Func<int, string> SkillName { get; set; } = id => $"métier {id}";

    // Ressource frappée ou recette fabriquée (null si inconnue).
    public Func<ActionInfo, string?> Subject { get; set; } = _ => null;

    // Métier de l'action d'après les données du jeu (null si inconnu : la fin de tâche le donne par l'XP).
    public Func<ActionInfo, int?> SkillOf { get; set; } = _ => null;

    public Func<int, bool, string> ItemName { get; set; } = (id, cargo) => cargo ? $"cargaison n° {id}" : $"objet n° {id}";

    public bool HasRunningTask => _task is not null;

    private sealed class Running
    {
        public required string Type { get; init; }
        public required ulong? Target { get; init; }
        public required int? RecipeId { get; init; }
        public required string Label { get; init; }
        public required DateTime StartedAt { get; init; }
        public required Dictionary<int, int> XpAtStart { get; init; }
        public Dictionary<(int Id, bool Cargo), int>? ItemsAtStart { get; set; }
        public DateTime LastSeen { get; set; }
        public HashSet<long> Starts { get; } = [];
    }

    public IReadOnlyList<JournalEvent> Update(TaskInput input)
    {
        var events = new List<JournalEvent>();
        TrackLevels(input.Skills, events);

        var action = input.Action is { } a && !Ignored.Contains(a.Type) ? a : null;
        if (_task is { } running && action is not null
            && (running.Type != action.Type || running.Target != action.Target || running.RecipeId != action.RecipeId))
        {
            End(input, events);
        }
        if (action is not null)
        {
            if (_task is null)
            {
                Start(action, input, events);
            }
            _task!.LastSeen = input.Now;
            _task.Starts.Add(action.StartMs);
            // Le sac peut arriver après le début de la tâche : on garde le premier lu.
            _task.ItemsAtStart ??= Count(input.Items);
        }
        else if (_task is { } idle && input.Now - idle.LastSeen >= EndGrace)
        {
            End(input, events);
        }
        return events;
    }

    // Fin forcée (déconnexion du serveur) : la tâche en cours est écrite telle quelle.
    public IReadOnlyList<JournalEvent> Flush(TaskInput input)
    {
        var events = new List<JournalEvent>();
        if (_task is not null)
        {
            End(input, events);
        }
        return events;
    }

    private void TrackLevels(IReadOnlyList<SkillProgress>? skills, List<JournalEvent> events)
    {
        if (skills is null)
        {
            return;
        }
        foreach (var s in skills)
        {
            var level = s.Level;
            if (_levels.TryGetValue(s.SkillId, out var before) && level > before)
            {
                events.Add(new JournalEvent("Métier", $"{SkillName(s.SkillId)} passe niveau {level} (était {before})."));
            }
            _levels[s.SkillId] = level;
            _xp[s.SkillId] = s.Experience;
        }
    }

    private void Start(ActionInfo action, TaskInput input, List<JournalEvent> events)
    {
        var subject = Subject(action);
        var label = subject is null ? action.French : $"{action.French} « {subject} »";
        _task = new Running
        {
            Type = action.Type,
            Target = action.Target,
            RecipeId = action.RecipeId,
            Label = label,
            StartedAt = input.Now,
            XpAtStart = new Dictionary<int, int>(_xp),
            ItemsAtStart = Count(input.Items),
            LastSeen = input.Now,
        };
        var parts = new List<string> { $"Début : {label}" };
        if (SkillOf(action) is { } skill)
        {
            parts.Add(_levels.TryGetValue(skill, out var lvl) ? $"{SkillName(skill)} niv. {lvl}" : SkillName(skill));
        }
        if (input.Position is { } p)
        {
            parts.Add($"à {p.ScreenText}");
        }
        events.Add(new JournalEvent("Tâche", string.Join(", ", parts) + "."));
    }

    private void End(TaskInput input, List<JournalEvent> events)
    {
        var task = _task!;
        _task = null;
        var duration = task.LastSeen - task.StartedAt;
        var parts = new List<string>
        {
            $"Fin : {task.Label}",
            FormatDuration(duration),
            $"{task.Starts.Count} {(task.Type is "Extract" or "Attack" ? "coup" : "étape")}{(task.Starts.Count > 1 ? "s" : "")}",
        };
        var xp = _xp.Select(kv => (Skill: kv.Key, Gain: kv.Value - task.XpAtStart.GetValueOrDefault(kv.Key, kv.Value)))
            .Where(x => x.Gain > 0)
            .Select(x => $"{SkillName(x.Skill)} +{x.Gain} XP")
            .ToList();
        parts.Add(xp.Count > 0 ? string.Join(", ", xp) : "aucune XP");
        var after = Count(input.Items);
        if (task.ItemsAtStart is { } beforeItems && after is not null)
        {
            var diff = after.Keys.Union(beforeItems.Keys)
                .Select(k => (Key: k, Delta: after.GetValueOrDefault(k) - beforeItems.GetValueOrDefault(k)))
                .Where(x => x.Delta != 0)
                .OrderByDescending(x => x.Delta)
                .Select(x => $"{(x.Delta > 0 ? "+" : "−")}{Math.Abs(x.Delta)} {ItemName(x.Key.Id, x.Key.Cargo)}")
                .ToList();
            parts.Add(diff.Count > 0 ? "objets : " + string.Join(", ", diff) : "aucun objet gagné");
        }
        events.Add(new JournalEvent("Tâche", string.Join(", ", parts) + "."));
    }

    private static Dictionary<(int Id, bool Cargo), int>? Count(IReadOnlyList<ItemStackInfo>? items) =>
        items?.GroupBy(i => (i.ItemId, i.IsCargo)).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

    private static string FormatDuration(TimeSpan d) =>
        d.TotalMinutes >= 1 ? $"{(int)d.TotalMinutes} min {d.Seconds:00} s" : $"{Math.Max(0, d.TotalSeconds):0} s";
}
