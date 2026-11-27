namespace BitcraftBot.Server.World;

// Reprise automatique des récoltes (v0.4.24, demande de Julien) : quand une récolte (arbre, buisson, plante… jamais une
// fabrication) s'arrête faute d'endurance alors que la ressource n'est pas épuisée, le bot attend que l'endurance
// remonte, puis relance la récolte. Il vérifie sur le serveur que c'est bien cette ressource qui est frappée.
// Le personnage ne bouge pas. Arrêt : ressource épuisée, sac plein, Julien bouge ou fait autre chose, ou M sans effet
// deux fois de suite.
// v0.4.25 : M (Interact) agit sur l'élément que la souris survole (contour vert). Tab est retiré (inutile : l'arbre reste
// sélectionné). Le bot retient le point de l'écran où Julien a cliqué pour lancer la récolte ; pour reprendre, il pose
// la souris sur ce point puis appuie sur M. Ce point n'est valable que si le personnage n'a pas bougé (moins d'une
// demi-case) et si la caméra n'a pas été tournée depuis le clic. Sans point valable : rien n'est appuyé.
public enum ResumeStep { Rien, Interact }

// Clic de Julien dans le jeu (gauche ou droit), en pixels écran, avec la position du personnage et le compteur de
// mouvements de caméra à ce moment-là.
public sealed record ClickPoint(int X, int Y, DateTimeOffset At, ServerPosition? Position, int CameraMoves);

public sealed record ResumeDecision(ResumeStep Step, string State, string? Log = null, bool Alert = false, int X = 0, int Y = 0);

public sealed record ResumeInput
{
    public required DateTimeOffset Now { get; init; }
    public bool Enabled { get; init; }
    public ActionInfo? Action { get; init; }
    public ulong? LastExtractTarget { get; init; }
    public ServerPosition? Position { get; init; }
    public double? Stamina { get; init; }
    public double? MaxStamina { get; init; }
    // Cible sélectionnée dans le jeu (target_state), 0 ou null = aucune. Seulement pour le journal depuis la v0.4.25.
    public ulong? Target { get; init; }
    public IReadOnlyList<NearbyResource> Resources { get; init; } = [];
    public bool BagFull { get; init; }
    // Dernier clic de Julien dans le jeu, et nombre de mouvements de caméra vus depuis le démarrage du bot.
    public ClickPoint? LastClick { get; init; }
    public int CameraMoves { get; init; }
}

public sealed class HarvestResume
{
    // Endurance sous laquelle un arrêt est « faute d'endurance » (sinon Julien a arrêté lui-même : on ne reprend pas).
    public const double StoppedBelow = 0.15;
    // Endurance à retrouver avant de reprendre.
    public const double ResumeAbove = 0.95;
    // Déplacement (cases du jeu) au-delà duquel Julien est parti.
    public const double MovedCases = 1.0;
    // Déplacement au-delà duquel le point de la souris n'est plus sur la ressource (la caméra suit le personnage).
    public const double PointMovedCases = 0.5;
    // Le clic qui lance une récolte arrive au plus ClickBefore avant que le serveur la montre (ou juste après).
    public static readonly TimeSpan ClickBefore = TimeSpan.FromSeconds(8);
    public static readonly TimeSpan ClickAfter = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan RetryPause = TimeSpan.FromMilliseconds(700);
    public static readonly TimeSpan ConfirmTimeout = TimeSpan.FromSeconds(5);
    public const int MaxFailures = 2;

    private enum Phase { Rien, Recolte, Attente, Relance, Confirmation }

    private Phase _phase;
    private ulong _resource;
    private string _name = "";
    private ServerPosition _anchor;
    private int _failures;
    private DateTimeOffset _lastKey;
    private DateTimeOffset _pressedAt;
    private DateTimeOffset _extractSince;
    private ClickPoint? _point;

    public bool Active => _phase != Phase.Rien;

    // Point de la souris retenu pour la ressource suivie (null = aucun).
    public ClickPoint? Point => _point;

    public int Failures => _failures;

    // Le geste n'a pas pu partir (contrôleur qui refuse) : on refera l'essai, sans le compter comme un échec.
    public void NotSent()
    {
        if (_phase == Phase.Confirmation)
        {
            _phase = Phase.Relance;
        }
    }

    // Le point est-il encore sur la ressource ? null = oui, sinon la raison.
    public static string? PointProblem(ClickPoint? point, ServerPosition? now, int cameraMoves)
    {
        if (point is null)
        {
            return "aucun clic retenu";
        }
        if (point.CameraMoves != cameraMoves)
        {
            return "caméra tournée depuis le clic";
        }
        if (point.Position is not { } from || now is not { } p)
        {
            return "position pas lue";
        }
        if (p.ScreenDistanceTo(from) > PointMovedCases)
        {
            return "personnage déplacé depuis le clic";
        }
        return null;
    }

    public ResumeDecision Update(ResumeInput i)
    {
        if (!i.Enabled)
        {
            _phase = Phase.Rien;
            _failures = 0;
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
            var ours = _phase == Phase.Confirmation;
            if (ours)
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
            else if (_phase is not Phase.Recolte || _resource != t)
            {
                // Nouvelle récolte lancée par Julien : compteur remis à zéro (défaut du 27/11 à 00:29), nouveau point.
                _failures = 0;
                _extractSince = i.Now;
                if (t != _resource)
                {
                    _point = null;
                }
            }
            if (!ours && i.LastClick is { } c && c.At >= _extractSince - ClickBefore && c.At <= _extractSince + ClickAfter
                && PointProblem(c, i.Position, i.CameraMoves) is null)
            {
                // Clic de Julien qui a lancé (ou relancé) cette récolte, personnage arrêté : c'est le meilleur point.
                _point = c;
            }
            _phase = alert ? Phase.Rien : Phase.Recolte;
            if (alert)
            {
                _point = null;
            }
            _resource = t;
            _name = Name(i, t);
            _anchor = i.Position ?? _anchor;
            var known = _point is null ? " (pas de clic retenu)" : " (point de la souris retenu)";
            return new(ResumeStep.Rien, alert ? "arrêtée (autre ressource frappée)" : $"suit la récolte de « {_name} »{known}", log, alert);
        }

        // Une autre action (fabrication, marche…) : Julien fait autre chose.
        if (action is not null)
        {
            var was = _phase;
            _phase = Phase.Rien;
            _failures = 0;
            return new(ResumeStep.Rien, "attend une récolte", was is Phase.Attente or Phase.Relance ? $"Reprise de « {_name} » annulée : vous faites autre chose ({action.French})." : null);
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
                    $"Récolte de « {_name} » arrêtée faute d'endurance ({st:0}/{max:0}) : reprise quand l'endurance sera revenue"
                    + (_point is null ? " (attention : aucun clic retenu sur la ressource)." : "."));

            case Phase.Attente:
                if (i.Stamina is { } s2 && i.MaxStamina is { } m2 && s2 >= m2 * ResumeAbove)
                {
                    _phase = Phase.Relance;
                    return Relaunch(i);
                }
                return new(ResumeStep.Rien, $"attend l'endurance ({i.Stamina ?? 0:0}/{i.MaxStamina ?? 0:0}) pour reprendre « {_name} »");

            case Phase.Relance:
                return Relaunch(i);

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
                _phase = Phase.Relance;
                return new(ResumeStep.Rien, "nouvel essai", $"Reprise de « {_name} » : M n'a rien lancé, nouvel essai.");
        }
        return new(ResumeStep.Rien, "attend une récolte");
    }

    // Relance : souris posée sur le point retenu, puis Interact (M). Sans point valable : rien n'est appuyé.
    private ResumeDecision Relaunch(ResumeInput i)
    {
        if (i.Now - _lastKey < RetryPause)
        {
            return new(ResumeStep.Rien, $"relance « {_name} »…");
        }
        if (PointProblem(_point, i.Position, i.CameraMoves) is { } why)
        {
            return Stop($"Reprise de « {_name} » impossible ({why}) : cliquez une fois sur la ressource, le bot retiendra ce point.",
                state: "reprise impossible : cliquez une fois sur l'arbre");
        }
        _phase = Phase.Confirmation;
        _pressedAt = i.Now;
        _lastKey = i.Now;
        var point = _point!;
        return new(ResumeStep.Interact, $"relance « {_name} »…",
            $"Reprise : souris posée sur « {_name} » (point du clic en ({point.X}, {point.Y})), puis touche Interact.", X: point.X, Y: point.Y);
    }

    private ResumeDecision Stop(string? log, bool alert = false, string state = "attend une récolte")
    {
        _phase = Phase.Rien;
        _failures = 0;
        return new(ResumeStep.Rien, state, log, alert);
    }

    private static string Name(ResumeInput i, ulong id) =>
        i.Resources.FirstOrDefault(r => r.EntityId == id)?.Name ?? $"entité {id}";
}
