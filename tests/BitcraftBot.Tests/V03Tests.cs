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

// v0.3.1 : jauge de métier à gauche de la minicarte (demande de Julien).
public class ProfessionGaugeTests
{
    private static readonly string Folder = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", "jauge-metier");

    public static IEnumerable<object[]> Fixtures() =>
        Directory.GetFiles(Folder, "*.png").Order().Select(f => new object[] { Path.GetFileName(f) });

    // Nom du fichier : Metier_niveau_pourcentage_xxx.png (« absente » = pas de jauge). Découpe dont le coin est (1480, 0).
    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task ReadsEveryLabeledCrop(string fileName)
    {
        var parts = fileName.Split('_');
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Path.Combine(Folder, fileName));

        var gauge = ProfessionGaugeReader.Read(frame, (0, 0));

        if (parts[0] == "absente")
        {
            Assert.Null(gauge);
            return;
        }
        Assert.NotNull(gauge);
        Assert.Equal(parts[0], gauge!.Profession);
        Assert.Equal(int.Parse(parts[1]), gauge.Level);
        Assert.InRange(gauge.Fill * 100, int.Parse(parts[2]) - 2, int.Parse(parts[2]) + 2);
    }

    // Capture complète 1920×1080 : la jauge est lue à sa place normale.
    [Fact]
    public async Task ReadsGaugeOnFullReferenceCapture()
    {
        var path = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", "20261116_reference-ui_hud-coupe-arbre_001.png");
        var frame = await HudReaderReferenceTests.LoadFrameAsync(path);
        var gauge = ProfessionGaugeReader.Read(frame);
        Assert.NotNull(gauge);
        Assert.Equal("Forestry", gauge!.Profession);
        Assert.Equal(23, gauge.Level);
    }

    // Jauge dessinée : cadre blanc, intérieur vert jusqu'à une hauteur donnée → pourcentage attendu.
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public void MeasuresDrawnFill(double fill)
    {
        const int width = 140, height = 340;
        var pixels = new byte[width * height * 4];
        void Set(int x, int y, byte r, byte g, byte b)
        {
            var i = (y * width + x) * 4;
            pixels[i] = b; pixels[i + 1] = g; pixels[i + 2] = r; pixels[i + 3] = 255;
        }
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Set(x, y, 26, 44, 38);
            }
        }
        var top = (int)Math.Round(245 - fill * (245 - 22));
        for (var y = 22; y <= 245; y++)
        {
            Set(54, y, 188, 189, 189);
            Set(55, y, 188, 189, 189);
            Set(70, y, 188, 189, 189);
            Set(71, y, 188, 189, 189);
            for (var x = 57; x <= 68; x++)
            {
                if (y is 78 or 134 or 190)
                {
                    // Trait blanc d'un quart (blanc-vert sur la partie remplie, comme dans le jeu).
                    Set(x, y, 188, 222, 193);
                }
                else if (y >= top && fill > 0)
                {
                    Set(x, y, 17, 182, 74);
                }
            }
        }
        var frame = new Frame { Pixels = pixels, Width = width, Height = height, CapturedAt = DateTime.Now, Source = new ScreenRect(0, 0, width, height) };

        var gauge = ProfessionGaugeReader.Read(frame, (0, 0));

        Assert.NotNull(gauge);
        Assert.InRange(gauge!.Fill, fill - 0.01, fill + 0.01);
        // Pas d'icône ni de niveau dessinés : inconnus, sans invention.
        Assert.Null(gauge.Profession);
        Assert.Null(gauge.Level);
    }
}

// v0.3.2 : zone et région confirmées sur deux lectures ; images de diagnostic.
public class ConfirmedTextTests
{
    [Fact]
    public void IgnoresIsolatedMisread()
    {
        var region = new BitcraftBot.Core.Logging.ConfirmedText();
        Assert.Null(region.Update("Keldan (R18)"));
        Assert.Null(region.Update("Keldan (R10)")); // lecture ratée isolée (journal du 16/11 à 19:46)
        Assert.Null(region.Update("Keldan (R18)"));
        Assert.Null(region.Update(null));
    }

    [Fact]
    public void ConfirmsRealChange()
    {
        var zone = new BitcraftBot.Core.Logging.ConfirmedText();
        zone.Update("Safe Meadows");
        Assert.Null(zone.Update("Calm Forest"));
        Assert.Equal(("Safe Meadows", "Calm Forest"), zone.Update("Calm Forest"));
        Assert.Null(zone.Update("Calm Forest"));
    }
}

public class FrameSaverTests
{
    [Fact]
    public async Task SavesReadablePng()
    {
        var path = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", "20261116_reference-ui_hud-coupe-arbre_001.png");
        var frame = await HudReaderReferenceTests.LoadFrameAsync(path);
        var output = Path.Combine(Path.GetTempPath(), "bitcraftbot-tests", Guid.NewGuid().ToString("N"), "diagnostic.png");

        await FrameSaver.SavePngAsync(frame, output);

        // L'image relue donne la même lecture de la position.
        var reloaded = await HudReaderReferenceTests.LoadFrameAsync(output);
        Assert.Equal((8927, 6707), GlyphReader.ReadCoordinates(reloaded, HudLayout.CoordinatesSearch, out _));
    }
}

public class CaptureKeyTests
{
    [Theory]
    [InlineData(0x52, false, true)]  // Num 0
    [InlineData(0x52, true, false)]  // Inser (touche étendue, même scancode)
    [InlineData(0x4C, false, false)] // Num 5 = arrêt d'urgence, pas une capture
    public void RecognizesOnlyNumpad0(int scanCode, bool extended, bool expected) =>
        Assert.Equal(expected, BitcraftBot.Input.Keyboard.CaptureKey.IsCapture(scanCode, extended));
}
