using BitcraftBot.Server.World;

namespace BitcraftBot.Server.Aim;

public enum CameraState
{
    // Pas encore vu W, Q, D ni la molette depuis le démarrage du bot : orientation supposée de référence.
    Inconnue,

    // W appuyé (orientation et zoom de référence), rien touché depuis.
    Recentree,

    // Q, D, molette ou bouton du milieu depuis le dernier W : les clics ne servent pas.
    Tournee,
}

// Ce que le bot écrit dans le journal (catégorie « Caméra »).
public sealed record CameraEvent(bool Important, string Message);

// R1 (v0.4.0) : apprend la formule de la caméra en regardant Julien jouer. AUCUNE action.
// Un clic droit de Julien dans le jeu, puis le serveur dit quelle ressource le personnage frappe :
// la paire « pixel cliqué / position de la ressource » sert d'indice (D-049) pour la formule de la caméra.
// Paire gardée seulement si :
// - caméra pas tournée (W appuyé depuis Q, D ou la molette, ou rien touché depuis le démarrage) ;
// - personnage immobile au moment du clic ;
// - le serveur confirme une NOUVELLE récolte dans les 20 s, sur une ressource qu'il connaît.
// Puis le calcul de la formule écarte les paires qui ne s'accordent pas avec les autres.
public sealed class CameraLearner
{
    public static readonly TimeSpan ConfirmWindow = TimeSpan.FromSeconds(20);

    // Après W, la caméra du jeu revient en douceur : 1 s d'attente avant de croire les clics (Julien, D-050 corrigée le 2026-11-22).
    public static readonly TimeSpan ResetSettle = TimeSpan.FromSeconds(1);

    // Personnage immobile : déplacé de moins de ça (unités de l'écran) dans la seconde avant le clic.
    public const double StillDistance = 0.4;

    // Ressource trop loin pour être un clic sur elle (unités de l'écran).
    public const double MaxDistance = 40;

    public const int MaxPairs = 300;

    private readonly List<CameraPair> _pairs = [];
    private readonly LinkedList<(DateTime At, ServerPosition Position)> _history = new();
    private Pending? _pending;

    private sealed record Pending(DateTime At, int X, int Y, int Width, int Height, ActionInfo? Action, ExtractInfo? Extract, double? Heading);

    public CameraState State { get; private set; } = CameraState.Inconnue;

    public DateTime StateSince { get; private set; } = DateTime.MinValue;

    // Ce qui a tourné la caméra (« Q », « D », « la molette »…).
    public string? MovedBy { get; private set; }

    public IReadOnlyList<CameraPair> Pairs => _pairs;

    public CameraFit? Fit { get; private set; }

    public CameraPair? LastPair { get; private set; }

    // Écart entre le dernier clic appris et la formule calculée SANS lui (le vrai test de la formule).
    public double? LastPredictionErrorPx { get; private set; }

    public bool WaitingConfirmation => _pending is not null;

    public void Load(IEnumerable<CameraPair> pairs)
    {
        _pairs.Clear();
        _pairs.AddRange(pairs.Where(p => p.ClientHeight > 0).TakeLast(MaxPairs));
        Recompute();
    }

    public void Forget()
    {
        _pairs.Clear();
        _pending = null;
        LastPair = null;
        LastPredictionErrorPx = null;
        Fit = null;
    }

    // W : caméra remise en orientation et zoom de référence (Julien).
    public void OnReset(DateTime now)
    {
        State = CameraState.Recentree;
        StateSince = now;
        MovedBy = null;
    }

    // Q, D (rotation), molette (zoom), bouton du milieu (rotation à la souris ? à vérifier).
    public CameraEvent? OnMoved(DateTime now, string by)
    {
        var was = State;
        State = CameraState.Tournee;
        StateSince = now;
        MovedBy = by;
        return was == CameraState.Tournee ? null : new CameraEvent(false, $"Caméra tournée ({by}) : clics ignorés jusqu'au prochain W.");
    }

    // Clic droit de Julien dans la zone utile du jeu. x, y : depuis le coin haut gauche de cette zone.
    // heading : cap de la caméra lu sur la mini-carte au moment du clic (v0.4.3), null si pas lu.
    public CameraEvent? OnRightClick(DateTime now, int x, int y, int width, int height, PlayerSnapshot? s, double? heading = null)
    {
        if (s?.Position is null)
        {
            return new CameraEvent(false, "Clic droit ignoré : position du personnage inconnue (serveur pas lu).");
        }
        if (State == CameraState.Tournee)
        {
            _pending = null;
            return new CameraEvent(false, $"Clic droit ignoré : caméra tournée ({MovedBy}), appuyer sur W.");
        }
        if (State == CameraState.Recentree && now - StateSince < ResetSettle)
        {
            _pending = null;
            return new CameraEvent(false, "Clic droit ignoré : W appuyé il y a moins de 1 s (la caméra revient en douceur).");
        }
        // Un nouveau clic remplace le précédent : c'est le dernier qui compte pour le jeu.
        _pending = new Pending(now, x, y, width, height, s.Action, s.LastExtract, heading);
        return null;
    }

    // Appelé à chaque lecture du serveur (500 ms).
    public IReadOnlyList<CameraEvent> Update(DateTime now, PlayerSnapshot s)
    {
        var events = new List<CameraEvent>();
        if (s.Position is { } p)
        {
            _history.AddLast((now, p));
            while (_history.Count > 0 && now - _history.First!.Value.At > TimeSpan.FromSeconds(30))
            {
                _history.RemoveFirst();
            }
        }
        if (_pending is not { } pending)
        {
            return events;
        }
        if (now - pending.At > ConfirmWindow)
        {
            _pending = null;
            events.Add(new CameraEvent(false, "Clic droit sans récolte dans les 20 s : pas appris."));
            return events;
        }
        if (NewTarget(pending, s) is not { } target)
        {
            return events;
        }
        _pending = null;
        var resource = s.Resources.FirstOrDefault(r => r.EntityId == target);
        if (resource is null)
        {
            events.Add(new CameraEvent(false, $"Récolte confirmée mais ressource n° {target} inconnue : pas apprise."));
            return events;
        }
        if (PositionAt(pending.At) is not { } at)
        {
            events.Add(new CameraEvent(false, "Clic droit pas appris : position du personnage au moment du clic inconnue."));
            return events;
        }
        if (!StillAround(pending.At))
        {
            events.Add(new CameraEvent(false, $"Clic sur « {resource.Name} » pas appris : personnage en mouvement au moment du clic."));
            return events;
        }
        var pair = new CameraPair
        {
            At = pending.At,
            East = resource.Position.ScreenEast - at.ScreenEast,
            North = resource.Position.ScreenNorth - at.ScreenNorth,
            PixelX = pending.X,
            PixelY = pending.Y,
            ClientWidth = pending.Width,
            ClientHeight = pending.Height,
            Resource = resource.Name,
            Tag = resource.Tag,
            EntityId = resource.EntityId,
            Heading = pending.Heading,
        };
        var distance = Math.Sqrt(pair.East * pair.East + pair.North * pair.North);
        if (distance > MaxDistance)
        {
            events.Add(new CameraEvent(false, $"Clic sur « {resource.Name} » pas appris : ressource à {distance:0} cases, trop loin."));
            return events;
        }
        // Le vrai test : la formule calculée AVANT ce clic le prévoyait-elle ?
        LastPredictionErrorPx = Fit?.ErrorPx(pair);
        _pairs.Add(pair);
        if (_pairs.Count > MaxPairs)
        {
            _pairs.RemoveAt(0);
        }
        LastPair = pair;
        Recompute();
        var direction = ResourceNames.Direction(pair.East, pair.North);
        var before = LastPredictionErrorPx is { } e ? $" Formule d'avant : écart {e:0} px." : "";
        events.Add(new CameraEvent(true,
            $"Clic appris n° {_pairs.Count} : « {resource.Name} » à {distance:0.0} cases {direction}, pixel ({pair.PixelX}, {pair.PixelY}), caméra {(pair.Heading is { } hd ? $"vers {hd:0}°" : "direction pas lue")}.{before} {Summary()}"));
        return events;
    }

    // « Précision 14 px (12 clics gardés, 1 écarté) » : pour le journal et la carte Caméra.
    public string Summary()
    {
        if (Fit is not { } f)
        {
            var need = CameraFit.MinimumPairs(CameraFormula.Plate);
            return _pairs.Count < need ? $"Formule : {_pairs.Count} clic(s) sur {need} minimum." : "Formule : impossible (clics alignés ?).";
        }
        var precision = f.MedianErrorPx is not { } m ? "précision pas encore mesurable (5 clics)"
            : f.Reliable ? $"précision {m:0} px" : $"PAS FIABLE (précision {m:0} px sur trop peu de clics gardés)";
        var rejected = f.Rejected > 0 ? $", {f.Rejected} écarté(s)" : "";
        return $"Formule {(f.Formula == CameraFormula.Plate ? "plate" : "perspective")}, {precision} ({f.Used} clic(s) gardé(s){rejected}).";
    }

    private void Recompute() => Fit = CameraFit.Compute(_pairs);

    // Nouvelle récolte depuis le clic : une action « Extract » qui n'est pas la même que celle en cours au moment du clic,
    // ou un nouveau coup sur une autre ressource. L'arbre qu'on coupait déjà ne compte pas (sinon chaque coup le confirmerait).
    private static ulong? NewTarget(Pending pending, PlayerSnapshot s)
    {
        var oldTarget = pending.Action is { Type: "Extract" } a ? a.Target ?? pending.Extract?.Target : null;
        if (s.Action is { Type: "Extract" } now && now.StartMs != pending.Action?.StartMs)
        {
            var target = now.Target ?? s.LastExtract?.Target;
            if (target is { } t && t != oldTarget)
            {
                return t;
            }
        }
        if (s.LastExtract is { } hit && hit.Target != pending.Extract?.Target && hit.Target != oldTarget)
        {
            return hit.Target;
        }
        return null;
    }

    private ServerPosition? PositionAt(DateTime at)
    {
        ServerPosition? best = null;
        foreach (var (time, position) in _history)
        {
            if (time > at)
            {
                // Pas de lecture avant le clic : la première après (au plus 1 s plus tard) fait l'affaire.
                return best ?? (time - at <= TimeSpan.FromSeconds(1) ? position : null);
            }
            best = position;
        }
        return best;
    }

    // Personnage immobile dans la seconde avant le clic (après le clic, il marche vers la ressource : normal).
    private bool StillAround(DateTime at)
    {
        var window = _history.Where(h => h.At >= at - TimeSpan.FromSeconds(1) && h.At <= at).ToList();
        if (window.Count == 0)
        {
            return PositionAt(at) is not null;
        }
        var first = window[0].Position;
        return window.All(h => h.Position.ScreenDistanceTo(first) <= StillDistance);
    }
}
