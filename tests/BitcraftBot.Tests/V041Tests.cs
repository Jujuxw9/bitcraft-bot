using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.1 : ce que voit le bot (bureau, autre programme, jeu réduit, accueil, connexion, chargement, en jeu).
public class V041ScreenTests
{
    private static string Reference(params string[] parts) =>
        Path.Combine([HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", .. parts]);

    [Fact]
    public async Task Capture_en_jeu_reconnue_par_les_coordonnees()
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("20261116_reference-ui_hud-coupe-arbre_001.png"));
        var reading = await new GameScreenClassifier(ocr).ClassifyAsync(frame);
        Assert.Equal(GameScreen.EnJeu, reading.Screen);
        Assert.Contains("coordonnées", reading.Evidence);
    }

    [Fact]
    public async Task Ecran_de_chargement_reconnu()
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("ecrans", "20261116_reference-ui_ecran-chargement_001.png"));
        var reading = await new GameScreenClassifier(ocr).ClassifyAsync(frame);
        Assert.Equal(GameScreen.Chargement, reading.Screen);
    }

    // Captures de Julien du 2026-11-22 (e-mail et discussion masqués).
    [Theory]
    [InlineData("20261122_reference-ui_ecran-accueil_001.png", GameScreen.Accueil)]
    [InlineData("20261122_reference-ui_ecran-connexion_001.png", GameScreen.Connexion)]
    [InlineData("20261122_reference-ui_ecran-jeu-bandeau_001.png", GameScreen.EnJeu)]
    [InlineData("20261122_reference-ui_ecran-jeu-coupe_001.png", GameScreen.EnJeu)]
    public async Task Captures_du_22_11_reconnues(string file, GameScreen expected)
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("ecrans", file));
        var reading = await new GameScreenClassifier(ocr).ClassifyAsync(frame);
        Assert.True(expected == reading.Screen, $"{file} : {reading.Screen} ({reading.Evidence}) {reading.Text}");
    }

    // Deux lectures en même temps (HUD + « ce que voit le bot ») : plus d'erreur « Another RecognizeAsync… ».
    [Fact]
    public async Task Lectures_simultanees_sans_erreur()
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("ecrans", "20261122_reference-ui_ecran-jeu-coupe_001.png"));
        var classifier = new GameScreenClassifier(ocr);
        var readings = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => classifier.ClassifyAsync(frame))));
        Assert.All(readings, r => Assert.Equal(GameScreen.EnJeu, r.Screen));
    }

    [Fact]
    public async Task Image_noire_ecran_inconnu()
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = new Frame
        {
            Pixels = new byte[1920 * 1080 * 4],
            Width = 1920,
            Height = 1080,
            CapturedAt = DateTime.Now,
            Source = new ScreenRect(0, 0, 1920, 1080),
        };
        var reading = await new GameScreenClassifier(ocr).ClassifyAsync(frame);
        Assert.Equal(GameScreen.Inconnu, reading.Screen);
    }

    [Theory]
    [InlineData("LOADING Initializing player...", GameScreen.Chargement)]
    [InlineData("Welcome back LOGIN Settings Quit", GameScreen.Connexion)]
    [InlineData("PRESS ANY BUTTON TO START", GameScreen.Accueil)]
    [InlineData("Press any key to continue", GameScreen.Accueil)]
    [InlineData("WELCOME WORLD Starfell ENTER BITCRAFT LOG OUT", GameScreen.Connexion)]
    // Texte réel du 22/11 à 23:44:58 (le début seulement) : les mots utiles sont plus loin que 300 caractères.
    [InlineData("BITCRÅFT Starfell English BitCraft 1.0 World is now available! 1.0 UPDATE - Starfell Awakened " +
                "gitCrcft s 1.0 update brings major overhauls to manv game systems and content. Full list of changes can be " +
                "found in the patchnotes and BitCraft Online Welcome to aircraft Online! If you do not receive an email code, " +
                "please check your spam settings. HAVING TROUBLE? Visit our support site CHANGE YOUR EMAIL", GameScreen.Connexion)]
    [InlineData("Mature Oak Tree Axe Tier 1 11923/12000 Forestry 30 3:46 O N 8945. E 6766 SAFE MEADOWS", GameScreen.EnJeu)]
    [InlineData("BITCRAFT ONLINE", GameScreen.Inconnu)]
    [InlineData("", GameScreen.Inconnu)]
    public void Mots_cles_de_l_ecran(string text, GameScreen expected) =>
        Assert.Equal(expected, GameScreenClassifier.ClassifyText(text).Screen);

    [Fact]
    public void Mot_cherche_entier_et_sans_casse()
    {
        Assert.Equal("login", GameScreenClassifier.FindWord("Please LOGIN now", GameScreenClassifier.LoginWords));
        Assert.Null(GameScreenClassifier.FindWord("Logistics", GameScreenClassifier.LoginWords));
        Assert.Null(GameScreenClassifier.FindWord("If you are disconnecting", GameScreenClassifier.LoadingWords));
        Assert.Equal("loading", GameScreenClassifier.FindWord("LOADING...", GameScreenClassifier.LoadingWords));
    }

    [Fact]
    public void Zones_ramenees_a_la_taille_de_l_image()
    {
        var small = new Frame { Pixels = [], Width = 960, Height = 540, CapturedAt = DateTime.Now, Source = default };
        Assert.Equal(new ScreenRect(85, 441, 130, 25), GameScreenClassifier.Scale(GameScreenClassifier.LoadingText, small));
    }

    [Fact]
    public void Situations_hors_du_jeu()
    {
        var desktop = ScreenSituations.Decide(false, false, false, new ForegroundInfo("Progman", "explorer"), "BitcraftBot");
        Assert.Equal(Place.Bureau, desktop.Place);
        Assert.Equal("Bureau de Windows (jeu fermé)", desktop.Describe());

        var discord = ScreenSituations.Decide(true, false, false, new ForegroundInfo("Chrome_WidgetWin_1", "Discord"), "BitcraftBot");
        Assert.Equal(Place.AutreProgramme, discord.Place);
        Assert.Equal("Autre programme : Discord (jeu caché)", discord.Describe());

        var bot = ScreenSituations.Decide(false, false, false, new ForegroundInfo("HwndWrapper", "BitcraftBot"), "BitcraftBot");
        Assert.Equal("Autre programme : le bot (jeu fermé)", bot.Describe());

        var reduced = ScreenSituations.Decide(true, true, false, new ForegroundInfo("Progman", "explorer"), "BitcraftBot");
        Assert.Equal(Place.JeuReduit, reduced.Place);

        var game = ScreenSituations.Decide(true, false, true, new ForegroundInfo("UnityWndClass", "BitCraft"), "BitcraftBot") with { Game = GameScreen.Chargement };
        Assert.Equal("Jeu : écran de chargement", game.Describe());
        Assert.False(game.InGame);
        Assert.True((game with { Game = GameScreen.EnJeu }).InGame);
    }

    [Fact]
    public void Meme_situation_pas_de_nouvelle_ligne()
    {
        var a = new ScreenSituation(Place.Jeu, true, GameScreen.EnJeu, "BitCraft", "coordonnées N 1, E 2");
        var b = a with { Evidence = "coordonnées N 3, E 4" };
        Assert.True(a.SameAs(b));
        Assert.False(a.SameAs(b with { Game = GameScreen.Chargement }));
        Assert.False(new ScreenSituation(Place.AutreProgramme, true, null, "Discord", "").SameAs(new ScreenSituation(Place.AutreProgramme, true, null, "Chrome", "")));
    }
}

public class V041ActionScreenTests
{
    private sealed class FakeDevice : IInputDevice
    {
        public (int X, int Y) CursorPosition() => (0, 0);
        public bool IsForeground(IntPtr window) => true;
        public void MoveTo(int x, int y) { }
        public void LeftDown() { }
        public void LeftUp() { }
        public void KeyDown(ushort scanCode) { }
        public void KeyUp(ushort scanCode) { }
    }

    [Fact]
    public void Pas_d_action_pendant_le_chargement()
    {
        string? screen = "le jeu n'est pas prêt (écran de chargement)";
        var controller = new ActionController(new FakeDevice(), new ActionContext
        {
            GameWindow = () => new IntPtr(1),
            GameVisible = () => true,
            ScreenNotReady = () => screen,
        });
        Assert.Equal("le jeu n'est pas prêt (écran de chargement)", controller.WhyNot());
        screen = null;
        Assert.Null(controller.WhyNot());
    }
}

public class V041CoordinatesShapeTests
{
    [Theory]
    [InlineData("N 8958, E 6747", true)]
    [InlineData("N 0927, E 6707", true)]
    [InlineData("N 8923r E 6713", true)]
    [InlineData("18m N 8958 E 6 74 7", true)]
    [InlineData("E 707", false)]
    [InlineData("LOADING", false)]
    [InlineData("", false)]
    public void Forme_des_coordonnees(string text, bool expected) =>
        Assert.Equal(expected, GameScreenClassifier.LooksLikeCoordinates(text));
}

public class V041CameraReliabilityTests
{
    [Fact]
    public void Essai_du_22_novembre_annonce_pas_fiable()
    {
        // Les 17 vrais clics de l'essai R1 (artifacts/release/camera-clics.json, 2026-11-22), résumés.
        (double E, double N, int X, int Y)[] clicks =
        [
            (-1.36, -5.51, 1358, 184), (2.91, -0.99, 1021, 268), (0.89, 1.81, 567, 441), (-0.75, 2.30, 779, 339),
            (1.66, 12.54, 1041, 126), (3.81, 2.24, 928, 241), (7.86, 5.66, 1387, 135), (9.96, -12.87, 387, 62),
            (-2.57, 21.11, 479, 58), (8.93, 14.26, 531, 10), (-0.22, -7.04, 798, 198), (9.31, 10.45, 77, 127),
            (-11.31, -7.86, 244, 14), (-10.18, -2.42, 335, 142), (1.98, 16.45, 1352, 36), (-1.87, 13.20, 1459, 54),
            (-8.00, 3.15, 389, 25),
        ];
        var pairs = clicks.Select(c => new BitcraftBot.Server.Aim.CameraPair
        {
            East = c.E,
            North = c.N,
            PixelX = c.X,
            PixelY = c.Y,
            ClientWidth = 1920,
            ClientHeight = 1080,
        }).ToList();
        var fit = BitcraftBot.Server.Aim.CameraFit.Compute(pairs)!;
        Assert.False(fit.Reliable);
        Assert.True(fit.Rejected > fit.Used);
    }

    [Fact]
    public void Clics_coherents_annonce_fiable() =>
        Assert.True(BitcraftBot.Server.Aim.CameraFit.Compute(FakeCamera.Clicks(15, 7))!.Reliable);
}

// v0.4.2 : réglages du jeu (registre, lecture seule, liste blanche).
public class V042GameSettingsTests
{
    [Fact]
    public void Valeurs_comparees_aux_conseils()
    {
        var values = new Dictionary<string, string>
        {
            ["EarlyAccess:Language"] = "en",
            ["EarlyAccess:UIScale"] = "1",
            ["Screenmanager Resolution Width"] = "1920",
            ["Screenmanager Resolution Height"] = "1080",
            ["EarlyAccess:LockMinimap"] = "1",
            ["EarlyAccess:CameraObjectCollision"] = "1",
        };
        var checks = GameSettings.Check(n => values.GetValueOrDefault(n));
        Assert.True(checks.Where(c => c.Required).All(c => c.Ok));
        Assert.False(checks.Single(c => c.Name == "EarlyAccess:CameraObjectCollision").Ok);
        Assert.Null(checks.Single(c => c.Name == "EarlyAccess:GrassDensity").Value);
        // Jamais de jeton ni d'e-mail dans la liste blanche.
        Assert.DoesNotContain(checks, c => c.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Formats_du_registre_unity()
    {
        Assert.Equal("1", GameSettings.Format(4607182418800017408L));   // double 1,0
        Assert.Equal("0.5", GameSettings.Format(4602678819172646912L)); // double 0,5
        Assert.Equal("500", GameSettings.Format(500));
        Assert.Equal("en", GameSettings.Format(new byte[] { (byte)'e', (byte)'n', 0 }));
    }

    [Fact]
    public void Lecture_du_vrai_registre_sans_secret()
    {
        var checks = GameSettings.Read();
        // Sur le PC de Julien : les réglages existent. Ailleurs : liste vide, rien à vérifier.
        if (checks.Count == 0)
        {
            return;
        }
        Assert.Contains(checks, c => c.Name == "EarlyAccess:Language" && c.Value is not null);
        Assert.All(checks, c => Assert.DoesNotContain("@", c.Value ?? ""));
    }
}
