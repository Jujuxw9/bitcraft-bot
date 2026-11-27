using System.Globalization;
using BitcraftBot.Core.Monitoring;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.23 : overlay (système arrondi), zones découvertes lues une fois, mémoire de la carte (version).
public class V0423Tests
{
    [Fact]
    public void OverlayRoundsCpuAndRam()
    {
        Assert.Equal(1.5, SystemSummary.RoundCpu(1.3));
        Assert.Equal(1.0, SystemSummary.RoundCpu(1.2));
        Assert.Equal(585, SystemSummary.RoundRam(586));
        Assert.Equal(590, SystemSummary.RoundRam(588));
        var sample = new ResourceSample
        {
            At = DateTime.Now,
            BotCpuPercent = 2.3,
            BotRamMb = 603,
            BotManagedMb = 50,
            BotThreads = 20,
            BotHandles = 500,
            SystemCpuPercent = 10,
            SystemRamTotalMb = 32000,
            SystemRamAvailableMb = 16000,
            GameCpuPercent = 16.6,
            GameRamMb = 4421,
        };
        var text = SystemSummary.Overlay(sample, CultureInfo.GetCultureInfo("fr-FR"));
        // v0.4.25 : ligne courte (overlay plus étroit).
        Assert.StartsWith("Bot 2,5 % · 605 Mo", text);
        Assert.Contains("Jeu 17 % · 4,3 Go", text);
    }

    [Fact]
    public void DiscoveredChunksAreAddedOnce()
    {
        var area = Area.Around(27600, 27600, 90);
        var normal = Queries.Terrain(area, 1);
        var near = TerrainChunk.ChunksAround(area.CenterX / 3.0, area.CenterZ / 3.0, Queries.TerrainRadius)
            .Select(c => TerrainChunk.IndexOf(c.X, c.Z)).ToList();
        // Un morceau déjà lu autour n'est pas demandé deux fois ; un morceau lointain s'ajoute.
        var withExtra = Queries.Terrain(area, 1, [near[0], 123456]);
        Assert.Equal(normal.Count + 1, withExtra.Count);
        Assert.Contains(withExtra, q => q.EndsWith("chunk_index = 123456", StringComparison.Ordinal));
    }

    [Fact]
    public void TerrainStoreVersionChangesOnlyOnNews()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-v0423-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new TerrainStore(folder);
            var chunk = new TerrainChunk(TerrainChunk.IndexOf(3, 4), 3, 4, 1, new uint[TerrainChunk.Cells], new uint[TerrainChunk.Cells],
                new short[TerrainChunk.Cells], new short[TerrainChunk.Cells]);
            Assert.False(store.Contains(chunk.Index));
            Assert.True(store.Put(chunk));
            var version = store.Version;
            Assert.True(store.Contains(chunk.Index));
            Assert.False(store.Put(chunk with { }));
            Assert.Equal(version, store.Version);
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
