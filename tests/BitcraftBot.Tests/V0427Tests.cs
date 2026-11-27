using System.IO;
using BitcraftBot.Server;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.27 (D-084) : carte, partie données : zoom jusqu'à 200 cases, morceaux découverts cherchés à 230 cases,
// mémoire des ressources sur le disque (72 h), contrôle des données reçues, terrain gardé pas redemandé.
public class V0427Tests
{
    private static NearbyResource Res(ulong id, double x, double z, string name = "Pin mûr", string? tag = "Tree") =>
        new(id, 7, name, 2, tag, new ServerPosition(x, z), 0);

    private static string TempFile() => Path.Combine(Path.GetTempPath(), "bitcraft-v0427-" + Guid.NewGuid().ToString("N"), "ressources.bin");

    [Fact]
    public void CleanupRemovesWhatWasNotSeenFor72Hours()
    {
        var now = new DateTime(2026, 11, 27, 3, 0, 0, DateTimeKind.Utc);
        var memory = new ResourceMemory(() => now);
        memory.Update([Res(1, 100, 100)], null);
        now = now.AddHours(10);
        memory.Update([Res(2, 900, 900)], null);
        now = now.AddHours(63); // la 1re a 73 h, la 2e 63 h
        var (removed, kept) = memory.Cleanup();
        Assert.Equal(1, removed);
        Assert.Equal(1, kept);
        Assert.Null(memory.LastSeen(1));
        Assert.Equal("vue il y a 63 h", memory.SeenText(2));
    }

    [Fact]
    public void FileIsReadBackWithItsDates()
    {
        var path = TempFile();
        try
        {
            var seen = new DateTime(2026, 11, 26, 20, 0, 0, DateTimeKind.Utc);
            var memory = new ResourceMemory(() => seen);
            memory.Update([Res(1, 100, 200), Res(2, 300, 400, "Rocher", null)], null);
            Assert.True(memory.Dirty);
            Assert.Equal(2, memory.Save(path));
            Assert.False(memory.Dirty);

            var again = new ResourceMemory(() => seen.AddHours(1));
            Assert.Equal(2, again.Load(path));
            Assert.Equal(seen, again.LastSeen(1));
            var all = again.Update([], null);
            var rock = Assert.Single(all, r => r.EntityId == 2);
            Assert.Equal("Rocher", rock.Name);
            Assert.Null(rock.Tag);
            Assert.Equal(300, rock.Position.X);
            Assert.Equal(2, rock.Tier);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void BrokenFileIsIgnored()
    {
        var path = TempFile();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, [1, 2, 3]);
            Assert.Equal(0, new ResourceMemory().Load(path));
            Assert.Equal(0, new ResourceMemory().Load(path + ".absent"));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void LiveAreaRemovesACutResourceButKeepsFarOnes()
    {
        var memory = new ResourceMemory();
        var area = new Area(0, 200, 0, 200);
        memory.Update([Res(1, 100, 100), Res(2, 5000, 5000)], null);
        var remembered = memory.Update([], area); // l'arbre 1 a été coupé : absent de la zone lue
        Assert.Null(memory.LastSeen(1));
        Assert.Single(remembered, r => r.EntityId == 2);
    }

    [Fact]
    public void InvalidPositionsAndDuplicatesAreRejected()
    {
        var memory = new ResourceMemory();
        memory.Update([Res(1, -5, 10), Res(2, double.NaN, 10), Res(0, 10, 10), Res(3, 1e9, 10), Res(4, 10, 10), Res(4, 10, 10)], null);
        Assert.Equal(1, memory.Count);
        Assert.Equal(4, memory.RejectedPosition);
        Assert.Equal(1, memory.Duplicates);
    }

    [Fact]
    public void AllFamiliesAreKept()
    {
        var memory = new ResourceMemory();
        var tags = new[] { "Tree", "Rock", "Ore Vein", "Flower", "Berry", "Clay", "Fish", null };
        memory.Update(tags.Select((t, i) => Res((ulong)i + 1, 10 + i, 10, "R", t)).ToList(), null);
        Assert.Equal(tags.Length, memory.Count);
    }

    [Fact]
    public void ZoomGoesTo200AndDiscoveredRadiusIs230()
    {
        Assert.Equal(230, ServerReader.DiscoveredRadius);
        var map = File.ReadAllText(Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "src", "BitcraftBot.App", "ResourceMap.cs"));
        Assert.Contains("ViewSteps = [10, 15, 20, 30, 45, 65, 100, 140, 200]", map);
    }

    [Fact]
    public void FreshTerrainIsNotAskedAgainExceptAroundThePlayer()
    {
        var area = Area.Around(6852 * 3, 9082 * 3, ServerReader.ResourceRadius);
        var all = Queries.Terrain(area, 42);
        var some = Queries.Terrain(area, 42, skip: _ => true);
        // Toujours les 3 × 3 autour du personnage, et les zones découvertes.
        var (cx, cz) = TerrainChunk.ChunkOf(6852, 9082);
        Assert.Contains(some, q => q.EndsWith($"= {TerrainChunk.IndexOf(cx, cz)}"));
        Assert.Contains(some, q => q.Contains("exploration_chunks_state_v2"));
        Assert.True(some.Count <= 10);
        Assert.True(some.Count <= all.Count);
    }

    [Fact]
    public void TerrainStoreFreshnessFollowsTheClock()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-v0427-" + Guid.NewGuid().ToString("N"));
        try
        {
            var now = new DateTime(2026, 11, 27, 3, 0, 0, DateTimeKind.Utc);
            var store = new TerrainStore(folder) { Clock = () => now };
            var chunk = new TerrainChunk(TerrainChunk.IndexOf(3, 4), 3, 4, 1, new uint[TerrainChunk.Cells], new uint[TerrainChunk.Cells],
                new short[TerrainChunk.Cells], new short[TerrainChunk.Cells]);
            Assert.False(store.IsFresh(chunk.Index, ServerReader.TerrainFreshFor));
            store.Put(chunk);
            Assert.True(store.IsFresh(chunk.Index, ServerReader.TerrainFreshFor));
            now = now.AddHours(25);
            Assert.False(store.IsFresh(chunk.Index, ServerReader.TerrainFreshFor));
            store.Put(chunk with { }); // même contenu reçu : la date est remise à jour
            Assert.True(store.IsFresh(chunk.Index, ServerReader.TerrainFreshFor));

            // Relu depuis le disque : la date d'écriture du fichier sert de date de réception.
            var again = new TerrainStore(folder) { Clock = () => DateTime.UtcNow };
            Assert.Equal(1, again.LoadAll());
            Assert.True(again.IsFresh(chunk.Index, ServerReader.TerrainFreshFor));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
