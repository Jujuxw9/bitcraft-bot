using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.30 (T-12) : corrections de « Se déplacer ici » après relecture (l'essai en jeu de T-11 n'a pas pu se faire).
public class V0430Tests
{
    private static string Reference(params string[] parts) =>
        Path.GetFullPath(Path.Combine([HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", .. parts]));

    // ----- Barre de la carte lue sur une vraie capture de la grande carte -----

    [Fact]
    public async Task Barre_de_la_vraie_carte_lue_dans_sa_zone()
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("fenetres", "carte-du-monde_001.png"));
        Assert.True(GameWindows.IsWorldMap(frame));
        string? text = null;
        foreach (var threshold in new[] { 140, 110, 170 })
        {
            text = await ocr.ReadAsync(frame, WorldMapZones.BarZone(frame), TextStyle.DarkText, threshold);
            if (MapScale.ParseBar(text) is not null)
            {
                break;
            }
        }
        Assert.True(MapScale.ParseBar(text) == (9196, 7258), $"barre lue : « {text} »");
        Assert.True(WorldMapZones.SaysSetDestination(text), $"barre lue : « {text} »");
        Assert.Equal(MapOpenOutcome.Ouverte, WorldMapZones.AfterKey(text, true));
    }

    [Fact]
    public void Zone_de_la_barre_dans_le_bas_de_l_image()
    {
        var zone = WorldMapZones.BarZone(1920, 1080);
        Assert.Equal(576, zone.X);
        Assert.Equal(982, zone.Y);
        Assert.True(zone.Y + zone.Height <= 1080);
        Assert.True(zone.X + zone.Width <= 1920 * 0.70);
    }

    // ----- Que faire après la touche de la carte -----

    [Fact]
    public void Apres_la_touche_rappuyer_seulement_si_la_carte_est_vue()
    {
        Assert.Equal(MapOpenOutcome.Ouverte, WorldMapZones.AfterKey("Keldan | (1, 2) Set Destination", false));
        Assert.Equal(MapOpenOutcome.ARefermer, WorldMapZones.AfterKey("", true));
        Assert.Equal(MapOpenOutcome.ARefermer, WorldMapZones.AfterKey(null, true));
        // Avant (v0.4.29) : la touche était rappuyée même si rien n'était ouvert → la carte s'ouvrait pour rien.
        Assert.Equal(MapOpenOutcome.RienOuvert, WorldMapZones.AfterKey("", false));
        Assert.Equal(MapOpenOutcome.RienOuvert, WorldMapZones.AfterKey(null, false));
    }

    // ----- Attente du retour sur le jeu après le clic dans le menu du bot -----

    private sealed class SwitchingDevice : IInputDevice
    {
        public bool Foreground { get; set; }
        public List<ushort> Keys { get; } = [];
        public (int X, int Y) CursorPosition() => (0, 0);
        public bool IsForeground(IntPtr window) => Foreground;
        public void MoveTo(int x, int y) { }
        public void LeftDown() { }
        public void LeftUp() { }
        public void KeyDown(ushort scanCode) => Keys.Add(scanCode);
        public void KeyUp(ushort scanCode) { }
    }

    [Fact]
    public async Task Le_trajet_attend_que_le_jeu_revienne_au_premier_plan()
    {
        // Juste après le clic sur « Se déplacer ici », c'est le bot qui est au premier plan.
        var device = new SwitchingDevice { Foreground = false };
        var controller = new ActionController(device, new ActionContext { GameWindow = () => new IntPtr(1), GameVisible = () => true },
            delay: (_, _) => Task.CompletedTask);
        Assert.Equal("BitCraft n'est pas la fenêtre active", controller.WhyNot());
        // v0.4.29 : la touche partait tout de suite et était refusée → trajet raté.
        Assert.False((await controller.PressAsync("carte", GameKeys.MapKey)).Done);
        var waits = 0;
        var why = await ActionReadiness.WaitAsync(controller.WhyNot, TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(500), delay: _ =>
        {
            // Julien fait Alt + Tab au bout de 2 s.
            if (++waits == 4)
            {
                device.Foreground = true;
            }
            return Task.CompletedTask;
        });
        Assert.Null(why);
        Assert.Equal(4, waits);
        Assert.True((await controller.PressAsync("carte", GameKeys.MapKey)).Done);
        Assert.Equal(new[] { GameKeys.MapKey }, device.Keys.ToArray());
    }

    [Fact]
    public async Task Attente_limitee_a_20_secondes()
    {
        var waits = 0;
        var why = await ActionReadiness.WaitAsync(() => "BitCraft n'est pas la fenêtre active", TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(500),
            delay: _ => { waits++; return Task.CompletedTask; });
        Assert.Equal("BitCraft n'est pas la fenêtre active", why);
        Assert.Equal(40, waits);
    }

    [Fact]
    public async Task Attente_coupee_par_l_arret_ou_un_blocage()
    {
        Assert.Equal("arrêt demandé", await ActionReadiness.WaitAsync(() => "vous tapez au clavier", TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(500),
            stop: () => true, delay: _ => Task.CompletedTask));
        var waits = 0;
        var halted = await ActionReadiness.WaitAsync(() => "actions bloquées (carte restée ouverte)", TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(500),
            delay: _ => { waits++; return Task.CompletedTask; });
        Assert.StartsWith("actions bloquées", halted);
        Assert.Equal(0, waits);
    }

    // ----- Carte qui met du temps à se refermer -----

    [Fact]
    public async Task Fermeture_lente_de_la_carte_pas_prise_pour_une_carte_restee_ouverte()
    {
        var looks = 0;
        // Encore vue au premier regard (0,5 s), fermée au deuxième (1 s) : pas d'alerte.
        Assert.True(await ActionReadiness.UntilAsync(() => Task.FromResult(++looks >= 2), 3, TimeSpan.FromMilliseconds(500), _ => Task.CompletedTask));
        Assert.Equal(2, looks);
        looks = 0;
        Assert.False(await ActionReadiness.UntilAsync(() => Task.FromResult(++looks > 10), 3, TimeSpan.FromMilliseconds(500), _ => Task.CompletedTask));
        Assert.Equal(3, looks);
    }
}
