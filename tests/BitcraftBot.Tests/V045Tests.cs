using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// v0.4.5 : chat ouvert reconnu à l'écran, réponse « @GPT » écrite au clavier (chat refermé et vérifié).
public class V045Tests
{
    private static readonly string ChatFolder = Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui", "chat");

    // Découpe 520 × 80 (prise en (0, 940) d'une capture 1920 × 1080) remise à sa place sur une image noire.
    private static async Task<Frame> ChatFrameAsync(string name)
    {
        var crop = await HudReaderReferenceTests.LoadFrameAsync(Path.Combine(ChatFolder, name));
        var pixels = new byte[1920 * 1080 * 4];
        for (var y = 0; y < crop.Height; y++)
        {
            Array.Copy(crop.Pixels, y * crop.Stride, pixels, ((940 + y) * 1920) * 4, crop.Stride);
        }
        return new Frame { Pixels = pixels, Width = 1920, Height = 1080, CapturedAt = DateTime.Now, Source = new ScreenRect(0, 0, 1920, 1080) };
    }

    [Fact]
    public async Task Chat_ouvert_reconnu_sur_la_capture_du_23_11()
    {
        var frame = await ChatFrameAsync("ouvert-region_001.png");
        Assert.True(ChatInput.IsOpen(frame), $"score {ChatInput.Score(frame):0.00}");
    }

    [Theory]
    [InlineData("ferme-messages_001.png")]
    [InlineData("ferme-sans-message_001.png")]
    public async Task Chat_ferme_reconnu(string name)
    {
        var frame = await ChatFrameAsync(name);
        Assert.False(ChatInput.IsOpen(frame), $"score {ChatInput.Score(frame):0.00}");
    }

    // Aucune capture de référence (jeu, fenêtres, écrans) n'a le chat ouvert.
    public static IEnumerable<object[]> FullCaptures() =>
        Directory.GetFiles(Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "images", "reference-ui"), "*.png", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "chat" + Path.DirectorySeparatorChar))
            .Order().Select(f => new object[] { Path.GetRelativePath(HudReaderReferenceTests.FindRepoRoot(), f) });

    [Theory]
    [MemberData(nameof(FullCaptures))]
    public async Task Pas_de_chat_ouvert_sur_les_autres_captures(string relative)
    {
        var frame = await HudReaderReferenceTests.LoadFrameAsync(Path.Combine(HudReaderReferenceTests.FindRepoRoot(), relative));
        if (frame.Width != 1920 || frame.Height != 1080)
        {
            return; // découpes (coordonnées, jauge…) : pas une image entière
        }
        Assert.False(ChatInput.IsOpen(frame), $"score {ChatInput.Score(frame):0.00}");
    }

    // Le bouton du canal se lit par OCR (Windows, anglais) sur la vraie capture.
    [Fact]
    public async Task Canal_region_lu_par_ocr_sur_la_capture()
    {
        var ocr = BitcraftBot.Perception.Ocr.OcrReader.TryCreate();
        if (ocr is null)
        {
            return; // OCR anglais absent sur ce PC
        }
        var frame = await ChatFrameAsync("ouvert-region_001.png");
        var text = await ocr.ReadAsync(frame, ChatInput.ChannelZone, BitcraftBot.Perception.Ocr.TextStyle.BrightText, 150);
        Assert.True(ChatInput.IsRegion(text), $"lu « {text} »");
    }

    [Theory]
    [InlineData("REGION", true)]
    [InlineData("REG1ON", true)]
    [InlineData("RECION", true)]
    [InlineData("region", true)]
    [InlineData("LOCAL", false)]
    [InlineData("TRADE", false)]
    [InlineData("", false)]
    public void Canal_region_lu(string text, bool expected) => Assert.Equal(expected, ChatInput.IsRegion(text));

    [Fact]
    public void Requete_du_chat_limitee_aux_messages_recents() =>
        Assert.Equal("SELECT * FROM chat_message_state WHERE channel_id = 3 AND timestamp > 1795000000", ChatMessages.RegionQuerySince(1_795_000_000));

    // ── Envoi dans le chat ──

    private sealed class FakeDevice : IInputDevice
    {
        public readonly List<string> Events = [];
        public bool ChatOpen;
        public bool EnterOpensChat = true;
        public bool EnterClosesChat = true;
        public bool EscapeClosesChat = true;
        public Action<char>? OnType;

        public (int X, int Y) CursorPosition() => (0, 0);
        public bool IsForeground(IntPtr window) => true;
        public void MoveTo(int x, int y) => Events.Add("move");
        public void LeftDown() => Events.Add("left down");
        public void LeftUp() => Events.Add("left up");

        public void KeyDown(ushort scanCode)
        {
            Events.Add(scanCode == GameKeys.Enter ? "Entrée" : scanCode == GameKeys.Escape ? "Échap" : $"down {scanCode}");
            if (scanCode == GameKeys.Enter)
            {
                ChatOpen = ChatOpen ? !EnterClosesChat : EnterOpensChat;
            }
            else if (scanCode == GameKeys.Escape && EscapeClosesChat)
            {
                ChatOpen = false;
            }
        }

        public void KeyUp(ushort scanCode) { }

        public void TypeChar(char c)
        {
            Events.Add(c.ToString());
            OnType?.Invoke(c);
        }

        public string Typed => string.Concat(Events.Where(e => e.Length == 1));
    }

    private static readonly Func<int, CancellationToken, Task> NoWait = (_, token) =>
    {
        token.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    };

    private static (ActionController Controller, FakeDevice Device, ChatProbe Probe, List<string> Log) Make(
        string channel = "REGION", Func<DateTime>? lastKey = null)
    {
        var device = new FakeDevice();
        var context = new ActionContext
        {
            GameWindow = () => new IntPtr(42),
            GameVisible = () => true,
            LastPhysicalKey = lastKey ?? (() => DateTime.MinValue),
        };
        var controller = new ActionController(device, context, random: new Random(1), delay: NoWait);
        var log = new List<string>();
        controller.Log += (level, message) => log.Add($"{level}: {message}");
        var probe = new ChatProbe
        {
            IsOpen = () => Task.FromResult<bool?>(device.ChatOpen),
            Channel = () => Task.FromResult(channel),
            ChannelOk = ChatInput.IsRegion,
        };
        return (controller, device, probe, log);
    }

    [Fact]
    public async Task Message_ecrit_puis_chat_referme()
    {
        var (controller, device, probe, log) = Make();
        var result = await controller.ChatAsync("Réponse", "Bip boup, Toto. Ça va ?", probe);
        Assert.True(result.Done, result.Detail);
        Assert.Equal("Entrée", device.Events[0]);
        Assert.Equal("Entrée", device.Events[^1]);
        Assert.Equal("Bip boup, Toto. Ça va ?", device.Typed);
        Assert.False(device.ChatOpen);
        Assert.DoesNotContain(log, l => l.StartsWith("Alerte"));
    }

    [Fact]
    public async Task Rien_si_le_chat_est_deja_ouvert()
    {
        var (controller, device, probe, _) = Make();
        device.ChatOpen = true;
        var result = await controller.ChatAsync("Réponse", "Salut", probe);
        Assert.False(result.Done);
        Assert.Empty(device.Events);
        Assert.True(device.ChatOpen); // le chat de Julien n'est pas touché
    }

    [Fact]
    public async Task Rien_tape_si_le_chat_ne_s_ouvre_pas()
    {
        var (controller, device, probe, _) = Make();
        device.EnterOpensChat = false;
        var result = await controller.ChatAsync("Réponse", "Salut", probe);
        Assert.False(result.Done);
        Assert.Equal("", device.Typed);
        Assert.DoesNotContain("Échap", device.Events); // chat fermé : pas d'Échap (ouvrirait le menu)
    }

    [Fact]
    public async Task Mauvais_canal_rien_tape_et_chat_referme()
    {
        var (controller, device, probe, _) = Make(channel: "LOCAL");
        var result = await controller.ChatAsync("Réponse", "Salut", probe);
        Assert.False(result.Done);
        Assert.Contains("canal", result.Detail);
        Assert.Equal("", device.Typed);
        Assert.Contains("Échap", device.Events);
        Assert.False(device.ChatOpen);
    }

    [Fact]
    public async Task Julien_tape_pendant_la_frappe_arret_et_chat_referme()
    {
        var keyAt = DateTime.MinValue;
        var (controller, device, probe, _) = Make(lastKey: () => keyAt);
        device.OnType = c =>
        {
            if (c == 'l')
            {
                keyAt = DateTime.Now.AddSeconds(1);
            }
        };
        var result = await controller.ChatAsync("Réponse", "Salut à tous", probe);
        Assert.Equal(ActionOutcome.Interrompu, result.Outcome);
        Assert.Equal("Sal", device.Typed);
        Assert.Equal("Échap", device.Events[^1]);
        Assert.False(device.ChatOpen);
    }

    [Fact]
    public async Task Chat_reste_ouvert_apres_envoi_echap_puis_alerte_si_rien()
    {
        var (controller, device, probe, log) = Make();
        device.EnterClosesChat = false;
        device.EscapeClosesChat = false;
        var result = await controller.ChatAsync("Réponse", "Salut", probe);
        Assert.False(result.Done);
        Assert.Equal(2, device.Events.Count(e => e == "Échap"));
        Assert.True(controller.IsHalted); // chat peut-être resté ouvert : plus aucune action
    }

    [Fact]
    public async Task Texte_trop_long_ou_vide_refuse()
    {
        var (controller, device, probe, _) = Make();
        Assert.False((await controller.ChatAsync("Réponse", new string('a', 251), probe)).Done);
        Assert.False((await controller.ChatAsync("Réponse", "  ", probe)).Done);
        Assert.False((await controller.ChatAsync("Réponse", "a\nb", probe)).Done);
        Assert.Empty(device.Events);
    }

    [Fact]
    public async Task Entree_toujours_refusee_hors_du_chat()
    {
        var (controller, device, _, _) = Make();
        Assert.Equal(ActionOutcome.Refuse, (await controller.PressAsync("Essai", GameKeys.Enter)).Outcome);
        Assert.Empty(device.Events);
    }
}
