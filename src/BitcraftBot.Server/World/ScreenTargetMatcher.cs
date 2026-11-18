namespace BitcraftBot.Server.World;

// Retrouve, parmi les ressources lues sur le serveur, celle que le joueur cible dans le jeu
// (nom et santé lus sur l'écran). Corrige deux défauts vus à l'essai v0.3.11 :
// - le choix sautait vers un autre arbre plus loin qui avait la même santé (ex. 3000/3000) ;
// - « sûr » et « probable » alternaient, car l'écran a un léger retard sur le serveur pendant la coupe.
public sealed class ScreenTargetMatcher
{
    // On ne cible que des ressources proches (en cases du jeu).
    public const double MaxDistance = 8;

    // Écart de santé toléré entre l'écran et le serveur (retard d'affichage), en part de la santé max.
    public const double HealthTolerance = 0.03;

    private ulong? _previous;
    private bool _previousSure;

    public ScreenTargetMatch? Match(string? name, int? screenHealth, IReadOnlyList<NearbyResource> resources)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _previous = null;
            _previousSure = false;
            return null;
        }
        var candidates = resources
            .Where(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase) && r.ScreenDistance <= MaxDistance)
            .ToList(); // déjà triées de la plus proche à la plus lointaine
        if (candidates.Count == 0)
        {
            _previous = null;
            _previousSure = false;
            return null;
        }
        // Écran illisible : on garde la ressource déjà retrouvée.
        if (screenHealth is not { } h)
        {
            var known = candidates.FirstOrDefault(r => r.EntityId == _previous);
            return known is not null ? new ScreenTargetMatch(known, _previousSure) : Remember(candidates[0], false);
        }

        var close = candidates.Where(r => Close(r, h)).ToList();
        // 1. Une seule ressource proche avec cette santé : c'est elle.
        if (close.Count == 1)
        {
            return Remember(close[0], true);
        }
        // 2. Plusieurs (ex. arbres intacts à 3000 / 3000) : on garde la précédente si elle en fait partie.
        if (close.Count > 1)
        {
            var kept = close.FirstOrDefault(r => r.EntityId == _previous);
            return kept is not null ? Remember(kept, _previousSure) : Remember(close[0], false);
        }
        // 3. Aucune santé ne colle : la plus proche du même nom, probable seulement.
        return Remember(candidates[0], false);
    }

    private ScreenTargetMatch Remember(NearbyResource r, bool sure)
    {
        _previous = r.EntityId;
        _previousSure = sure;
        return new ScreenTargetMatch(r, sure);
    }

    private static bool Close(NearbyResource r, int screenHealth)
    {
        if (r.Health is not { } server)
        {
            return false;
        }
        // Le serveur est en avance : sa santé est égale ou un peu plus basse que celle affichée.
        var tolerance = Math.Max(15, (r.MaxHealth ?? 0) * HealthTolerance);
        var lead = screenHealth - server;
        return lead >= 0 && lead <= tolerance;
    }
}

// Sure : une seule ressource proche a ce nom et cette santé (c'est bien elle). Sinon : probable.
public sealed record ScreenTargetMatch(NearbyResource Resource, bool Sure);
