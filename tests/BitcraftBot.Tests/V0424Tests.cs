using BitcraftBot.Input.Actions;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.24 : reprise automatique des récoltes (Tab jusqu'à la bonne cible, puis Interact, vérifié par le serveur).
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
    };

    [Fact]
    public void ResumesAfterStaminaRunsOut()
    {
        var r = new HarvestResume();
        Assert.Equal(ResumeStep.Rien, r.Update(In(0, "Extract", 50)).Step);
        var stop = r.Update(In(1, null, 2));
        Assert.Contains("faute d'endurance", stop.Log);
        Assert.Equal(ResumeStep.Rien, r.Update(In(30, null, 60)).Step); // endurance pas encore revenue
        Assert.Equal(ResumeStep.Tab, r.Update(In(60, null, 97, target: Lavender)).Step);
        Assert.Equal(ResumeStep.Rien, r.Update(In(60.3, null, 97, target: Lavender)).Step); // pause entre deux Tab
        Assert.Equal(ResumeStep.Interact, r.Update(In(61, null, 97, target: Tree)).Step);
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
    public void GivesUpAfterTooManyTabs()
    {
        var r = new HarvestResume();
        r.Update(In(0, "Extract", 50));
        r.Update(In(1, null, 1));
        for (var i = 0; i < HarvestResume.MaxTabs; i++)
        {
            Assert.Equal(ResumeStep.Tab, r.Update(In(60 + i, null, 98, target: Lavender)).Step);
        }
        var last = r.Update(In(60 + HarvestResume.MaxTabs, null, 98, target: Lavender));
        Assert.True(last.Alert);
        Assert.False(r.Active);
    }

    [Fact]
    public void ResumeKeysAreAllowed()
    {
        Assert.Equal("Tab", GameKeys.Name(GameKeys.Tab));
        Assert.Equal("M", GameKeys.Name(GameKeys.Interact));
    }
}
