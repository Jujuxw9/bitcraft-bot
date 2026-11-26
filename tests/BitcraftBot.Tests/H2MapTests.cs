using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;
using Xunit;
using static BitcraftBot.Tests.H1ServerTests;

namespace BitcraftBot.Tests;

// H2 : coordonnées de l'écran, santé, âge et direction des arbres.
public class H2MapTests
{
    [Theory]
    // Valeurs réelles relevées à l'arrêt pendant l'essai v0.3.9 (serveur → écran).
    [InlineData(20099.0, 26716.0, 8905, 6700)]
    [InlineData(20095.8, 26716.0, 8905, 6699)]
    [InlineData(20121.8, 26708.9, 8903, 6707)]
    public void Position_serveur_donne_les_coordonnees_de_l_ecran(double x, double z, int north, int east)
    {
        var p = new ServerPosition(x, z);
        Assert.Equal(north, p.RoundedNorth);
        Assert.Equal(east, p.RoundedEast);
        Assert.Equal($"N {north}, E {east}", p.ScreenText);
    }

    [Fact]
    public void Distance_en_cases_de_l_ecran()
    {
        var a = new ServerPosition(300, 300);
        Assert.Equal(5, a.ScreenDistanceTo(new ServerPosition(309, 312)), 3); // 3 E, 4 N
    }

    [Theory]
    [InlineData(0, 2, "N")]
    [InlineData(2, 2, "NE")]
    [InlineData(2, 0, "E")]
    [InlineData(1, -1, "SE")]
    [InlineData(0, -3, "S")]
    [InlineData(-1, -1, "SO")]
    [InlineData(-4, 0.5, "O")]
    [InlineData(-1, 1, "NO")]
    [InlineData(0.1, -0.1, "ici")]
    public void Direction_a_huit_points(double dEast, double dNorth, string expected) =>
        Assert.Equal(expected, ResourceNames.Direction(dEast, dNorth));

    [Theory]
    [InlineData("Mature Oak Tree", "Mûr")]
    [InlineData("Young Beech Tree", "Jeune")]
    [InlineData("Dead Tree", "Mort")]
    [InlineData("Planted Birch Tree", "Planté")]
    [InlineData("Fully Grown Oak Tree", "Planté, adulte")]
    [InlineData("Large Tree Stump", "Souche")]
    [InlineData("Daisy", "—")]
    public void Age_des_arbres(string name, string age) => Assert.Equal(age, ResourceNames.Age(name));

    [Fact]
    public void Arbre_interdit_jamais_vise()
    {
        var maker = new NearbyResource(1, 1, "Mature Maker's Tree", 1, "Tree", new ServerPosition(0, 0), 1);
        var oak = new NearbyResource(2, 21, "Mature Oak Tree", 1, "Tree", new ServerPosition(0, 0), 1);
        var young = new NearbyResource(3, 14, "Young Oak Tree", 1, "Tree", new ServerPosition(0, 0), 1);
        Assert.False(ResourceNames.IsHarvestTarget(maker));
        Assert.True(ResourceNames.IsHarvestTarget(oak));
        Assert.False(ResourceNames.IsHarvestTarget(young));
    }

    [Fact]
    public void Sante_des_arbres_lue_sur_le_serveur()
    {
        var schema = Schema.Parse(SchemaJson);
        var cache = new TableCache();
        // Personnage en x 300, z 300 (petites cases) = N 100, E 100 à l'écran.
        var initial = Initial(
            Table("user_state", Rows($$"""[{"__identity__":"0x{{Identity}}"}, 7, true]""")),
            Table("mobile_entity_state", Rows("[7, 0, 0, 300000, 300000, 300000, 300000, 0, false]")),
            Table("location_state", Rows("[501, 0, 306, 306, 0]", "[502, 0, 300, 291, 0]")),
            Table("resource_state", Rows("[501, 10, 0]", "[502, 11, 0]")),
            Table("resource_health_state", Rows("[501, 1417]")));
        cache.Reset(Assert.IsType<SubscriptionEvent>(ServerMessages.Parse(initial, schema)).Tables);

        var resources = SnapshotBuilder.Build(cache, Identity, Describe).Resources;
        var mature = resources.Single(r => r.EntityId == 501);
        Assert.Equal(1417, mature.Health);
        Assert.Equal(12000, mature.MaxHealth);
        Assert.Equal("Mûr", mature.Age);
        Assert.Equal("NE", mature.Direction);
        Assert.Equal(Math.Sqrt(8), mature.ScreenDistance, 3);
        Assert.Equal("N 102, E 102", mature.Position.ScreenText);

        var young = resources.Single(r => r.EntityId == 502);
        Assert.Null(young.Health); // pas de ligne de santé reçue
        Assert.Equal("S", young.Direction);
        Assert.Equal(3, young.ScreenDistance, 3);
    }

    [Fact]
    public void Requete_de_sante_seulement_en_lecture_et_retirable()
    {
        var area = Area.Around(100, 100, 40);
        var with = Queries.Resources(area);
        Assert.Contains(with, q => q.StartsWith("SELECT h.* FROM resource_health_state h JOIN location_state l ON h.entity_id = l.entity_id WHERE", StringComparison.Ordinal));
        Assert.All(with, q => Assert.StartsWith("SELECT ", q));
        Assert.DoesNotContain(Queries.Resources(area, withHealth: false), q => q.Contains("resource_health_state", StringComparison.Ordinal));
        // v0.4.19 : une requête par morceau de carte touché (ici 2 × 2 morceaux).
        Assert.Equal(8, Queries.Resources(area, withHealth: false).Count);
    }

    // v0.4.19 : le serveur refuse un carré de coordonnées seul ; chaque requête de zone porte le numéro du morceau.
    [Fact]
    public void Requetes_de_zone_par_morceau_de_carte()
    {
        // Zone de l'essai du 26/11 au soir (N 9155, E 6903) : 4 morceaux, celui du personnage (286216) compris.
        var area = new Area(20649, 20770, 27406, 27527);
        Assert.Equal(new ulong[] { 285216, 285217, 286216, 286217 }, area.ChunkIndexes());
        var queries = Queries.FollowPlayer("abcd", 7, area, withTerrain: true);
        Assert.All(queries.Where(q => q.Contains("JOIN location_state", StringComparison.Ordinal)),
            q => Assert.Contains("l.chunk_index = ", q));
        Assert.Contains("SELECT r.* FROM resource_state r JOIN location_state l ON r.entity_id = l.entity_id WHERE l.chunk_index = 286216 AND l.x >= 20649 AND l.x <= 20770 AND l.z >= 27406 AND l.z <= 27527", queries);
        // Sacs au sol retirables seuls, sans toucher au chat ni à la barre d'action.
        var noBags = Queries.FollowPlayer("abcd", 7, area, withBags: false);
        Assert.DoesNotContain(noBags, q => q.Contains("dropped_inventory", StringComparison.Ordinal));
        Assert.Contains(noBags, q => q.Contains("action_bar_state", StringComparison.Ordinal));
        Assert.Contains(noBags, q => q.Contains("chat_message_state", StringComparison.Ordinal));
    }
}
