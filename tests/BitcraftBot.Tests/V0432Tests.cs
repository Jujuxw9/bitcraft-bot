using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.32 (T-14) : « Aller à cette ressource » / « Récolter jusqu'à épuisement ».
public class V0432Tests
{
    private static ServerPosition At(double east, double north) => new(east * 3, north * 3);

    private static NearbyResource Res(ulong id, double east, double north, string name = "Mature Oak Tree") =>
        new(id, 1, name, 2, "Tree", At(east, north), 0) { Health = 100, MaxHealth = 100 };

    private static PathCheck.Result Flat(double e, double n) => PathCheck.Result.Ok(0);

    // ----- Choix de la ressource et de la case voisine -----

    [Fact]
    public void Ressource_sous_le_clic_trouvee_la_plus_proche()
    {
        var list = new[] { Res(1, 100, 200), Res(2, 101, 200) };
        Assert.Equal(2UL, ResourceApproach.At(list, 101.4, 200)!.EntityId);
        Assert.Null(ResourceApproach.At(list, 110, 200));
    }

    [Fact]
    public void Case_voisine_jamais_sur_la_ressource_et_du_cote_du_personnage()
    {
        var choice = ResourceApproach.Choose(100, 200, 90, 200, Flat);
        Assert.NotNull(choice.Cell);
        var (e, n) = choice.Cell!.Value;
        var d = Math.Sqrt((e - 100) * (e - 100) + (n - 200) * (n - 200));
        Assert.InRange(d, 1.4, 2.9);
        Assert.True(e < 100, $"case E {e} : doit être du côté ouest (personnage à l'ouest)");
    }

    [Fact]
    public void Case_refusee_on_essaie_la_suivante()
    {
        var tried = new List<(double, double)>();
        PathCheck.Result Check(double e, double n)
        {
            tried.Add((e, n));
            return tried.Count < 3 ? new PathCheck.Result(false, "escalade") : PathCheck.Result.Ok(0);
        }
        var choice = ResourceApproach.Choose(100, 200, 90, 200, Check);
        Assert.Equal(3, tried.Count);
        Assert.Equal(tried[2], choice.Cell);
    }

    [Fact]
    public void Toutes_les_cases_refusees_donne_la_raison()
    {
        var choice = ResourceApproach.Choose(100, 200, 90, 200, (_, _) => new PathCheck.Result(false, "eau profonde (nage)"));
        Assert.Null(choice.Cell);
        Assert.Contains("eau profonde", choice.Detail);
    }

    [Fact]
    public void Deja_a_cote_pas_de_trajet()
    {
        var choice = ResourceApproach.Choose(100, 200, 101, 201, Flat);
        Assert.Null(choice.Cell);
        Assert.Equal("déjà à côté de la ressource", choice.Detail);
    }

    [Fact]
    public void Trop_loin_refuse_par_la_verification_du_trajet()
    {
        var choice = ResourceApproach.Choose(100, 200, 50, 200, (e, n) => PathCheck.Check(50, 200, e, n, (_, _) => ((short)0, (short)0)));
        Assert.Null(choice.Cell);
    }

    // ----- Suivi jusqu'à épuisement -----

    private static readonly DateTimeOffset T0 = new(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);

    private static HarvestWatchInput In(int seconds, IReadOnlyList<NearbyResource> res, ulong? extract = null, ulong? action = null, bool attacked = false,
        double east = 98, double north = 200) =>
        new() { Now = T0.AddSeconds(seconds), Position = At(east, north), Resources = res, LastExtractTarget = extract, ActionTarget = action, UnderAttack = attacked };

    [Fact]
    public void Premier_coup_puis_epuisement()
    {
        var tree = Res(7, 100, 200);
        var w = new HarvestWatch(7, tree.Name, T0);
        Assert.Null(w.Update(In(1, [tree])));
        Assert.Equal(HarvestWatchState.AttenteClic, w.State);
        Assert.Contains("Premier coup", w.Update(In(5, [tree], action: 7)));
        Assert.Equal(HarvestWatchState.Recolte, w.State);
        Assert.Contains("épuisée", w.Update(In(60, [])));
        Assert.Equal(HarvestWatchState.Finie, w.State);
        Assert.Null(w.Update(In(61, [])));
    }

    [Fact]
    public void Ancien_coup_sur_un_autre_arbre_ne_compte_pas()
    {
        var tree = Res(7, 100, 200);
        var other = Res(8, 96, 200);
        var w = new HarvestWatch(7, tree.Name, T0);
        Assert.Null(w.Update(In(1, [tree, other], extract: 8)));
        Assert.Equal(HarvestWatchState.AttenteClic, w.State);
        Assert.Contains("Premier coup", w.Update(In(3, [tree, other], extract: 7)));
    }

    [Fact]
    public void Autre_ressource_recoltee_abandon()
    {
        var tree = Res(7, 100, 200);
        var other = Res(8, 96, 200);
        var w = new HarvestWatch(7, tree.Name, T0);
        w.Update(In(1, [tree, other]));
        Assert.Contains("autre ressource", w.Update(In(2, [tree, other], action: 8)));
        Assert.Equal(HarvestWatchState.Abandon, w.State);
    }

    [Fact]
    public void Pas_de_clic_en_3_minutes_abandon()
    {
        var tree = Res(7, 100, 200);
        var w = new HarvestWatch(7, tree.Name, T0);
        Assert.Null(w.Update(In(100, [tree])));
        Assert.Contains("aucun coup", w.Update(In(200, [tree])));
    }

    [Fact]
    public void Vie_qui_baisse_arret()
    {
        var tree = Res(7, 100, 200);
        var w = new HarvestWatch(7, tree.Name, T0);
        w.Update(In(1, [tree], action: 7));
        Assert.Contains("vie qui baisse", w.Update(In(2, [tree], attacked: true)));
        Assert.Equal(HarvestWatchState.Abandon, w.State);
    }

    [Fact]
    public void Eloigne_abandon_et_ressource_hors_zone_pas_epuisee()
    {
        var tree = Res(7, 100, 200);
        var w = new HarvestWatch(7, tree.Name, T0);
        w.Update(In(1, [tree], action: 7));
        // Ressource absente mais personnage loin (hors de la zone lue) : on ne conclut pas.
        Assert.Null(w.Update(In(2, [], east: 130)));
        Assert.Equal(HarvestWatchState.Recolte, w.State);
        Assert.Contains("éloigné", w.Update(In(3, [tree], east: 110)));
    }
}
