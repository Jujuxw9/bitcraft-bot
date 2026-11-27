namespace BitcraftBot.Server.World;

// Ressources déjà vues (v0.4.22) : la carte du bot les montre (plus pâles) quand Julien s'éloigne ou dézoome,
// sans rien redemander au serveur. Dans la zone lue en direct, ce que dit le serveur fait foi : une ressource
// gardée mais absente de la lecture en direct (arbre coupé, rocher épuisé) est oubliée.
// v0.4.27 (D-084) : gardées aussi sur le disque (dossier « carte/ressources » à côté du .exe), avec la date où elles
// ont été vues pour la dernière fois ; retirées quand elles n'ont pas été revues depuis 72 h (règle de Julien).
// Toutes les familles (bois, pierre, plantes…) passent par ici ; les ennemis viendront dans une mémoire voisine.
public sealed class ResourceMemory(Func<DateTime>? clock = null)
{
    public const int MaxKept = 200_000;
    public static readonly TimeSpan KeepFor = TimeSpan.FromHours(72);
    public const string FolderName = "ressources";
    public const string FileName = "ressources.bin";
    private const string Magic = "BCR1";

    // Monde : 400 morceaux de 32 grandes cases, 3 petites cases par grande case (marge comprise).
    public const double WorldSmallCells = 400 * 32 * 3 + 300;

    private readonly Func<DateTime> _clock = clock ?? (() => DateTime.UtcNow);
    private readonly Dictionary<ulong, (NearbyResource Resource, DateTime Seen)> _known = [];
    private readonly object _lock = new();
    private bool _dirty;

    // Lignes rejetées depuis le démarrage (contrôle des données reçues).
    public long RejectedPosition { get; private set; }

    public long Duplicates { get; private set; }

    public bool Dirty
    {
        get
        {
            lock (_lock)
            {
                return _dirty;
            }
        }
    }

    // Met à jour avec la lecture en direct ; renvoie les ressources gardées hors de la zone lue.
    public IReadOnlyList<NearbyResource> Update(IReadOnlyList<NearbyResource> live, Area? liveArea)
    {
        var now = _clock();
        lock (_lock)
        {
            var liveIds = new HashSet<ulong>();
            foreach (var r in live)
            {
                if (!Valid(r))
                {
                    RejectedPosition++;
                    continue;
                }
                if (!liveIds.Add(r.EntityId))
                {
                    Duplicates++;
                    continue;
                }
                _known[r.EntityId] = (r, now);
            }
            if (live.Count > 0)
            {
                _dirty = true;
            }
            if (liveArea is { } a)
            {
                foreach (var id in _known.Where(k => !liveIds.Contains(k.Key) && Inside(k.Value.Resource.Position, a)).Select(k => k.Key).ToList())
                {
                    _known.Remove(id);
                    _dirty = true;
                }
            }
            TrimToMax();
            return _known.Values.Where(v => !liveIds.Contains(v.Resource.EntityId)).Select(v => v.Resource).ToList();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _known.Count;
            }
        }
    }

    // Date (UTC) où la ressource a été vue pour la dernière fois, null si elle n'est pas gardée.
    public DateTime? LastSeen(ulong entityId)
    {
        lock (_lock)
        {
            return _known.TryGetValue(entityId, out var k) ? k.Seen : null;
        }
    }

    // Texte « vue il y a N h » (ou N min) pour le survol et le tableau.
    public string? SeenText(ulong entityId)
    {
        if (LastSeen(entityId) is not { } seen)
        {
            return null;
        }
        var age = _clock() - seen;
        return age.TotalMinutes < 60 ? $"vue il y a {Math.Max(0, age.TotalMinutes):0} min" : $"vue il y a {age.TotalHours:0} h";
    }

    // Retire ce qui n'a pas été revu depuis 72 h. Renvoie (retirées, gardées).
    public (int Removed, int Kept) Cleanup()
    {
        var limit = _clock() - KeepFor;
        lock (_lock)
        {
            var old = _known.Where(k => k.Value.Seen < limit).Select(k => k.Key).ToList();
            foreach (var id in old)
            {
                _known.Remove(id);
            }
            if (old.Count > 0)
            {
                _dirty = true;
            }
            return (old.Count, _known.Count);
        }
    }

    // Contrôle : identifiant non nul, position lisible et dans le monde.
    public static bool Valid(NearbyResource r) =>
        r.EntityId != 0 && double.IsFinite(r.Position.X) && double.IsFinite(r.Position.Z)
        && r.Position.X >= 0 && r.Position.Z >= 0 && r.Position.X <= WorldSmallCells && r.Position.Z <= WorldSmallCells;

    // Écrit toute la mémoire dans un fichier (format BCR1, écrit à côté puis renommé). Renvoie le nombre écrit.
    public int Save(string path)
    {
        List<(NearbyResource Resource, DateTime Seen)> items;
        lock (_lock)
        {
            items = [.. _known.Values];
            _dirty = false;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        using (var w = new BinaryWriter(File.Create(temp)))
        {
            w.Write(Magic.ToCharArray());
            w.Write(items.Count);
            foreach (var (r, seen) in items)
            {
                w.Write(r.EntityId);
                w.Write(r.ResourceId);
                w.Write(r.Name);
                w.Write(r.Tier ?? -1);
                w.Write(r.Tag ?? "");
                w.Write(r.Position.X);
                w.Write(r.Position.Z);
                w.Write(seen.Ticks);
            }
        }
        File.Move(temp, path, overwrite: true);
        return items.Count;
    }

    // Lit le fichier (absent ou abîmé : rien). Les ressources déjà connues en direct ne sont pas écrasées.
    public int Load(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }
        var read = new List<(NearbyResource, DateTime)>();
        try
        {
            using var r = new BinaryReader(File.OpenRead(path));
            if (new string(r.ReadChars(Magic.Length)) != Magic)
            {
                return 0;
            }
            var count = r.ReadInt32();
            if (count is < 0 or > MaxKept * 2)
            {
                return 0;
            }
            for (var i = 0; i < count; i++)
            {
                var id = r.ReadUInt64();
                var resourceId = r.ReadInt32();
                var name = r.ReadString();
                var tier = r.ReadInt32();
                var tag = r.ReadString();
                var x = r.ReadDouble();
                var z = r.ReadDouble();
                var seen = new DateTime(r.ReadInt64(), DateTimeKind.Utc);
                var resource = new NearbyResource(id, resourceId, name, tier < 0 ? null : tier, tag.Length == 0 ? null : tag, new ServerPosition(x, z), 0);
                if (Valid(resource))
                {
                    read.Add((resource, seen));
                }
            }
        }
        catch (Exception ex) when (ex is EndOfStreamException or IOException or ArgumentOutOfRangeException or UnauthorizedAccessException)
        {
            // Fichier coupé ou abîmé : on garde ce qui a été lu jusque-là.
        }
        lock (_lock)
        {
            foreach (var (resource, seen) in read)
            {
                _known.TryAdd(resource.EntityId, (resource, seen));
            }
            TrimToMax();
        }
        return read.Count;
    }

    private void TrimToMax()
    {
        if (_known.Count > MaxKept)
        {
            foreach (var id in _known.OrderBy(k => k.Value.Seen).Take(_known.Count - MaxKept).Select(k => k.Key).ToList())
            {
                _known.Remove(id);
            }
        }
    }

    private static bool Inside(ServerPosition p, Area a) => p.X >= a.MinX && p.X <= a.MaxX && p.Z >= a.MinZ && p.Z <= a.MaxZ;
}
