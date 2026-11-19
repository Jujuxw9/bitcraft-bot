using System.Text.Json;
using BitcraftBot.GameData;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.3.14 : sac et banques (inventory_state), livre des recettes.
public class V0314InventoryTests
{
    private static IReadOnlyList<ItemStackInfo> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Inventories.ParsePockets(doc.RootElement.Clone());
    }

    [Fact]
    public void Poches_en_tableaux_avec_option_some_none()
    {
        var stacks = Parse("""
        [
          [6000, {"some": [1020003, 12, {"Item": []}, {"none": []}]}, false],
          [6000, {"none": []}, false],
          [60000, {"some": [7, 1, {"Cargo": []}, {"none": []}]}, false]
        ]
        """);
        Assert.Equal(2, stacks.Count);
        Assert.Equal(new ItemStackInfo(1020003, 12, false), stacks[0]);
        Assert.Equal(new ItemStackInfo(7, 1, true), stacks[1]);
    }

    [Fact]
    public void Poches_en_objets()
    {
        var stacks = Parse("""
        [
          {"volume": 6000, "contents": {"item_id": 11001, "quantity": 3, "item_type": "Item", "durability": null}, "locked": false},
          {"volume": 6000, "contents": null, "locked": false}
        ]
        """);
        Assert.Equal([new ItemStackInfo(11001, 3, false)], stacks);
    }

    [Fact]
    public void Option_en_somme_tableau_et_type_en_nombre()
    {
        var stacks = Parse("""[[6000, [0, [5, 2, 1, [1, []]]], false], [6000, [1, []], false]]""");
        Assert.Equal([new ItemStackInfo(5, 2, true)], stacks);
    }

    [Fact]
    public void Poches_illisibles_ignorees()
    {
        Assert.Empty(Parse("""{"pas": "une liste"}"""));
        Assert.Empty(Parse("""[[6000, {"some": ["x"]}, false]]"""));
    }

    [Fact]
    public void Banque_regionale_sans_batiment()
    {
        Assert.True(new InventoryInfo(1, 0, 7, 0, 0, []).IsRegionalBank);
        Assert.False(new InventoryInfo(1, 7, 0, 0, 0, []).IsRegionalBank);
    }

    [Fact]
    public void Requetes_d_inventaire_en_lecture_et_retirables()
    {
        var with = Queries.FollowPlayer("abcd", 7, null);
        Assert.Contains("SELECT * FROM inventory_state WHERE owner_entity_id = 7", with);
        Assert.Contains("SELECT * FROM inventory_state WHERE player_owner_entity_id = 7", with);
        Assert.Contains("SELECT * FROM bank_state", with);
        Assert.All(with, q => Assert.StartsWith("SELECT ", q));
        Assert.DoesNotContain(Queries.FollowPlayer("abcd", 7, null, withInventory: false), q => q.Contains("inventory_state", StringComparison.Ordinal));
    }
}

public class V0314RecipeTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bitcraft-recettes-" + Guid.NewGuid().ToString("N"));

    public V0314RecipeTests()
    {
        Directory.CreateDirectory(_folder);
        Write("item_desc", """
            [{"id": 1, "name": "Stripped Wood", "description": "Bois écorcé.", "tier": 1, "tag": "Wood", "rarity": "Common"},
             {"id": 2, "name": "Rough Plank", "description": "A plank of wood.", "tier": 1, "tag": "Plank", "rarity": "Common"},
             {"id": 3, "name": "Rough Log", "description": "", "tier": 1, "tag": "Log", "rarity": "Common"}]
            """);
        Write("cargo_desc", """[{"id": 1, "name": "Sagi Bird", "description": "Un animal.", "tier": 1, "tag": "Animal", "rarity": "Common"}]""");
        Write("skill_desc", """[{"id": 1, "name": "ANY"}, {"id": 2, "name": "Forestry"}, {"id": 3, "name": "Carpentry"}]""");
        Write("tool_type_desc", """[{"id": 1, "name": "Axe"}, {"id": 2, "name": "Saw"}]""");
        Write("building_type_desc", """[{"id": 13, "name": "Carpentry Station"}]""");
        Write("resource_desc", """[{"id": 21, "name": "Mature Oak Tree"}]""");
        Write("crafting_recipe_desc", """
            [{"id": 10, "name": "Saw {0}", "time_requirement": 1.6, "stamina_requirement": 0.75,
              "building_requirement": {"building_type": 13, "tier": 1},
              "level_requirements": [{"skill_id": 3, "level": 2}], "tool_requirements": [{"tool_type": 2, "level": 1}],
              "consumed_item_stacks": [{"item_id": 1, "quantity": 2, "item_type": "Item"}],
              "crafted_item_stacks": [{"item_id": 2, "quantity": 1, "item_type": "Item"}],
              "actions_required": 20, "required_knowledges": [5], "required_claim_tech_id": 0, "allow_use_hands": false}]
            """);
        Write("extraction_recipe_desc", """
            [{"id": 1, "resource_id": 21, "time_requirement": 1.6, "stamina_requirement": 1.0, "verb_phrase": "Chop",
              "extracted_item_stacks": [{"item_stack": {"item_id": 3, "quantity": 2, "item_type": "Item"}, "probability": 0.5}],
              "consumed_item_stacks": [], "tool_requirements": [{"tool_type": 1, "level": 1}],
              "level_requirements": [{"skill_id": 2, "level": 1}], "allow_use_hands": false, "required_knowledges": []}]
            """);
    }

    private void Write(string table, string json) => File.WriteAllText(Path.Combine(_folder, table + ".json"), json);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Recherche_par_morceau_de_nom()
    {
        var book = RecipeBook.Load(_folder);
        Assert.Equal(["Rough Plank", "Rough Log"], book.Search("rough").Select(e => e.Name).OrderByDescending(n => n).ToList());
        Assert.Equal("Rough Plank", book.Search("rough plank")[0].Name);
        Assert.Equal("Sagi Bird", Assert.Single(book.Search("sagi")).Name);
        Assert.True(Assert.Single(book.Search("sagi")).IsCargo);
        Assert.Empty(book.Search("   "));
    }

    [Fact]
    public void Fabrication_avec_atelier_outil_metier_et_conditions()
    {
        var book = RecipeBook.Load(_folder);
        var recipe = Assert.Single(book.Producing("I2"));
        Assert.Equal("Fabrication", recipe.Kind);
        Assert.Equal("Saw Rough Plank", recipe.Name);
        Assert.Equal("Carpentry Station, palier 1 minimum", recipe.Place);
        Assert.Equal([new Requirement("Carpentry", 2)], recipe.Skills);
        Assert.Equal([new Requirement("Saw", 1)], recipe.Tools);
        Assert.Equal([new EntryAmount("I1", 2)], recipe.Inputs);
        Assert.Equal(20, recipe.Actions);
        Assert.True(recipe.NeedsKnowledge);
        Assert.Same(recipe, Assert.Single(book.Using("I1")));
    }

    [Fact]
    public void Recolte_sur_une_ressource_avec_probabilite()
    {
        var book = RecipeBook.Load(_folder);
        var recipe = Assert.Single(book.Producing("I3"));
        Assert.Equal("Récolte", recipe.Kind);
        Assert.Equal("Chop : Mature Oak Tree", recipe.Name);
        Assert.Equal(0.5, recipe.Outputs[0].Probability);
        Assert.Equal([new Requirement("Axe", 1)], recipe.Tools);
        Assert.Empty(book.Producing("C1"));
    }

    // Vraies données (lancé seulement si BITCRAFT_GAMEDATA pointe vers un dossier complet).
    [Fact]
    public void Vraies_donnees_planche_et_bois()
    {
        var folder = Environment.GetEnvironmentVariable("BITCRAFT_GAMEDATA");
        if (string.IsNullOrEmpty(folder) || !RecipeBook.CanLoad(folder))
        {
            return;
        }
        var book = RecipeBook.Load(folder);
        var plank = book.Search("Rough Plank")[0];
        Assert.Equal("Rough Plank", plank.Name);
        Assert.NotEmpty(book.Producing(plank.Key));
        var log = book.Search("Rough Log")[0];
        Assert.Contains(book.Producing(log.Key), r => r.Kind == "Récolte");
    }
}
