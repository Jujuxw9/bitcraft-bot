using BitcraftBot.GameData;

namespace BitcraftBot.Tests;

// v0.4.15 : matières premières d'une tâche, stock déduit à chaque niveau.
public class RawMaterialsTests
{
    private static GameEntry Entry(string key, string name) => new(int.Parse(key[1..]), false, name, "", 1, "", "Common");

    private static Recipe Harvest(string place, string output) =>
        new("Récolte", $"Récolter : {place}", place, [], [], [], [new EntryAmount(output, 1)], 0, 2, 0, false, false, true);

    private static Recipe Craft(string output, int outQty, params EntryAmount[] inputs) =>
        new("Fabrication", "Fabriquer", "Établi", [], [], inputs, [new EntryAmount(output, outQty)], 1, 1, 0, false, false, false);

    // Mur (I1) = 4 briques (I2) + 1 planche (I3) ; brique = 2 argiles (I4) ; argile se récolte ; planche = 1 bois (I5) ; bois se récolte.
    private static RecipeBook Book() => new()
    {
        Entries = new[] { Entry("I1", "Mur"), Entry("I2", "Brique"), Entry("I3", "Planche"), Entry("I4", "Argile"), Entry("I5", "Bois"), Entry("I6", "Relique") }
            .ToDictionary(e => e.Key),
        Recipes =
        [
            Craft("I1", 1, new EntryAmount("I2", 4), new EntryAmount("I3", 1)),
            Craft("I2", 1, new EntryAmount("I4", 2)),
            Harvest("Argile", "I4"),
            Craft("I3", 1, new EntryAmount("I5", 1)),
            Harvest("Arbre", "I5"),
        ],
    };

    [Fact]
    public void GoesDownToHarvestedResources()
    {
        var raw = RawMaterials.For(Book(), [("I1", 2)], new Dictionary<string, int>());
        Assert.Equal(16, raw.Single(r => r.Key == "I4").Quantity);
        Assert.Equal(2, raw.Single(r => r.Key == "I5").Quantity);
        Assert.Equal("Argile", raw.Single(r => r.Key == "I4").Harvest!.Place);
    }

    [Fact]
    public void IntermediateStockIsDeducted()
    {
        // 5 briques en banque : il en manque 3 → 6 argiles ; 1 bois en stock : 1 bois à récolter.
        var raw = RawMaterials.For(Book(), [("I1", 2)], new Dictionary<string, int> { ["I2"] = 5, ["I5"] = 1 });
        Assert.Equal(6, raw.Single(r => r.Key == "I4").Quantity);
        Assert.Equal(1, raw.Single(r => r.Key == "I5").Quantity);
    }

    [Fact]
    public void RequestedItemStockIsNotCountedTwice()
    {
        // Les 2 murs demandés sont déjà « manquants » : un mur en stock ne les réduit pas une deuxième fois.
        var raw = RawMaterials.For(Book(), [("I1", 2)], new Dictionary<string, int> { ["I1"] = 1 });
        Assert.Equal(16, raw.Single(r => r.Key == "I4").Quantity);
    }

    [Fact]
    public void UnknownSourceIsListedWithoutHarvest()
    {
        var raw = RawMaterials.For(Book(), [("I6", 3)], new Dictionary<string, int>());
        Assert.Null(Assert.Single(raw).Harvest);
    }
}
