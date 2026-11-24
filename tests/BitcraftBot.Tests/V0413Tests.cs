using System.Text.Json;
using BitcraftBot.GameData;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.13 (session tâches) : colonies lues sur le serveur, conversions d'objets dans les recettes.
public class V0413Tests
{
    [Fact]
    public void ReadsClaimsWithNamePositionAndNeutral()
    {
        var cache = new TableCache();
        cache.Apply(new TableChange("claim_state",
        [
            Row.Parse("{\"entity_id\":5,\"owner_player_entity_id\":77,\"owner_building_entity_id\":9,\"name\":\"Keldan Nord\",\"neutral\":false}", null),
            Row.Parse("{\"entity_id\":6,\"owner_player_entity_id\":0,\"owner_building_entity_id\":10,\"name\":\"Ruines\",\"neutral\":true}", null),
        ], []));
        cache.Apply(new TableChange("claim_local_state",
        [
            Row.Parse("{\"entity_id\":5,\"supplies\":120,\"num_tiles\":40,\"location\":{\"some\":{\"x\":100,\"z\":200,\"dimension\":1}}}", null),
        ], []));
        var claims = Claims.Of(cache);
        Assert.Equal(2, claims.Count);
        var mine = claims.Single(c => c.Name == "Keldan Nord");
        Assert.Equal(40, mine.Tiles);
        Assert.Equal(120, mine.Supplies);
        Assert.Equal(new ServerPosition(100, 200), mine.Position);
        Assert.True(claims.Single(c => c.Name == "Ruines").Neutral);
        Assert.Null(claims.Single(c => c.Name == "Ruines").Position);
    }

    [Theory]
    [InlineData("[0,{\"x\":3,\"z\":4,\"dimension\":1}]")]
    [InlineData("{\"x\":3,\"z\":4}")]
    [InlineData("[3,4,1]")]
    public void ClaimPositionAcceptsSeveralFormats(string json)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(new ServerPosition(3, 4), Claims.Position(doc.RootElement));
    }

    [Fact]
    public void ClaimQueriesAreSentWithExtras()
    {
        Assert.Contains("SELECT * FROM claim_state", Queries.FollowPlayer("ab", 77, null));
        Assert.DoesNotContain("SELECT * FROM claim_state", Queries.FollowPlayer("ab", 77, null, withExtras: false));
    }

    // Vraies données du jeu : une conversion (remplir un seau d'eau) est une façon d'obtenir l'objet ; pas de « Resolve Item ».
    [Fact]
    public void Vraies_donnees_conversions()
    {
        var folder = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "artifacts", "release", "donnees-jeu");
        if (!RecipeBook.CanLoad(folder))
        {
            return;
        }
        var book = RecipeBook.Load(folder);
        Assert.DoesNotContain(book.Recipes, r => r.Name.StartsWith("Resolve Item", StringComparison.OrdinalIgnoreCase));
        var water = book.Entries.Values.First(e => e.Id == 11007 && !e.IsCargo);
        Assert.Contains(book.Producing(water.Key), r => r.Name == "Fill Bucket Of Water");
    }
}
