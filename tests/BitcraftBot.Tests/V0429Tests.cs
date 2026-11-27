using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.29 (T-10) : « Se déplacer ici » : barre de la carte, échelle, relief, suivi du trajet, touches interdites, Get Up.
public class V0429Tests
{
    // ----- Barre de la carte -----

    [Theory]
    [InlineData("Keldan (R18) | (8918, 6700)  Set destination  Zoom  Pan", 8918, 6700)]
    [InlineData("Keldan (R18) | ( 9196 , 7258 )", 9196, 7258)]
    [InlineData("Keldan (R18) | (8918. 6700)", 8918, 6700)]
    public void Barre_lue(string text, int a, int b) => Assert.Equal((a, b), MapScale.ParseBar(text));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Set destination Zoom Pan")]
    [InlineData("Keldan (R18) | (89l8, 6700)")]
    [InlineData("Keldan (R18) | (0, 6700)")]
    [InlineData("Keldan (R18) | (98918, 6700)")]
    public void Barre_abimee_refusee(string? text) => Assert.Null(MapScale.ParseBar(text));

    // ----- Échelle -----

    [Fact]
    public void Calibration_ordre_N_E()
    {
        // 0,1 case par pixel : 200 px à droite = E + 20 ; 200 px en bas = N - 20.
        var (scale, refusal) = MapScale.Calibrate((9000, 6800), (9000, 6820), (8980, 6800), 960, 540, 200, 200,
            new ServerPosition(6800 * 3, 9000 * 3));
        Assert.Null(refusal);
        Assert.True(scale!.FirstIsNorth);
        Assert.Equal(0.1, scale.CellsPerPixelX, 3);
        Assert.Equal((6800, 9000), scale.ToEastNorth((9000, 6800)));
        // Case 5 à l'est = 50 px à droite ; 5 au nord = 50 px plus haut.
        Assert.Equal((1010, 540), scale.ToScreen(6805, 9000));
        Assert.Equal((960, 490), scale.ToScreen(6800, 9005));
    }

    [Fact]
    public void Calibration_ordre_E_N()
    {
        var (scale, refusal) = MapScale.Calibrate((6800, 9000), (6820, 9000), (6800, 8980), 960, 540, 200, 200);
        Assert.Null(refusal);
        Assert.False(scale!.FirstIsNorth);
        Assert.Equal((6800, 9000), scale.ToEastNorth((6800, 9000)));
    }

    [Fact]
    public void Calibration_zoom_trop_faible_ou_trop_fort()
    {
        Assert.Contains("dézoomée", MapScale.Calibrate((9000, 6800), (9000, 6950), (8850, 6800), 960, 540, 200, 200).Refusal);
        Assert.Contains("trop zoomée", MapScale.Calibrate((9000, 6800), (9000, 6801), (8999, 6800), 960, 540, 200, 200).Refusal);
    }

    [Fact]
    public void Calibration_incoherente_ou_loin_du_serveur()
    {
        Assert.Contains("incohérentes", MapScale.Calibrate((9000, 6800), (9000, 6800), (9000, 6800), 960, 540, 200, 200).Refusal);
        Assert.Contains("loin de la position", MapScale.Calibrate((9000, 6800), (9000, 6820), (8980, 6800), 960, 540, 200, 200,
            new ServerPosition(6700 * 3, 9000 * 3)).Refusal);
        Assert.Contains("différentes", MapScale.Calibrate((9000, 6800), (9000, 6820), (8960, 6800), 960, 540, 200, 200).Refusal);
    }

    [Fact]
    public void Visee_corrigee()
    {
        var scale = MapScale.Calibrate((9000, 6800), (9000, 6820), (8980, 6800), 960, 540, 200, 200).Scale!;
        // La souris est en (1010, 540) mais la barre lit E 6803 au lieu de 6805 : il faut 20 px de plus à droite.
        Assert.Equal((1030, 540), scale.Correct((1010, 540), (6803, 9000), 6805, 9000));
        Assert.True(MapScale.OnTarget((6804, 9001), 6805, 9000));
        Assert.False(MapScale.OnTarget((6803, 9000), 6805, 9000));
    }

    // ----- Relief -----

    private static Func<int, int, (short Elevation, short Water)?> Flat(Func<int, int, (short, short)?>? special = null) =>
        (e, n) => special?.Invoke(e, n) ?? ((short)10, (short)0);

    [Fact]
    public void Plat_permis() => Assert.True(PathCheck.Check(100.5, 100.5, 110.5, 100.5, Flat()).Allowed);

    [Fact]
    public void Marche_de_5_refusee_marche_de_4_permise()
    {
        var five = PathCheck.Check(100.5, 100.5, 110.5, 100.5, Flat((e, _) => e >= 105 ? ((short)15, (short)0) : null));
        Assert.False(five.Allowed);
        Assert.Contains("escalade", five.Detail);
        Assert.True(PathCheck.Check(100.5, 100.5, 110.5, 100.5, Flat((e, _) => e >= 105 ? ((short)14, (short)0) : null)).Allowed);
    }

    [Fact]
    public void Eau_profonde_refusee_eau_peu_profonde_permise()
    {
        var deep = PathCheck.Check(100.5, 100.5, 110.5, 100.5, Flat((e, _) => e == 105 ? ((short)10, (short)15) : null));
        Assert.False(deep.Allowed);
        Assert.Contains("nage", deep.Detail);
        var shallow = PathCheck.Check(100.5, 100.5, 110.5, 100.5, Flat((e, _) => e == 105 ? ((short)10, (short)14) : null));
        Assert.True(shallow.Allowed);
        Assert.True(shallow.ShallowWaterCells > 0);
    }

    [Fact]
    public void Cible_dans_l_eau_refusee() =>
        Assert.False(PathCheck.Check(100.5, 100.5, 110.5, 100.5, Flat((e, _) => e == 110 ? ((short)0, (short)8) : null)).Allowed);

    [Fact]
    public void Relief_inconnu_refuse()
    {
        var r = PathCheck.Check(100.5, 100.5, 110.5, 100.5, (e, n) => e >= 108 ? null : ((short)10, (short)0));
        Assert.False(r.Allowed);
        Assert.Contains("relief inconnu", r.Detail);
    }

    [Fact]
    public void Obstacle_a_une_case_du_trajet_refuse()
    {
        // Falaise sur la ligne parallèle au nord (N 101), pas sur la ligne droite (N 100).
        var r = PathCheck.Check(100.5, 100.5, 110.5, 100.5, Flat((e, n) => n == 101 && e == 105 ? ((short)30, (short)0) : null));
        Assert.False(r.Allowed);
        Assert.Contains("à une case du trajet", r.Detail);
    }

    [Fact]
    public void Trop_loin_refuse()
    {
        var r = PathCheck.Check(100, 100, 140, 100, Flat());
        Assert.False(r.Allowed);
        Assert.Contains("trop loin", r.Detail);
    }

    [Fact]
    public void Relief_lu_dans_les_morceaux()
    {
        var elevations = new short[TerrainChunk.Cells];
        elevations[3 * TerrainChunk.Size + 2] = 42;
        var chunk = new TerrainChunk(TerrainChunk.IndexOf(10, 20), 10, 20, TerrainChunk.Overworld, new uint[TerrainChunk.Cells],
            new uint[TerrainChunk.Cells], elevations, new short[TerrainChunk.Cells]);
        var cell = PathCheck.FromChunks([chunk]);
        Assert.Equal(((short)42, (short)0), cell(10 * 32 + 2, 20 * 32 + 3));
        Assert.Null(cell(0, 0));
    }

    // ----- Suivi du trajet -----

    private static readonly DateTime T0 = new(2026, 11, 27, 6, 0, 0);

    [Fact]
    public void Arrivee_apres_une_seconde_immobile()
    {
        var g = new TravelGuard(105, 100, 5, T0);
        Assert.Equal(TravelState.EnRoute, g.Update(T0.AddSeconds(1), 102, 100, 100, T0.AddSeconds(1)));
        Assert.Equal(TravelState.EnRoute, g.Update(T0.AddSeconds(2), 104, 100, 100, T0.AddSeconds(2)));
        Assert.Equal(TravelState.EnRoute, g.Update(T0.AddSeconds(2.5), 104, 100, 100, T0.AddSeconds(2.5)));
        Assert.Equal(TravelState.Arrive, g.Update(T0.AddSeconds(3.5), 104, 100, 100, T0.AddSeconds(3.5)));
    }

    [Fact]
    public void Blocage_si_pas_de_progres_en_5_s()
    {
        var g = new TravelGuard(120, 100, 20, T0);
        TravelState last = TravelState.EnRoute;
        for (var i = 1; i <= 14 && last == TravelState.EnRoute; i++)
        {
            last = g.Update(T0.AddSeconds(i * 0.5), 105 + (i % 2) * 0.01, 100, 100, T0.AddSeconds(i * 0.5));
        }
        Assert.Equal(TravelState.Bloque, last);
    }

    [Fact]
    public void Temps_depasse()
    {
        var g = new TravelGuard(105, 100, 5, T0);
        Assert.Equal(20, g.MaxDuration.TotalSeconds);
        g.Update(T0.AddSeconds(1), 100, 100, 100, T0.AddSeconds(1));
        // Marche lente mais régulière : pas bloqué, mais temps dépassé.
        Assert.Equal(TravelState.TropLong, g.Update(T0.AddSeconds(21), 100.2, 100, 100, T0.AddSeconds(21)));
    }

    [Fact]
    public void Vie_qui_baisse_vie_a_zero_et_serveur_muet()
    {
        var g = new TravelGuard(110, 100, 10, T0);
        g.Update(T0.AddSeconds(0.5), 101, 100, 100, T0.AddSeconds(0.5));
        Assert.Equal(TravelState.Danger, g.Update(T0.AddSeconds(1), 102, 100, 90, T0.AddSeconds(1)));
        Assert.Equal(TravelState.ATerre, g.Update(T0.AddSeconds(1.5), 102, 100, 0, T0.AddSeconds(1.5)));
        Assert.Equal(TravelState.LiaisonPerdue, g.Update(T0.AddSeconds(10), 102, 100, 50, T0.AddSeconds(1.5)));
    }

    [Fact]
    public void Fuite_a_l_oppose()
    {
        var (e, n) = TravelGuard.FleePoint(100, 100, 110, 100);
        Assert.Equal(90, e, 3);
        Assert.Equal(100, n, 3);
    }

    // ----- Touches interdites -----

    private sealed class RecordingDevice : IInputDevice
    {
        public List<ushort> Keys { get; } = [];
        public (int X, int Y) CursorPosition() => (0, 0);
        public bool IsForeground(IntPtr window) => true;
        public void MoveTo(int x, int y) { }
        public void LeftDown() { }
        public void LeftUp() { }
        public void KeyDown(ushort scanCode) => Keys.Add(scanCode);
        public void KeyUp(ushort scanCode) { }
    }

    [Theory]
    [InlineData(new ushort[] { 0x0C })]
    [InlineData(new ushort[] { 0x0D })]
    [InlineData(new ushort[] { GameKeys.Shift, GameKeys.V })]
    [InlineData(new ushort[] { GameKeys.Shift, GameKeys.B })]
    public async Task Touches_interdites_jamais_envoyees(ushort[] keys)
    {
        var device = new RecordingDevice();
        var controller = new ActionController(device, new ActionContext { GameWindow = () => new IntPtr(1), GameVisible = () => true },
            delay: (_, _) => Task.CompletedTask);
        var result = await controller.PressAsync("essai", keys);
        Assert.Equal(ActionOutcome.Refuse, result.Outcome);
        Assert.Contains("interdite", result.Detail);
        Assert.Empty(device.Keys);
        Assert.All(keys.Where(GameKeys.Forbidden.Contains), k => Assert.False(GameKeys.Allowed.ContainsKey(k)));
    }

    [Fact]
    public async Task Touches_de_la_carte_et_sprint_permises()
    {
        var device = new RecordingDevice();
        var controller = new ActionController(device, new ActionContext { GameWindow = () => new IntPtr(1), GameVisible = () => true },
            delay: (_, _) => Task.CompletedTask);
        Assert.True((await controller.PressAsync("carte", GameKeys.MapKey)).Done);
        Assert.True((await controller.PressAsync("carte pavé", GameKeys.MapKeyNumpad)).Done);
        Assert.True((await controller.PressAsync("sprint", GameKeys.Sprint)).Done);
        Assert.Equal(new[] { GameKeys.MapKey, GameKeys.MapKeyNumpad, GameKeys.Sprint }, device.Keys.ToArray());
    }

    // ----- Get Up -----

    [Fact]
    public void Get_Up_choisi_seulement_si_Go_Home_lu_ailleurs()
    {
        var getUp = new GetUpChooser.Word("Get Up", 700, 600, 80, 30);
        var goHome = new GetUpChooser.Word("Go Home", 1100, 600, 100, 30);
        Assert.Equal((740, 615), GetUpChooser.Choose([getUp, goHome]));
        Assert.Null(GetUpChooser.Choose([getUp]));
        Assert.Null(GetUpChooser.Choose([goHome]));
        Assert.Null(GetUpChooser.Choose([getUp, new GetUpChooser.Word("Go Home", 760, 600, 100, 30)]));
        Assert.Null(GetUpChooser.Choose([new GetUpChooser.Word("Get Up Go Home", 700, 600, 300, 30), goHome]));
        Assert.Equal((740, 615), GetUpChooser.Choose([new GetUpChooser.Word("GET UP", 700, 600, 80, 30), new GetUpChooser.Word("GO HOME", 1100, 600, 100, 30)]));
    }

    [Fact]
    public void Rectangle_OCR_ramene_a_l_image()
    {
        // Marge 12, agrandi 3 fois : (12 + 30, 12 + 60) → (10, 20) dans la zone, + origine de la zone.
        var box = OcrReader.ToFrame(new ScreenRect(100, 200, 500, 500), 3, 42, 72, 342, 132);
        Assert.Equal(new ScreenRect(110, 220, 100, 20), box);
    }
}
