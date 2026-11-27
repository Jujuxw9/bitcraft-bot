using BitcraftBot.GameData;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.31 (T-13) : petits défauts. Cible lue à l'écran avec le nouveau panneau sombre du jeu.
public class V0431Tests
{
    private static string Reference(params string[] parts) =>
        Path.GetFullPath(Path.Combine([HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", .. parts]));

    // ----- Panneau de la cible : nouvelle apparence sombre (bande du haut de vraies captures du 23 et 25/11) -----

    [Theory]
    [InlineData("lavande_001.png")]
    [InlineData("cible_002.png")]
    [InlineData("cible_003.png")]
    public async Task Panneau_sombre_reconnu(string file)
    {
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("cible-sombre", file));
        Assert.Equal(TargetPanelStyle.Dark, BarReader.GetTargetPanelStyle(frame));
    }

    [Fact]
    public async Task Menu_plein_ecran_sombre_pas_pris_pour_une_cible()
    {
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("cible-sombre", "menu-sans-cible_001.png"));
        Assert.Equal(TargetPanelStyle.None, BarReader.GetTargetPanelStyle(frame));
    }

    [Fact]
    public async Task Ancien_panneau_creme_toujours_reconnu()
    {
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("20261116_reference-ui_hud-coupe-arbre_001.png"));
        Assert.Equal(TargetPanelStyle.Cream, BarReader.GetTargetPanelStyle(frame));
    }

    [Theory]
    [InlineData("lavande_001.png", "Lavender", "Tier 1 - Flower", 40, 550)]
    [InlineData("cible_003.png", "Rough Hieroglyphs", "Tier 1 - Research", 1700, 1700)]
    public async Task Cible_lue_sur_le_panneau_sombre(string file, string name, string category, int health, int max)
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            // Pas d'OCR anglais sur cette machine : test non applicable.
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("cible-sombre", file));
        var hud = await new HudReader(ocr, KnownNames.Resources).ReadAsync(frame);
        Assert.NotNull(hud.Target);
        Assert.Equal(name, hud.Target!.Name);
        Assert.Equal(category, hud.Target.Category);
        Assert.Equal(health, hud.Target.Health);
        Assert.Equal(max, hud.Target.MaxHealth);
    }

    [Fact]
    public async Task Batiment_cible_nom_et_sante_lus()
    {
        var ocr = OcrReader.TryCreate();
        if (ocr is null)
        {
            return;
        }
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Reference("cible-sombre", "cible_002.png"));
        var hud = await new HudReader(ocr).ReadAsync(frame);
        Assert.NotNull(hud.Target);
        Assert.Equal("Simple Fishing Station", hud.Target!.Name);
        Assert.Equal(120, hud.Target.Health);
        Assert.Equal(120, hud.Target.MaxHealth);
    }

    // ----- Barre de fabrication : va jusqu'au bout -----

    private static readonly Dictionary<int, int> PerItem = new() { [7] = 10, [8] = 5 };

    private static ActionInfo Craft(int recipe) => new("Craft", 0, 1000, 99, recipe);

    [Fact]
    public void Fabrication_courte_finie_montre_100_meme_avec_un_autre_atelier()
    {
        var bar = new CraftBarTracker();
        // Recette 7 (2 objets × 10 gestes) en cours ; recette 8 attend dans un autre atelier.
        Assert.Equal(0.5, bar.Update(Craft(7), [new(7, 10, 2), new(8, 2, 1)], PerItem));
        Assert.Equal(1, bar.Update(Craft(7), [new(7, 20, 2), new(8, 2, 1)], PerItem));
        // Fin : l'action disparaît. Avant : deux fabrications, aucune recette connue → rien (barre « en cours »).
        Assert.Equal(1, bar.Update(null, [new(7, 20, 2), new(8, 2, 1)], PerItem));
    }

    [Fact]
    public void Objets_ramasses_tout_de_suite_la_barre_reste_pleine()
    {
        var bar = new CraftBarTracker();
        Assert.Equal(1, bar.Update(Craft(7), [new(7, 10, 1)], PerItem));
        // Fabrication ramassée : plus de ligne sur le serveur, la dernière part lue reste.
        Assert.Equal(1, bar.Update(null, [], PerItem));
        bar.Reset();
        Assert.Null(bar.Update(null, [], PerItem));
    }

    [Fact]
    public void Meme_recette_dans_deux_ateliers_suit_celle_en_cours()
    {
        var bar = new CraftBarTracker();
        // Une fabrication finie (pas ramassée) et une en cours, même recette : la barre suit celle en cours.
        Assert.Equal(0.3, bar.Update(Craft(7), [new(7, 10, 1), new(7, 3, 1)], PerItem));
    }

    [Fact]
    public void Nouvelle_recette_oublie_l_ancienne_part()
    {
        var bar = new CraftBarTracker();
        Assert.Equal(1, bar.Update(Craft(7), [new(7, 10, 1)], PerItem));
        Assert.Equal(0.2, bar.Update(Craft(8), [new(8, 1, 1)], PerItem));
        Assert.Equal(0.2, bar.Update(null, [], PerItem));
    }

    // ----- Conseil : pêche ou minage sans capacités -----

    private static readonly DateTime T0 = new(2026, 11, 27, 9, 0, 0);
    private const int Mining = 5;
    private const int HeavySwings = 100;

    private static TaskJournal AdviceJournal() => new()
    {
        SkillName = id => id switch { Mining => "Mining", 2 => "Forestry", 6 => "Fishing", _ => $"métier {id}" },
        SkillOf = a => a.Target switch { 50 => Mining, 42 => 2, _ => null },
        SkillAbilities = id => id == Mining ? [(HeavySwings, "Heavy Swings")] : id == 2 ? [(200, "Hewing Strikes")] : [],
    };

    private static long Ms(double seconds) => new DateTimeOffset(T0.AddSeconds(seconds)).ToUnixTimeMilliseconds();

    private static AbilityInfo Ability(int id, long cooldownStartMs) => new(1, AbilityKind.Speciale, id, cooldownStartMs, 30);

    private static TaskInput Mine(double seconds, long heavySwingsCooldownStart, ulong target = 50) =>
        new(T0.AddSeconds(seconds), null, new ActionInfo("Extract", Ms(seconds), 1500, target, null), null, null,
            [Ability(HeavySwings, heavySwingsCooldownStart), Ability(200, 0)]);

    private static TaskInput Idle(double seconds) =>
        new(T0.AddSeconds(seconds), null, null, null, null, [Ability(HeavySwings, 0), Ability(200, 0)]);

    private static List<JournalEvent> Run(TaskJournal journal, IEnumerable<TaskInput> inputs) =>
        inputs.SelectMany(i => journal.Update(i)).ToList();

    [Fact]
    public void Minage_de_3_min_sans_Heavy_Swings_donne_un_conseil()
    {
        var journal = AdviceJournal();
        var events = Run(journal, Enumerable.Range(0, 181).Select(i => Mine(i, Ms(-600))).Append(Idle(184)));
        var advice = Assert.Single(events, e => e.Category == "Conseil");
        Assert.Equal("Minage de 3 min 00 s sans Heavy Swings : pensez à l'allumer (Capacités auto).", advice.Message);
        // Le conseil suit la ligne de fin de tâche.
        Assert.Equal("Tâche", events[events.IndexOf(advice) - 1].Category);
    }

    [Fact]
    public void Heavy_Swings_lancee_pendant_le_minage_pas_de_conseil()
    {
        var journal = AdviceJournal();
        var inputs = Enumerable.Range(0, 60).Select(i => Mine(i, i < 30 ? Ms(-600) : Ms(30))).Append(Idle(64));
        Assert.DoesNotContain(Run(journal, inputs), e => e.Category == "Conseil");
    }

    [Fact]
    public void Heavy_Swings_lancee_juste_avant_le_premier_coup_compte()
    {
        var journal = AdviceJournal();
        var inputs = Enumerable.Range(0, 60).Select(i => Mine(i, Ms(-2))).Append(Idle(64));
        Assert.DoesNotContain(Run(journal, inputs), e => e.Category == "Conseil");
    }

    [Fact]
    public void Minage_court_ou_coupe_de_bois_pas_de_conseil()
    {
        var journal = AdviceJournal();
        Assert.DoesNotContain(Run(journal, Enumerable.Range(0, 10).Select(i => Mine(i, 0)).Append(Idle(14))), e => e.Category == "Conseil");
        // Coupe de bois de 2 min sans Hewing Strikes : pas un métier concerné (pêche, minage seulement).
        Assert.DoesNotContain(Run(journal, Enumerable.Range(0, 120).Select(i => Mine(100 + i, 0, target: 42)).Append(Idle(224))),
            e => e.Category == "Conseil");
    }

    [Fact]
    public void Aucune_capacite_du_metier_sur_la_barre_pas_de_conseil()
    {
        var journal = AdviceJournal();
        journal.SkillAbilities = _ => [];
        Assert.DoesNotContain(Run(journal, Enumerable.Range(0, 60).Select(i => Mine(i, 0)).Append(Idle(64))), e => e.Category == "Conseil");
    }

    [Fact]
    public void Capacites_jamais_lues_pas_de_conseil()
    {
        var journal = AdviceJournal();
        var inputs = Enumerable.Range(0, 60)
            .Select(i => new TaskInput(T0.AddSeconds(i), null, new ActionInfo("Extract", Ms(i), 1500, 50, null), null, null))
            .Append(new TaskInput(T0.AddSeconds(64), null, null, null, null));
        Assert.DoesNotContain(Run(journal, inputs), e => e.Category == "Conseil");
    }
}
