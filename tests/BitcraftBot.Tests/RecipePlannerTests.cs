using BitcraftBot.GameData;
using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.12 (session tâches) : recettes sans paquets, plus simple d'abord, arborescence, plan d'actions, effort, avancement des tâches.
public class RecipePlannerTests
{
    private static GameEntry Entry(string key, string name, string tag = "") =>
        new(int.Parse(key[1..]), key[0] == 'C', name, "", 1, tag, "Common");

    private static Recipe Harvest(string place, string output, int quantity = 1, double seconds = 2) =>
        new("Récolte", $"Récolter : {place}", place, [], [], [], [new EntryAmount(output, quantity)], 0, seconds, 0, false, false, true);

    private static Recipe Craft(string name, string output, int outQty, int actions, double seconds, params EntryAmount[] inputs) =>
        new("Fabrication", name, "Établi, palier 1 minimum", [], [], inputs, [new EntryAmount(output, outQty)], actions, seconds, 0, false, false, false);

    // A (I1) = 5 B (I2) + 5 C (I3) ; B se récolte ; C = 1 D (I4) ; D se récolte sur de la pierre : trois niveaux.
    private static RecipeBook ThreeLevels(params Recipe[] extra)
    {
        var entries = new[] { Entry("I1", "A"), Entry("I2", "B"), Entry("I3", "C"), Entry("I4", "D"), Entry("I9", "Colis", "Package") }
            .ToDictionary(e => e.Key);
        Recipe[] recipes =
        [
            Craft("Fabriquer A", "I1", 1, 2, 3, new EntryAmount("I2", 5), new EntryAmount("I3", 5)),
            Harvest("Arbre", "I2"),
            Craft("Fabriquer C", "I3", 1, 1, 4, new EntryAmount("I4", 1)),
            Harvest("Pierre", "I4"),
            .. extra,
        ];
        return new RecipeBook { Entries = entries, Recipes = recipes };
    }

    [Fact]
    public void PackageRecipesAreDroppedAndSelfConsumingToo()
    {
        var packageRecipe = Craft("Unpack Colis", "I1", 1, 1, 1, new EntryAmount("I9", 1));
        var packRecipe = Craft("Package A", "I9", 1, 1, 1, new EntryAmount("I1", 5));
        var selfRecipe = Craft("Convertir A", "I1", 1, 1, 1, new EntryAmount("I1", 1));
        var book = ThreeLevels(packageRecipe, packRecipe, selfRecipe);
        var ways = book.Simplest("I1");
        Assert.Single(ways);
        Assert.Equal("Fabriquer A", ways[0].Name);
        Assert.True(book.IsPackage(packageRecipe));
        Assert.True(book.IsPackage(packRecipe));
    }

    [Fact]
    public void SimplestPutsHarvestBeforeCraftAndFewerIngredientsFirst()
    {
        var entries = new[] { Entry("I1", "A"), Entry("I2", "B"), Entry("I3", "C") }.ToDictionary(e => e.Key);
        var heavy = Craft("Lourd", "I1", 1, 1, 1, new EntryAmount("I2", 1), new EntryAmount("I3", 1));
        var light = Craft("Léger", "I1", 1, 1, 1, new EntryAmount("I2", 1));
        var harvest = Harvest("Arbre", "I1");
        var book = new RecipeBook { Entries = entries, Recipes = [heavy, light, harvest] };
        Assert.Equal(["Récolter : Arbre", "Léger", "Lourd"], book.Simplest("I1").Select(r => r.Name));
    }

    [Fact]
    public void TreeHasThreeLevelsWithMultipliedQuantities()
    {
        var tree = RecipePlanner.Tree(ThreeLevels(), "I1", 2);
        Assert.Equal(2, tree.Quantity);
        Assert.Equal(["I2", "I3"], tree.Children.Select(c => c.Key));
        Assert.Equal(10, tree.Children[0].Quantity);
        Assert.Equal("Récolte", tree.Children[0].Recipe!.Kind);
        var c = tree.Children[1];
        Assert.Equal(10, c.Quantity);
        Assert.Equal("I4", c.Children.Single().Key);
        Assert.Equal(10, c.Children.Single().Quantity);
        Assert.Equal("Pierre", c.Children.Single().Recipe!.Place);
    }

    [Fact]
    public void StepsAreOrderedBaseFirstAndTotalsAdd()
    {
        var steps = RecipePlanner.Steps(ThreeLevels(), "I1", 2);
        Assert.Equal("I1", steps[^1].Key);
        Assert.Equal(["I1", "I2", "I3", "I4"], steps.Select(s => s.Key).OrderBy(k => k));
        Assert.True(steps.ToList().FindIndex(s => s.Key == "I4") < steps.ToList().FindIndex(s => s.Key == "I3"));
        Assert.Equal(10, steps.Single(s => s.Key == "I2").Quantity);
        Assert.Equal(10, steps.Single(s => s.Key == "I4").Crafts);
        Assert.Equal(2, steps.Single(s => s.Key == "I1").Crafts);
    }

    [Fact]
    public void SharedIngredientIsMergedIntoOneStep()
    {
        // A = 2 B + 1 C ; C = 3 B : B apparaît deux fois dans l'arbre, une seule fois dans le plan (2 + 3 = 5).
        var entries = new[] { Entry("I1", "A"), Entry("I2", "B"), Entry("I3", "C") }.ToDictionary(e => e.Key);
        var book = new RecipeBook
        {
            Entries = entries,
            Recipes =
            [
                Craft("A", "I1", 1, 1, 1, new EntryAmount("I2", 2), new EntryAmount("I3", 1)),
                Craft("C", "I3", 1, 1, 1, new EntryAmount("I2", 3)),
                Harvest("Arbre", "I2"),
            ],
        };
        var steps = RecipePlanner.Steps(book, "I1", 1);
        Assert.Equal(5, steps.Single(s => s.Key == "I2").Quantity);
        Assert.Equal(3, steps.Count);
    }

    [Fact]
    public void CraftsAreRoundedUpToTheOutputQuantity()
    {
        var entries = new[] { Entry("I1", "A"), Entry("I2", "B") }.ToDictionary(e => e.Key);
        var book = new RecipeBook { Entries = entries, Recipes = [Craft("A", "I1", 3, 1, 1, new EntryAmount("I2", 2)), Harvest("Arbre", "I2")] };
        var steps = RecipePlanner.Steps(book, "I1", 7);
        Assert.Equal(3, steps.Single(s => s.Key == "I1").Crafts);
        Assert.Equal(6, steps.Single(s => s.Key == "I2").Quantity);
    }

    [Fact]
    public void CyclesDoNotLoopForever()
    {
        var entries = new[] { Entry("I1", "X"), Entry("I2", "Y") }.ToDictionary(e => e.Key);
        var book = new RecipeBook { Entries = entries, Recipes = [Craft("X", "I1", 1, 1, 1, new EntryAmount("I2", 1)), Craft("Y", "I2", 1, 1, 1, new EntryAmount("I1", 1))] };
        Assert.NotEmpty(RecipePlanner.Steps(book, "I1", 1));
        Assert.NotNull(RecipePlanner.Tree(book, "I1", 1));
    }

    [Fact]
    public void EffortCountsCraftStepsUnknownSourcesAndTime()
    {
        var book = ThreeLevels();
        var harvestOnly = RecipePlanner.EffortOf(book, "I2", 10);
        Assert.Equal(0, harvestOnly.CraftSteps);
        Assert.Equal(0, harvestOnly.Unknown);
        Assert.Equal(20, harvestOnly.Seconds);
        Assert.Contains("récolte seulement", harvestOnly.Text);

        var full = RecipePlanner.EffortOf(book, "I1", 1);
        Assert.Equal(2, full.CraftSteps);
        Assert.True(full.SortKey.CompareTo(harvestOnly.SortKey) > 0);

        var unknown = RecipePlanner.EffortOf(book, "I9", 1);
        Assert.Equal(1, unknown.Unknown);
        Assert.True(unknown.SortKey.CompareTo(full.SortKey) > 0);
    }

    [Fact]
    public void TaskProgressAndEffortToFinish()
    {
        var book = ThreeLevels();
        var desc = new TravelerTaskDesc(1, 13, 1, 120, [new TaskItem(2, 10, false), new TaskItem(4, 10, false)], "t", 13, 1);
        var stock = new[] { new StockStack("Sac", true, new ItemStackInfo(2, 10, false)), new StockStack("Banque", false, new ItemStackInfo(4, 5, false)) };
        var check = TravelerTaskPlanner.Check([new TravelerTaskInfo(1, 3, 1, false)], new Dictionary<int, TravelerTaskDesc> { [1] = desc }, stock).Single();
        Assert.Equal(15.0 / 20.0, check.Progress, 3);
        // Il manque 5 × D (I4) à récolter : une récolte, aucune fabrication.
        var effort = TravelerTaskPlanner.EffortToFinish(check, book);
        Assert.Equal(0, effort.CraftSteps);
        Assert.Equal(10, effort.Seconds);
    }

    // Vraies données du jeu (dossier de la version publiée), si elles sont là : plan d'une planche solide, sans paquet, terminé.
    [Fact]
    public void Vraies_donnees_plan_d_une_planche()
    {
        var folder = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "artifacts", "release", "donnees-jeu");
        if (!RecipeBook.CanLoad(folder))
        {
            return;
        }
        var book = RecipeBook.Load(folder);
        var plank = book.Search("Sturdy Plank")[0];
        var steps = RecipePlanner.Steps(book, plank.Key, 10);
        Assert.NotEmpty(steps);
        Assert.Equal(plank.Key, steps[^1].Key);
        Assert.All(steps, st => Assert.True(st.Recipe is null || !book.IsPackage(st.Recipe)));
        Assert.True(RecipePlanner.Tree(book, plank.Key, 10).Children.Count > 0);
        // Les fleurs séchées n'ont aucune source dans les données : source inconnue, pas d'erreur.
        var flowers = book.Search("Dried Basic Flowers")[0];
        Assert.Equal(1, RecipePlanner.EffortOf(book, flowers.Key, 1).Unknown);
    }
}
