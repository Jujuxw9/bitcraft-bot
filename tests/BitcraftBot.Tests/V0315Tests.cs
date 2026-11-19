using System.Text.Json;
using BitcraftBot.GameData;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.3.15 : repas automatique (D-042), bonus actifs, faim maximale, aliments.
public class V0315MealTests
{
    private static readonly FoodInfo Fish = new(1, "Plain Roasted Fish", 25, 0, 0, [100], true);
    private static readonly FoodInfo Potion = new(2, "Healing Potion", 0, 50, 0, [200], false);
    private static readonly FoodInfo Berry = new(3, "Berries", 5, 0, 0, [], false);

    private static MealDecision Decide(double? sat, int? regen, FoodInfo? next = null, bool series = false, double? max = 100, bool sacRead = true, FoodInfo? blocker = null) =>
        MealPlanner.Decide(new MealSituation(sat, max, regen, sacRead, next ?? (sacRead ? Fish : null), blocker, series));

    [Fact]
    public void Rien_si_faim_haute_et_bonus_actif()
    {
        Assert.Equal(MealAction.Rien, Decide(80, 600).Action);
        Assert.Equal(MealAction.Rien, Decide(null, 0).Action);
    }

    [Fact]
    public void Faim_sous_30_pourcent_on_mange_et_on_continue_sans_gaspiller()
    {
        var first = Decide(20, 600);
        Assert.Equal(MealAction.Manger, first.Action);
        Assert.True(first.ContinuesSeries);
        // Série en cours : 70 de manque, le poisson rend 25 → on continue.
        Assert.Equal(MealAction.Manger, Decide(30, 600, series: true).Action);
        // 20 de manque < 25 → fin du repas.
        Assert.Equal(MealAction.Rien, Decide(80, 600, series: true).Action);
    }

    [Fact]
    public void Seuil_relatif_a_la_faim_maximale()
    {
        Assert.Equal(MealAction.Rien, Decide(40, 600, max: 100).Action);
        Assert.Equal(MealAction.Manger, Decide(40, 600, max: 200).Action);
    }

    [Fact]
    public void Plus_de_bonus_un_seul_aliment_meme_faim_haute()
    {
        var d = Decide(95, 0);
        Assert.Equal(MealAction.Manger, d.Action);
        Assert.False(d.ContinuesSeries);
    }

    [Fact]
    public void Bonus_inconnu_seule_la_faim_compte()
    {
        Assert.Equal(MealAction.Rien, Decide(95, null).Action);
    }

    [Fact]
    public void Bloque_sans_nourriture_ou_si_une_potion_passe_avant()
    {
        Assert.Equal(MealAction.Bloque, MealPlanner.Decide(new MealSituation(10, 100, 0, true, null, null, false)).Action);
        Assert.Equal(MealAction.Bloque, Decide(10, 0, blocker: Potion).Action);
    }

    [Fact]
    public void Sac_non_lu_aliment_suppose_de_25()
    {
        Assert.Equal(MealAction.Manger, MealPlanner.Decide(new MealSituation(20, 100, null, false, null, null, false)).Action);
        Assert.Equal(MealAction.Rien, MealPlanner.Decide(new MealSituation(80, 100, null, false, null, null, true)).Action);
    }

    [Fact]
    public void Premier_aliment_du_sac_dans_l_ordre_des_poches()
    {
        var foods = new Dictionary<int, FoodInfo> { [1] = Fish, [2] = Potion, [3] = Berry };
        var sac = new[] { new ItemStackInfo(50, 3, false), new ItemStackInfo(1, 4, true), new ItemStackInfo(3, 2, false), new ItemStackInfo(1, 5, false) };
        Assert.Equal((Berry, (FoodInfo?)null), MealPlanner.FirstFood(sac, foods));
        var withPotion = new[] { new ItemStackInfo(2, 1, false), new ItemStackInfo(1, 5, false) };
        Assert.Equal(((FoodInfo?)null, Potion), MealPlanner.FirstFood(withPotion, foods));
        Assert.Equal(((FoodInfo?)null, (FoodInfo?)null), MealPlanner.FirstFood([], foods));
    }

    [Fact]
    public void Secondes_restantes_du_bonus()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);
        var regen = new HashSet<int> { 100 };
        Assert.Null(MealPlanner.FoodRegenSeconds([], false, regen, now));
        Assert.Equal(0, MealPlanner.FoodRegenSeconds([], true, regen, now));
        Assert.Equal(0, MealPlanner.FoodRegenSeconds([new ActiveBuffInfo(100, 1_794_999_000, 900)], true, regen, now));
        Assert.Equal(400, MealPlanner.FoodRegenSeconds([new ActiveBuffInfo(100, 1_794_999_500, 900), new ActiveBuffInfo(7, 1_794_999_999, 5000)], true, regen, now));
        Assert.Equal(-1, new ActiveBuffInfo(5, 0, -1).RemainingSeconds(now));
    }

    [Fact]
    public void Bonus_actifs_en_tableaux_ou_en_objets()
    {
        using var arrays = JsonDocument.Parse("""[[100, [9500], 900, [1.5]], [7, {"value": 9999}, 5000, []], ["x"]]""");
        Assert.Equal([new ActiveBuffInfo(100, 9500, 900), new ActiveBuffInfo(7, 9999, 5000)], PlayerBuffs.Parse(arrays.RootElement));
        using var objects = JsonDocument.Parse("""[{"buff_id": 3, "buff_start_timestamp": {"value": 12}, "buff_duration": -1, "values": []}]""");
        Assert.Equal([new ActiveBuffInfo(3, 12, -1)], PlayerBuffs.Parse(objects.RootElement));
    }

    [Fact]
    public void Requetes_bonus_et_stats_en_lecture()
    {
        var q = Queries.FollowPlayer("abcd", 7, null);
        Assert.Contains("SELECT * FROM active_buff_state WHERE entity_id = 7", q);
        Assert.Contains("SELECT * FROM character_stats_state WHERE entity_id = 7", q);
        Assert.DoesNotContain(Queries.FollowPlayer("abcd", 7, null, withInventory: false), x => x.Contains("active_buff_state", StringComparison.Ordinal));
    }
}

public class V0315FoodBookTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bitcraft-aliments-" + Guid.NewGuid().ToString("N"));

    public V0315FoodBookTests()
    {
        Directory.CreateDirectory(_folder);
        Write("item_desc", """[{"id": 1, "name": "Plain Roasted Fish", "icon_asset_name": "Items/Fish"}, {"id": 2, "name": "Hot Tea"}]""");
        Write("buff_type_desc", """[{"id": 50, "name": "Food Regen", "category": 1}, {"id": 51, "name": "Teas", "category": 1}]""");
        Write("buff_desc", """[{"id": 100, "buff_type_id": 50, "description": "Level 1 Food Regen"}, {"id": 200, "buff_type_id": 51, "description": "Hot Tea"}]""");
        Write("food_desc", """
            [{"item_id": 1, "hunger": 25.0, "hp": 0, "stamina": 0, "buffs": [{"buff_id": 100, "duration": 900}]},
             {"item_id": 2, "hunger": 0, "hp": 0, "stamina": 0, "buffs": [{"buff_id": 200, "duration": 60}]}]
            """);
    }

    private void Write(string table, string json) => File.WriteAllText(Path.Combine(_folder, table + ".json"), json);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Aliments_et_bonus_de_nourriture()
    {
        Assert.True(FoodBook.CanLoad(_folder));
        var book = FoodBook.Load(_folder);
        Assert.Equal(new HashSet<int> { 100 }, book.FoodRegenBuffIds);
        Assert.True(book.Foods[1].GivesFoodRegen);
        Assert.True(book.Foods[1].IsMeal);
        Assert.Equal(25, book.Foods[1].Hunger);
        Assert.False(book.Foods[2].IsMeal);
        Assert.Equal("Level 1 Food Regen", book.Buffs[100].Name);
        Assert.Equal("Food Regen", book.Buffs[100].Type);
    }

    // Vraies données (si BITCRAFT_GAMEDATA pointe vers un dossier complet) : le poisson grillé donne le bonus.
    [Fact]
    public void Vraies_donnees_poisson_grille()
    {
        var folder = Environment.GetEnvironmentVariable("BITCRAFT_GAMEDATA");
        if (string.IsNullOrEmpty(folder) || !FoodBook.CanLoad(folder))
        {
            return;
        }
        var book = FoodBook.Load(folder);
        var fish = Assert.Single(book.Foods.Values, f => f.Name == "Plain Roasted Fish");
        Assert.Equal(25, fish.Hunger);
        Assert.True(fish.GivesFoodRegen);
        Assert.Equal(11, book.FoodRegenBuffIds.Count);
        var recipes = RecipeBook.Load(folder);
        Assert.StartsWith("Items/", recipes.Search("Plain Cooked Berries")[0].Icon);
    }
}
