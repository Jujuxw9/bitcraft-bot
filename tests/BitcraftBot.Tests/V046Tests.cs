using BitcraftBot.GameData;

namespace BitcraftBot.Tests;

// v0.4.6 : fiche des recettes en logigramme ; info-bulle « comment l'obtenir » (façons les plus simples d'abord).
public class V046Tests
{
    private static Recipe Craft(string name, string place, IReadOnlyList<EntryAmount> inputs, IReadOnlyList<EntryAmount> outputs, int skillLevel = 1) =>
        new("Fabrication", name, place, [new Requirement("Carpentry", skillLevel)], [new Requirement("Saw", 1)], inputs, outputs, 10, 2, 1, false, false, false);

    private static RecipeBook Book()
    {
        var entries = new Dictionary<string, GameEntry>
        {
            ["I1"] = new(1, false, "Rough Log", "", 1, "Log", "Common"),
            ["I2"] = new(2, false, "Rough Plank", "", 1, "Plank", "Common"),
            ["I3"] = new(3, false, "Rough Stick", "", 1, "Stick", "Common"),
            ["C9"] = new(9, true, "Rough Plank Package", "", 1, "Package", "Common"),
        };
        var recipes = new List<Recipe>
        {
            Craft("Unpack Rough Plank Package", "Station, palier 1 minimum", [new("C9", 1), new("I2", 1)], [new("I2", 100)]),
            Craft("Rough Plank (lot)", "Carpentry Station, palier 1 minimum", [new("I1", 4), new("I3", 2)], [new("I2", 2)], 5),
            Craft("Rough Plank", "Carpentry Station, palier 1 minimum", [new("I1", 2)], [new("I2", 1)], 3),
            new("Récolte", "Chop : Rough Tree", "Rough Tree", [new Requirement("Forestry", 1)], [new Requirement("Axe", 1)], [], [new("I1", 1), new("I3", 1, 0.25)], 0, 1.5, 1, false, false, true),
        };
        return new RecipeBook { Entries = entries, Recipes = recipes };
    }

    [Fact]
    public void Facons_les_plus_simples_d_abord()
    {
        var ways = Book().Simplest("I2");
        // v0.4.12 : les paquets (« Unpack … Package ») ne sont plus proposés (demande de Julien).
        Assert.Equal("Rough Plank", ways[0].Name);
        Assert.DoesNotContain(ways, r => r.Name.StartsWith("Unpack"));
        Assert.All(ways, r => Assert.False(Book().IsPackage(r)));
        Assert.Single(Book().Simplest("I2", 1));
    }

    [Fact]
    public void Recolte_avant_fabrication()
    {
        var book = Book();
        Assert.Equal("Récolte", book.Simplest("I3")[0].Kind);
    }

    [Fact]
    public void Resume_d_une_fabrication()
    {
        var book = Book();
        Assert.Equal("Fabrication (Carpentry Station, palier 1 minimum) : 2 × Rough Log · Carpentry 3 · Saw 1",
            book.Summary(book.Simplest("I2")[0]));
    }

    [Fact]
    public void Resume_d_une_recolte()
    {
        var book = Book();
        Assert.Equal("Récolte sur « Rough Tree » · Forestry 1 · Axe 1", book.Summary(book.Simplest("I1")[0]));
    }

    [Fact]
    public void Resume_signale_une_recette_a_debloquer()
    {
        var book = Book();
        var locked = Book().Recipes[2] with { NeedsKnowledge = true };
        Assert.EndsWith("· à débloquer", book.Summary(locked));
    }

    [Fact]
    public void Objet_sans_recette()
    {
        Assert.Empty(Book().Simplest("C9"));
    }

    // Vraies données du jeu (dossier de la version publiée), si elles sont là.
    [Fact]
    public void Vraies_donnees_planche()
    {
        var folder = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "artifacts", "release", "donnees-jeu");
        if (!RecipeBook.CanLoad(folder))
        {
            return;
        }
        var book = RecipeBook.Load(folder);
        var plank = book.Search("Rough Plank")[0];
        var ways = book.Simplest(plank.Key);
        Assert.NotEmpty(ways);
        Assert.DoesNotContain(plank.Key, ways[0].Inputs.Select(i => i.Key)); // pas « déballer » en premier
        Assert.StartsWith("Fabrication (", book.Summary(ways[0]));
        Assert.All(ways, r => Assert.False(book.IsPackage(r))); // v0.4.12 : aucun paquet
        var log = book.Search("Rough Log")[0];
        Assert.Equal("Récolte", book.Simplest(log.Key)[0].Kind);
    }
}
