using System.Text.Json;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.18 (D-070) : fond de carte lu sur le serveur (terrain, zones découvertes) et filtres en pastilles.
public class V0418Tests
{
    private static readonly TableSchema TerrainSchema = new("terrain_chunk_state",
        ["chunk_index", "chunk_x", "chunk_z", "dimension", "biomes", "biome_density", "elevations", "water_levels", "water_body_types", "zoning_types", "original_elevations"],
        [0], true);

    private static Row TerrainRow(int cx, int cz, Func<int, int, int> elevation, Func<int, int, int>? water = null, uint biome = 1)
    {
        var elev = new int[TerrainChunk.Cells];
        var wat = new int[TerrainChunk.Cells];
        for (var z = 0; z < TerrainChunk.Size; z++)
        {
            for (var x = 0; x < TerrainChunk.Size; x++)
            {
                elev[z * TerrainChunk.Size + x] = elevation(x, z);
                wat[z * TerrainChunk.Size + x] = water?.Invoke(x, z) ?? 0;
            }
        }
        var biomes = Enumerable.Repeat(biome, TerrainChunk.Cells).ToArray();
        var density = Enumerable.Repeat(128u, TerrainChunk.Cells).ToArray();
        var row = new object[]
        {
            TerrainChunk.IndexOf(cx, cz), cx, cz, 1u, biomes, density, elev, wat, "00", "00", elev,
        };
        return Row.Parse(JsonSerializer.Serialize(row), TerrainSchema);
    }

    [Fact]
    public void ChunkIndexFollowsServerFormula()
    {
        // (dimension-1)*1000000 + chunk_z*1000 + chunk_x + 1
        Assert.Equal(231_101UL, TerrainChunk.IndexOf(100, 231));
        Assert.Equal(1_231_101UL, TerrainChunk.IndexOf(100, 231, 2));
    }

    [Fact]
    public void ChunksAroundCoverTheMap()
    {
        // Personnage au milieu d'un morceau : 21 + 3 cases de chaque côté tiennent dans 3 × 3 morceaux au plus.
        var chunks = TerrainChunk.ChunksAround(3216, 7392, Queries.TerrainRadius);
        Assert.InRange(chunks.Count, 4, 9);
        Assert.Contains((3216 / 32, 7392 / 32), chunks);
        Assert.All(chunks, c => Assert.InRange(c.X, 3216 / 32 - 1, 3216 / 32 + 1));
        // Au bord du monde, pas de morceau négatif.
        Assert.All(TerrainChunk.ChunksAround(5, 5, 24), c => Assert.True(c.X >= 0 && c.Z >= 0));
    }

    [Fact]
    public void TerrainQueriesAreFewAndReadOnly()
    {
        var area = Area.Around(3216 * 3, 7392 * 3, 60);
        var queries = Queries.Terrain(area, 42);
        Assert.InRange(queries.Count, 5, 10);
        Assert.All(queries, q => Assert.StartsWith("SELECT * FROM ", q));
        Assert.Contains(queries, q => q == "SELECT * FROM exploration_chunks_state_v2 WHERE entity_id = 42");
        Assert.DoesNotContain(Queries.FollowPlayer("ab", 42, area), q => q.Contains("terrain_chunk_state"));
        Assert.Contains(Queries.FollowPlayer("ab", 42, area, withTerrain: true), q => q.Contains("terrain_chunk_state WHERE chunk_index = "));
        Assert.DoesNotContain(Queries.FollowPlayer("ab", 42, null, withTerrain: true), q => q.Contains("terrain_chunk_state"));
    }

    [Fact]
    public void ReadsTerrainRow()
    {
        var chunk = TerrainChunk.FromRow(TerrainRow(10, 20, (x, z) => x + z));
        Assert.NotNull(chunk);
        Assert.Equal(10, chunk!.ChunkX);
        Assert.Equal(20, chunk.ChunkZ);
        Assert.Equal((short)(3 + 5), chunk.Elevations[chunk.CellIndex(3, 5)]);
        Assert.Equal("Forêt calme", TerrainColors.BiomeName(chunk.Biomes[0]));
    }

    [Fact]
    public void IncompleteTerrainRowIsIgnored()
    {
        var row = Row.Parse("[1, 0, 0, 1, [1,2,3], [], [1], [1], \"\", \"\", []]", TerrainSchema);
        Assert.Null(TerrainChunk.FromRow(row));
    }

    [Fact]
    public void WaterIsBlueAndDeeperIsDarker()
    {
        var land = TerrainColors.Cell(1, 128, 10, 0, 1, true);
        var shallow = TerrainColors.Cell(1, 128, 10, 11, 1, true);
        var deep = TerrainColors.Cell(1, 128, 0, 20, 1, true);
        static (uint R, uint G, uint B) Rgb(uint c) => ((c >> 16) & 0xFF, (c >> 8) & 0xFF, c & 0xFF);
        Assert.True(Rgb(shallow).B > Rgb(shallow).R && Rgb(shallow).B > Rgb(shallow).G);
        Assert.True(Rgb(deep).B < Rgb(shallow).B);
        Assert.True(Rgb(land).G > Rgb(land).B); // forêt : vert
        Assert.Equal(0xFF000000u, land & 0xFF000000u);
    }

    [Fact]
    public void BiomesAreBlendedByShare()
    {
        // Moitié désert (7), moitié océan (10).
        var mixed = TerrainColors.BiomeColor(7u | (10u << 8), 64u | (64u << 8));
        var desert = TerrainColors.BiomeColor(7, 128);
        var ocean = TerrainColors.BiomeColor(10, 128);
        Assert.Equal((desert.R + ocean.R) / 2, mixed.R, 3);
    }

    [Fact]
    public void ReliefIsShadedAndUnexploredIsDarker()
    {
        Assert.True(TerrainColors.Shade(5, 0, 0, 0) > 1);   // pente face au nord-ouest : éclairée
        Assert.True(TerrainColors.Shade(0, 5, 0, 0) < 1);   // pente opposée : à l'ombre
        Assert.Equal(1, TerrainColors.Shade(3, 3, 3, 3));
        var seen = TerrainColors.Cell(4, 128, 5, 0, 1, true);
        var unseen = TerrainColors.Cell(4, 128, 5, 0, 1, false);
        Assert.True((unseen & 0xFF00) < (seen & 0xFF00));
    }

    [Fact]
    public void ImageIsNorthUpAndCoversChunks()
    {
        // Deux morceaux côte à côte ; le sol monte vers le nord dans le premier.
        var a = TerrainChunk.FromRow(TerrainRow(100, 200, (_, z) => z))!;
        var b = TerrainChunk.FromRow(TerrainRow(101, 200, (_, _) => 0, (_, _) => 5))!;
        var image = TerrainImage.Build([a, b], null)!;
        Assert.Equal(64, image.Width);
        Assert.Equal(32, image.Height);
        Assert.Equal(3200, image.WestCell);
        Assert.Equal(200 * 32 + 31, image.NorthCell);
        // Colonne de droite : de l'eau (bleu dominant).
        var water = image.Pixels[10 * 64 + 40];
        Assert.True((water & 0xFF) > ((water >> 16) & 0xFF));
        Assert.Equal(image.Signature, TerrainImage.SignatureOf([a, b], null));
        Assert.Null(TerrainImage.Build([], null));
    }

    [Fact]
    public void ExploredBitmapFollowsServerIndex()
    {
        // Monde de 100 morceaux de large ; morceau (3, 2) → indice 203 → mot 3, bit 11.
        var bitmap = new ulong[10];
        bitmap[3] = 1UL << 11;
        var explored = new ExploredChunks(bitmap, 100);
        Assert.True(explored.IsExplored(3, 2));
        Assert.False(explored.IsExplored(4, 2));
        Assert.False(explored.IsExplored(-1, 0));
        Assert.Equal(1, explored.Count);
        var region = Row.Parse("{\"region_width_chunks\": 80, \"region_count_sqrt\": 3}", null);
        Assert.Equal(240, ExploredChunks.WorldWidth(region));
        var row = Row.Parse("{\"entity_id\": 1, \"bitmap\": [0, 0, 0, 2048]}", null);
        Assert.True(ExploredChunks.FromRow(row, 100)!.IsExplored(3, 2));
    }

    private static NearbyResource Res(string tag, int? tier) =>
        new(1, 1, "x", tier, tag, new ServerPosition(0, 0), 0);

    [Fact]
    public void CategoriesGroupFamilies()
    {
        // v0.4.20 : pierre et minerai séparés, 8 catégories.
        Assert.Equal(8, MapCategories.All.Count);
        Assert.Equal(MapCategories.Wood, MapCategories.Of(Res("Tree", 1)));
        Assert.Equal(MapCategories.Ore, MapCategories.Of(Res("Ore Vein", 3)));
        Assert.Equal(MapCategories.Stone, MapCategories.Of(Res("Rock", 1)));
        Assert.Equal(MapCategories.Food, MapCategories.Of(Res("Mushroom", 2)));
        Assert.Equal(MapCategories.Other, MapCategories.Of(Res("Obstacle", 1)));
        // Chaque famille est dans une seule catégorie.
        Assert.All(ResourceFamilies.All, f => Assert.Single(MapCategories.All, c => c.Families.Contains(f)));
    }

    [Fact]
    public void FilterTogglesTiersAndCategories()
    {
        var filter = new MapFilter();
        Assert.True(filter.AllShown);
        var t3Ore = Res("Ore Vein", 3);
        var t1Tree = Res("Tree", 1);
        Assert.True(filter.Shows(t3Ore));
        filter.ToggleTier(3);
        Assert.False(filter.Shows(t3Ore));
        Assert.True(filter.Shows(t1Tree));
        filter.ToggleTier(3);
        Assert.True(filter.Shows(t3Ore));
        filter.ToggleCategory(MapCategories.Wood);
        Assert.False(filter.Shows(t1Tree));
        filter.ShowAll();
        Assert.True(filter.Shows(t1Tree));
        // Palier hors 1..10 ou absent : « ? ».
        Assert.Equal(MapFilter.UnknownTier, MapFilter.TierOf(Res("Tree", null)));
        Assert.Equal(MapFilter.UnknownTier, MapFilter.TierOf(Res("Tree", 0)));
    }

    [Fact]
    public void OnlyTierShowsOneThenAll()
    {
        var filter = new MapFilter();
        filter.OnlyTier(4);
        Assert.True(filter.IsShown(4));
        Assert.False(filter.IsShown(3));
        Assert.False(filter.IsShown(MapFilter.UnknownTier));
        filter.OnlyTier(4);
        Assert.True(MapFilter.Tiers.All(filter.IsShown));
    }

    [Fact]
    public void CountsFollowTheOtherFilter()
    {
        var filter = new MapFilter();
        var list = new[] { Res("Tree", 1), Res("Tree", 2), Res("Ore Vein", 2) };
        filter.ToggleCategory(MapCategories.Ore);
        Assert.Equal(1, filter.TierCounts(list)[2]);       // le minerai masqué n'est pas compté
        filter.ToggleTier(1);
        Assert.Equal(1, filter.CategoryCounts(list)[MapCategories.Wood]); // l'arbre T1 masqué n'est pas compté
    }

    [Fact]
    public void FilterIsRestoredFromSettings()
    {
        var filter = new MapFilter([MapCategories.Wood, "catégorie disparue"], [5, 6]);
        Assert.Equal([MapCategories.Wood], filter.ShownCategories);
        Assert.False(filter.IsShown(5));
        Assert.True(filter.IsShown(4));
    }

    // v0.4.20 : ancienne catégorie « Pierre et minerai » enregistrée = les deux nouvelles ; ancien palier « ? » masqué ignoré.
    [Fact]
    public void OldSettingsStillWork()
    {
        var filter = new MapFilter([MapCategories.OldStone], [MapFilter.UnknownTier, 2]);
        Assert.True(filter.IsShown(MapCategories.Stone));
        Assert.True(filter.IsShown(MapCategories.Ore));
        Assert.False(filter.IsShown(MapCategories.Wood));
        Assert.Equal([2], filter.HiddenTiers);
        Assert.DoesNotContain(MapFilter.UnknownTier, MapFilter.Tiers);
        // Palier inconnu : montré tant qu'aucun palier n'est masqué.
        var all = new MapFilter();
        Assert.True(all.Shows(Res("Rock", null)));
        all.OnlyTier(3);
        Assert.False(all.Shows(Res("Rock", null)));
    }

    // v0.4.21 : boutons T1 à T5 seulement ; un tier sans bouton masqué avant est réaffiché, sauf en « ce tier seul ».
    [Fact]
    public void TierPillsStopAtFive()
    {
        Assert.Equal([1, 2, 3, 4, 5], MapFilter.TierPills);
        var restored = new MapFilter(null, [2, 7]);
        Assert.Equal([2], restored.HiddenTiers);
        var only = new MapFilter();
        only.OnlyTier(3);
        var reloaded = new MapFilter(null, only.HiddenTiers);
        Assert.True(reloaded.IsShown(3));
        Assert.False(reloaded.IsShown(7));
    }
}
