using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.7 : « @gpt » / « @chatgpt » dans Local, Région, LFG et Trade ; réponse dans le même canal (commandes du jeu) ;
// 30 phrases de plus, smiley 30 % ; temps de lecture ; frappe irrégulière ; minuteur avant la prochaine réponse.
public class V047Tests
{
    private const ulong Me = 648518346637266951;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_795_000_000);

    private static ChatMessage Msg(ulong id, string text, DateTimeOffset at, int channel = 3, string user = "en/Bob") =>
        new(id, 42, user.Split('/')[1], user.Split('/')[0], channel, text, at);

    [Theory]
    [InlineData(2, "/s", "LOCAL")]
    [InlineData(3, "/y", "REGION")]
    [InlineData(7, "/lfg", "GROUP")]
    [InlineData(8, "/t", "TRADE")]
    public void Commande_et_bouton_de_chaque_canal(int channel, string command, string word)
    {
        Assert.Equal(command, ChatChannels.Command(channel));
        Assert.Equal(word, ChatChannels.ButtonWord(channel));
        Assert.True(ChatChannels.IsAnswered(channel));
    }

    [Fact]
    public void Canal_systeme_jamais_repondu()
    {
        Assert.False(ChatChannels.IsAnswered(0));
        Assert.Null(ChatChannels.Command(0));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(8)]
    public void Repond_aussi_hors_de_la_region(int channel)
    {
        var picker = new GptChatPicker(Me);
        var m = picker.Pick([Msg(1, "@chatgpt hello", Now, channel)], Now);
        Assert.Equal(channel, m!.Channel);
    }

    [Fact]
    public void Abonnement_une_requete_par_canal()
    {
        var queries = ChatMessages.AnsweredQueriesSince(1_795_000_000);
        Assert.Equal(4, queries.Count);
        Assert.Contains("SELECT * FROM chat_message_state WHERE channel_id = 2 AND timestamp > 1795000000", queries);
        Assert.Contains("SELECT * FROM chat_message_state WHERE channel_id = 8 AND timestamp > 1795000000", queries);
    }

    [Fact]
    public void Minuteur_avant_la_prochaine_reponse()
    {
        var picker = new GptChatPicker(Me);
        Assert.Equal(TimeSpan.Zero, picker.Cooldown(Now));
        picker.Pick([Msg(1, "@gpt", Now)], Now);
        Assert.Equal(TimeSpan.FromSeconds(40), picker.Cooldown(Now.AddSeconds(20)));
        Assert.Equal(TimeSpan.Zero, picker.Cooldown(Now.AddSeconds(61)));
    }

    [Fact]
    public void Temps_de_lecture_selon_la_longueur_du_message()
    {
        var random = new Random(3);
        for (var i = 0; i < 50; i++)
        {
            var shortRead = GptChatPicker.ReadingTime("@gpt", random);
            Assert.InRange(shortRead.TotalSeconds, 2.2, 4.3);
            var longRead = GptChatPicker.ReadingTime(new string('a', 500), random);
            Assert.InRange(longRead.TotalSeconds, 8.0, 10.0); // plafonné
        }
    }

    [Fact]
    public void Quarante_phrases_par_langue_toutes_differentes()
    {
        Assert.Equal(40, CannedReplies.French.Count);
        Assert.Equal(40, CannedReplies.English.Count);
        Assert.Equal(40, CannedReplies.French.Distinct().Count());
        Assert.Equal(40, CannedReplies.English.Distinct().Count());
    }

    // D-055 : jamais d'allusion au fait que c'est un bot.
    [Fact]
    public void Aucune_phrase_ne_parle_de_bot()
    {
        var forbidden = new[] { "bot", "robot", " ia ", " ai ", "circuit", "bip", "beep", "program", "intelligence" };
        foreach (var p in CannedReplies.French.Concat(CannedReplies.English))
        {
            var lower = " " + p.ToLowerInvariant() + " ";
            Assert.DoesNotContain(forbidden, f => lower.Contains(f, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Smiley_ajoute_selon_la_chance()
    {
        var m = Msg(1, "@gpt", Now);
        var always = CannedReplies.For(m, new Random(5), smileyChance: 1);
        Assert.Contains(CannedReplies.Smileys, s => always.EndsWith(" " + s, StringComparison.Ordinal));
        var never = CannedReplies.For(m, new Random(5), smileyChance: 0);
        Assert.DoesNotContain(CannedReplies.Smileys, s => never.EndsWith(" " + s, StringComparison.Ordinal));

        // Environ 30 % sur 2000 tirages.
        var random = new Random(9);
        var count = Enumerable.Range(0, 2000).Count(_ =>
            CannedReplies.Smileys.Any(s => CannedReplies.For(m, random, smileyChance: CannedReplies.SmileyChance).EndsWith(" " + s, StringComparison.Ordinal)));
        Assert.InRange(count, 500, 700);
    }

    [Theory]
    [InlineData("LOCAL", "LOCAL", true)]
    [InlineData("L0CAL", "LOCAL", true)]
    [InlineData("TRADE", "TRADE", true)]
    [InlineData("LOOKING FOR GROUP", "GROUP", true)]
    [InlineData("REGION", "LOCAL", false)]
    [InlineData("LOCAL", "REGION", false)]
    [InlineData("", "TRADE", false)]
    public void Bouton_du_canal_reconnu(string read, string target, bool expected) =>
        Assert.Equal(expected, ChatInput.IsChannel(read, target));

    // ── Envoi avec la commande du canal ──

    private sealed class FakeDevice : IInputDevice
    {
        public readonly List<string> Events = [];
        public bool ChatOpen;

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
        public void TypeChar(char c) => Events.Add(c.ToString());
        public string Typed => string.Concat(Events.Where(e => e.Length == 1));
    }

    private static (ActionController Controller, FakeDevice Device, List<int> Delays) Make()
    {
        var device = new FakeDevice();
        var delays = new List<int>();
        var context = new ActionContext
        {
            GameWindow = () => new IntPtr(42),
            GameVisible = () => true,
            LastPhysicalKey = () => DateTime.MinValue,
        };
        var controller = new ActionController(device, context, random: new Random(1), delay: (ms, token) =>
        {
            token.ThrowIfCancellationRequested();
            delays.Add(ms);
            return Task.CompletedTask;
        });
        return (controller, device, delays);
    }

    private static ChatProbe Probe(FakeDevice device, Func<string> channel, string target) => new()
    {
        IsOpen = () => Task.FromResult<bool?>(device.ChatOpen),
        Channel = () => Task.FromResult(channel()),
        ChannelOk = t => ChatInput.IsChannel(t, target),
    };

    [Fact]
    public async Task Bon_canal_deja_affiche_pas_de_commande()
    {
        var (controller, device, _) = Make();
        var result = await controller.ChatAsync("Réponse", "Salut", Probe(device, () => "REGION", "REGION"), "/y");
        Assert.True(result.Done, result.Detail);
        Assert.Equal("Salut", device.Typed);
    }

    [Fact]
    public async Task Autre_canal_commande_tapee_puis_le_bouton_change()
    {
        var (controller, device, _) = Make();
        var probe = Probe(device, () => device.Typed.StartsWith("/s ", StringComparison.Ordinal) ? "LOCAL" : "REGION", "LOCAL");
        var result = await controller.ChatAsync("Réponse", "Salut", probe, "/s");
        Assert.True(result.Done, result.Detail);
        Assert.Equal("/s Salut", device.Typed);
        Assert.False(device.ChatOpen);
    }

    [Fact]
    public async Task Bouton_pas_change_la_commande_reste_au_debut_du_message()
    {
        var (controller, device, _) = Make();
        var result = await controller.ChatAsync("Réponse", "Salut", Probe(device, () => "REGION", "TRADE"), "/t");
        Assert.True(result.Done, result.Detail);
        Assert.Equal("/t Salut", device.Typed);
    }

    [Theory]
    [InlineData("y")]
    [InlineData("/Y")]
    [InlineData("/y x")]
    [InlineData("/toolong")]
    public async Task Commande_bizarre_refusee(string command)
    {
        var (controller, device, _) = Make();
        var result = await controller.ChatAsync("Réponse", "Salut", Probe(device, () => "LOCAL", "REGION"), command);
        Assert.Equal(ActionOutcome.Refuse, result.Outcome);
        Assert.Empty(device.Events);
    }

    [Fact]
    public void Frappe_irreguliere_comme_une_main()
    {
        var (controller, _, _) = Make();
        var letters = Enumerable.Range(0, 400).Select(_ => controller.KeyDelay('a', 'b')).ToList();
        Assert.All(letters, d => Assert.InRange(d, 55, 165 + 900));
        Assert.True(letters.Distinct().Count() > 50); // pas toujours le même temps
        Assert.Contains(letters, d => d > 300); // quelques hésitations
        var afterDot = Enumerable.Range(0, 100).Select(_ => controller.KeyDelay('O', '.')).ToList();
        Assert.All(afterDot, d => Assert.True(d >= 205));
    }
}
