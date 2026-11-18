using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.3.12 : familles de ressources et cible à l'écran stable (cas vus à l'essai v0.3.11).
public class H2TargetTests
{
    private static NearbyResource Tree(ulong id, string name, int health, int max, double distance) =>
        new(id, 1, name, 1, "Tree", new ServerPosition(0, 0), distance * 3) { Health = health, MaxHealth = max, ScreenDistance = distance };

    [Theory]
    [InlineData("Tree", ResourceFamilies.Trees)]
    [InlineData("Sapling", ResourceFamilies.Trees)]
    [InlineData("Rock Boulder", ResourceFamilies.Rocks)]
    [InlineData("Ore Vein", ResourceFamilies.Ores)]
    [InlineData("Flower", ResourceFamilies.Flowers)]
    [InlineData("Fiber Plant", ResourceFamilies.Fibers)]
    [InlineData("Bountiful Berry Bush", ResourceFamilies.Berries)]
    [InlineData("Wild Grain", ResourceFamilies.Crops)]
    [InlineData("Clay", ResourceFamilies.Soils)]
    [InlineData("Lake Fish School", ResourceFamilies.Fishing)]
    [InlineData("Obstacle", ResourceFamilies.Obstacles)]
    [InlineData("Research", ResourceFamilies.Others)]
    [InlineData(null, ResourceFamilies.Others)]
    public void Familles_des_categories_du_jeu(string? tag, string family) => Assert.Equal(family, ResourceFamilies.Of(tag));

    [Fact]
    public void Arbres_intacts_identiques_donnent_probable()
    {
        var m = new ScreenTargetMatcher();
        var near = Tree(1, "Young Beech Tree", 3000, 3000, 0.9);
        var far = Tree(2, "Young Beech Tree", 3000, 3000, 3.0);
        var result = m.Match("Young Beech Tree", 3000, [near, far])!;
        Assert.Equal(1UL, result.Resource.EntityId);
        Assert.False(result.Sure);
    }

    [Fact]
    public void Arbre_entame_devient_sur_et_le_reste_malgre_le_retard_de_l_ecran()
    {
        var m = new ScreenTargetMatcher();
        // Le joueur coupe l'arbre le plus lointain : sa santé baisse, il devient le seul à coller.
        var near = Tree(1, "Young Beech Tree", 3000, 3000, 0.9);
        var cut = Tree(2, "Young Beech Tree", 2824, 3000, 3.0);
        // L'écran affiche encore 2835 (retard) : avant, on passait à « probable ».
        var result = m.Match("Young Beech Tree", 2835, [near, cut])!;
        Assert.Equal(2UL, result.Resource.EntityId);
        Assert.True(result.Sure);
        // Puis l'écran affiche exactement la même valeur que le serveur.
        result = m.Match("Young Beech Tree", 2824, [near, cut])!;
        Assert.Equal(2UL, result.Resource.EntityId);
        Assert.True(result.Sure);
    }

    [Fact]
    public void Arbre_lointain_avec_la_meme_sante_ignore()
    {
        var m = new ScreenTargetMatcher();
        var cut = Tree(1, "Young Beech Tree", 2967, 3000, 0.9);
        var far = Tree(2, "Young Beech Tree", 2967, 3000, ScreenTargetMatcher.MaxDistance + 1);
        Assert.Equal(1UL, m.Match("Young Beech Tree", 2967, [cut, far])!.Resource.EntityId);
    }

    [Fact]
    public void Arbre_intact_voisin_ne_vole_pas_la_cible()
    {
        var m = new ScreenTargetMatcher();
        // Serveur en avance : 2989 ; voisin intact à 3000. L'écran lit 2989 : seul l'arbre coupé colle.
        var cut = Tree(1, "Young Beech Tree", 2989, 3000, 2.0);
        var intact = Tree(2, "Young Beech Tree", 3000, 3000, 0.5);
        var result = m.Match("Young Beech Tree", 2989, [intact, cut])!;
        Assert.Equal(1UL, result.Resource.EntityId);
        Assert.True(result.Sure);
    }

    [Fact]
    public void Sante_illisible_garde_la_cible_precedente()
    {
        var m = new ScreenTargetMatcher();
        var cut = Tree(1, "Mature Oak Tree", 11854, 12000, 3.5);
        var other = Tree(2, "Mature Oak Tree", 12000, 12000, 1.0);
        Assert.True(m.Match("Mature Oak Tree", 11854, [other, cut])!.Sure);
        var result = m.Match("Mature Oak Tree", null, [other, cut])!;
        Assert.Equal(1UL, result.Resource.EntityId);
        Assert.True(result.Sure);
    }

    [Fact]
    public void Sans_cible_ou_sans_candidat_rien()
    {
        var m = new ScreenTargetMatcher();
        Assert.Null(m.Match(null, 100, [Tree(1, "Young Oak Tree", 3000, 3000, 1)]));
        Assert.Null(m.Match("Mature Oak Tree", 100, [Tree(1, "Young Oak Tree", 3000, 3000, 1)]));
    }
}
