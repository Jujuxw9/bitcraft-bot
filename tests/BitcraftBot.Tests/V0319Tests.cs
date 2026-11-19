using BitcraftBot.Perception.Hud;
using BitcraftBot.Server;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.3.19 : journal des niveaux et des tâches (serveur), HUD allégé, échantillon des messages bruts.
public class V0319TaskJournalTests
{
    private static readonly DateTime T0 = new(2026, 11, 19, 20, 0, 0);
    private static readonly ServerPosition Here = new(26781, 20121);

    private static TaskJournal Journal() => new()
    {
        SkillName = id => id == 2 ? "Forestry" : $"métier {id}",
        Subject = a => a.Target == 42 ? "Mature Oak Tree" : null,
        SkillOf = a => a.Type == "Extract" ? 2 : null,
        ItemName = (id, _) => id == 7 ? "Rough Bark" : $"objet {id}",
    };

    private static ActionInfo Hit(long startMs) => new("Extract", startMs, 1500, 42, null);

    private static TaskInput At(double seconds, int xp, ActionInfo? action, int bark) =>
        new(T0.AddSeconds(seconds), [new SkillProgress(2, xp)], action, [new ItemStackInfo(7, bark, false)], Here);

    [Fact]
    public void Passage_de_niveau_ecrit_une_seule_ligne()
    {
        var journal = Journal();
        Assert.Empty(journal.Update(At(0, 510, null, 0)));     // 1re lecture : rien (niveau 1)
        Assert.Empty(journal.Update(At(1, 515, null, 0)));     // XP sans changement de niveau
        var events = journal.Update(At(2, 520, null, 0));      // 520 XP = niveau 2
        var e = Assert.Single(events);
        Assert.Equal("Métier", e.Category);
        Assert.Equal("Forestry passe niveau 2 (était 1).", e.Message);
        Assert.Empty(journal.Update(At(3, 530, null, 0)));
    }

    [Fact]
    public void Premiere_lecture_ne_compte_pas_comme_un_passage_de_niveau()
    {
        var journal = Journal();
        Assert.Empty(journal.Update(At(0, 1_000_000, null, 0)));
    }

    [Fact]
    public void Tache_debut_et_fin_avec_duree_coups_xp_et_objets()
    {
        var journal = Journal();
        journal.Update(At(0, 100, null, 3));
        var start = Assert.Single(journal.Update(At(1, 100, Hit(1000), 3)));
        Assert.Equal("Tâche", start.Category);
        Assert.Equal("Début : récolte « Mature Oak Tree », Forestry niv. 1, à N 6707, E 8927.", start.Message);

        // Plusieurs lectures pendant un même coup : un seul coup compté. Une ligne par événement, rien entre.
        Assert.Empty(journal.Update(At(1.5, 110, Hit(1000), 3)));
        Assert.Empty(journal.Update(At(3, 120, Hit(2600), 4)));
        Assert.Empty(journal.Update(At(4.5, 130, Hit(4200), 5)));
        // Court vide entre deux coups : pas encore la fin.
        Assert.Empty(journal.Update(At(5, 130, null, 5)));
        Assert.True(journal.HasRunningTask);
        var end = Assert.Single(journal.Update(At(8, 130, null, 5)));
        Assert.Equal("Fin : récolte « Mature Oak Tree », 4 s, 3 coups, Forestry +30 XP, objets : +2 Rough Bark.", end.Message);
        Assert.False(journal.HasRunningTask);
    }

    [Fact]
    public void Changer_de_cible_termine_la_tache_et_en_commence_une_autre()
    {
        var journal = Journal();
        journal.Update(At(0, 100, Hit(0), 0));
        var events = journal.Update(At(1, 110, new ActionInfo("Extract", 1000, 1500, 99, null), 1));
        Assert.Equal(2, events.Count);
        Assert.StartsWith("Fin : récolte « Mature Oak Tree »", events[0].Message);
        Assert.StartsWith("Début : récolte, Forestry niv. 1", events[1].Message);
    }

    [Fact]
    public void Deplacements_et_repas_ne_sont_pas_des_taches()
    {
        var journal = Journal();
        Assert.Empty(journal.Update(At(0, 100, new ActionInfo("PlayerMove", 0, 500, null, null), 0)));
        Assert.Empty(journal.Update(At(1, 100, new ActionInfo("UseItem", 0, 500, null, null), 0)));
        Assert.False(journal.HasRunningTask);
    }

    [Fact]
    public void Fabrication_compte_des_etapes_et_les_objets_utilises()
    {
        var journal = Journal();
        TaskInput Craft(double s, long startMs, int bark, int planks) =>
            new(T0.AddSeconds(s), null, startMs < 0 ? null : new ActionInfo("Craft", startMs, 2000, null, 55),
                [new ItemStackInfo(7, bark, false), new ItemStackInfo(8, planks, false)], null);
        Assert.StartsWith("Début : fabrication.", Assert.Single(journal.Update(Craft(0, 0, 10, 0))).Message);
        journal.Update(Craft(2, 2000, 8, 1));
        var end = Assert.Single(journal.Update(Craft(6, -1, 6, 2)));
        Assert.Equal("Fin : fabrication, 2 s, 2 étapes, aucune XP, objets : +2 objet 8, −4 Rough Bark.", end.Message);
    }

    [Fact]
    public void Noms_francais_des_vehicules() =>
        Assert.Equal("sortie d'un véhicule", ActionNames.French("DeployDeployable"));

    [Fact]
    public void Fin_forcee_a_la_deconnexion()
    {
        var journal = Journal();
        journal.Update(At(0, 100, Hit(0), 0));
        var events = journal.Flush(new TaskInput(T0.AddSeconds(10), null, null, null, null));
        Assert.StartsWith("Fin : récolte « Mature Oak Tree », 0 s, 1 coup, aucune XP", Assert.Single(events).Message);
        Assert.Empty(journal.Flush(new TaskInput(T0.AddSeconds(11), null, null, null, null)));
    }
}

public class V0319HudTests
{
    [Fact]
    public void Plus_de_zone_vie_endurance_ni_metier()
    {
        var names = HudLayout.AllZones.Select(z => z.Name).ToList();
        Assert.DoesNotContain("Vie", names);
        Assert.DoesNotContain("Endurance", names);
        Assert.DoesNotContain("Métier", names);
        // v0.3.20 : la cible seulement (position, zone et région viennent du serveur, D-048).
        Assert.Equal(["Cible", "Catégorie", "Santé cible"], names);
    }

    [Fact]
    public void Messages_bruts_du_serveur_echantillonnes() => Assert.True(ServerReader.RawMessageSample >= 100);
}
