using BitcraftBot.GameData;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.11 (session tâches) : onglet Voyageurs, tâches de voyageur comparées au stock.
public class TravelerTaskTests
{
    private const ulong Me = 77;

    private static TravelerTaskDesc Desc(int id, params TaskItem[] items) =>
        new(id, 13, 1, 120, items, $"Tâche {id}", 13, 100);

    private static TravelerTaskInfo Task(int taskId, bool completed = false, ulong entity = 0) =>
        new(entity == 0 ? (ulong)taskId : entity, 3, taskId, completed);

    private static StockStack Bag(int id, int qty, bool cargo = false) => new("Sac", true, new ItemStackInfo(id, qty, cargo));

    private static StockStack Bank(int id, int qty, string where = "Banque à 100, 200") => new(where, false, new ItemStackInfo(id, qty, false));

    private static TaskCheck One(TravelerTaskDesc desc, params StockStack[] stock) =>
        TravelerTaskPlanner.Check([Task(desc.Id)], new Dictionary<int, TravelerTaskDesc> { [desc.Id] = desc }, stock).Single();

    [Fact]
    public void LoadsTasksAndTravelersFromGameDataFormat()
    {
        var folder = GameDataTests.WriteTables(new Dictionary<string, string>
        {
            ["traveler_task_desc"] = "[{\"id\":5,\"level_requirement\":{\"skill_id\":17,\"min_level\":40,\"max_level\":120}," +
                "\"required_items\":[{\"item_id\":60,\"quantity\":5,\"item_type\":\"Item\",\"durability\":0},{\"item_id\":9,\"quantity\":2,\"item_type\":\"Cargo\",\"durability\":0}]," +
                "\"rewarded_items\":[{\"item_id\":1,\"quantity\":330,\"item_type\":\"Item\",\"durability\":0}]," +
                "\"rewarded_experience\":{\"skill_id\":17,\"quantity\":2745.0},\"description\":\"Deliver some Healing Potions.\"}]",
            ["npc_desc"] = "[{\"npc_type\":3,\"name\":\"Heimlich\",\"task_skill_check\":[13]}]",
        });
        var tasks = TravelerTaskBook.LoadTasks(folder);
        var t = tasks[5];
        Assert.Equal(17, t.SkillId);
        Assert.Equal(40, t.MinLevel);
        Assert.Equal([new TaskItem(60, 5, false), new TaskItem(9, 2, true)], t.Required);
        Assert.Equal("Deliver some Healing Potions.", t.Description);
        Assert.Equal(2745.0, t.RewardExperience);
        Assert.Equal("Heimlich", TravelerTaskBook.LoadTravelers(folder)[3]);
    }

    [Fact]
    public void MissingFilesGiveEmptyLists()
    {
        var folder = GameDataTests.WriteTables([]);
        Assert.Empty(TravelerTaskBook.LoadTasks(folder));
        Assert.Empty(TravelerTaskBook.LoadTravelers(folder));
    }

    [Fact]
    public void EverythingInBagIsDoableNow()
    {
        var c = One(Desc(1, new TaskItem(10, 5, false)), Bag(10, 3), Bag(10, 4));
        Assert.Equal(TaskVerdict.FaisableMaintenant, c.Verdict);
        Assert.Equal(5, c.Items[0].InBag);
        Assert.Equal(0, c.Items[0].Missing);
    }

    [Fact]
    public void ItemsInBankOrBeltMustBeFetched()
    {
        var c = One(Desc(1, new TaskItem(10, 5, false)), Bag(10, 2), Bank(10, 2), new StockStack("Ceinture d'outils", false, new ItemStackInfo(10, 1, false)));
        Assert.Equal(TaskVerdict.FaisableApresBanque, c.Verdict);
        Assert.Equal(2, c.Items[0].InBag);
        Assert.Contains(("Banque à 100, 200", 2), c.Items[0].Elsewhere);
        Assert.Contains(("Ceinture d'outils", 1), c.Items[0].Elsewhere);
    }

    [Fact]
    public void PartialGivesExactMissing()
    {
        var c = One(Desc(1, new TaskItem(10, 5, false), new TaskItem(11, 3, false)), Bag(10, 5), Bank(11, 1));
        Assert.Equal(TaskVerdict.Partiel, c.Verdict);
        Assert.Equal(0, c.Items[0].Missing);
        Assert.Equal(2, c.Items[1].Missing);
    }

    [Fact]
    public void NothingIsImpossible()
    {
        var c = One(Desc(1, new TaskItem(10, 5, false)));
        Assert.Equal(TaskVerdict.Impossible, c.Verdict);
        Assert.Equal(5, c.Items[0].Missing);
    }

    [Fact]
    public void OtherTierOrCargoIsNotConfused()
    {
        // Même nom possible, autre palier = autre numéro ; une cargaison n° 10 n'est pas l'objet n° 10.
        var c = One(Desc(1, new TaskItem(10, 5, false)), Bag(12, 50), Bag(10, 50, cargo: true));
        Assert.Equal(TaskVerdict.Impossible, c.Verdict);
    }

    [Fact]
    public void SharedStockIsCountedOnce()
    {
        var descs = new Dictionary<int, TravelerTaskDesc>
        {
            [1] = Desc(1, new TaskItem(10, 4, false)),
            [2] = Desc(2, new TaskItem(10, 4, false)),
        };
        var checks = TravelerTaskPlanner.Check([Task(1), Task(2)], descs, [Bag(10, 6)]);
        Assert.Equal(TaskVerdict.FaisableMaintenant, checks[0].Verdict);
        Assert.Equal(TaskVerdict.Partiel, checks[1].Verdict);
        Assert.Equal(2, checks[1].Items[0].Missing);
        Assert.True(checks[0].Items[0].Shared);
        Assert.True(checks[1].Items[0].Shared);
    }

    [Fact]
    public void CompletedTaskUsesNoStock()
    {
        var descs = new Dictionary<int, TravelerTaskDesc>
        {
            [1] = Desc(1, new TaskItem(10, 4, false)),
            [2] = Desc(2, new TaskItem(10, 4, false)),
        };
        var checks = TravelerTaskPlanner.Check([Task(1, completed: true), Task(2)], descs, [Bag(10, 4)]);
        Assert.Equal(TaskVerdict.DejaRendue, checks[0].Verdict);
        Assert.Equal(TaskVerdict.FaisableMaintenant, checks[1].Verdict);
        Assert.False(checks[1].Items[0].Shared);
    }

    [Fact]
    public void UnknownTaskAndNoTask()
    {
        Assert.Equal(TaskVerdict.Inconnue, TravelerTaskPlanner.Check([Task(9)], new Dictionary<int, TravelerTaskDesc>(), []).Single().Verdict);
        Assert.Empty(TravelerTaskPlanner.Check([], new Dictionary<int, TravelerTaskDesc>(), [Bag(10, 1)]));
    }

    [Fact]
    public void ReadsOnlyThePlayersTasks()
    {
        var cache = new TableCache();
        cache.Apply(new TableChange("traveler_task_state",
        [
            Row.Parse($"{{\"entity_id\":1,\"player_entity_id\":{Me},\"traveler_id\":3,\"task_id\":50,\"completed\":false}}", null),
            Row.Parse($"{{\"entity_id\":2,\"player_entity_id\":{Me},\"traveler_id\":1,\"task_id\":60,\"completed\":true}}", null),
            Row.Parse("{\"entity_id\":3,\"player_entity_id\":99,\"traveler_id\":1,\"task_id\":70,\"completed\":false}", null),
        ], []));
        var tasks = TravelerTasks.Of(cache, Me);
        Assert.Equal(2, tasks.Count);
        Assert.Equal(new TravelerTaskInfo(2, 1, 60, true), tasks[0]);
        Assert.Equal(new TravelerTaskInfo(1, 3, 50, false), tasks[1]);
    }

    [Fact]
    public void QueryAsksForTasksUnlessRefused()
    {
        const string q = "SELECT * FROM traveler_task_state WHERE player_entity_id = 77";
        Assert.Contains(q, Queries.FollowPlayer("ab", Me, null));
        Assert.DoesNotContain(q, Queries.FollowPlayer("ab", Me, null, withTasks: false));
    }

    [Fact]
    public void VerdictTextsAreFrench()
    {
        Assert.Equal("Faisable maintenant", TravelerTaskPlanner.Text(TaskVerdict.FaisableMaintenant));
        Assert.Equal("Impossible", TravelerTaskPlanner.Text(TaskVerdict.Impossible));
    }
}
