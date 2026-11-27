using BitcraftBot.Input.Actions;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.24 : reprise automatique des récoltes (Interact, vérifié par le serveur). Tab retiré en v0.4.25.
public class V0424Tests
{
    private const ulong Tree = 42;
    private const ulong Lavender = 7;
    private static readonly DateTimeOffset T0 = new(2026, 11, 26, 21, 0, 0, TimeSpan.Zero);
    private static readonly ServerPosition Here = new(27249, 27552);

    private static NearbyResource Res(ulong id, string name, int health) =>
        new(id, 1, name, 2, "Tree", Here, 1) { Health = health, MaxHealth = 22000 };

    private static ResumeInput In(double seconds, string? action, double stamina, ulong? target = null, int treeHealth = 15000,
        ulong? actionTarget = Tree, ServerPosition? at = null) => new()
    {
        Now = T0.AddSeconds(seconds),
        Enabled = true,
        Action = action is null ? null : new ActionInfo(action, 0, 1000, actionTarget, null),
        Position = at ?? Here,
        Stamina = stamina,
        MaxStamina = 98,
        Target = target,
        Resources = [Res(Tree, "Mature Birch Tree", treeHealth), Res(Lavender, "Lavender", 10)],
        // v0.4.25 : clic de Julien sur l'arbre juste avant la récolte (point de la souris pour la reprise).
        LastClick = new ClickPoint(900, 500, T0.AddSeconds(-1), Here, 0),
    };

    [Fact]
    public void ResumesAfterStaminaRunsOut()
    {
        var r = new HarvestResume();
        Assert.Equal(ResumeStep.Rien, r.Update(In(0, "Extract", 50)).Step);
        var stop = r.Update(In(1, null, 2));
        Assert.Contains("faute d'endurance", stop.Log);
        Assert.Equal(ResumeStep.Rien, r.Update(In(30, null, 60)).Step); // endurance pas encore revenue
        // v0.4.25 : plus de Tab, la souris va sur le point du clic puis M (même si la cible du jeu est autre chose).
        var hover = r.Update(In(60, null, 97, target: Lavender));
        Assert.Equal(ResumeStep.Interact, hover.Step);
        Assert.Equal((900, 500), (hover.X, hover.Y));
        var confirmed = r.Update(In(62, "Extract", 96, target: Tree));
        Assert.Contains("reprise", confirmed.Log);
        Assert.False(confirmed.Alert);
    }

    [Fact]
    public void NoResumeWhenJulienStopsHimself()
    {
        var r = new HarvestResume();
        r.Update(In(0, "Extract", 80));
        r.Update(In(1, null, 70)); // endurance encore là : arrêt voulu
        Assert.Equal(ResumeStep.Rien, r.Update(In(60, null, 98)).Step);
        Assert.False(r.Active);
    }

    [Fact]
    public void StopsWhenMovedOrTreeDeadOrWrongTarget()
    {
        var moved = new HarvestResume();
        moved.Update(In(0, "Extract", 50));
        moved.Update(In(1, null, 1));
        Assert.Contains("déplacé", moved.Update(In(5, null, 20, at: new ServerPosition(Here.X + 9, Here.Z))).Log);

        var dead = new HarvestResume();
        dead.Update(In(0, "Extract", 50));
        Assert.Contains("épuisée", dead.Update(In(1, null, 1, treeHealth: 0)).Log);

        var wrong = new HarvestResume();
        wrong.Update(In(0, "Extract", 50));
        wrong.Update(In(1, null, 1));
        Assert.Equal(ResumeStep.Interact, wrong.Update(In(60, null, 98, target: Tree)).Step);
        var d = wrong.Update(In(61, "Extract", 98, actionTarget: Lavender));
        Assert.True(d.Alert);
        Assert.Contains("Lavender", d.Log);
    }

    [Fact]
    public void ResumeKeysAreAllowed()
    {
        Assert.Equal("Tab", GameKeys.Name(GameKeys.Tab));
        Assert.Equal("M", GameKeys.Name(GameKeys.Interact));
    }
}
