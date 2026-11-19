using BitcraftBot.Input.Actions;
using BitcraftBot.Input.Human;

namespace BitcraftBot.Tests;

// v0.3.18 : contrôleur d'actions commun (mêmes sécurités pour toutes les touches et tous les clics).
public class V0318Tests
{
    private sealed class FakeDevice : IInputDevice
    {
        public readonly List<string> Events = [];
        public readonly HashSet<ushort> Down = [];
        public bool LeftIsDown;
        public bool Foreground = true;
        public (int X, int Y) Cursor = (100, 100);
        public Func<(int X, int Y), (int X, int Y)>? CursorOverride;
        public Action? OnKeyDown;

        public (int X, int Y) CursorPosition() => CursorOverride?.Invoke(Cursor) ?? Cursor;
        public bool IsForeground(IntPtr window) => Foreground;
        public void MoveTo(int x, int y) { Cursor = (x, y); Events.Add($"move {x},{y}"); }
        public void LeftDown() { LeftIsDown = true; Events.Add("left down"); }
        public void LeftUp() { LeftIsDown = false; Events.Add("left up"); }
        public void KeyDown(ushort scanCode) { Down.Add(scanCode); Events.Add($"down {GameKeys.Name(scanCode)}"); OnKeyDown?.Invoke(); }
        public void KeyUp(ushort scanCode) { Down.Remove(scanCode); Events.Add($"up {GameKeys.Name(scanCode)}"); }
    }

    private static readonly Func<int, CancellationToken, Task> NoWait = (_, token) =>
    {
        token.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    };

    private static (ActionController Controller, FakeDevice Device, List<string> Log) Make(
        DateTime? lastKey = null, bool visible = true, IntPtr? game = null, Func<DateTime>? now = null)
    {
        var device = new FakeDevice();
        var context = new ActionContext
        {
            GameWindow = () => game ?? new IntPtr(42),
            GameVisible = () => visible,
            LastPhysicalKey = () => lastKey ?? DateTime.MinValue,
            Now = now ?? (() => DateTime.Now),
        };
        var controller = new ActionController(device, context, random: new Random(1), delay: NoWait);
        var log = new List<string>();
        controller.Log += (level, message) => log.Add($"{level}: {message}");
        return (controller, device, log);
    }

    [Fact]
    public async Task Combinaison_appuyee_dans_l_ordre_relachee_a_l_envers()
    {
        var (controller, device, log) = Make();
        var result = await controller.PressAsync("Repas", GameKeys.Shift, GameKeys.E);
        Assert.True(result.Done);
        Assert.Equal(["down Maj", "down E", "up E", "up Maj"], device.Events);
        Assert.Empty(device.Down);
        Assert.Contains(log, l => l.StartsWith("Info: Repas : Maj + E"));
    }

    [Fact]
    public async Task Touche_non_autorisee_refusee()
    {
        var (controller, device, _) = Make();
        var result = await controller.PressAsync("Essai", 0x01); // Échap : pas dans la liste
        Assert.Equal(ActionOutcome.Refuse, result.Outcome);
        Assert.Empty(device.Events);
    }

    [Fact]
    public async Task Refus_si_jeu_pas_actif_cache_ou_introuvable()
    {
        var (c1, d1, _) = Make();
        d1.Foreground = false;
        Assert.Equal("BitCraft n'est pas la fenêtre active", (await c1.PressAsync("x", GameKeys.E)).Detail);

        var (c2, _, _) = Make(visible: false);
        Assert.Equal("le jeu est caché par une autre fenêtre", (await c2.PressAsync("x", GameKeys.E)).Detail);

        var (c3, _, _) = Make(game: IntPtr.Zero);
        Assert.Equal("jeu introuvable", (await c3.PressAsync("x", GameKeys.E)).Detail);
    }

    [Fact]
    public async Task Refus_si_Julien_tape_au_clavier()
    {
        var (controller, device, _) = Make(lastKey: DateTime.Now.AddSeconds(-1));
        var result = await controller.PressAsync("Repas", GameKeys.Shift, GameKeys.E);
        Assert.Equal(ActionOutcome.Refuse, result.Outcome);
        Assert.Equal("vous tapez au clavier", result.Detail);
        Assert.Empty(device.Events);
    }

    [Fact]
    public async Task Limite_d_actions_par_minute()
    {
        var now = new DateTime(2026, 11, 20, 10, 0, 0);
        var (controller, _, _) = Make(now: () => now);
        controller.Options.MaxActionsPerMinute = 3;
        for (var i = 0; i < 3; i++)
        {
            Assert.True((await controller.PressAsync("x", GameKeys.E)).Done);
        }
        Assert.Equal(ActionOutcome.Refuse, (await controller.PressAsync("x", GameKeys.E)).Outcome);
        now = now.AddSeconds(61);
        Assert.True((await controller.PressAsync("x", GameKeys.E)).Done);
    }

    [Fact]
    public async Task Arret_d_urgence_pendant_l_appui_relache_tout_et_bloque_la_suite()
    {
        var (controller, device, _) = Make();
        device.OnKeyDown = () => { if (device.Down.Count == 1) controller.Halt("fermeture du bot"); };
        var result = await controller.PressAsync("Repas", GameKeys.Shift, GameKeys.E);
        Assert.Equal(ActionOutcome.Interrompu, result.Outcome);
        Assert.Empty(device.Down);
        Assert.DoesNotContain("down E", device.Events);

        var next = await controller.PressAsync("Repas", GameKeys.E);
        Assert.Equal(ActionOutcome.Refuse, next.Outcome);
        Assert.Contains("fermeture du bot", next.Detail);

        device.OnKeyDown = null;
        controller.Resume("essai");
        Assert.True((await controller.PressAsync("Repas", GameKeys.E)).Done);
    }

    [Fact]
    public async Task Jeu_quitte_pendant_l_appui_interrompt_et_relache()
    {
        var (controller, device, _) = Make();
        device.OnKeyDown = () => device.Foreground = false;
        var result = await controller.PressAsync("Repas", GameKeys.Shift, GameKeys.E);
        Assert.Equal(ActionOutcome.Interrompu, result.Outcome);
        Assert.Equal("BitCraft n'est plus la fenêtre active", result.Detail);
        Assert.Empty(device.Down);
        Assert.False(controller.IsHalted);
    }

    [Fact]
    public async Task Clic_suit_un_trajet_et_relache_le_bouton()
    {
        var (controller, device, _) = Make();
        var area = new SafeArea(0, 0, 1919, 1079);
        var result = await controller.ClickAsync("Clic", 900, 500, area, new HumanMouse(new Random(3)));
        Assert.True(result.Done);
        Assert.Equal((900, 500), device.Cursor);
        Assert.Equal(["left down", "left up"], device.Events.Where(e => e.StartsWith("left")).ToList());
        Assert.False(device.LeftIsDown);
    }

    [Fact]
    public async Task Clic_hors_zone_refuse()
    {
        var (controller, device, _) = Make();
        var result = await controller.ClickAsync("Clic", 3000, 500, new SafeArea(0, 0, 1919, 1079), new HumanMouse(new Random(3)));
        Assert.Equal(ActionOutcome.Refuse, result.Outcome);
        Assert.Empty(device.Events);
    }

    [Fact]
    public async Task Souris_bougee_par_Julien_arrete_tout()
    {
        var (controller, device, _) = Make();
        var moves = 0;
        // Après quelques pas, la souris n'est plus où le bot l'a mise : Julien la tient.
        device.CursorOverride = c => ++moves > 3 ? (c.X + 50, c.Y) : c;
        var result = await controller.ClickAsync("Clic", 900, 500, new SafeArea(0, 0, 1919, 1079), new HumanMouse(new Random(3)));
        Assert.Equal(ActionOutcome.Interrompu, result.Outcome);
        Assert.True(controller.IsHalted);
        Assert.DoesNotContain("left down", device.Events);
    }
}
