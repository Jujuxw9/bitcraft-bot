using BitcraftBot.Server;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.16 (D-068) : archive du chat en local (SQLite), messages privés, message écrit par le bot.
public class V0416Tests
{
    private const ulong Me = 648518346637266951;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static ArchivedChatMessage Msg(ulong id, string player, int channel, string text, int secondsAgo = 0, string kind = ChatArchive.KindChat) =>
        new(kind, id, Now.AddSeconds(-secondsAgo), channel, player, 1000 + id, "fr", text, "Snowrest", false);

    [Fact]
    public void Enregistrement_puis_lecture()
    {
        using var archive = ChatArchive.InMemory();
        Assert.Equal(0, archive.Count());
        var added = archive.Add([Msg(1, "Alice", 3, "salut tout le monde"), Msg(2, "Bob", 2, "hello", 5)]);
        Assert.Equal(2, added);
        Assert.Equal(2, archive.Count());
        var all = archive.Search();
        Assert.Equal(2, all.Count);
        var alice = all[0]; // le plus récent en premier
        Assert.Equal("Alice", alice.PlayerName);
        Assert.Equal(3, alice.Channel);
        Assert.Equal("Région", alice.ChannelName);
        Assert.Equal("salut tout le monde", alice.Text);
        Assert.Equal(Now, alice.SentAt);
        Assert.Equal(1001UL, alice.PlayerEntityId);
        Assert.Equal("Snowrest", alice.Region);
        Assert.False(alice.ByBot);
    }

    [Fact]
    public void Doublons_ignores()
    {
        using var archive = ChatArchive.InMemory();
        Assert.Equal(1, archive.Add([Msg(1, "Alice", 3, "salut")]));
        Assert.Equal(0, archive.Add([Msg(1, "Alice", 3, "salut")]));
        Assert.Equal(1, archive.Add([Msg(1, "Alice", 3, "salut"), Msg(2, "Alice", 3, "re")]));
        // Même identifiant, mais message privé : une autre sorte de message, donc gardé.
        Assert.Equal(1, archive.Add([Msg(1, "Alice", ChatArchive.PrivateChannel, "psst", kind: ChatArchive.KindPrivate)]));
        Assert.Equal(3, archive.Count());
    }

    [Fact]
    public void Recherche_par_joueur_sans_majuscules()
    {
        using var archive = ChatArchive.InMemory();
        archive.Add([Msg(1, "Alice", 3, "a"), Msg(2, "Bob", 3, "b"), Msg(3, "BobLeBricoleur", 8, "c"), Msg(4, "alicia", 2, "d")]);
        Assert.Equal(["Bob", "BobLeBricoleur"], archive.Search("bob").Select(m => m.PlayerName).Order().ToArray());
        Assert.Equal(2, archive.Search("ALI").Count);
        Assert.Empty(archive.Search("Zed"));
        Assert.Equal(4, archive.Search("  ").Count);
    }

    [Fact]
    public void Filtre_par_canal_et_limite()
    {
        using var archive = ChatArchive.InMemory();
        archive.Add(Enumerable.Range(1, 30).Select(i => Msg((ulong)i, "P" + i, i % 2 == 0 ? 3 : 8, "m" + i, secondsAgo: i)));
        var region = archive.Search(channel: 3);
        Assert.Equal(15, region.Count);
        Assert.All(region, m => Assert.Equal(3, m.Channel));
        Assert.Equal("P2", region[0].PlayerName); // le plus récent du canal
        Assert.Equal(5, archive.Search(limit: 5).Count);
        Assert.Equal("P1", archive.Search(limit: 5)[0].PlayerName);
        Assert.Single(archive.Search("P3", 8), m => m.PlayerName == "P3");
        Assert.Equal(5, archive.Search("P2", 8).Count); // P21, P23, P25, P27, P29 (morceau du nom)
        Assert.Empty(archive.Search("P4", 8));
    }

    [Fact]
    public void Fichier_sur_disque_garde_les_messages()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-archive-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var archive = ChatArchive.OpenFolder(folder))
            {
                archive.Add([Msg(ulong.MaxValue, "Alice", 3, "très grand identifiant")]);
            }
            using (var again = ChatArchive.OpenFolder(folder))
            {
                Assert.Equal(1, again.Count());
                Assert.Equal(ulong.MaxValue, again.Search()[0].ServerId);
                Assert.Equal(0, again.Add([Msg(ulong.MaxValue, "Alice", 3, "très grand identifiant")]));
            }
            Assert.True(File.Exists(Path.Combine(folder, ChatArchive.FileName)));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    [Fact]
    public void Message_du_chat_converti()
    {
        var m = new ChatMessage(77, Me, "Julien", "fr", 7, "qui pour un donjon ?", Now);
        var a = ChatArchive.From(m, "Snowrest", byBot: true);
        Assert.Equal(ChatArchive.KindChat, a.Kind);
        Assert.Equal(77UL, a.ServerId);
        Assert.Equal(Me, a.PlayerEntityId);
        Assert.Equal("Looking For Group", a.ChannelName);
        Assert.True(a.ByBot);
    }

    [Fact]
    public void Message_prive_lu_depuis_la_table()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            entity_id = 9,
            username = "Alice",
            title_id = 0,
            sender_entity_id = 42,
            receiver_entity_id = Me,
            text = "tu vends du bois ?",
            timestamp = Now.ToUnixTimeSeconds(),
            language_code = new { some = "fr" },
        });
        var cache = new TableCache();
        cache.Apply(new TableChange(DirectMessages.Table, [Row.Parse(json, null)], []));
        var dm = Assert.Single(DirectMessages.Read(cache));
        Assert.Equal(9UL, dm.EntityId);
        Assert.Equal(42UL, dm.SenderEntityId);
        Assert.Equal(Me, dm.ReceiverEntityId);
        Assert.Equal("Alice", dm.Username);
        Assert.Equal("fr", dm.Language);
        Assert.Equal(Now, dm.SentAt);
        var a = ChatArchive.From(dm);
        Assert.Equal(ChatArchive.KindPrivate, a.Kind);
        Assert.Equal("Privé", a.ChannelName);
        Assert.Equal(Me, a.ReceiverEntityId);
    }

    [Theory]
    [InlineData("\"fr\"", "fr")]
    [InlineData("{\"some\":\"en\"}", "en")]
    [InlineData("[0,\"de\"]", "de")]
    [InlineData("{\"none\":[]}", null)]
    [InlineData("[1,[]]", null)]
    public void Langue_facultative(string raw, string? expected)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        Assert.Equal(expected, DirectMessages.OptionalText(doc.RootElement.Clone()));
    }

    [Fact]
    public void Message_ecrit_par_le_bot_reconnu()
    {
        var picker = new GptChatPicker(Me);
        picker.NoteSent("salut, je coupe du bois :)");
        var own = new ChatMessage(1, Me, "Julien", "fr", 3, "salut, je coupe du bois :)", DateTimeOffset.UtcNow.AddMinutes(-3));
        var julien = new ChatMessage(2, Me, "Julien", "fr", 3, "autre chose", DateTimeOffset.UtcNow.AddMinutes(-3));
        var other = new ChatMessage(3, 42, "Bob", "fr", 3, "salut, je coupe du bois :)", DateTimeOffset.UtcNow.AddMinutes(-3));
        Assert.True(picker.WrittenByBot(own));
        Assert.False(picker.WrittenByBot(julien));
        Assert.False(picker.WrittenByBot(other));
    }

    [Fact]
    public void Decouverte_sans_messages_prives_par_defaut()
    {
        var d = RegionDiscovery.Fail("test");
        Assert.Empty(d.DirectMessages);
        Assert.Equal("messages privés pas demandés", d.DirectMessagesNote);
    }
}
