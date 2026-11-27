using BitcraftBot.GameData;
using BitcraftBot.Input.Actions;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.17 (D-069) : capacités automatiques de la barre d'action pendant les récoltes de Julien.
public class V0417Tests
{
    private const int Forestry = 2;
    private const int Mining = 5;
    private const int Hewing = 423294019;
    private const int HewingBuff = 205409142;
    private const int Heavy = 933265452;
    private const int Sprint = 594765058;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static readonly Dictionary<int, AbilityDef> Book = new()
    {
        [Hewing] = new AbilityDef(Hewing, "Hewing Strikes", 0, 60, [HewingBuff], 20, Forestry, "Forestry", "vitesse en forestry +10 %"),
        [Heavy] = new AbilityDef(Heavy, "Heavy Swings", 25, 120, [537057824], 20, Mining, "Mining", "mining power +8"),
        [Sprint] = new AbilityDef(Sprint, "Sprint", 50, 120, [1845851314], 15, null, null, "vitesse de marche +40 %"),
    };

    // Barre de Julien (journal du 25/11) : 4 Sprint, 7 Hewing Strikes, 8 Heavy Swings.
    private static List<ActionBarSlot> Bar(long hewingCooldownStartMs = 0) =>
    [
        new(0, 3, new AbilityInfo(1, AbilityKind.Speciale, Sprint, 0, 120)),
        new(0, 6, new AbilityInfo(2, AbilityKind.Speciale, Hewing, hewingCooldownStartMs, 60)),
        new(0, 7, new AbilityInfo(3, AbilityKind.Speciale, Heavy, 0, 120)),
        new(0, 0, new AbilityInfo(4, AbilityKind.AttaqueAuto, 0, 0, 0)),
    ];

    private static AbilitySituation Situation(int? skill = Forestry, string? action = "Extract") => new()
    {
        Enabled = true,
        Allowed = new HashSet<int> { Hewing, Heavy, Sprint },
        ActionType = action,
        ActionSkill = skill,
        Bar = Bar(),
        Buffs = [],
        Stamina = 80,
        MaxStamina = 100,
        Now = Now,
    };

    [Fact]
    public void Coupe_de_bois_lance_Hewing_Strikes_touche_7()
    {
        var d = AbilityPlanner.Decide(Situation(), Book);
        Assert.NotNull(d.Press);
        Assert.Equal("Hewing Strikes", d.Press!.Def.Name);
        Assert.Equal("7", d.Press.Slot.Key);
        Assert.Equal((ushort)0x08, GameKeys.ForSlot(d.Press.Slot.Slot));
        Assert.Equal("coupe de bois", AbilityPlanner.Activity(d.Press.Def.SkillName));
    }

    [Fact]
    public void Interrupteur_general_coupe_rien()
    {
        var d = AbilityPlanner.Decide(Situation() with { Enabled = false }, Book);
        Assert.Null(d.Press);
        Assert.Equal("coupé", d.Reason);
        Assert.Equal(3, d.Slots.Count); // la liste reste affichée
    }

    [Fact]
    public void Capacite_coupee_par_Julien()
    {
        var d = AbilityPlanner.Decide(Situation() with { Allowed = new HashSet<int> { Heavy } }, Book);
        Assert.Null(d.Press);
        Assert.Equal("coupée", d.Slots.Single(x => x.Def.Id == Hewing).Text);
    }

    [Fact]
    public void Metier_qui_ne_correspond_pas()
    {
        // Minage : Heavy Swings et pas Hewing Strikes.
        var d = AbilityPlanner.Decide(Situation(Mining), Book);
        Assert.Equal("Heavy Swings", d.Press!.Def.Name);
        Assert.Equal("pour Forestry", d.Slots.Single(x => x.Def.Id == Hewing).Text);
        // Pêche : rien.
        Assert.Null(AbilityPlanner.Decide(Situation(12), Book).Press);
    }

    [Fact]
    public void Rien_sans_recolte_ni_metier_connu()
    {
        Assert.Null(AbilityPlanner.Decide(Situation(action: null, skill: null), Book).Press);
        Assert.Null(AbilityPlanner.Decide(Situation(action: "Craft"), Book).Press);
        var unknown = AbilityPlanner.Decide(Situation(skill: null), Book);
        Assert.Null(unknown.Press);
        Assert.Equal("récolte en cours, métier inconnu", unknown.Reason);
    }

    [Fact]
    public void Recharge_pas_finie()
    {
        // Lancée il y a 20 s, recharge 60 s : encore 40 s.
        var s = Situation() with { Bar = Bar(Now.AddSeconds(-20).ToUnixTimeMilliseconds()) };
        var d = AbilityPlanner.Decide(s, Book);
        Assert.Null(d.Press);
        Assert.Equal("recharge 40 s", d.Slots.Single(x => x.Def.Id == Hewing).Text);
        // Recharge finie (lancée il y a 61 s) : repart.
        Assert.NotNull(AbilityPlanner.Decide(Situation() with { Bar = Bar(Now.AddSeconds(-61).ToUnixTimeMilliseconds()) }, Book).Press);
    }

    [Fact]
    public void Bonus_encore_actif_pas_de_relance()
    {
        var s = Situation() with { Buffs = [new ActiveBuffInfo(HewingBuff, Now.ToUnixTimeSeconds() - 5, 20)] };
        var d = AbilityPlanner.Decide(s, Book);
        Assert.Null(d.Press);
        Assert.Equal("bonus actif (15 s)", d.Slots.Single(x => x.Def.Id == Hewing).Text);
        // Bonus fini : repart.
        var over = Situation() with { Buffs = [new ActiveBuffInfo(HewingBuff, Now.ToUnixTimeSeconds() - 25, 20)] };
        Assert.NotNull(AbilityPlanner.Decide(over, Book).Press);
    }

    [Fact]
    public void Reserve_d_endurance_30_pourcent()
    {
        // Heavy Swings coûte 25 : 54 - 25 = 29 < 30 → non ; 55 - 25 = 30 → oui.
        Assert.Null(AbilityPlanner.Decide(Situation(Mining) with { Stamina = 54 }, Book).Press);
        Assert.Equal("endurance (réserve 30 %)", AbilityPlanner.Decide(Situation(Mining) with { Stamina = 54 }, Book).Slots.Single(x => x.Def.Id == Heavy).Text);
        Assert.NotNull(AbilityPlanner.Decide(Situation(Mining) with { Stamina = 55 }, Book).Press);
        // Endurance inconnue : prudence.
        Assert.Null(AbilityPlanner.Decide(Situation(Mining) with { MaxStamina = null }, Book).Press);
        // Hewing Strikes ne coûte rien : part même à 5 d'endurance.
        Assert.NotNull(AbilityPlanner.Decide(Situation() with { Stamina = 5 }, Book).Press);
    }

    [Fact]
    public void Une_touche_toutes_les_2_secondes_au_plus()
    {
        Assert.Null(AbilityPlanner.Decide(Situation() with { LastPress = Now.AddSeconds(-1) }, Book).Press);
        Assert.NotNull(AbilityPlanner.Decide(Situation() with { LastPress = Now.AddSeconds(-2) }, Book).Press);
    }

    [Fact]
    public void Capacite_en_pause_apres_echecs()
    {
        var blocked = new Dictionary<int, DateTimeOffset> { [Hewing] = Now.AddMinutes(3) };
        Assert.Null(AbilityPlanner.Decide(Situation() with { BlockedUntil = blocked }, Book).Press);
        var past = new Dictionary<int, DateTimeOffset> { [Hewing] = Now.AddSeconds(-1) };
        Assert.NotNull(AbilityPlanner.Decide(Situation() with { BlockedUntil = past }, Book).Press);
    }

    [Fact]
    public void Sprint_jamais_lance()
    {
        var d = AbilityPlanner.Decide(Situation(), Book);
        Assert.Equal("pas liée à une récolte", d.Slots.Single(x => x.Def.Id == Sprint).Text);
        Assert.DoesNotContain(d.Slots, x => x.Def.Id == Sprint && x.Ready);
    }

    [Fact]
    public void Barre_ou_bonus_pas_lus()
    {
        Assert.Equal("barre d'action pas lue", AbilityPlanner.Decide(Situation() with { Bar = null }, Book).Reason);
        Assert.Null(AbilityPlanner.Decide(Situation() with { Buffs = null }, Book).Press);
        // Autre barre que la première : ignorée.
        var other = new List<ActionBarSlot> { new(1, 6, new AbilityInfo(2, AbilityKind.Speciale, Hewing, 0, 60)) };
        Assert.Null(AbilityPlanner.Decide(Situation() with { Bar = other }, Book).Press);
    }

    [Fact]
    public void Touches_de_la_barre_autorisees()
    {
        Assert.Equal((ushort)0x02, GameKeys.ForSlot(0));
        Assert.Equal((ushort)0x0B, GameKeys.ForSlot(9));
        Assert.Equal((ushort)0x0D, GameKeys.ForSlot(11));
        Assert.Null(GameKeys.ForSlot(12));
        Assert.Equal("7", GameKeys.Name(0x08));
        Assert.All(GameKeys.ActionBarKeys, k => Assert.Equal(!GameKeys.Forbidden.Contains(k), GameKeys.Allowed.ContainsKey(k)));
        Assert.False(GameKeys.Allowed.ContainsKey(GameKeys.Enter));
    }

    [Fact]
    public void Metier_deduit_des_effets()
    {
        var professions = new Dictionary<int, string> { [2] = "Forestry", [5] = "Mining", [12] = "Fishing" };
        Assert.Equal(2, AbilityBook.SkillFromStats([new BuffStat("ForestrySpeed", 0.1, true)], professions));
        Assert.Equal(5, AbilityBook.SkillFromStats([new BuffStat("MiningPower", 8, false)], professions));
        // Let It Run : effets négatifs ignorés, la pêche gagne.
        Assert.Equal(12, AbilityBook.SkillFromStats([new BuffStat("GatheringSpeed", -0.15, true), new BuffStat("FishingPower", 2, false)], professions));
        Assert.Null(AbilityBook.SkillFromStats([new BuffStat("MovementMultiplier", 0.4, true)], professions));
    }

    [Fact]
    public void Lecture_des_donnees_du_jeu()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-capacites-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "skill_desc.json"),
                """[{"id":1,"name":"ANY","skill_category":"None"},{"id":2,"name":"Forestry","skill_category":"Profession"}]""");
            File.WriteAllText(Path.Combine(folder, "buff_desc.json"),
                """[{"id":205409142,"duration":20,"stats":[{"id":"ForestrySpeed","value":0.1,"is_pct":true}]},{"id":1845851314,"duration":15,"stats":[{"id":"MovementMultiplier","value":0.4,"is_pct":true}]},{"id":492653012,"duration":1,"stats":[{"id":"ForestrySpeed","value":0.01,"is_pct":true}]}]""");
            File.WriteAllText(Path.Combine(folder, "ability_custom_desc.json"),
                """[{"id":423294019,"ability_name":"Hewing Strikes","stamina_cost":0,"cooldown":60.0,"buffs":[{"buff_id":205409142,"duration":null}],"profession_hit_skill_id":1},{"id":594765058,"ability_name":"Sprint","stamina_cost":50,"cooldown":120.0,"buffs":[{"buff_id":1845851314,"duration":15}]},{"id":1052553567,"ability_name":"Break the Roots","stamina_cost":10,"cooldown":1.0,"buffs":[{"buff_id":492653012}]}]""");
            Assert.True(AbilityBook.CanLoad(folder));
            var book = AbilityBook.Load(folder);
            var hewing = book.Abilities[423294019];
            Assert.Equal(2, hewing.SkillId);
            Assert.Equal("Forestry", hewing.SkillName);
            Assert.Equal(20, hewing.BuffSeconds);
            Assert.True(hewing.DefaultOn);
            Assert.Null(book.Abilities[594765058].SkillId);
            Assert.False(book.Abilities[594765058].DefaultOn);
            // Break the Roots : recharge 1 s, coupée par défaut.
            Assert.Equal(2, book.Abilities[1052553567].SkillId);
            Assert.False(book.Abilities[1052553567].DefaultOn);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    // Vraies données (dossier de la version publiée), si elles sont là.
    [Fact]
    public void Vraies_donnees_metiers_des_capacites()
    {
        var folder = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "artifacts", "release", "donnees-jeu");
        if (!AbilityBook.CanLoad(folder))
        {
            return;
        }
        var book = AbilityBook.Load(folder).Abilities.Values.ToDictionary(a => a.Name);
        Assert.Equal("Forestry", book["Hewing Strikes"].SkillName);
        Assert.Equal("Mining", book["Heavy Swings"].SkillName);
        Assert.Equal("Fishing", book["Reel In"].SkillName);
        Assert.Equal("Fishing", book["Let It Run"].SkillName);
        Assert.Null(book["Sprint"].SkillName);
        Assert.True(book["Hewing Strikes"].DefaultOn);
        Assert.True(book["Heavy Swings"].DefaultOn);
        Assert.False(book["Break the Roots"].DefaultOn);
    }
}
