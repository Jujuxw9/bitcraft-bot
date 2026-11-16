using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;

namespace BitcraftBot.Tests;

// v0.3 : lecture des coordonnées par modèles de caractères (Q-016).
public class GlyphReaderTests
{
    private static readonly string FixturesFolder = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", "coordonnees");

    public static IEnumerable<object[]> Fixtures() =>
        Directory.GetFiles(FixturesFolder, "*.png").Order().Select(f => new object[] { Path.GetFileName(f) });

    // Chaque découpe étiquetée (nom = texte attendu) doit être lue exactement.
    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task ReadsEveryLabeledCrop(string fileName)
    {
        var parts = fileName.Split('_');
        var expected = (int.Parse(parts[0][1..]), int.Parse(parts[1][1..]));
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Path.Combine(FixturesFolder, fileName));

        var position = GlyphReader.ReadCoordinates(frame, new ScreenRect(0, 0, frame.Width, frame.Height), out var text);

        Assert.True(position == expected, $"Lu « {text} » au lieu de {expected}");
    }

    // Sur une capture complète 1920×1080, la zone de recherche du HUD suffit.
    [Theory]
    [InlineData("20261116_reference-ui_hud-coupe-arbre_001.png", 8927, 6707)]
    [InlineData("20261116_reference-ui_hud-cueillette-buisson_002.png", 8933, 6692)]
    public async Task ReadsFullCaptureInHudArea(string fileName, int north, int east)
    {
        var path = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", fileName);
        var frame = await HudReaderReferenceTests.LoadFrameAsync(path);

        var position = GlyphReader.ReadCoordinates(frame, HudLayout.CoordinatesSearch, out var text);

        Assert.True(position == (north, east), $"Lu « {text} »");
    }

    [Fact]
    public void AllCharactersHaveATemplate() =>
        Assert.Equal("0123456789EN", string.Concat(GlyphTemplates.Coordinates.Keys.Order()));

    // Deux caractères différents ne doivent pas se ressembler au point d'être confondus.
    [Fact]
    public void TemplatesAreWellSeparated()
    {
        var templates = GlyphTemplates.Coordinates.ToList();
        for (var i = 0; i < templates.Count; i++)
        {
            for (var j = i + 1; j < templates.Count; j++)
            {
                var distance = GlyphReader.Distance(templates[i].Value, templates[j].Value);
                Assert.True(distance > 0.1, $"{templates[i].Key} et {templates[j].Key} trop proches ({distance:0.000})");
            }
        }
    }

    // Image sans texte blanc : aucune lecture inventée.
    [Fact]
    public void ReturnsNothingOnEmptyImage()
    {
        var frame = new Frame
        {
            Pixels = Enumerable.Repeat((byte)90, 300 * 60 * 4).ToArray(),
            Width = 300,
            Height = 60,
            CapturedAt = DateTime.Now,
            Source = new ScreenRect(0, 0, 300, 60),
        };
        Assert.Null(GlyphReader.ReadCoordinates(frame, new ScreenRect(0, 0, 300, 60), out _));
    }
}

public class PositionFilterTests
{
    private static readonly DateTime T0 = new(2026, 11, 16, 18, 0, 0);

    [Fact]
    public void AcceptsNormalWalk()
    {
        var filter = new PositionFilter();
        Assert.Equal((8945, 6704), filter.Accept((8945, 6704), T0, out _));
        Assert.Equal((8948, 6705), filter.Accept((8948, 6705), T0.AddSeconds(0.5), out var rejected));
        Assert.False(rejected);
    }

    [Fact]
    public void RejectsIsolatedJump()
    {
        var filter = new PositionFilter();
        filter.Accept((8945, 6704), T0, out _);
        // Un 8 lu comme un 6 : 2 000 unités en 0,5 s, impossible.
        Assert.Null(filter.Accept((6945, 6704), T0.AddSeconds(0.5), out var rejected));
        Assert.True(rejected);
        // La lecture suivante, correcte, est acceptée normalement.
        Assert.Equal((8945, 6705), filter.Accept((8945, 6705), T0.AddSeconds(1), out _));
    }

    [Fact]
    public void AcceptsConfirmedTeleport()
    {
        var filter = new PositionFilter();
        filter.Accept((8945, 6704), T0, out _);
        Assert.Null(filter.Accept((5000, 3000), T0.AddSeconds(0.5), out _));
        // Deuxième lecture identique : vraie téléportation.
        Assert.Equal((5000, 3000), filter.Accept((5000, 3000), T0.AddSeconds(1), out var rejected));
        Assert.False(rejected);
    }

    [Fact]
    public void FailedReadDoesNotChangeAnything()
    {
        var filter = new PositionFilter();
        filter.Accept((8945, 6704), T0, out _);
        Assert.Null(filter.Accept(null, T0.AddSeconds(0.5), out var rejected));
        Assert.False(rejected);
        Assert.Equal((8945, 6704), filter.Last);
    }
}
