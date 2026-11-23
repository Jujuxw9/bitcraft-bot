using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.9 : langue du message, style relâché, faute corrigée par « *mot », autres bots ignorés, phrases à côté du .exe.
public class V049Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static ChatMessage Msg(string text, string user = "en/Bob", ulong id = 1) =>
        new(id, 42, user.Split('/')[1], user.Split('/')[0], 3, text, Now);

    [Theory]
    [InlineData("@gpt salut tu vas bien ?", "en/Bob", "fr")]
    [InlineData("@gpt où est la forêt", "en/Bob", "fr")]
    [InlineData("@gpt hi how are you", "en/Bob", "en")]
    [InlineData("@gpt", "en/Bob", "en")]
    [InlineData("@gpt hello", "fr/Bob", "fr")] // « fr/ » : toujours français
    public void Langue_du_message(string text, string user, string expected) =>
        Assert.Equal(expected, ChatStyle.LanguageOf(Msg(text, user)));

    [Fact]
    public void Style_relache()
    {
        var random = new Random(1);
        for (var i = 0; i < 200; i++)
        {
            var t = ChatStyle.Casual("Je ne sais pas, Bob. C'est la forêt.", "fr", "Bob", random);
            Assert.False(t.EndsWith('.'));
            Assert.True(char.IsLower(t[0]));
            Assert.Contains("Bob", t);
        }
        // Le pseudo en tête garde sa majuscule.
        Assert.StartsWith("Bob", ChatStyle.Casual("Bob, tu es là.", "fr", "Bob", new Random(2)));
        // Abréviations : vues au moins une fois.
        var seen = Enumerable.Range(0, 200).Select(_ => ChatStyle.Casual("No problem, thanks.", "en", "Bob", random)).ToList();
        Assert.Contains(seen, s => s.StartsWith("np", StringComparison.Ordinal));
        Assert.Contains(seen, s => s.Contains("thx", StringComparison.Ordinal));
    }

    [Fact]
    public void Faute_corrigee_une_fois_sur_deux()
    {
        var book = ChatPhraseBook.LoadEmbedded();
        var random = new Random(5);
        var typos = Enumerable.Range(0, 400).Select(_ => ChatReplies.For(Msg("@gpt les arbres", "fr/Bob"), book, random, typoChance: 1, casualChance: 0))
            .Where(r => r.Typo is not null).ToList();
        Assert.True(typos.Count > 300);
        var fixes = typos.Where(r => r.Correction is not null).ToList();
        Assert.InRange(fixes.Count, typos.Count * 3 / 10, typos.Count * 7 / 10);
        Assert.All(fixes, r => Assert.StartsWith("*", r.Correction));
        Assert.All(fixes, r => Assert.Contains(r.Correction![1..], r.Typo));
    }

    [Fact]
    public void Autres_bots_ignores()
    {
        var picker = new GptChatPicker(7);
        Assert.True(picker.LooksAutomated(Msg("@gpt hello", "en/HelperBot")));
        Assert.True(picker.LooksAutomated(Msg("[BOT] @gpt hello")));
        Assert.True(picker.LooksAutomated(Msg("@gpt as an AI I cannot")));
        Assert.False(picker.LooksAutomated(Msg("@gpt hello friend")));
        // Écho d'une réponse du bot.
        picker.NoteSent("Bob, chopping wood is my therapy.");
        Assert.True(picker.LooksAutomated(Msg("@gpt Bob, chopping wood is my therapy.")));

        var m = picker.Pick([Msg("@gpt hi", "en/ChatBot", 1), Msg("@gpt hi", "en/Alice", 2)], Now);
        Assert.Equal("Alice", m!.Username);
        Assert.Single(picker.TakeIgnored());
        Assert.Empty(picker.TakeIgnored());
    }

    [Fact]
    public void Phrases_modifiables_a_cote_du_exe()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraft-phrases-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Sans fichier : phrases intégrées, modèles écrits.
            var book = ChatPhraseBook.Load(folder, out var none);
            Assert.Empty(none);
            Assert.True(File.Exists(Path.Combine(folder, "modele-fr.txt")));
            Assert.True(File.Exists(Path.Combine(folder, "modele-en.txt")));
            Assert.Equal(ChatPhraseBook.LoadEmbedded().Themes.Count, book.Themes.Count);

            // fr.txt posé à côté : il remplace le français ; une phrase qui parle de bot est refusée.
            File.WriteAllText(Path.Combine(folder, "fr.txt"), "[salut]\nmots: salut\n- Hello {nom}\n- Je suis un robot\n[autre]\n- Hein ?\n");
            book = ChatPhraseBook.Load(folder, out var replaced);
            Assert.Equal(new[] { "fr" }, replaced);
            Assert.Single(book.Errors);
            Assert.Equal(new[] { "Hello {nom}" }, book.Theme("salut")!.Phrases["fr"]);
            Assert.True(book.Theme("arbres")!.Phrases.ContainsKey("en")); // anglais toujours intégré

            // Fichier inutilisable (pas de [autre]) : phrases intégrées gardées.
            File.WriteAllText(Path.Combine(folder, "fr.txt"), "n'importe quoi\n");
            File.WriteAllText(Path.Combine(folder, "en.txt"), "rien\n");
            book = ChatPhraseBook.Load(folder, out var fallback);
            Assert.Empty(fallback);
            Assert.Equal(12, book.Theme("arbres")!.Phrases["fr"].Count); // phrases intégrées
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
