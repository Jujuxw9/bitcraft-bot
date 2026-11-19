using BitcraftBot.GameData;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Perception.Ocr;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace BitcraftBot.Tests;

public class HudParserTests
{
    [Theory]
    [InlineData("N 8927, E 6707", 8927, 6707)]
    [InlineData("N 8927. E 6707", 8927, 6707)]
    [InlineData("N8927,E6707", 8927, 6707)]
    [InlineData("N 8927, E 67O7", 8927, 6707)]

    [InlineData("N 8932, E 6 69 7", 8932, 6697)] // espaces insérés par l'OCR (vu en réel)
    [InlineData("N 8 932, E 6697", 8932, 6697)]
    public void ParseCoordinates_Ok(string text, int north, int east) =>
        Assert.Equal((north, east), HudParser.ParseCoordinates(text));

    [Theory]
    [InlineData("")]
    [InlineData("SAFE MEADOWS")]
    [InlineData("N , E")]
    [InlineData("N 8933, E 6 69 Q")] // vu en réel : le 2 lu comme Q ; refusé plutôt que faux
    public void ParseCoordinates_Rejects(string text) => Assert.Null(HudParser.ParseCoordinates(text));

    [Theory]
    [InlineData("65/100", 65, 100)]
    [InlineData("65 / 100", 65, 100)]
    [InlineData("1OO/1OO", 100, 100)]
    [InlineData("893/12000", 893, 12000)]
    [InlineData("0/100", 0, 100)]
    public void ParseFraction_Ok(string text, int value, int max) =>
        Assert.Equal((value, max), HudParser.ParseFraction(text));

    [Theory]
    [InlineData("150/100")] // valeur au-dessus du maximum : lecture fausse
    [InlineData("100")]
    [InlineData("")]
    public void ParseFraction_Rejects(string text) => Assert.Null(HudParser.ParseFraction(text));

    [Theory]
    [InlineData("KELDAN (R18)", "Keldan", 18)]
    [InlineData("KELDAN ( R18 )", "Keldan", 18)]
    [InlineData("KELDAN (RI8)", "Keldan", 18)]
    public void ParseRegion_Ok(string text, string name, int number) =>
        Assert.Equal((name, number), HudParser.ParseRegion(text));

    [Theory]
    [InlineData("KELDA N", "KELDAN")]
    [InlineData("SAFE MEADOWS", "SAFE MEADOWS")]
    public void MergeLooseLetters(string text, string expected) => Assert.Equal(expected, HudParser.MergeLooseLetters(text));

    [Theory]
    [InlineData("Maturc Oak Tree", "Mature Oak Tree")]
    [InlineData("Young 0ak Tree", "Young Oak Tree")]
    [InlineData("Large Ferra1ith Vein", "Large Ferralith Vein")]
    [InlineData("Mature Oak Tree", "Mature Oak Tree")]
    public void NameMatcher_CorrectsSmallErrors(string text, string expected) =>
        Assert.Equal(expected, BitcraftBot.Perception.Ocr.NameMatcher.BestMatch(text, KnownNames.Resources));

    [Theory]
    [InlineData("Wolf")]
    [InlineData("Some items couldn't fit")]
    public void NameMatcher_RejectsUnknown(string text) =>
        Assert.Null(BitcraftBot.Perception.Ocr.NameMatcher.BestMatch(text, KnownNames.Resources));

    [Fact]
    public void CleanName_TitleCase() => Assert.Equal("Safe Meadows", HudParser.CleanName("  SAFE   MEADOWS ", titleCase: true));
}

public class BarReaderTests
{
    // Image de test : une ligne avec n pixels d'une couleur à partir de start, le reste noir.
    private static Frame Line(int width, int row, int start, int count, (byte R, byte G, byte B) color)
    {
        var pixels = new byte[width * (row + 1) * 4];
        for (var x = start; x < start + count; x++)
        {
            var i = (row * width + x) * 4;
            pixels[i] = color.B;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.R;
            pixels[i + 3] = 255;
        }
        return new Frame { Pixels = pixels, Width = width, Height = row + 1, CapturedAt = DateTime.Now, Source = new(0, 0, width, row + 1) };
    }

    [Fact]
    public void StaminaBar_HalfFull()
    {
        // Barre de 100 pixels (de 10 à 109), 50 pixels jaunes.
        var frame = Line(200, 5, 10, 50, (204, 170, 0));
        var fill = BarReader.LeftAnchoredFill(frame, 5, 10, 109, BarReader.IsStaminaYellow);
        Assert.Equal(0.5, fill!.Value, precision: 2);
    }

    [Fact]
    public void HungerLine_RightAnchored_70Percent()
    {
        // Ligne de 100 pixels (de 0 à 99), blanche sur les 70 derniers pixels.
        var frame = Line(120, 3, 30, 70, (254, 254, 254));
        var fill = BarReader.RightAnchoredFill(frame, 3, 0, 99, BarReader.IsWhite);
        Assert.Equal(0.7, fill!.Value, precision: 2);
    }

    [Fact]
    public void EmptyBar_IsZero()
    {
        var frame = Line(200, 2, 0, 0, (0, 0, 0));
        Assert.Equal(0, BarReader.LeftAnchoredFill(frame, 2, 10, 109, BarReader.IsHealthGreen));
    }

    [Fact]
    public void FrameTooSmall_ReturnsNull()
    {
        var frame = Line(50, 2, 0, 10, (0, 0, 0));
        Assert.Null(BarReader.LeftAnchoredFill(frame, 2, 10, 109, BarReader.IsHealthGreen));
    }
}

public class HudReaderReferenceTests
{
    private static readonly string ReferenceImage = Path.Combine(FindRepoRoot(), "images", "reference-ui",
        "20261116_reference-ui_hud-coupe-arbre_001.png");

    [Fact]
    public async Task ReadsReferenceCapture()
    {
        var reader = OcrReader.TryCreate();
        if (reader is null)
        {
            // Pas d'OCR anglais sur cette machine (ex. serveur de build) : test non applicable.
            return;
        }
        var frame = await LoadFrameAsync(ReferenceImage);
        var hud = await new HudReader(reader, KnownNames.Resources).ReadAsync(frame);

        Assert.NotNull(hud.Target);
        Assert.Equal("Mature Oak Tree", hud.Target!.Name);
        Assert.Equal("Tier 1 - Tree", hud.Target.Category);
        Assert.Equal(893, hud.Target.Health);
        Assert.Equal(12000, hud.Target.MaxHealth);
    }

    internal static async Task<Frame> LoadFrameAsync(string path)
    {
        var file = await StorageFile.GetFileFromPathAsync(path);
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyToBuffer(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(pixels));
        return new Frame
        {
            Pixels = pixels,
            Width = bitmap.PixelWidth,
            Height = bitmap.PixelHeight,
            CapturedAt = DateTime.Now,
            Source = new ScreenRect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
        };
    }

    internal static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BitcraftBot.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Racine du dépôt introuvable.");
    }
}
