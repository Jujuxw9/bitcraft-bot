using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server.Aim;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.3 : direction de la caméra (cône de la mini-carte) et fenêtres du jeu ouvertes.
public class V043Tests
{
    private static string Reference(params string[] parts) =>
        Path.GetFullPath(Path.Combine([HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", .. parts]));

    // Caps vérifiés à l'œil sur une planche de 48 mini-cartes (2026-11-23) : le trait coupe le cône en son milieu.
    [Theory]
    [InlineData("cap-000_repere-sur-le-cone_001.png", 0)]
    [InlineData("cap-001_degage_001.png", 1)]
    [InlineData("cap-054_sur-un-batiment_001.png", 54)]
    [InlineData("cap-080_degage_001.png", 80)]
    [InlineData("cap-237_degage_001.png", 237)]
    public async Task Direction_de_la_camera_lue_sur_la_mini_carte(string file, double expected)
    {
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("minicarte", file));
        var heading = MinimapCone.Read(frame, out var why);
        Assert.True(heading is not null, why);
        Assert.True(CameraHeading.Gap(heading.Bearing, expected) <= 12, $"{file} : lu {heading.Bearing:0}°, attendu {expected}°");
    }

    [Fact]
    public async Task Pas_de_direction_sans_mini_carte()
    {
        // Écran de connexion : pas de mini-carte, pas de personnage.
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("ecrans", "20261122_reference-ui_ecran-connexion_001.png"));
        Assert.Null(MinimapCone.Read(frame, out var why));
        Assert.Contains("personnage", why);
    }

    [Theory]
    [InlineData(0, "nord")]
    [InlineData(44, "nord-est")]
    [InlineData(90, "est")]
    [InlineData(181, "sud")]
    [InlineData(250, "ouest")]
    [InlineData(338, "nord")]
    public void Nom_de_la_direction(double bearing, string name) => Assert.Equal(name, CameraHeading.Compass(bearing));

    [Fact]
    public void Ecart_entre_deux_caps()
    {
        Assert.Equal(20, CameraHeading.Gap(350, 10));
        Assert.Equal(180, CameraHeading.Gap(0, 180));
        Assert.Equal("vers l'est (90°)", new CameraHeading(90, 1, 100).Toward);
        Assert.Equal("vers le sud-ouest (225°)", new CameraHeading(225, 1, 100).Toward);
    }

    [Theory]
    [InlineData("fenetres", "inventaire-et-chariot_001.png", 1, WindowKind.Fenetre)]
    [InlineData("fenetres", "banque_001.png", 2, WindowKind.Fenetre)]
    [InlineData("fenetres", "carte-du-monde_001.png", 1, WindowKind.Carte)]
    public async Task Fenetres_ouvertes_vues(string folder, string file, int count, WindowKind kind)
    {
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference(folder, file));
        var windows = GameWindows.Detect(frame);
        Assert.Equal(count, windows.Count);
        Assert.All(windows, w => Assert.Equal(kind, w.Kind));
    }

    // Le HUD normal (cible, panneau d'action, objet ramassé, bandeau « ENTERING ») n'est pas une fenêtre.
    [Theory]
    [InlineData("fenetres", "aucune-objet-ramasse_001.png")]
    [InlineData("ecrans", "20261122_reference-ui_ecran-jeu-bandeau_001.png")]
    [InlineData("ecrans", "20261122_reference-ui_ecran-jeu-coupe_001.png")]
    [InlineData("ecrans", "20261122_reference-ui_ecran-connexion_001.png")]
    [InlineData("", "20261116_reference-ui_hud-coupe-arbre_001.png")]
    [InlineData("", "20261116_reference-ui_hud-cueillette-buisson_002.png")]
    public async Task Hud_normal_sans_fenetre(string folder, string file)
    {
        var frame = await HudReaderReferenceTests.LoadFrameAsync(folder == "" ? Reference(file) : Reference(folder, file));
        Assert.Empty(GameWindows.Detect(frame));
    }

    [Fact]
    public async Task Titre_de_la_fenetre_lu()
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("fenetres", "inventaire-et-chariot_001.png"));
        var window = await GameWindows.ReadTitleAsync(ocr, frame, GameWindows.Detect(frame)[0]);
        Assert.Contains("INVENTORY", window.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INVENTORY", window.French, StringComparison.OrdinalIgnoreCase);
    }

    // Toutes les captures de coordonnées (jour, nuit, rivière, survols) : aucune fenêtre ouverte.
    [Fact]
    public async Task Captures_de_reference_sans_fenetre()
    {
        foreach (var file in Directory.GetFiles(Reference("coordonnees"), "*.png"))
        {
            var frame = await HudReaderReferenceTests.LoadFrameAsync(Path.GetFullPath(file));
            Assert.True(GameWindows.Detect(frame).Count == 0, Path.GetFileName(file));
        }
    }

    // La direction de la caméra est notée avec chaque clic appris (pour la formule de la v0.4.4).
    [Fact]
    public void Direction_notee_avec_le_clic()
    {
        var pair = new CameraPair { Heading = 80 };
        var json = System.Text.Json.JsonSerializer.Serialize(new[] { pair });
        Assert.Contains("\"Heading\":80", json);
        var old = System.Text.Json.JsonSerializer.Deserialize<List<CameraPair>>("[{\"East\":1,\"North\":2}]")!;
        Assert.Null(old[0].Heading);
    }
}
