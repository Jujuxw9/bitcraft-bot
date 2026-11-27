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
    ServerPosition? Position,
    IReadOnlyList<AbilityInfo>? Abilities = null);

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

    // v0.4.31 (T-13, D-079) : conseil en fin de pêche ou de minage quand les capacités du métier ne sont jamais parties.
    // Métiers concernés (nom du jeu → nom de l'activité dans le conseil).
    public static readonly IReadOnlyDictionary<string, string> AdvisedSkills = new Dictionary<string, string>
    {
        ["Mining"] = "Minage",
        ["Fishing"] = "Pêche",
    };

    // Durée minimale d'une tâche pour un conseil (un coup isolé ne compte pas).
    public static readonly TimeSpan AdviceMinDuration = TimeSpan.FromSeconds(20);

    // Marge pour une capacité lancée juste avant le premier coup (horloge du PC et du serveur pas tout à fait égales).
    public static readonly TimeSpan AbilityEarlyMargin = TimeSpan.FromSeconds(5);

    // Capacités spéciales qui aident ce métier et qui sont sur la barre du joueur (id, nom). Vide = pas de conseil.
    public Func<int, IReadOnlyList<(int Id, string Name)>> SkillAbilities { get; set; } = _ => [];

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
        public int? Skill { get; init; }
        // Début de recharge de chaque capacité spéciale vu au début de la tâche, et capacités lancées depuis.
        public Dictionary<int, long>? CooldownsAtStart { get; set; }
        public HashSet<int> AbilitiesUsed { get; } = [];
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
            TrackAbilities(_task, input);
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
            Skill = SkillOf(action),
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
        if (Advice(task, duration) is { } advice)
        {
            events.Add(new JournalEvent("Conseil", advice));
        }
    }

    // Capacité spéciale lancée pendant la tâche : sa recharge a recommencé (début de recharge changé depuis le
    // début de la tâche, ou tout juste avant le premier coup).
    private static void TrackAbilities(Running task, TaskInput input)
    {
        if (input.Abilities is not { } abilities)
        {
            return;
        }
        var special = abilities.Where(a => a.Kind == AbilityKind.Speciale).ToList();
        if (task.CooldownsAtStart is null)
        {
            task.CooldownsAtStart = special.GroupBy(a => a.Value).ToDictionary(g => g.Key, g => g.Max(a => a.CooldownStartMs));
            var early = new DateTimeOffset(task.StartedAt).ToUnixTimeMilliseconds() - (long)AbilityEarlyMargin.TotalMilliseconds;
            foreach (var a in special.Where(a => a.CooldownStartMs > 0 && a.CooldownStartMs >= early))
            {
                task.AbilitiesUsed.Add(a.Value);
            }
            return;
        }
        foreach (var a in special)
        {
            if (a.CooldownStartMs > task.CooldownsAtStart.GetValueOrDefault(a.Value, long.MinValue))
            {
                task.AbilitiesUsed.Add(a.Value);
            }
        }
    }

    // « Minage de 3 min 05 s sans Heavy Swings : pensez à l'allumer (Capacités auto). » ou null.
    private string? Advice(Running task, TimeSpan duration)
    {
        // Capacités jamais lues pendant la tâche (déconnexion…) : pas de conseil, faute de preuve.
        if (duration < AdviceMinDuration || task.CooldownsAtStart is null)
        {
            return null;
        }
        var skill = task.Skill ?? _xp.Select(kv => (kv.Key, Gain: kv.Value - task.XpAtStart.GetValueOrDefault(kv.Key, kv.Value)))
            .Where(x => x.Gain > 0).OrderByDescending(x => x.Gain).Select(x => (int?)x.Key).FirstOrDefault();
        if (skill is not { } id || !AdvisedSkills.TryGetValue(SkillName(id), out var activity))
        {
            return null;
        }
        var abilities = SkillAbilities(id);
        if (abilities.Count == 0 || abilities.Any(a => task.AbilitiesUsed.Contains(a.Id)))
        {
            return null;
        }
        var names = string.Join(" ni ", abilities.Select(a => a.Name).Distinct());
        var them = abilities.Count > 1 ? "les allumer" : "l'allumer";
        return $"{activity} de {FormatDuration(duration)} sans {names} : pensez à {them} (Capacités auto).";
    }

    private static Dictionary<(int Id, bool Cargo), int>? Count(IReadOnlyList<ItemStackInfo>? items) =>
        items?.GroupBy(i => (i.ItemId, i.IsCargo)).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

    private static string FormatDuration(TimeSpan d) =>
        d.TotalMinutes >= 1 ? $"{(int)d.TotalMinutes} min {d.Seconds:00} s" : $"{Math.Max(0, d.TotalSeconds):0} s";
}
