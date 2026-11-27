using System.Diagnostics;
using System.IO;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.28 : carte, partie affichage : fond calculé point par point (hors interface), carte plus grande, tableau plus étroit.
public class V0428Tests
{
    private static TerrainChunk Chunk(int x, int z, uint biome = 1) =>
        new(TerrainChunk.IndexOf(x, z), x, z, TerrainChunk.Overworld,
            Enumerable.Repeat(biome, TerrainChunk.Cells).ToArray(), new uint[TerrainChunk.Cells],
            new short[TerrainChunk.Cells], new short[TerrainChunk.Cells]);

    private static TerrainImage Image(int side)
    {
        var chunks = new List<TerrainChunk>();
        for (var x = 0; x < side; x++)
        {
            for (var z = 0; z < side; z++)
            {
                chunks.Add(Chunk(200 + x, 280 + z, (uint)(1 + (x + z) % 3)));
            }
        }
        return TerrainImage.Build(chunks, null)!;
    }

    [Fact]
    public void RasterHasTheSameSizeAsTheOldDrawing()
    {
        var image = Image(2);
        var pixels = HexRaster.Render(image, 6, out var w, out var h);
        Assert.Equal((int)Math.Ceiling((image.Width + 0.5) * 6), w);
        Assert.Equal((int)Math.Ceiling(image.Height * 6 * Math.Sqrt(3) / 2), h);
        Assert.Equal(w * h, pixels.Length);
    }

    [Fact]
    public void EachCellCenterHasItsCellColorAndThereAreNoHoles()
    {
        var image = Image(2);
        const int hp = 10;
        var pixels = HexRaster.Render(image, hp, out var w, out var h);
        var rowPixels = hp * HexRaster.RowFactor;
        for (var row = 0; row < image.Height; row += 7)
        {
            var shift = ((image.NorthCell - row) & 1) == 1 ? 0.5 : 0.0;
            for (var col = 0; col < image.Width; col += 5)
            {
                var x = (int)((col + 0.5 + shift) * hp);
                var y = (int)((row + 0.5) * rowPixels);
                Assert.Equal(image.Pixels[row * image.Width + col], pixels[y * w + x]);
            }
        }
        // Aucun trou à l'intérieur (les bords gauche et droit gardent la demi-case de décalage).
        for (var y = hp; y < h - hp; y++)
        {
            for (var x = hp; x < w - hp; x++)
            {
                Assert.NotEqual(0u, pixels[y * w + x]);
            }
        }
    }

    [Fact]
    public void EdgesOnlyWhenCellsAreBig()
    {
        var image = Image(1);
        var small = HexRaster.Render(image, 4, out _, out _);
        var big = HexRaster.Render(image, 12, out _, out _);
        var colors = image.Pixels.ToHashSet();
        Assert.All(small.Where(p => p != 0), p => Assert.Contains(p, colors));
        Assert.Contains(big, p => p != 0 && !colors.Contains(p));
    }

    [Fact]
    public void ViewOf200CasesIsComputedQuickly()
    {
        // ≈ 240 morceaux à 200 cases de vue : ici 16 × 16 = 256 morceaux.
        var image = Image(16);
        var hp = HexRaster.PixelsPerCell(image);
        Assert.InRange(hp, 2, 5);
        var watch = Stopwatch.StartNew();
        var pixels = HexRaster.Render(image, hp, out var w, out var h);
        watch.Stop();
        Assert.True(w <= 2600 && h <= 2600);
        Assert.True(watch.ElapsedMilliseconds < 3000, $"{watch.ElapsedMilliseconds} ms");
        Assert.Equal(w * h, pixels.Length);
    }

    [Fact]
    public void MapIsLargeAndTableIsNarrow()
    {
        var root = HudReaderReferenceTests.FindRepoRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "BitcraftBot.App", "MainWindow.xaml"));
        Assert.DoesNotContain("MaxWidth=\"560\"", xaml);
        Assert.DoesNotContain("Header=\"Famille\"", xaml);
        Assert.DoesNotContain("Header=\"Position\"", xaml);
        Assert.Contains("Header=\"Santé\"", xaml);
        var map = File.ReadAllText(Path.Combine(root, "src", "BitcraftBot.App", "ResourceMap.cs"));
        Assert.DoesNotContain("DrawGeometry", map);
        Assert.Contains("Task.Run", map);
        Assert.Contains("CellRightClicked", map);
    }
}
