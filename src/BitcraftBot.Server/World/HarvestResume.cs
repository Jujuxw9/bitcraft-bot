namespace BitcraftBot.Server.World;

// Reprise automatique des récoltes (v0.4.24, demande de Julien) : quand une récolte (arbre, buisson, plante… jamais une
// fabrication) s'arrête faute d'endurance alors que la ressource n'est pas épuisée, le bot attend que l'endurance
// remonte, sélectionne la ressource avec Tab (le serveur dit quelle cible est sélectionnée : target_state), puis appuie
// sur Interact (M). Il vérifie sur le serveur que c'est bien cette ressource qui est frappée. Le personnage ne bouge pas.
// Arrêt : ressource épuisée, sac plein, Julien bouge ou fait autre chose, ou essai raté deux fois.
public enum ResumeStep { Rien, Tab, Interact }

public sealed record ResumeDecision(ResumeStep Step, string State, string? Log = null, bool Alert = false);

public sealed record ResumeInput
{
    public required DateTimeOffset Now { get; init; }
    public bool Enabled { get; init; }
    public ActionInfo? Action { get; init; }
    public ulong? LastExtractTarget { get; init; }
    public ServerPosition? Position { get; init; }
    public double? Stamina { get; init; }
    public double? MaxStamina { get; init; }
    // Cible sélectionnée dans le jeu (target_state), 0 ou null = aucune.
    public ulong? Target { get; init; }
    public IReadOnlyList<NearbyResource> Resources { get; init; } = [];
    public bool BagFull { get; init; }
}

public sealed class HarvestResume
{
    // Endurance sous laquelle un arrêt est « faute d'endurance » (sinon Julien a arrêté lui-même : on ne reprend pas).
    public const double StoppedBelow = 0.15;
    // Endurance à retrouver avant de reprendre.
    public const double ResumeAbove = 0.95;
    // Déplacement (cases du jeu) au-delà duquel Julien est parti.
    public const double MovedCases = 1.0;
    public const int MaxTabs = 8;
    public static readonly TimeSpan TabPause = TimeSpan.FromMilliseconds(700);
    public static readonly TimeSpan ConfirmTimeout = TimeSpan.FromSeconds(5);
    public const int MaxFailures = 2;

    private enum Phase { Rien, Recolte, Attente, Selection, Confirmation }

    private Phase _phase;
    private ulong _resource;
    private string _name = "";
    private ServerPosition _anchor;
    private int _tabs;
    private int _failures;
    private DateTimeOffset _lastKey;
    private DateTimeOffset _pressedAt;

    public bool Active => _phase != Phase.Rien;

    public ResumeDecision Update(ResumeInput i)
    {
        if (!i.Enabled)
        {
            _phase = Phase.Rien;
            return new(ResumeStep.Rien, "coupée");
        }
        var action = i.Action;

        // Une récolte est en cours : c'est elle qu'on reprendra.
        if (action?.Type == "Extract")
        {
            var target = action.Target ?? i.LastExtractTarget;
            if (target is not { } t || t == 0)
            {
                return new(ResumeStep.Rien, "récolte en cours (ressource pas encore lue)");
            }
            string? log = null;
            var alert = false;
            if (_phase == Phase.Confirmation)
            {
                if (t == _resource)
                {
                    log = $"Récolte de « {_name} » reprise (confirmé par le serveur).";
                    _failures = 0;
                }
                else
                {
                    log = $"Reprise : M a lancé la récolte d'une autre ressource (« {Name(i, t)} ») au lieu de « {_name} ». Reprise arrêtée pour cette ressource.";
                    alert = true;
                }
            }
            if (_phase is not Phase.Recolte || _resource != t)
            {
                _tabs = 0;
                if (t != _resource)
                {
                    _failures = 0;
                }
            }
            _phase = alert ? Phase.Rien : Phase.Recolte;
            _resource = t;
            _name = Name(i, t);
            _anchor = i.Position ?? _anchor;
            return new(ResumeStep.Rien, alert ? "arrêtée (autre ressource frappée)" : $"suit la récolte de « {_name} »", log, alert);
        }

        // Une autre action (fabrication, marche…) : Julien fait autre chose.
        if (action is not null)
        {
            var was = _phase;
            _phase = Phase.Rien;
            return new(ResumeStep.Rien, "attend une récolte", was is Phase.Attente or Phase.Selection ? $"Reprise de « {_name} » annulée : vous faites autre chose ({action.French})." : null);
        }

        if (_phase == Phase.Rien)
        {
            return new(ResumeStep.Rien, "attend une récolte");
        }

        // Plus d'action : on regarde pourquoi, et si on peut reprendre.
        var resource = i.Resources.FirstOrDefault(r => r.EntityId == _resource);
        if (resource is null || resource.Health is <= 0)
        {
            return Stop($"« {_name} » est épuisée (ou n'est plus lue) : rien à reprendre.");
        }
        if (i.Position is { } p && p.ScreenDistanceTo(_anchor) > MovedCases)
        {
            return Stop($"Reprise de « {_name} » annulée : vous vous êtes déplacé.");
        }
        if (i.BagFull)
        {
            return Stop($"Reprise de « {_name} » arrêtée : sac plein.", alert: true);
        }

        switch (_phase)
        {
            case Phase.Recolte:
                if (i.Stamina is not { } st || i.MaxStamina is not { } max || max <= 0)
                {
                    return Stop("Reprise impossible : endurance pas lue.");
                }
                if (st > max * StoppedBelow)
                {
                    // Arrêt voulu par Julien (endurance encore là) : on ne relance pas.
                    return Stop(null);
                }
                _phase = Phase.Attente;
                return new(ResumeStep.Rien, $"attend l'endurance pour reprendre « {_name} »",
                    $"Récolte de « {_name} » arrêtée faute d'endurance ({st:0}/{max:0}) : reprise quand l'endurance sera revenue.");

            case Phase.Attente:
                if (i.Stamina is { } s2 && i.MaxStamina is { } m2 && s2 >= m2 * ResumeAbove)
                {
                    _phase = Phase.Selection;
                    _tabs = 0;
                    return Select(i);
                }
                return new(ResumeStep.Rien, $"attend l'endurance ({i.Stamina ?? 0:0}/{i.MaxStamina ?? 0:0}) pour reprendre « {_name} »");

            case Phase.Selection:
                return Select(i);

            case Phase.Confirmation:
                if (i.Now - _pressedAt < ConfirmTimeout)
                {
                    return new(ResumeStep.Rien, $"vérifie la reprise de « {_name} »…");
                }
                _failures++;
                if (_failures >= MaxFailures)
                {
                    return Stop($"Reprise de « {_name} » : M n'a rien lancé {MaxFailures} fois de suite. Reprise arrêtée.", alert: true);
                }
                _phase = Phase.Selection;
                _tabs = 0;
                return new(ResumeStep.Rien, "nouvel essai", $"Reprise de « {_name} » : M n'a rien lancé, nouvel essai.");
        }
        return new(ResumeStep.Rien, "attend une récolte");
    }

    // Sélection de la ressource : Tab jusqu'à ce que le serveur montre la bonne cible, puis Interact.
    private ResumeDecision Select(ResumeInput i)
    {
        if (i.Now - _lastKey < TabPause)
        {
            return new(ResumeStep.Rien, $"sélectionne « {_name} »…");
        }
        if (i.Target == _resource)
        {
            _phase = Phase.Confirmation;
            _pressedAt = i.Now;
            _lastKey = i.Now;
            return new(ResumeStep.Interact, $"relance « {_name} »…", $"Reprise : « {_name} » sélectionnée (confirmé par le serveur), touche Interact.");
        }
        if (_tabs >= MaxTabs)
        {
            return Stop($"Reprise de « {_name} » : pas sélectionnée après {MaxTabs} appuis sur Tab (cible du jeu : {(i.Target is { } t && t != 0 ? Name(i, t) : "aucune")}). Reprise arrêtée.", alert: true);
        }
        _tabs++;
        _lastKey = i.Now;
        return new(ResumeStep.Tab, $"sélectionne « {_name} » (Tab {_tabs}/{MaxTabs})…");
    }

    private ResumeDecision Stop(string? log, bool alert = false)
    {
        _phase = Phase.Rien;
        return new(ResumeStep.Rien, "attend une récolte", log, alert);
    }

    private static string Name(ResumeInput i, ulong id) =>
        i.Resources.FirstOrDefault(r => r.EntityId == id)?.Name ?? $"entité {id}";
}
