namespace BitcraftBot.Server.World;

// Mémoire du fond de carte (v0.4.22, demande de Julien) : chaque morceau de terrain reçu du serveur est gardé en mémoire
// et sur le disque (dossier « carte » à côté du .exe, un petit fichier d'environ 12 Ko par morceau). Quand Julien revient
// dans une zone déjà vue, ou dézoome, le fond est dessiné tout de suite avec ce qui est gardé. Les morceaux autour du
// personnage restent lus en direct sur le serveur : un terrain modifié (terraformé) remplace l'ancien.
public sealed class TerrainStore
{
    public const string FolderName = "carte";
    private const string Magic = "BCT1";

    private readonly string _folder;
    private readonly Dictionary<ulong, TerrainChunk> _chunks = [];
    private readonly object _lock = new();

    public TerrainStore(string folder)
    {
        _folder = folder;
    }

    // Augmente à chaque morceau nouveau ou changé (la carte se redessine quand il change).
    public int Version => Volatile.Read(ref _version);

    private int _version;

    // Le morceau est-il gardé ?
    public bool Contains(ulong index)
    {
        lock (_lock)
        {
            return _chunks.ContainsKey(index);
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _chunks.Count;
            }
        }
    }

    // Lit les morceaux gardés sur le disque (fichiers abîmés ignorés). Renvoie le nombre lu.
    public int LoadAll()
    {
        if (!Directory.Exists(_folder))
        {
            return 0;
        }
        var read = 0;
        foreach (var file in Directory.EnumerateFiles(_folder, "*.bin"))
        {
            try
            {
                if (Read(File.ReadAllBytes(file)) is { } chunk)
                {
                    lock (_lock)
                    {
                        _chunks.TryAdd(chunk.Index, chunk);
                    }
                    read++;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Fichier en cours d'écriture ou inaccessible : il sera refait à la prochaine lecture du serveur.
            }
        }
        return read;
    }

    // Morceau reçu du serveur : gardé, et écrit sur le disque s'il est nouveau ou a changé. Renvoie true si c'est le cas.
    public bool Put(TerrainChunk chunk)
    {
        lock (_lock)
        {
            if (_chunks.TryGetValue(chunk.Index, out var known) && SameCells(known, chunk))
            {
                return false;
            }
            _chunks[chunk.Index] = chunk;
            _version++;
        }
        try
        {
            Directory.CreateDirectory(_folder);
            var path = Path.Combine(_folder, $"{chunk.Index}.bin");
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, Write(chunk));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Disque plein ou dossier protégé : le morceau reste en mémoire pour cette session.
        }
        return true;
    }

    // Morceaux gardés autour d'un point (grandes cases E, N), dans un rayon en grandes cases.
    public IReadOnlyList<TerrainChunk> Around(double east, double north, double radius)
    {
        var wanted = TerrainChunk.ChunksAround(east, north, radius).Select(c => TerrainChunk.IndexOf(c.X, c.Z)).ToList();
        lock (_lock)
        {
            return wanted.Where(_chunks.ContainsKey).Select(i => _chunks[i]).ToList();
        }
    }

    private static bool SameCells(TerrainChunk a, TerrainChunk b) =>
        a.Biomes.AsSpan().SequenceEqual(b.Biomes) && a.BiomeDensity.AsSpan().SequenceEqual(b.BiomeDensity)
        && a.Elevations.AsSpan().SequenceEqual(b.Elevations) && a.WaterLevels.AsSpan().SequenceEqual(b.WaterLevels);

    public static byte[] Write(TerrainChunk c)
    {
        using var stream = new MemoryStream();
        using (var w = new BinaryWriter(stream))
        {
            w.Write(Magic.ToCharArray());
            w.Write(DateTime.UtcNow.Ticks);
            w.Write(c.Index);
            w.Write(c.ChunkX);
            w.Write(c.ChunkZ);
            w.Write(c.Dimension);
            WriteArray(w, c.Biomes, w.Write);
            WriteArray(w, c.BiomeDensity, w.Write);
            WriteArray(w, c.Elevations, w.Write);
            WriteArray(w, c.WaterLevels, w.Write);
        }
        return stream.ToArray();
    }

    public static TerrainChunk? Read(byte[] data)
    {
        try
        {
            using var r = new BinaryReader(new MemoryStream(data));
            if (new string(r.ReadChars(Magic.Length)) != Magic)
            {
                return null;
            }
            r.ReadInt64(); // date d'écriture (pour plus tard : rafraîchir les vieux morceaux)
            var index = r.ReadUInt64();
            var x = r.ReadInt32();
            var z = r.ReadInt32();
            var dimension = r.ReadUInt32();
            var biomes = ReadArray(r, r.ReadUInt32);
            var density = ReadArray(r, r.ReadUInt32);
            var elevations = ReadArray(r, r.ReadInt16);
            var water = ReadArray(r, r.ReadInt16);
            if (biomes.Length != TerrainChunk.Cells || density.Length != TerrainChunk.Cells
                || elevations.Length != TerrainChunk.Cells || water.Length != TerrainChunk.Cells)
            {
                return null;
            }
            return new TerrainChunk(index, x, z, dimension, biomes, density, elevations, water);
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }

    private static void WriteArray<T>(BinaryWriter w, T[] values, Action<T> write)
    {
        w.Write(values.Length);
        foreach (var v in values)
        {
            write(v);
        }
    }

    private static T[] ReadArray<T>(BinaryReader r, Func<T> read)
    {
        var length = r.ReadInt32();
        if (length is < 0 or > TerrainChunk.Cells * 4)
        {
            throw new EndOfStreamException();
        }
        var values = new T[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = read();
        }
        return values;
    }
}
