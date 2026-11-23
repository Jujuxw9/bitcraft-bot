using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// Module « @GPT » (D-054) : lecture du chat Région et choix du message auquel répondre. Aucun envoi.
public class GptChatTests
{
    private const ulong Me = 648518346637266951;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static string Json(ulong id, ulong owner, string user, int channel, string text, DateTimeOffset at) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            entity_id = id, username = user, title_id = 0, channel_id = channel, target_id = 0, text,
            timestamp = at.ToUnixTimeSeconds(), owner_entity_id = owner,
        });

    private static ChatMessage Msg(ulong id, string text, DateTimeOffset at, ulong owner = 42, int channel = 3, string user = "fr/Bob") =>
        ChatMessages.Parse(Row.Parse(Json(id, owner, user, channel, text, at), null))!;

    [Fact]
    public void Message_lu_depuis_la_table()
    {
        var cache = new TableCache();
        cache.Apply(new TableChange("chat_message_state",
        [
            Row.Parse(Json(2, 42, "en/Alice", 3, "hello @GPT", Now), null),
            Row.Parse(Json(1, 43, "fr/Bob", 2, "salut", Now.AddSeconds(-5)), null),
        ], []));
        var messages = ChatMessages.Read(cache);
        Assert.Equal(2, messages.Count);
        Assert.Equal(1UL, messages[0].EntityId); // triés par heure
        var alice = messages[1];
        Assert.Equal("Alice", alice.Username);
        Assert.Equal("en", alice.Language);
        Assert.Equal(3, alice.Channel);
        Assert.Equal("hello @GPT", alice.Text);
        Assert.Equal(Now, alice.SentAt);
        Assert.Equal("SELECT * FROM chat_message_state WHERE channel_id = 3", ChatMessages.RegionQuery);
    }

    [Theory]
    [InlineData("fr/Bob", "fr", "Bob")]
    [InlineData("Bob", "", "Bob")]
    [InlineData("en/A/B", "en", "A/B")]
    public void Pseudo_separe_de_la_langue(string raw, string lang, string name)
    {
        Assert.Equal((lang, name), ChatMessages.SplitUsername(raw));
    }

    [Theory]
    [InlineData("@GPT tu vas bien ?", true)]
    [InlineData("salut @GPT", true)]
    [InlineData("@gpt tu vas bien ?", true)]
    [InlineData("@Gpt", true)]
    [InlineData("@ChatGPT ça va ?", true)]
    [InlineData("hey @chatgpt, help", true)]
    [InlineData("GPT sans arobase", false)]
    [InlineData("@gpts", false)]
    [InlineData("mail@gpt.fr", false)]
    public void Declencheur_gpt_ou_chatgpt_sans_tenir_compte_des_majuscules(string text, bool expected)
    {
        Assert.Equal(expected, GptChatPicker.IsTrigger(text));
    }

    [Fact]
    public void Ignore_les_autres_canaux_Julien_et_les_vieux_messages()
    {
        var picker = new GptChatPicker(Me);
        var messages = new[]
        {
            Msg(1, "@GPT système", Now, channel: 0),
            Msg(2, "@GPT moi", Now, owner: Me),
            Msg(3, "@GPT vieux", Now.AddMinutes(-3)),
            Msg(4, "pas de déclencheur", Now),
        };
        Assert.Null(picker.Pick(messages, Now));
    }

    [Fact]
    public void Repond_au_plus_recent_puis_attend_une_minute()
    {
        var picker = new GptChatPicker(Me);
        var first = new[] { Msg(1, "@GPT un", Now.AddSeconds(-20)), Msg(2, "@GPT deux", Now.AddSeconds(-5)) };
        Assert.Equal(2UL, picker.Pick(first, Now)!.EntityId);
        // Les deux sont traités : aucun ne revient.
        Assert.Null(picker.Pick(first, Now.AddMinutes(1)));

        // Nouveau message 30 s après : il attend la fin de la minute.
        var second = first.Append(Msg(3, "@GPT trois", Now.AddSeconds(30))).ToArray();
        Assert.Null(picker.Pick(second, Now.AddSeconds(30)));
        Assert.Single(picker.Pending(second, Now.AddSeconds(30)));
        Assert.Equal(3UL, picker.Pick(second, Now.AddSeconds(60))!.EntityId);
    }

    [Fact]
    public void Repond_meme_sans_question()
    {
        var picker = new GptChatPicker(Me);
        Assert.NotNull(picker.Pick([Msg(1, "@GPT j'aime les arbres", Now)], Now));
    }

    [Fact]
    public void Phrase_dans_la_langue_du_joueur_avec_son_pseudo()
    {
        var fr = CannedReplies.For(Msg(1, "@GPT", Now, user: "fr/Bob"), new Random(1));
        Assert.Contains("Bob", fr);
        Assert.Contains(fr, CannedReplies.French.Select(p => string.Format(p, "Bob")));
        var en = CannedReplies.For(Msg(2, "@GPT", Now, user: "en/Alice"), new Random(1), "[GPT] ");
        Assert.StartsWith("[GPT] ", en);
        Assert.Contains(en[6..], CannedReplies.English.Select(p => string.Format(p, "Alice")));
    }

    [Fact]
    public void Toutes_les_phrases_tiennent_dans_250_caracteres()
    {
        var longName = new string('x', 24);
        foreach (var p in CannedReplies.French.Concat(CannedReplies.English))
        {
            Assert.Contains("{0}", p);
            Assert.True(("[GPT] " + string.Format(p, longName)).Length <= CannedReplies.MaxLength);
        }
    }

    [Theory]
    [InlineData("Bob<script>", "Bobscript")]
    [InlineData("Jean-Luc_2", "Jean-Luc_2")]
    [InlineData("***", "aventurier")]
    public void Pseudo_nettoye(string raw, string expected)
    {
        Assert.Equal(expected, CannedReplies.CleanName(raw));
    }
}
