using System.Globalization;
using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Fond de carte (v0.4.18, D-070) : morceaux de terrain lus sur le serveur (table publique terrain_chunk_state).
// Un morceau = 32 × 32 grandes cases (les cases du jeu, celles des coordonnées N et E du HUD).
// Case (x, z) du morceau à l'indice z * 32 + x. Grande case x ≈ E de l'écran, z ≈ N (à une demi-case près).
public sealed record TerrainChunk(ulong Index, int ChunkX, int ChunkZ, uint Dimension, uint[] Biomes, uint[] BiomeDensity,
    short[] Elevations, short[] WaterLevels)
{
    public const int Size = 32;
    public const int Cells = Size * Size;
    public const uint Overworld = 1;

    // Indice du morceau, comme dans le code du serveur : (dimension-1)*1000000 + chunk_z*1000 + chunk_x + 1.
    public static ulong IndexOf(int chunkX, int chunkZ, uint dimension = Overworld) =>
        (ulong)((dimension - 1) * 1_000_000L + chunkZ * 1000L + chunkX + 1);

    // Morceau qui contient une grande case (E, N de l'écran).
    public static (int X, int Z) ChunkOf(double east, double north) =>
        ((int)Math.Floor(east / Size), (int)Math.Floor(north / Size));

    // Morceaux à lire pour couvrir un carré de « radius » cases autour du personnage (2 × 2 ou 3 × 3 pour 21 cases).
    public static IReadOnlyList<(int X, int Z)> ChunksAround(double east, double north, double radius)
    {
        var (minX, minZ) = ChunkOf(east - radius, north - radius);
        var (maxX, maxZ) = ChunkOf(east + radius, north + radius);
        var list = new List<(int, int)>();
        for (var z = minZ; z <= maxZ; z++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (x >= 0 && z >= 0 && x < 1000)
                {
                    list.Add((x, z));
                }
            }
        }
        return list;
    }

    // Lecture d'une ligne de terrain_chunk_state (null si une colonne manque ou n'a pas la bonne taille).
    public static TerrainChunk? FromRow(Row row)
    {
        if (row.U64("chunk_index") is not { } index || row.I32("chunk_x") is not { } cx || row.I32("chunk_z") is not { } cz)
        {
            return null;
        }
        var dimension = row.U64("dimension") is { } d ? (uint)d : Overworld;
        var biomes = Numbers(row.Get("biomes"));
        var density = Numbers(row.Get("biome_density"));
        var elevations = Numbers(row.Get("elevations"));
        var water = Numbers(row.Get("water_levels"));
        if (biomes.Count != Cells || elevations.Count != Cells || water.Count != Cells)
        {
            return null;
        }
        return new TerrainChunk(index, cx, cz, dimension,
            biomes.Select(v => (uint)v).ToArray(),
            density.Count == Cells ? density.Select(v => (uint)v).ToArray() : new uint[Cells],
            elevations.Select(v => (short)v).ToArray(),
            water.Select(v => (short)v).ToArray());
    }

    // Tableau de nombres en JSON (ou chaîne hexadécimale, format possible des tableaux d'octets).
    public static List<long> Numbers(JsonElement? value)
    {
        var list = new List<long>();
        if (value is not { } v)
        {
            return list;
        }
        if (v.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in v.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var n))
                {
                    list.Add(n);
                }
                else if (item.ValueKind == JsonValueKind.Number && item.TryGetUInt64(out var u))
                {
                    list.Add(unchecked((long)u));
                }
            }
        }
        else if (v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } hex && hex.Length % 2 == 0)
        {
            for (var i = 0; i < hex.Length; i += 2)
            {
                if (!byte.TryParse(hex.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                {
                    return [];
                }
                list.Add(b);
            }
        }
        return list;
    }

    public int CellIndex(int localX, int localZ) => localZ * Size + localX;
}

// Zones découvertes par le personnage (table exploration_chunks_state_v2) : un bit par morceau de tout le monde,
// indice = chunk_z * largeur du monde en morceaux + chunk_x (code du serveur).
public sealed class ExploredChunks(ulong[] bitmap, int worldWidthChunks)
{
    public int WorldWidthChunks => worldWidthChunks;

    public int Count => bitmap.Sum(b => System.Numerics.BitOperations.PopCount(b));

    public bool IsExplored(int chunkX, int chunkZ)
    {
        if (chunkX < 0 || chunkZ < 0 || chunkX >= worldWidthChunks)
        {
            return false;
        }
        var global = (long)chunkZ * worldWidthChunks + chunkX;
        var word = global / 64;
        return word < bitmap.Length && (bitmap[word] & (1UL << (int)(global % 64))) != 0;
    }

    // Largeur du monde en morceaux : region_width_chunks × region_count_sqrt (world_region_state).
    public static int? WorldWidth(Row? region) =>
        region?.I32("region_width_chunks") is { } w && region.I32("region_count_sqrt") is { } n && w > 0 && n > 0 ? w * n : null;

    public static ExploredChunks? FromRow(Row? row, int? worldWidthChunks)
    {
        if (row is null || worldWidthChunks is not { } width)
        {
            return null;
        }
        var words = TerrainChunk.Numbers(row.Get("bitmap"));
        return words.Count == 0 ? null : new ExploredChunks(words.Select(w => unchecked((ulong)w)).ToArray(), width);
    }
}

// Couleurs du fond de carte : biome (mélange des biomes de la case selon leur part), eau en bleu selon la profondeur,
// relief ombré (lumière venant du nord-ouest), zones pas découvertes assombries.
public static class TerrainColors
{
    // Ordre de l'énumération Biome du serveur (messages/components.rs).
    public static IReadOnlyList<string> BiomeNames { get; } =
    [
        "Dev", "Forêt calme", "Bois de pins", "Pics enneigés", "Plaines venteuses", "Forêt d'automne", "Toundra", "Désert",
        "Marais", "Canyon", "Océan", "Prairies sûres", "Grotte", "Jungle", "Sapwoods", "Plage déserte", "Canopée tropicale",
        "Crêtes volcaniques", "Océan inexploré",
    ];

    private static readonly (byte R, byte G, byte B)[] Biome =
    [
        (128, 128, 128), // Dev
        (98, 140, 72),   // Forêt calme
        (62, 104, 70),   // Bois de pins
        (226, 231, 236), // Pics enneigés
        (156, 178, 92),  // Plaines venteuses
        (172, 116, 62),  // Forêt d'automne
        (162, 172, 152), // Toundra
        (222, 198, 132), // Désert
        (92, 108, 72),   // Marais
        (186, 112, 76),  // Canyon
        (40, 90, 150),   // Océan
        (126, 176, 96),  // Prairies sûres
        (92, 86, 80),    // Grotte
        (50, 122, 56),   // Jungle
        (112, 152, 82),  // Sapwoods
        (226, 212, 162), // Plage déserte
        (60, 142, 82),   // Canopée tropicale
        (82, 66, 60),    // Crêtes volcaniques
        (26, 60, 110),   // Océan inexploré
    ];

    private static readonly (byte R, byte G, byte B) Shallow = (78, 140, 196);
    private static readonly (byte R, byte G, byte B) Deep = (22, 56, 112);

    // v0.4.27 : morceau jamais lu (pas découvert : le jeu ne le montre pas non plus) en gris-bleu discret, plus en noir.
    public const uint Unknown = 0xFF18222E;

    public static string BiomeName(uint biomes) =>
        (biomes & 0xFF) is var b && b < BiomeNames.Count ? BiomeNames[(int)b] : $"biome {biomes & 0xFF}";

    // Couleur d'un biome, mélangée selon la part de chaque biome de la case (4 biomes au plus, part sur 128).
    public static (double R, double G, double B) BiomeColor(uint biomes, uint density)
    {
        double r = 0, g = 0, b = 0, total = 0;
        for (var i = 0; i < 4; i++)
        {
            var index = (biomes >> (i * 8)) & 0xFF;
            var part = (density >> (i * 8)) & 0xFF;
            if (part == 0 || index >= Biome.Length)
            {
                continue;
            }
            var c = Biome[index];
            r += c.R * part;
            g += c.G * part;
            b += c.B * part;
            total += part;
        }
        if (total == 0)
        {
            var c = Biome[(biomes & 0xFF) < Biome.Length ? (int)(biomes & 0xFF) : 0];
            return (c.R, c.G, c.B);
        }
        return (r / total, g / total, b / total);
    }

    // Ombre du relief : pente entre les voisins (ouest - est, nord - sud), une marche de hauteur = 12 %.
    public static double Shade(double west, double east, double north, double south)
    {
        var slope = (west - east) + (north - south);
        return Math.Clamp(1 + slope * 0.12, 0.55, 1.4);
    }

    // Couleur ARGB d'une case. elevation et water : hauteurs du sol et de l'eau ; shade : ombre du relief.
    public static uint Cell(uint biomes, uint density, short elevation, short water, double shade, bool explored)
    {
        (double R, double G, double B) c;
        if (water > elevation)
        {
            var t = Math.Clamp((water - elevation) / 6.0, 0, 1);
            c = (Shallow.R + (Deep.R - Shallow.R) * t, Shallow.G + (Deep.G - Shallow.G) * t, Shallow.B + (Deep.B - Shallow.B) * t);
            shade = 1 + (shade - 1) * 0.25; // l'eau est presque plate
        }
        else
        {
            c = BiomeColor(biomes, density);
        }
        var f = shade * (explored ? 1 : 0.38);
        return Argb(c.R * f, c.G * f, c.B * f);
    }

    public static uint Argb(double r, double g, double b) =>
        0xFF000000u | (Byte(r) << 16) | (Byte(g) << 8) | Byte(b);

    private static uint Byte(double v) => (uint)Math.Clamp((int)Math.Round(v), 0, 255);
}

// Image du fond : les morceaux lus, assemblés sur une grille de cases (une case = un point), nord en haut.
public sealed class TerrainImage
{
    public int MinChunkX { get; }
    public int MinChunkZ { get; }
    public int WidthChunks { get; }
    public int HeightChunks { get; }
    public int Width => WidthChunks * TerrainChunk.Size;
    public int Height => HeightChunks * TerrainChunk.Size;

    // Grande case à gauche (E minimale) et en haut (N maximale) de l'image.
    public int WestCell => MinChunkX * TerrainChunk.Size;
    public int NorthCell => (MinChunkZ + HeightChunks) * TerrainChunk.Size - 1;

    // Points ARGB, ligne par ligne, du nord au sud.
    public uint[] Pixels { get; }

    public int ChunkCount { get; }

    // Signature des morceaux utilisés : l'image n'est refaite que si elle change.
    public string Signature { get; }

    private TerrainImage(int minX, int minZ, int w, int h, uint[] pixels, int count, string signature)
    {
        MinChunkX = minX;
        MinChunkZ = minZ;
        WidthChunks = w;
        HeightChunks = h;
        Pixels = pixels;
        ChunkCount = count;
        Signature = signature;
    }

    public static string SignatureOf(IReadOnlyCollection<TerrainChunk> chunks, ExploredChunks? explored) =>
        string.Join(",", chunks.OrderBy(c => c.Index).Select(c => $"{c.Index}:{c.GetHashCode()}:{(explored?.IsExplored(c.ChunkX, c.ChunkZ) ?? true ? 1 : 0)}"));

    public static TerrainImage? Build(IReadOnlyCollection<TerrainChunk> chunks, ExploredChunks? explored)
    {
        var overworld = chunks.Where(c => c.Dimension == TerrainChunk.Overworld).ToList();
        if (overworld.Count == 0)
        {
            return null;
        }
        var minX = overworld.Min(c => c.ChunkX);
        var maxX = overworld.Max(c => c.ChunkX);
        var minZ = overworld.Min(c => c.ChunkZ);
        var maxZ = overworld.Max(c => c.ChunkZ);
        var w = maxX - minX + 1;
        var h = maxZ - minZ + 1;
        var byPos = overworld.ToDictionary(c => (c.ChunkX, c.ChunkZ));
        var width = w * TerrainChunk.Size;
        var height = h * TerrainChunk.Size;
        var west = minX * TerrainChunk.Size;
        var south = minZ * TerrainChunk.Size;

        // Hauteur de la surface (sol ou eau) d'une grande case, null si son morceau n'est pas lu.
        double? Surface(int cellX, int cellZ)
        {
            var key = (FloorDiv(cellX), FloorDiv(cellZ));
            if (!byPos.TryGetValue(key, out var chunk))
            {
                return null;
            }
            var i = chunk.CellIndex(cellX - key.Item1 * TerrainChunk.Size, cellZ - key.Item2 * TerrainChunk.Size);
            return Math.Max(chunk.Elevations[i], chunk.WaterLevels[i]);
        }

        var pixels = new uint[width * height];
        for (var row = 0; row < height; row++)
        {
            var cellZ = south + height - 1 - row;
            for (var col = 0; col < width; col++)
            {
                var cellX = west + col;
                var key = (FloorDiv(cellX), FloorDiv(cellZ));
                if (!byPos.TryGetValue(key, out var chunk))
                {
                    pixels[row * width + col] = TerrainColors.Unknown;
                    continue;
                }
                var i = chunk.CellIndex(cellX - key.Item1 * TerrainChunk.Size, cellZ - key.Item2 * TerrainChunk.Size);
                double here = Math.Max(chunk.Elevations[i], chunk.WaterLevels[i]);
                var shade = TerrainColors.Shade(Surface(cellX - 1, cellZ) ?? here, Surface(cellX + 1, cellZ) ?? here,
                    Surface(cellX, cellZ + 1) ?? here, Surface(cellX, cellZ - 1) ?? here);
                var isExplored = explored?.IsExplored(chunk.ChunkX, chunk.ChunkZ) ?? true;
                pixels[row * width + col] = TerrainColors.Cell(chunk.Biomes[i], chunk.BiomeDensity[i], chunk.Elevations[i], chunk.WaterLevels[i], shade, isExplored);
            }
        }
        return new TerrainImage(minX, minZ, w, h, pixels, overworld.Count, SignatureOf(overworld, explored));
    }

    private static int FloorDiv(int cell) => (int)Math.Floor(cell / (double)TerrainChunk.Size);
}
