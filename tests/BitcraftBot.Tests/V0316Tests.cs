using BitcraftBot.GameData;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.3.16 : bonus actifs (en pause hors ligne, effets lisibles).
public class V0316BuffTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_115_000);

    [Fact]
    public void Bonus_actif_inactif_et_permanent()
    {
        Assert.Equal(300, new ActiveBuffInfo(1, 1_795_114_700, 600).RemainingSeconds(Now));
        Assert.Equal(0, new ActiveBuffInfo(1, 0, 0).RemainingSeconds(Now));
        Assert.Equal(-1, new ActiveBuffInfo(11, 1_795_114_941, -1).RemainingSeconds(Now));
        Assert.False(new ActiveBuffInfo(1, 1_795_114_700, 600).IsPaused);
    }

    // Vu dans l'instantané du 2026-11-19 : [9,[4974],15,[]] = bonus en pause (4974 s déjà écoulées sur 15).
    [Fact]
    public void Bonus_en_pause_hors_ligne()
    {
        var finished = new ActiveBuffInfo(9, 4974, 15);
        Assert.True(finished.IsPaused);
        Assert.Equal(0, finished.RemainingSeconds(Now));
        var paused = new ActiveBuffInfo(1, 300, 900);
        Assert.True(paused.IsPaused);
        Assert.Equal(600, paused.RemainingSeconds(Now));
        Assert.Equal(900, new ActiveBuffInfo(1, -1, 900).RemainingSeconds(Now));
    }

    [Fact]
    public void Effets_en_francais()
    {
        Assert.Equal("vitesse de récolte -40 %", new BuffStat("GatheringSpeed", -0.4, true).Text);
        Assert.Equal("endurance regagnée +5", new BuffStat("PassiveStaminaRegenRate", 5, false).Text);
        Assert.Equal("some new stat +1", new BuffStat("SomeNewStat", 1, false).Text);
    }

    [Fact]
    public void Bonus_de_nourriture_en_pause_ne_declenche_pas_de_repas()
    {
        var regen = new HashSet<int> { 100 };
        Assert.Equal(600, MealPlanner.FoodRegenSeconds([new ActiveBuffInfo(100, 300, 900)], true, regen, Now));
    }
}
