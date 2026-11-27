namespace BitcraftBot.Server.World;

// v0.4.32 (T-14) : « Aller à cette ressource » / « Récolter jusqu'à épuisement ».
// Le bot va sur une case voisine de la ressource (jamais sur elle), avec les mêmes refus que « Se déplacer ici »
// (distance, escalade, eau). Le premier coup reste à Julien : après le trajet, le point de la ressource à l'écran
// est inconnu (la caméra a suivi le personnage) et aucune méthode n'a pu être essayée en jeu (D-089).
public static class ResourceApproach
{
    // Rayon (en cases) pour trouver la ressource sous un clic droit de la carte.
    public const double PickRadius = 1.5;

    // Distance (en cases) entre la case d'arrivée et la ressource. À vérifier en jeu : portée de récolte.
    public const double ApproachDistance = 2;

    // Ressource la plus proche du point cliqué, à PickRadius cases au plus ; null sinon.
    public static NearbyResource? At(IEnumerable<NearbyResource> resources, double east, double north) =>
        resources
            .Select(r => (r, d: Dist(r.Position.ScreenEast, r.Position.ScreenNorth, east, north)))
            .Where(x => x.d <= PickRadius)
            .OrderBy(x => x.d)
            .Select(x => x.r)
            .FirstOrDefault();

    // Huit cases autour de la ressource, à ApproachDistance, de la plus proche du personnage à la plus loin.
    public static IReadOnlyList<(double East, double North)> Candidates(double resEast, double resNorth, double fromEast, double fromNorth)
    {
        var list = new List<(double East, double North)>();
        for (var i = 0; i < 8; i++)
        {
            var angle = i * Math.PI / 4;
            list.Add((Math.Round(resEast + ApproachDistance * Math.Cos(angle)), Math.Round(resNorth + ApproachDistance * Math.Sin(angle))));
        }
        return list.Distinct().OrderBy(c => Dist(c.East, c.North, fromEast, fromNorth)).ToList();
    }

    public sealed record Choice((double East, double North)? Cell, string Detail);

    // Première case voisine permise par la vérification du trajet ; sinon la raison du dernier refus.
    public static Choice Choose(double resEast, double resNorth, double fromEast, double fromNorth,
        Func<double, double, PathCheck.Result?> check)
    {
        if (Dist(resEast, resNorth, fromEast, fromNorth) <= ApproachDistance + 0.5)
        {
            return new Choice(null, "déjà à côté de la ressource");
        }
        string? last = null;
        foreach (var c in Candidates(resEast, resNorth, fromEast, fromNorth))
        {
            var r = check(c.East, c.North);
            if (r is { Allowed: true })
            {
                return new Choice(c, r.Detail);
            }
            last = r?.Detail ?? "vérification impossible";
        }
        return new Choice(null, $"aucune case voisine accessible ({last})");
    }

    private static double Dist(double e1, double n1, double e2, double n2) => Math.Sqrt((e1 - e2) * (e1 - e2) + (n1 - n2) * (n1 - n2));
}

public enum HarvestWatchState
{
    // Arrivé : attend le premier coup de Julien sur la ressource.
    AttenteClic,
    // La ressource est frappée (serveur) : la reprise des récoltes relance quand l'endurance tombe.
    Recolte,
    Finie,
    Abandon,
}

public sealed record HarvestWatchInput
{
    public required DateTimeOffset Now { get; init; }
    public ServerPosition? Position { get; init; }
    public IReadOnlyList<NearbyResource> Resources { get; init; } = [];
    public ulong? LastExtractTarget { get; init; }
    public ulong? ActionTarget { get; init; }
    public bool UnderAttack { get; init; }
}

// Suivi de « Récolter jusqu'à épuisement » après l'arrivée. Ne fait aucun geste : il dit seulement où on en est
// (journal et état), la reprise des récoltes (HarvestResume) fait les relances.
public sealed class HarvestWatch(ulong entityId, string name, DateTimeOffset arrivedAt)
{
    public static readonly TimeSpan ClickWait = TimeSpan.FromMinutes(3);

    // Au-delà, la ressource peut être hors de la zone lue : son absence ne veut plus dire « épuisée ».
    public const double ReadDistance = 10;
    public const double LeftDistance = 6;

    // Dernier coup connu à l'arrivée (une récolte d'avant) : ne compte pas comme « autre ressource ».
    private bool _started;
    private ulong? _baseline;

    public ulong EntityId => entityId;

    public string Name => name;

    public HarvestWatchState State { get; private set; } = HarvestWatchState.AttenteClic;

    public bool Over => State is HarvestWatchState.Finie or HarvestWatchState.Abandon;

    // Rend une ligne de journal quand l'état change, sinon null.
    public string? Update(HarvestWatchInput i)
    {
        if (Over)
        {
            return null;
        }
        if (!_started)
        {
            _started = true;
            _baseline = i.LastExtractTarget;
        }
        if (i.UnderAttack)
        {
            State = HarvestWatchState.Abandon;
            return $"Récolte de « {name} » arrêtée : vie qui baisse (fuir d'abord).";
        }
        var res = i.Resources.FirstOrDefault(r => r.EntityId == entityId);
        if (res is null)
        {
            if (i.Position is { } p0 && LastKnown is { } k && p0.ScreenDistanceTo(k) <= ReadDistance)
            {
                State = HarvestWatchState.Finie;
                return $"« {name} » est épuisée (plus lue par le serveur) : récolte finie.";
            }
            return null;
        }
        LastKnown = res.Position;
        if (i.Position is { } p && p.ScreenDistanceTo(res.Position) > LeftDistance)
        {
            State = HarvestWatchState.Abandon;
            return $"Récolte de « {name} » arrêtée : vous vous êtes éloigné ({p.ScreenDistanceTo(res.Position):0} cases).";
        }
        var hit = i.ActionTarget == entityId || i.LastExtractTarget == entityId;
        if (hit && State == HarvestWatchState.AttenteClic)
        {
            State = HarvestWatchState.Recolte;
            return $"Premier coup sur « {name} » vu sur le serveur : la reprise des récoltes continuera jusqu'à épuisement.";
        }
        var other = i.ActionTarget is { } a && a != entityId ? a
            : i.LastExtractTarget is { } x && x != entityId && x != _baseline ? x : (ulong?)null;
        if (other is { } o && i.Resources.Any(r => r.EntityId == o))
        {
            State = HarvestWatchState.Abandon;
            return $"Récolte de « {name} » abandonnée : vous récoltez une autre ressource.";
        }
        if (State == HarvestWatchState.AttenteClic && i.Now - arrivedAt > ClickWait)
        {
            State = HarvestWatchState.Abandon;
            return $"Récolte de « {name} » abandonnée : aucun coup en {ClickWait.TotalMinutes:0} min.";
        }
        return null;
    }

    public ServerPosition? LastKnown { get; private set; }
}
