using BitcraftBot.Core.Monitoring;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.22 : RAM (pics brefs ignorés), progression des fabrications, mémoire de la carte.
public class V0422Tests
{
    private static ResourceSample Ram(double mb) => new()
    {
        At = DateTime.Now, BotCpuPercent = 1, BotRamMb = mb, BotManagedMb = 50, BotThreads = 20, BotHandles = 500,
        SystemCpuPercent = 10, SystemRamTotalMb = 32000, SystemRamAvailableMb = 16000,
    };

    [Fact]
    public void BriefRamPeakIsIgnored()
    {
        var limits = new ResourceLimits();
        Assert.Equal(1000, limits.BotRamWarningMb);
        Assert.Equal(1600, limits.BotRamCriticalMb);
        var guard = new ResourceGuard(limits);
        Assert.Empty(guard.Evaluate(Ram(1100)));   // pic au démarrage
        Assert.Empty(guard.Evaluate(Ram(600)));
        Assert.Empty(guard.Evaluate(Ram(1100)));
        Assert.Empty(guard.Evaluate(Ram(1100)));
        Assert.Equal(ResourceLevel.Alerte, Assert.Single(guard.Evaluate(Ram(1100))).Level); // 3 mesures de suite
    }

    [Fact]
    public void CraftProgressUsesAllItems()
    {
        // 3 objets de 10 gestes : 15 gestes faits = moitié.
        Assert.Equal(0.5, new CraftProgress(7, 15, 3).Done(10));
        Assert.Equal(0, new CraftProgress(7, 15, 3).Done(0));
        Assert.Equal(1, new CraftProgress(7, 40, 3).Done(10));
    }

    [Fact]
    public void TerrainChunkSurvivesTheDisk()
    {
        var n = TerrainChunk.Cells;
        var chunk = new TerrainChunk(TerrainChunk.IndexOf(215, 286), 215, 286, TerrainChunk.Overworld,
            Enumerable.Range(0, n).Select(i => (uint)(i % 7)).ToArray(), Enumerable.Repeat(255u, n).ToArray(),
            Enumerable.Range(0, n).Select(i => (short)(i % 40)).ToArray(), new short[n]);
        var back = TerrainStore.Read(TerrainStore.Write(chunk));
        Assert.NotNull(back);
        Assert.Equal(chunk.Index, back.Index);
        Assert.Equal(chunk.Elevations, back.Elevations);
        Assert.Null(TerrainStore.Read([1, 2, 3]));

        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-carte-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new TerrainStore(folder);
            Assert.True(store.Put(chunk));
            Assert.False(store.Put(chunk)); // même terrain : rien à réécrire
            var reloaded = new TerrainStore(folder);
            Assert.Equal(1, reloaded.LoadAll());
            Assert.Single(reloaded.Around(215 * 32 + 10, 286 * 32 + 10, 5));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void RememberedResourcesForgetWhatIsGoneNearby()
    {
        NearbyResource Tree(ulong id, double x, double z) => new(id, 1, "Arbre", 1, "Tree", new ServerPosition(x, z), 0);
        var memory = new ResourceMemory();
        var area = new Area(0, 100, 0, 100);
        Assert.Empty(memory.Update([Tree(1, 10, 10), Tree(2, 50, 50)], area));
        // Le personnage part plus loin : les deux arbres sont gardés, hors de la nouvelle zone.
        var far = new Area(1000, 1100, 1000, 1100);
        Assert.Equal(2, memory.Update([Tree(3, 1050, 1050)], far).Count);
        // Retour : l'arbre 2 a été coupé (absent de la lecture en direct) → oublié.
        Assert.Single(memory.Update([Tree(1, 10, 10)], area), r => r.EntityId == 3);
    }
}
