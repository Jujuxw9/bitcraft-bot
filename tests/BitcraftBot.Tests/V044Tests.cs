using BitcraftBot.Perception.Screen;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.4 : barre d'action, capacités et sprint, sacs au sol (lecture seule du serveur), « en jeu » par la mini-carte.
public class V044Tests
{
    private const ulong Me = 648518346637266951;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static TableCache Cache(params (string Table, string Json)[] rows)
    {
        var cache = new TableCache();
        foreach (var g in rows.GroupBy(r => r.Table))
        {
            cache.Apply(new TableChange(g.Key, g.Select(r => Row.Parse(r.Json, null)).ToList(), []));
        }
        return cache;
    }

    private static string Ability(ulong id, string ability, long startMs, double cooldown) =>
        $"{{\"entity_id\":{id},\"owner_entity_id\":{Me},\"ability\":{ability},\"cooldown\":[{startMs},{cooldown.ToString(System.Globalization.CultureInfo.InvariantCulture)}]}}";

    [Fact]
    public void Barre_d_action_lue_avec_ses_capacites()
    {
        var nowMs = Now.ToUnixTimeMilliseconds();
        var cache = Cache(
            ("ability_state", Ability(11, "[4,594765058]", nowMs - 30_000, 120)),       // Sprint lancé il y a 30 s
            ("ability_state", Ability(12, "[1,1001]", 0, 0)),                            // manger
            ("ability_state", Ability(13, "{\"AutoAttack\":[]}", 0, 0)),                 // forme objet
            ("action_bar_state", $"{{\"entity_id\":1,\"player_entity_id\":{Me},\"action_bar_index\":0,\"local_ability_index\":3,\"ability_entity_id\":11}}"),
            ("action_bar_state", $"{{\"entity_id\":2,\"player_entity_id\":{Me},\"action_bar_index\":0,\"local_ability_index\":4,\"ability_entity_id\":12}}"),
            ("action_bar_state", $"{{\"entity_id\":3,\"player_entity_id\":{Me},\"action_bar_index\":0,\"local_ability_index\":0,\"ability_entity_id\":13}}"));
        var bar = ActionBars.Of(cache, Me)!;
        Assert.Equal(3, bar.Count);
        Assert.Equal("1", bar[0].Key);
        Assert.Equal(AbilityKind.AttaqueAuto, bar[0].Ability!.Kind);
        Assert.Equal("4", bar[1].Key);
        Assert.Equal(AbilityKind.Speciale, bar[1].Ability!.Kind);
        Assert.Equal(ActionBars.SprintAbilityId, bar[1].Ability!.Value);
        Assert.Equal(90, bar[1].Ability!.RemainingSeconds(Now), 1);
        Assert.Equal(AbilityKind.Manger, bar[2].Ability!.Kind);
        Assert.Equal(0, bar[2].Ability!.RemainingSeconds(Now));
    }

    [Fact]
    public void Barre_pas_lue_si_table_absente() => Assert.Null(ActionBars.Of(new TableCache(), Me));

    [Fact]
    public void Etats_du_sprint()
    {
        var nowMs = Now.ToUnixTimeMilliseconds();
        var slot = new ActionBarSlot(0, 3, new AbilityInfo(11, AbilityKind.Speciale, ActionBars.SprintAbilityId, 0, 120));
        var ready = SprintStatus.Of([slot], [slot.Ability!], [], Now);
        Assert.True(ready.Ready);
        Assert.Equal("prêt", ready.French);
        Assert.Equal("4", ready.Slot!.Key);

        var used = new AbilityInfo(11, AbilityKind.Speciale, ActionBars.SprintAbilityId, nowMs - 5_000, 120);
        var buff = new ActiveBuffInfo(ActionBars.SprintBuffId, Now.ToUnixTimeSeconds() - 5, 15);
        var active = SprintStatus.Of([slot with { Ability = used }], [used], [buff], Now);
        Assert.True(active.Active);
        Assert.Equal("actif (10 s)", active.French);

        var cooling = SprintStatus.Of([slot with { Ability = used }], [used], [], Now);
        Assert.False(cooling.Ready);
        Assert.StartsWith("en recharge (115", cooling.French);

        Assert.False(SprintStatus.Of([], [], [], Now).AbilityKnown);
    }

    [Theory]
    [InlineData(0, "1")]
    [InlineData(3, "4")]
    [InlineData(9, "0")]
    [InlineData(10, "-")]
    [InlineData(11, "=")]
    public void Touche_de_chaque_case(int slot, string key) => Assert.Equal(key, new ActionBarSlot(0, slot, null).Key);

    [Fact]
    public void Sacs_au_sol_proprietaire_et_contenu()
    {
        var micros = (Now.ToUnixTimeMilliseconds() + 300_000) * 1000; // dans 5 min
        var cache = Cache(
            ("dropped_inventory_state", $"{{\"entity_id\":501,\"owner_entity_id\":{Me},\"active_timer_id\":7}}"),
            ("dropped_inventory_state", "{\"entity_id\":502,\"owner_entity_id\":999,\"active_timer_id\":8}"),
            ("dropped_inventory_state", "{\"entity_id\":503,\"owner_entity_id\":0,\"active_timer_id\":9}"),
            ("location_state", "{\"entity_id\":501,\"x\":30,\"z\":30}"),
            ("location_state", "{\"entity_id\":502,\"x\":60,\"z\":30}"),
            ("location_state", "{\"entity_id\":503,\"x\":33,\"z\":36}"),
            ("inventory_state", "{\"entity_id\":601,\"owner_entity_id\":501,\"player_owner_entity_id\":0,\"pockets\":[[6000,[0,[20,3,[1,[]],[1,[]]]],false],[6000,[1,[]],false]]}"),
            ("dropped_inventory_ownership_timer", $"{{\"scheduled_id\":7,\"scheduled_at\":[1,{{\"__timestamp_micros_since_unix_epoch__\":{micros}}}],\"entity_id\":501}}"));
        var bags = GroundBags.Near(cache, Me, new ServerPosition(30, 30));
        Assert.Equal(3, bags.Count);
        Assert.Equal(501ul, bags[0].EntityId);
        Assert.Equal(BagOwner.AMoi, bags[0].Owner);
        Assert.True(bags[0].Mine);
        var stack = Assert.Single(bags[0].Stacks);
        Assert.Equal(20, stack.ItemId);
        Assert.Equal(3, stack.Quantity);
        Assert.Equal(Now.AddMinutes(5).ToUnixTimeSeconds(), bags[0].PublicAt!.Value.ToUnixTimeSeconds());
        Assert.Equal(BagOwner.Libre, bags[1].Owner);
        Assert.Equal(BagOwner.AutreJoueur, bags[2].Owner);
        Assert.False(bags[2].Mine);
        Assert.Equal(10, bags[2].ScreenDistance, 1);
    }

    [Fact]
    public void Requetes_en_lecture_seule()
    {
        var queries = Queries.FollowPlayer("ab", Me, new Area(0, 10, 0, 10));
        Assert.Contains(queries, q => q.Contains("action_bar_state"));
        Assert.Contains(queries, q => q.Contains("dropped_inventory_state"));
        Assert.All(queries, q => Assert.StartsWith("SELECT", q));
        Assert.DoesNotContain(Queries.FollowPlayer("ab", Me, new Area(0, 10, 0, 10), withExtras: false), q => q.Contains("dropped") || q.Contains("action_bar"));
    }

    // Le personnage au centre de la mini-carte suffit à dire « en jeu » ; jamais sur l'accueil, la connexion, le chargement.
    [Theory]
    [InlineData("ecrans", "20261122_reference-ui_ecran-jeu-bandeau_001.png", true)]
    [InlineData("ecrans", "20261122_reference-ui_ecran-jeu-coupe_001.png", true)]
    [InlineData("ecrans", "20261122_reference-ui_ecran-accueil_001.png", false)]
    [InlineData("ecrans", "20261122_reference-ui_ecran-connexion_001.png", false)]
    [InlineData("ecrans", "20261116_reference-ui_ecran-chargement_001.png", false)]
    [InlineData("fenetres", "carte-du-monde_001.png", false)]
    public async Task Personnage_au_centre_de_la_mini_carte(string folder, string file, bool expected)
    {
        var path = Path.GetFullPath(Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", folder, file));
        var frame = await HudReaderReferenceTests.LoadFrameAsync(path);
        Assert.Equal(expected, MinimapCone.PlayerVisible(frame));
    }
}
