using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.8 : chat vérifié avant chaque lettre, clic de Julien = arrêt ; base de phrases par thème ; fautes volontaires 5 %.
public class V048Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static ChatMessage Msg(string text, string user = "en/Bob") =>
        new(1, 42, user.Split('/')[1], user.Split('/')[0], 3, text, Now);

    // ── Sécurité de la frappe (journal du 24/11 00:03 : chat fermé, lettres parties dans le jeu) ──

    private sealed class FakeDevice : IInputDevice
    {
        public readonly List<string> Events = [];
        public bool ChatOpen;

        // Le chat se ferme tout seul après N lettres (clic de Julien, par exemple). -1 = jamais.
        public int CloseAfterLetters = -1;

        public (int X, int Y) CursorPosition() => (0, 0);
        public bool IsForeground(IntPtr window) => true;
        public void MoveTo(int x, int y) { }
        public void LeftDown() { }
        public void LeftUp() { }

        public void KeyDown(ushort scanCode)
        {
            Events.Add(scanCode == GameKeys.Enter ? "Entrée" : scanCode == GameKeys.Escape ? "Échap" : $"down {scanCode}");
            if (scanCode == GameKeys.Enter)
            {
                ChatOpen = !ChatOpen;
            }
            else if (scanCode == GameKeys.Escape)
            {
                ChatOpen = false;
            }
        }

        public void KeyUp(ushort scanCode) { }

        public void TypeChar(char c)
        {
            Events.Add(c.ToString());
            if (CloseAfterLetters >= 0 && Typed.Length >= CloseAfterLetters)
            {
                ChatOpen = false;
            }
        }

        public string Typed => string.Concat(Events.Where(e => e.Length == 1));
        public int Enters => Events.Count(e => e == "Entrée");
    }

    private static (ActionController Controller, FakeDevice Device) Make()
    {
        var device = new FakeDevice();
        var context = new ActionContext
        {
            GameWindow = () => new IntPtr(42),
            GameVisible = () => true,
            LastPhysicalKey = () => DateTime.MinValue,
        };
        var controller = new ActionController(device, context, random: new Random(1), delay: (ms, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });
        return (controller, device);
    }

    private static ChatProbe Probe(FakeDevice device, Func<DateTime>? click = null, Func<bool>? open = null) => new()
    {
        IsOpen = () => Task.FromResult<bool?>(open?.Invoke() ?? device.ChatOpen),
        Channel = () => Task.FromResult("REGION"),
        ChannelOk = t => ChatInput.IsChannel(t, "REGION"),
        LastUserClick = click ?? (() => DateTime.MinValue),
    };

    [Fact]
    public async Task Chat_vu_avant_chaque_lettre()
    {
        var (controller, device) = Make();
        var checks = 0;
        var probe = Probe(device, open: () => { checks++; return device.ChatOpen; });
        var result = await controller.ChatAsync("Réponse", "Salut Bob", probe, "/y");
        Assert.True(result.Done, result.Detail);
        Assert.Equal("Salut Bob", device.Typed);
        // Au moins une image par lettre (9), plus celles avant Entrée et après.
        Assert.True(checks >= 9 + 3, $"{checks} vérifications");
    }

    [Fact]
    public async Task Chat_ferme_pendant_la_frappe_plus_aucune_lettre_ni_Entree()
    {
        var (controller, device) = Make();
        device.CloseAfterLetters = 4;
        var result = await controller.ChatAsync("Réponse", "Salut tout le monde", Probe(device), "/y");
        Assert.Equal(ActionOutcome.Refuse, result.Outcome);
        Assert.Contains("chat fermé pendant la frappe", result.Detail);
        Assert.Equal("Salu", device.Typed); // la 5e lettre n'est jamais partie
        Assert.Equal(1, device.Enters); // seulement celle qui a ouvert le chat
        Assert.DoesNotContain("Échap", device.Events); // chat déjà fermé : pas d'Échap à l'aveugle
    }

    [Fact]
    public async Task Chat_ferme_juste_avant_Entree_pas_d_Entree()
    {
        var (controller, device) = Make();
        device.CloseAfterLetters = 5;
        var result = await controller.ChatAsync("Réponse", "Salut", Probe(device), "/y");
        Assert.Equal(ActionOutcome.Refuse, result.Outcome);
        Assert.Equal("Salut", device.Typed);
        Assert.Equal(1, device.Enters); // Entrée aurait rouvert le chat
    }

    [Fact]
    public async Task Clic_de_Julien_pendant_la_frappe_arret()
    {
        var (controller, device) = Make();
        var clickAt = DateTime.MinValue;
        var probe = Probe(device, click: () => clickAt, open: () =>
        {
            // Julien clique au milieu de la frappe.
            if (device.Typed.Length == 3)
            {
                clickAt = DateTime.MaxValue;
            }
            return device.ChatOpen;
        });
        var result = await controller.ChatAsync("Réponse", "Salut tout le monde", probe, "/y");
        Assert.Equal(ActionOutcome.Interrompu, result.Outcome);
        Assert.Contains("cliqué", result.Detail);
        Assert.Equal("Sal", device.Typed);
        Assert.Equal(1, device.Enters);
        Assert.False(device.ChatOpen); // chat refermé (Échap)
    }

    // ── Base de phrases ──

    private static readonly ChatPhraseBook Book = ChatPhraseBook.LoadEmbedded();

    [Fact]
    public void Fichiers_de_phrases_sans_erreur_et_memes_themes()
    {
        Assert.Empty(Book.Errors);
        Assert.True(Book.Themes.Count >= 25, $"{Book.Themes.Count} thèmes");
        Assert.Equal(ChatPhraseBook.DefaultTheme, Book.Themes[^1].Name);
        foreach (var theme in Book.Themes)
        {
            Assert.True(theme.Phrases.TryGetValue("fr", out var fr) && fr.Count >= 8, $"[{theme.Name}] fr");
            Assert.True(theme.Phrases.TryGetValue("en", out var en) && en.Count >= 8, $"[{theme.Name}] en");
            Assert.Equal(fr.Count, fr.Distinct().Count());
            Assert.Equal(en.Count, en.Distinct().Count());
            if (theme.Name != ChatPhraseBook.DefaultTheme)
            {
                Assert.NotEmpty(theme.Keywords);
            }
        }
    }

    // D-055 : jamais d'allusion au fait que c'est un bot (test repris de la v0.4.7, sur toute la base).
    [Fact]
    public void Aucune_phrase_ne_parle_de_bot()
    {
        var forbidden = new[] { "bot", "robot", " ia ", " ai ", "circuit", "bip", "beep", "program", "intelligence", "openai", "chatgpt" };
        foreach (var p in Book.Themes.SelectMany(t => t.Phrases.Values.SelectMany(l => l)))
        {
            var lower = " " + p.ToLowerInvariant() + " ";
            Assert.DoesNotContain(forbidden, f => lower.Contains(f, StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("@gpt salut !", "salutation")]
    [InlineData("hello @chatgpt", "salutation")]
    [InlineData("@gpt ça va ?", "cava")]
    [InlineData("@gpt how are you?", "cava")]
    [InlineData("merci @gpt", "merci")]
    [InlineData("@gpt thx mate", "merci")]
    [InlineData("@gpt bye", "aurevoir")]
    [InlineData("@gpt bonne nuit", "aurevoir")]
    [InlineData("@gpt t'es un bot ?", "toi")]
    [InlineData("@gpt check ChatGPT's vitals", "toi")]
    [InlineData("@gpt où est la banque ?", "ou")]
    [InlineData("@gpt where can I find iron", "ou")]
    [InlineData("@gpt j'ai besoin de bois", "arbres")]
    [InlineData("@gpt any good trees around?", "arbres")]
    [InlineData("@gpt tu vends quoi", "commerce")]
    [InlineData("@gpt wts copper", "minage")]
    [InlineData("@gpt lol", "blague")]
    [InlineData("@gpt t'es nul", "insulte")]
    [InlineData("@gpt gg", "compliment")]
    [InlineData("@gpt tu as quel âge", "perso")]
    [InlineData("@gpt j'ai faim", "cuisine")]
    [InlineData("@gpt rejoins notre guilde", "groupe")]
    [InlineData("@gpt nay kaung lar ?", "question")]
    [InlineData("@gpt nihow", "autre")]
    [InlineData("@gpt", "autre")]
    public void Theme_reconnu(string message, string theme) => Assert.Equal(theme, Book.DetectTheme(message));

    [Fact]
    public void Mots_reconnus_sans_accents_ni_majuscules()
    {
        Assert.Equal(["ou", "est", "la", "foret"], ChatPhraseBook.Tokens("OÙ est la Forêt ?"));
        Assert.Equal("arbres", Book.DetectTheme("@GPT La FORÊT"));
    }

    [Fact]
    public void Format_simple_et_erreurs_signalees()
    {
        var book = ChatPhraseBook.Parse([("fr", "# commentaire\n[salut]\nmots: coucou, bon*\n- Coucou {nom} !\n\n[autre]\n- Hein ?\nn'importe quoi\n- {x}\n")]);
        Assert.Equal(2, book.Errors.Count); // « n'importe quoi » et « {x} »
        Assert.Equal("salut", book.DetectTheme("bonjour"));
        Assert.Equal("autre", book.DetectTheme("rien"));
        Assert.Equal("Coucou {nom} !", book.Pick("salut", "fr", new Random(1)));
        // Langue absente : anglais, sinon la phrase de la langue demandée n'existe pas → null ici (pas d'anglais).
        Assert.Null(book.Pick("salut", "en", new Random(1)));
    }

    [Fact]
    public void Pas_deux_fois_la_meme_phrase_de_suite()
    {
        var book = ChatPhraseBook.LoadEmbedded();
        var random = new Random(4);
        var picks = Enumerable.Range(0, 10).Select(_ => book.Pick("arbres", "fr", random)).ToList();
        Assert.Equal(10, picks.Distinct().Count());
    }

    [Fact]
    public void Reponse_avec_theme_et_pseudo()
    {
        var reply = ChatReplies.For(Msg("@gpt salut", "fr/Bob"), ChatPhraseBook.LoadEmbedded(), new Random(2), smileyChance: 0, typoChance: 0, casualChance: 0);
        Assert.Equal("salutation", reply.Theme);
        Assert.Null(reply.Typo);
        Assert.Contains(reply.Text, Book.Theme("salutation")!.Phrases["fr"].Select(p => p.Replace("{nom}", "Bob")));
    }

    // ── Fautes volontaires ──

    [Fact]
    public void Faute_jamais_sur_le_pseudo_et_une_seule()
    {
        var random = new Random(7);
        const string text = "Salut TheKrazyTurkey, je coupe du bois et je reviens.";
        for (var i = 0; i < 300; i++)
        {
            var typo = ChatTypos.Apply(text, "fr", "TheKrazyTurkey", random);
            Assert.NotNull(typo);
            Assert.Contains("TheKrazyTurkey", typo.Text);
            Assert.NotEqual(text, typo.Text);
            // Un seul mot changé (les mots joints comme « alot » mis à part, ici en français).
            var before = text.Split(' ');
            var after = typo.Text.Split(' ');
            Assert.Equal(before.Length, after.Length);
            Assert.Equal(1, before.Zip(after).Count(p => p.First != p.Second));
        }
    }

    [Fact]
    public void Fautes_de_grammaire_courantes()
    {
        var seen = new HashSet<string>();
        var random = new Random(3);
        for (var i = 0; i < 400; i++)
        {
            if (ChatTypos.Apply("Bob, c'est a toi de voir.", "fr", "Bob", random) is { Description: var d } && d.EndsWith("(grammaire)", StringComparison.Ordinal))
            {
                seen.Add(d);
            }
        }
        Assert.Contains("« s'est » au lieu de « c'est » (grammaire)", seen);
        Assert.Contains("« à » au lieu de « a » (grammaire)", seen);
        var en = ChatTypos.Apply("your axe", "en", "Bob", new Random(0));
        Assert.NotNull(en);
    }

    [Fact]
    public void Faute_environ_5_pourcent_des_reponses()
    {
        var book = ChatPhraseBook.LoadEmbedded();
        var random = new Random(11);
        var count = Enumerable.Range(0, 4000).Count(_ => ChatReplies.For(Msg("@gpt salut", "fr/Bob"), book, random, smileyChance: 0).Typo is not null);
        Assert.InRange(count, 130, 280); // 5 % de 4000 = 200
    }
}
