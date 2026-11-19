using System.Text.Json;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.3.17 : tableau de bord, jauge de métier lue sur le serveur (experience_state), action en cours.
public class V0317ActivityTests
{
    [Fact]
    public void Formule_des_niveaux_du_serveur()
    {
        Assert.Equal(0, SkillLevels.ForLevel(1));
        Assert.Equal(520, SkillLevels.ForLevel(2));
        Assert.Equal(1, SkillLevels.LevelFor(0));
        Assert.Equal(1, SkillLevels.LevelFor(519));
        Assert.Equal(2, SkillLevels.LevelFor(520));
        Assert.Equal(-1, SkillLevels.ForLevel(126));
        Assert.Equal(SkillLevels.MaxLevel, SkillLevels.LevelFor(int.MaxValue));
        // Chaque niveau demande plus que le précédent.
        for (var level = 2; level < SkillLevels.MaxLevel; level++)
        {
            Assert.True(SkillLevels.ForLevel(level + 1) - SkillLevels.ForLevel(level) > SkillLevels.ForLevel(level) - SkillLevels.ForLevel(level - 1));
        }
    }

    [Fact]
    public void Progression_dans_le_niveau()
    {
        var from = SkillLevels.ForLevel(20);
        var to = SkillLevels.ForLevel(21);
        var skill = new SkillProgress(5, from + (to - from) * 3 / 4);
        Assert.Equal(20, skill.Level);
        Assert.InRange(skill.Progress, 0.74, 0.76);
        Assert.Equal(to - skill.Experience, skill.ExperienceToNextLevel);
    }

    [Fact]
    public void Experience_en_tableaux_ou_en_objets()
    {
        using var arrays = JsonDocument.Parse("[[2, 12000], [5, 300], [\"x\"]]");
        Assert.Equal([new SkillProgress(2, 12000), new SkillProgress(5, 300)], PlayerActivity.ParseSkills(arrays.RootElement));
        using var objects = JsonDocument.Parse("""[{"skill_id": 3, "quantity": 7}]""");
        Assert.Equal([new SkillProgress(3, 7)], PlayerActivity.ParseSkills(objects.RootElement));
    }

    [Fact]
    public void Types_d_action()
    {
        using var doc = JsonDocument.Parse("""[[4, []], {"Craft": []}, 22, "Sleep", [99, []]]""");
        var e = doc.RootElement;
        Assert.Equal("Extract", PlayerActivity.EnumName(e[0], PlayerActivity.ActionTypes));
        Assert.Equal("Craft", PlayerActivity.EnumName(e[1], PlayerActivity.ActionTypes));
        Assert.Equal("PlayerMove", PlayerActivity.EnumName(e[2], PlayerActivity.ActionTypes));
        Assert.Equal("Sleep", PlayerActivity.EnumName(e[3], PlayerActivity.ActionTypes));
        Assert.Equal("type 99", PlayerActivity.EnumName(e[4], PlayerActivity.ActionTypes));
        Assert.Equal("récolte", ActionNames.French("Extract"));
    }

    [Fact]
    public void Progression_de_l_action()
    {
        var now = DateTimeOffset.FromUnixTimeMilliseconds(1_795_000_001_000);
        var action = new ActionInfo("Extract", 1_795_000_000_000, 1600, 7, null);
        Assert.InRange(action.Progress(now), 0.62, 0.63);
        Assert.Equal(1, new ActionInfo("Extract", 1_795_000_000_000, 500, 7, null).Progress(now));
        Assert.Equal(0, new ActionInfo("Extract", 1_795_000_000_000, 0, 7, null).Progress(now));
    }

    [Fact]
    public void Metier_en_cours_celui_qui_gagne_de_l_experience()
    {
        var tracker = new SkillTracker();
        var t0 = new DateTime(2026, 11, 19, 20, 0, 0);
        tracker.Update([new SkillProgress(2, 100), new SkillProgress(5, 50)], t0);
        Assert.Null(tracker.ActiveSkill); // première lecture : rien ne bouge encore
        tracker.Update([new SkillProgress(2, 100), new SkillProgress(5, 80)], t0.AddSeconds(2));
        Assert.Equal(5, tracker.ActiveSkill);
        Assert.True(tracker.IsActive(t0.AddSeconds(10), TimeSpan.FromSeconds(20)));
        Assert.False(tracker.IsActive(t0.AddSeconds(30), TimeSpan.FromSeconds(20)));
        tracker.Update(null, t0.AddSeconds(31));
        Assert.Equal(5, tracker.ActiveSkill);
    }

    [Fact]
    public void Requetes_du_tableau_de_bord_en_lecture()
    {
        var q = Queries.FollowPlayer("abcd", 7, null);
        foreach (var table in new[] { "experience_state", "player_action_state", "extract_outcome_state", "player_state", "teleportation_energy_state" })
        {
            Assert.Contains($"SELECT * FROM {table} WHERE entity_id = 7", q);
        }
        Assert.All(q, x => Assert.StartsWith("SELECT ", x));
    }

    [Fact]
    public void Nombre_de_cases_du_sac()
    {
        Assert.Equal(0, new InventoryInfo(1, 2, 0, 0, 0, []).PocketCount);
        Assert.Equal(24, new InventoryInfo(1, 2, 0, 0, 0, [], 24).PocketCount);
    }
}
