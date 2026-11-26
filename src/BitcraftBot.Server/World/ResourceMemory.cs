

namespace BitcraftBot.Server.World;

// Ressources déjà vues (v0.4.22) : la carte du bot les montre (plus pâles) quand Julien s'éloigne ou dézoome,
// sans rien redemander au serveur. Dans la zone lue en direct, ce que dit le serveur fait foi : une ressource
// gardée mais absente de la lecture en direct (arbre coupé, rocher épuisé) est oubliée.
// Mémoire seulement (pas sur le disque) ; au plus MaxKept ressources, les plus anciennes vues partent d'abord.
public sealed class ResourceMemory
{
    public const int MaxKept = 40_000;

    private readonly Dictionary<ulong, (NearbyResource Resource, long Seen)> _known = [];
    private long _tick;

    // Met à jour avec la lecture en direct ; renvoie les ressources gardées hors de la zone lue.
    public IReadOnlyList<NearbyResource> Update(IReadOnlyList<NearbyResource> live, Area? liveArea)
    {
        _tick++;
        var liveIds = new HashSet<ulong>();
        foreach (var r in live)
        {
            _known[r.EntityId] = (r, _tick);
            liveIds.Add(r.EntityId);
        }
        if (liveArea is { } a)
        {
            foreach (var id in _known.Where(k => !liveIds.Contains(k.Key) && Inside(k.Value.Resource.Position, a)).Select(k => k.Key).ToList())
            {
                _known.Remove(id);
            }
        }
        if (_known.Count > MaxKept)
        {
            foreach (var id in _known.OrderBy(k => k.Value.Seen).Take(_known.Count - MaxKept).Select(k => k.Key).ToList())
            {
                _known.Remove(id);
            }
        }
        return _known.Values.Where(v => !liveIds.Contains(v.Resource.EntityId)).Select(v => v.Resource).ToList();
    }

    public int Count => _known.Count;

    private static bool Inside(ServerPosition p, Area a) => p.X >= a.MinX && p.X <= a.MaxX && p.Z >= a.MinZ && p.Z <= a.MaxZ;
}
