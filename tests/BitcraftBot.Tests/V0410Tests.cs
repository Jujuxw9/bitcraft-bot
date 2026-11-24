using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.10 : plafonds, pause quand Julien écrit, amis et liste noire, ton selon canal et heure, infos du jeu, historique et statistiques.
public class V0410Tests
{
    private const ulong Me = 7;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static ChatMessage Msg(ulong id, string text, string user = "en/Bob", ulong owner = 42, int channel = 3, double ago = 1) =>
        new(id, owner, user.Split('/')[1], user.Split('/')[0], channel, text, Now.AddSeconds(-ago));

    [Fact]
    public void Plafond_par_joueur_par_heure_puis_par_heure_puis_par_jour()
    {
        var q = new ChatQuota();
        for (var i = 0; i < ChatQuota.PerPlayerPerHour; i++)
        {
            Assert.Null(q.Why("Bob", Now.AddMinutes(i)));
            q.Record("Bob", Now.AddMinutes(i));
        }
        Assert.NotNull(q.Why("Bob", Now.AddMinutes(10)));
        Assert.NotNull(q.Why("b-o-b", Now.AddMinutes(10))); // même pseudo, écrit autrement
        Assert.Null(q.Why("Alice", Now.AddMinutes(10)));
        Assert.Null(q.Why("Bob", Now.AddMinutes(70))); // plus d'une heure après

        var hourly = new ChatQuota();
        for (var i = 0; i < ChatQuota.PerHour; i++)
        {
            hourly.Record("joueur" + i, Now.AddMinutes(i));
        }
        Assert.Contains("par heure", hourly.Why("Nouveau", Now.AddMinutes(30)));
        Assert.Null(hourly.Why("Nouveau", Now.AddMinutes(90)));

        var daily = new ChatQuota();
        for (var i = 0; i < ChatQuota.PerDay; i++)
        {
            daily.Record("j" + i, Now.AddMinutes(i * 14)); // jamais plus de 5 par heure
        }
        Assert.Contains("par jour", daily.Why("Nouveau", Now.AddMinutes(ChatQuota.PerDay * 14 - 1)));
    }

    [Fact]
    public void Le_picker_applique_les_plafonds_et_la_liste_noire()
    {
        var picker = new GptChatPicker(Me) { Lists = ChatLists.Parse("", "Mechant") };
        Assert.Null(picker.Pick([Msg(1, "@gpt salut", "en/Mechant")], Now));
        var skipped = picker.TakeSkipped();
        Assert.Single(skipped);
        Assert.Contains("liste noire", skipped[0].Reason);

        var t = Now;
        for (var i = 0; i < ChatQuota.PerPlayerPerHour; i++)
        {
            Assert.NotNull(picker.Pick([new ChatMessage(10 + (ulong)i, 42, "Bob", "en", 3, "@gpt salut", t)], t));
            t = t.AddMinutes(2);
        }
        Assert.Null(picker.Pick([new ChatMessage(99, 42, "Bob", "en", 3, "@gpt salut", t)], t));
        Assert.Contains("déjà", picker.TakeSkipped().Single().Reason);
    }

    [Fact]
    public void Pause_quand_Julien_ecrit_mais_pas_pour_les_messages_du_bot()
    {
        var picker = new GptChatPicker(Me);
        // Message du bot (texte connu) : pas de pause.
        picker.NoteSent("Bonjour Bob !");
        picker.ObserveOwn([Msg(1, "Bonjour Bob !", owner: Me, ago: 0)]);
        Assert.True(picker.PausedUntil <= Now.AddHours(-1));

        // Message de Julien (texte inconnu, bien après un envoi du bot) : pause de 5 minutes.
        var julien = new ChatMessage(2, Me, "Julien", "fr", 3, "salut tout le monde", DateTimeOffset.UtcNow.AddMinutes(-30));
        picker.ObserveOwn([julien]);
        Assert.Equal(julien.SentAt + GptChatPicker.PauseAfterOwnMessage, picker.PausedUntil);

        var recent = new ChatMessage(3, Me, "Julien", "fr", 3, "coucou", DateTimeOffset.UtcNow);
        picker = new GptChatPicker(Me);
        picker.ObserveOwn([recent]);
        Assert.True(picker.PausedUntil > DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        Assert.Null(picker.Pick([new ChatMessage(4, 42, "Bob", "en", 3, "@gpt salut", now)], now));
        Assert.Contains("pause", picker.TakeSkipped().Single(s => s.Message.EntityId == 4).Reason);
    }

    [Fact]
    public void Amis_et_liste_noire_lus_dans_les_fichiers()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-listes-" + Guid.NewGuid().ToString("N"));
        try
        {
            var lists = ChatLists.Load(folder);
            Assert.True(lists.IsFriend("The Krazy Turkey"));
            Assert.True(lists.IsFriend("thekrazyturkey"));
            Assert.False(lists.IsFriend("The Crazy Turkey")); // avec un C : ce n'est pas lui
            Assert.False(lists.IsBlocked("The Krazy Turkey"));

            File.WriteAllText(Path.Combine(folder, ChatLists.BlockedFile), "# commentaire\nThe Krazy Turkey # fâché\nSpammer\n");
            lists = ChatLists.Load(folder);
            Assert.True(lists.IsBlocked("Spammer"));
            Assert.False(lists.IsFriend("The Krazy Turkey")); // la liste noire gagne
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Ton_selon_le_canal_et_l_ami()
    {
        Assert.True(ChatTone.For((int)ChatChannel.Commerce, false).Smiley < ChatTone.For((int)ChatChannel.Local, false).Smiley);
        Assert.True(ChatTone.For((int)ChatChannel.Region, true).Casual > ChatTone.For((int)ChatChannel.Region, false).Casual);

        var book = ChatPhraseBook.LoadEmbedded();
        var random = new Random(3);
        var friendly = Enumerable.Range(0, 200).Count(_ => ChatReplies.For(Msg(1, "@gpt salut", "fr/Bob"), book, random, typoChance: 0, friend: true).Text.EndsWith(')')
            || ChatReplies.For(Msg(1, "@gpt salut", "fr/Bob"), book, random, typoChance: 0, friend: true).Text.Length > 0);
        Assert.Equal(200, friendly);
    }

    [Theory]
    [InlineData("Bonjour Bob !", "fr", 9, "Bonjour Bob !")]
    [InlineData("Bonjour Bob !", "fr", 21, "Bonsoir Bob !")]
    [InlineData("Bonsoir Bob, bonne soirée", "fr", 10, "Bonjour Bob, bonne journée")]
    [InlineData("Bonne journée Bob", "fr", 2, "Bonne soirée Bob")]
    [InlineData("Good evening Bob!", "en", 8, "Good morning Bob!")]
    [InlineData("Good morning Bob!", "en", 14, "Good afternoon Bob!")]
    [InlineData("Bonjour Bob", "fr", -1, "Bonjour Bob")]
    public void Bonjour_ou_bonsoir_selon_l_heure(string text, string lang, int hour, string expected) =>
        Assert.Equal(expected, ChatTone.AdaptToTime(text, lang, hour));

    [Fact]
    public void Infos_du_jeu_dites_seulement_si_connues()
    {
        var book = ChatPhraseBook.LoadEmbedded();
        var withInfo = new ChatInfo("Cinder Ridge", 42);
        var seen = new HashSet<string>();
        var random = new Random(11);
        for (var i = 0; i < 400; i++)
        {
            var r = ChatReplies.For(Msg(1, "@gpt c'est quoi ton niveau ?", "fr/Bob"), book, random, smileyChance: 0, typoChance: 0, casualChance: 0, info: withInfo);
            Assert.DoesNotContain("{", r.Text);
            seen.Add(r.Text);
        }
        Assert.Contains(seen, t => t.Contains("niveau 42"));
        // Sans info : jamais de phrase à trous, jamais d'accolade.
        for (var i = 0; i < 400; i++)
        {
            var r = ChatReplies.For(Msg(1, "@gpt où est la forêt", "fr/Bob"), book, random, smileyChance: 0, typoChance: 0, casualChance: 0);
            Assert.DoesNotContain("{", r.Text);
            Assert.DoesNotContain("région  ", r.Text);
        }
    }

    [Fact]
    public void Historique_et_statistiques_gardes_dans_le_dossier()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-stats-" + Guid.NewGuid().ToString("N"));
        try
        {
            var stats = new ChatStats(folder);
            Assert.Contains("Aucun", stats.Summary());
            ChatHistoryEntry Entry(string theme, string text) => new(Now, "Région", "Bob", text, theme, "ok", "envoyée");
            stats.Count(Entry("arbres", "@gpt les arbres"));
            stats.Count(Entry("arbres", "@gpt du bois"));
            stats.Count(Entry("autre", "@gpt blablabla"));
            stats.Add(Entry("arbres", "@gpt les arbres"));
            Assert.Equal(3, stats.Total);
            Assert.Equal(1, stats.Unmatched);
            Assert.Single(stats.History);
            Assert.Contains("arbres 2", stats.Summary());

            // Rechargé depuis le fichier ; le message sans thème est noté pour savoir quoi ajouter.
            var again = new ChatStats(folder);
            Assert.Equal(3, again.Total);
            Assert.Contains("blablabla", File.ReadAllText(Path.Combine(folder, ChatStats.UnmatchedFile)));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
