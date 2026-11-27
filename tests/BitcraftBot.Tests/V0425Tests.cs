using System.Globalization;
using BitcraftBot.Core.Monitoring;
using BitcraftBot.Input.Actions;
using BitcraftBot.Input.Human;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.25 : overlay plus étroit ; reprise des récoltes avec la souris posée sur le point du clic de Julien (plus de Tab) ;
// compteur d'échecs remis à zéro.
public class V0425Tests
{
    private const ulong Tree = 42;
    private static readonly DateTimeOffset T0 = new(2026, 11, 27, 0, 20, 0, TimeSpan.Zero);
    private static readonly ServerPosition Here = new(27249, 27552);
    private static readonly ClickPoint OnTree = new(900, 500, T0.AddSeconds(-1), Here, 0);

    private static NearbyResource Res(int health) =>
        new(Tree, 1, "Mature Birch Tree", 2, "Tree", Here, 1) { Health = health, MaxHealth = 22000 };

    private static ResumeInput In(double seconds, string? action, double stamina, ClickPoint? click = null, int cameraMoves = 0,
        ServerPosition? at = null) => new()
    {
        Now = T0.AddSeconds(seconds),
        Enabled = true,
        Action = action is null ? null : new ActionInfo(action, 0, 1000, Tree, null),
        Position = at ?? Here,
        Stamina = stamina,
        MaxStamina = 98,
        Resources = [Res(15000)],
        LastClick = click,
        CameraMoves = cameraMoves,
    };

    [Fact]
    public void PointValidity()
    {
        Assert.Null(HarvestResume.PointProblem(OnTree, Here, 0));
        Assert.Null(HarvestResume.PointProblem(OnTree, new ServerPosition(Here.X + 1, Here.Z), 0)); // petit écart toléré
        Assert.Contains("aucun", HarvestResume.PointProblem(null, Here, 0));
        Assert.Contains("caméra", HarvestResume.PointProblem(OnTree, Here, 1));
        Assert.Contains("déplacé", HarvestResume.PointProblem(OnTree, new ServerPosition(Here.X + 6, Here.Z), 0));
    }

    [Fact]
    public void HoversClickPointThenInteract()
    {
        var r = new HarvestResume();
        r.Update(In(0, "Extract", 50, OnTree));
        Assert.Equal(OnTree, r.Point);
        r.Update(In(1, null, 2, OnTree));
        var d = r.Update(In(60, null, 97, OnTree));
        Assert.Equal(ResumeStep.Interact, d.Step);
        Assert.Equal((900, 500), (d.X, d.Y));
    }

    [Fact]
    public void NoPressWithoutValidPoint()
    {
        // Aucun clic connu : rien n'est appuyé, message clair.
        var none = new HarvestResume();
        none.Update(In(0, "Extract", 50));
        none.Update(In(1, null, 2));
        var d = none.Update(In(60, null, 97));
        Assert.Equal(ResumeStep.Rien, d.Step);
        Assert.Contains("cliquez une fois", d.Log);
        Assert.Contains("cliquez une fois", d.State);
        Assert.False(none.Active);

        // Caméra tournée pendant l'attente : le point n'est plus sur l'arbre.
        var turned = new HarvestResume();
        turned.Update(In(0, "Extract", 50, OnTree));
        turned.Update(In(1, null, 2, OnTree));
        var t = turned.Update(In(60, null, 97, OnTree, cameraMoves: 1));
        Assert.Equal(ResumeStep.Rien, t.Step);
        Assert.Contains("caméra", t.Log);

        // Clic trop ancien (avant une autre action) : pas retenu.
        var old = new HarvestResume();
        old.Update(In(0, "Extract", 50, OnTree with { At = T0.AddSeconds(-30) }));
        Assert.Null(old.Point);

        // Clic fait ailleurs (personnage pas encore arrivé à l'arbre) : pas retenu.
        var far = new HarvestResume();
        far.Update(In(0, "Extract", 50, OnTree with { Position = new ServerPosition(Here.X + 9, Here.Z) }));
        Assert.Null(far.Point);
    }

    [Fact]
    public void JulienRelaunchClickUpdatesPoint()
    {
        var r = new HarvestResume();
        r.Update(In(0, "Extract", 50, OnTree));
        r.Update(In(1, null, 2, OnTree));
        // Julien relance lui-même, avec un nouveau clic (personnage arrêté).
        var relaunch = new ClickPoint(950, 520, T0.AddSeconds(40), Here, 0);
        r.Update(In(40.5, "Extract", 60, relaunch));
        Assert.Equal(relaunch, r.Point);
    }

    [Fact]
    public void FailureCounterResetsOnJulienRelaunch()
    {
        // Cas du 27/11 à 00:29 : un M sans effet, Julien relance lui-même, deuxième arrêt sur le même arbre :
        // le bot doit refaire deux essais, pas abandonner après le premier.
        var r = new HarvestResume();
        r.Update(In(0, "Extract", 50, OnTree));
        r.Update(In(1, null, 2, OnTree));
        Assert.Equal(ResumeStep.Interact, r.Update(In(60, null, 97, OnTree)).Step);
        var retry = r.Update(In(66, null, 97, OnTree));
        Assert.Contains("nouvel essai", retry.Log);
        Assert.Equal(1, r.Failures);
        var click = new ClickPoint(910, 505, T0.AddSeconds(67), Here, 0);
        r.Update(In(67.5, "Extract", 97, click));
        Assert.Equal(0, r.Failures);
        r.Update(In(100, null, 1, click));
        Assert.Equal(ResumeStep.Interact, r.Update(In(160, null, 97, click)).Step);
        var second = r.Update(In(166, null, 97, click));
        Assert.Contains("nouvel essai", second.Log);
        Assert.False(second.Alert);
        Assert.True(r.Active);
    }

    [Fact]
    public void StopResetsFailures()
    {
        var r = new HarvestResume();
        r.Update(In(0, "Extract", 50, OnTree));
        r.Update(In(1, null, 2, OnTree));
        r.Update(In(60, null, 97, OnTree));
        r.Update(In(66, null, 97, OnTree)); // échec 1
        Assert.Equal(1, r.Failures);
        r.Update(In(67, null, 97, OnTree, at: new ServerPosition(Here.X + 9, Here.Z))); // Julien part : arrêt
        Assert.Equal(0, r.Failures);
        Assert.False(r.Active);
    }

    [Fact]
    public void NotSentIsNotAFailure()
    {
        var r = new HarvestResume();
        r.Update(In(0, "Extract", 50, OnTree));
        r.Update(In(1, null, 2, OnTree));
        Assert.Equal(ResumeStep.Interact, r.Update(In(60, null, 97, OnTree)).Step);
        r.NotSent();
        Assert.Equal(ResumeStep.Interact, r.Update(In(61, null, 97, OnTree)).Step);
        Assert.Equal(0, r.Failures);
    }

    [Fact]
    public void NoTabStepAnymore()
    {
        Assert.DoesNotContain("Tab", Enum.GetNames<ResumeStep>());
    }

    [Fact]
    public async Task MoveWithoutClick()
    {
        var device = new MoveDevice();
        var controller = new ActionController(device, new ActionContext
        {
            GameWindow = () => new IntPtr(1),
            GameVisible = () => true,
        }, delay: (_, token) => { token.ThrowIfCancellationRequested(); return Task.CompletedTask; });
        var area = new SafeArea(0, 0, 1919, 1079);
        var result = await controller.MoveAsync("essai", 900, 500, area, new HumanMouse(new Random(3)));
        Assert.True(result.Done);
        Assert.Equal((900, 500), device.Cursor);
        Assert.Equal(0, device.Clicks);
        var outside = await controller.MoveAsync("essai", 3000, 500, area, new HumanMouse(new Random(3)));
        Assert.Equal(ActionOutcome.Refuse, outside.Outcome);
    }

    [Fact]
    public void OverlaySystemLineIsShort()
    {
        var sample = new ResourceSample
        {
            At = DateTime.Now, BotCpuPercent = 12.3, BotRamMb = 1588, BotManagedMb = 50, BotThreads = 20, BotHandles = 500,
            SystemCpuPercent = 10, SystemRamTotalMb = 32000, SystemRamAvailableMb = 16000, GameCpuPercent = 100, GameRamMb = 12000,
        };
        var text = SystemSummary.Overlay(sample, CultureInfo.GetCultureInfo("fr-FR"));
        Assert.True(text.Length <= SystemSummary.MaxLength, text);
        Assert.Equal("Bot 12,5 % · 1590 Mo │ Jeu 100 % · 11,7 Go", text);
    }

    private sealed class MoveDevice : IInputDevice
    {
        public (int X, int Y) Cursor = (100, 100);
        public int Clicks;
        public (int X, int Y) CursorPosition() => Cursor;
        public bool IsForeground(IntPtr window) => true;
        public void MoveTo(int x, int y) => Cursor = (x, y);
        public void LeftDown() => Clicks++;
        public void LeftUp() { }
        public void KeyDown(ushort scanCode) { }
        public void KeyUp(ushort scanCode) { }
    }
}
