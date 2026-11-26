using System.Windows;
using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Module « @GPT » (v0.4.5, D-054, D-055) : quand un joueur écrit « @GPT » dans le chat, le bot répond
// une phrase simple, au clavier, comme Julien (jamais par le serveur, D-039).
// v0.4.7 : « @gpt » ou « @chatgpt » (majuscules ou minuscules), dans Local, Région, Looking For Group ou Trade ;
// réponse dans le même canal (commandes /s, /y, /lfg, /t) ; temps de lecture avant de répondre ; smiley 30 %.
// Règles : 1 réponse par minute au plus, au message le plus récent ; jamais aux messages de Julien ;
// pas d'IA (phrases toutes faites) ; le chat est toujours refermé et vérifié à l'écran.
// Sécurités du contrôleur d'actions : jeu actif et visible, en jeu, aucune fenêtre du jeu ouverte,
// chat fermé avant de commencer, Julien ne tape pas (une touche de Julien pendant la frappe = arrêt).
public partial class MainWindow
{
    private const int ChatLogTextMax = 80;

    private readonly Random _gptRandom = new();

    // Base de phrases par thème (v0.4.8, fichiers World/Phrases/*.txt intégrés au .exe) ;
    // v0.4.9 : un fichier fr.txt ou en.txt dans « phrases-chat » à côté du .exe le remplace.
    private ChatPhraseBook _gptPhrases = ChatPhraseBook.LoadEmbedded();
    private bool _gptEnabled = true;
    private GptChatPicker? _gptPicker;
    private ulong _gptPickerFor;
    private GptReply? _gptPending;
    private volatile bool _gptSending;
    private string _gptLastState = "";
    private int _gptSent;

    // ReadyAt : fin du temps de lecture (on ne répond pas à la seconde où le message arrive).
    // Correction : « *mot » à envoyer juste après, quand la réponse a une faute volontaire (v0.4.9).
    private sealed record GptReply(ChatMessage Message, string Text, DateTimeOffset ReadyAt, string? Correction = null, string Theme = "autre");

    // v0.4.10 : amis et liste noire (fichiers du dossier phrases-chat), historique et statistiques (onglet CHAT),
    // vraies infos du jeu (région, plus haut niveau) lues sur le serveur.
    private ChatLists _gptLists = new();
    private ChatStats _gptStats = new();
    private ChatInfo? _gptInfo;

    // v0.4.8 : dernier vrai clic de Julien (heure en ticks) et dernier moment où le jeu n'était pas au premier plan.
    // Après un retour dans le jeu ou un clic, le bot attend 2 s avant d'ouvrir le chat (journal du 24/11 00:03).
    private static readonly TimeSpan GptCalmTime = TimeSpan.FromSeconds(2);
    private long _gptLastClickTicks = DateTime.MinValue.Ticks;
    private DateTime _gptGameInactiveAt = DateTime.MinValue;

    private DateTime GptLastClick => new(Interlocked.Read(ref _gptLastClickTicks));

    private void InitGpt()
    {
        // Écoute de la souris déjà ouverte par la caméra (lecture seule) : on note seulement l'heure des vrais clics.
        if (_mouseHook is not null)
        {
            _mouseHook.ButtonDown += (_, _, _, injected) =>
            {
                if (!injected)
                {
                    Interlocked.Exchange(ref _gptLastClickTicks, DateTime.Now.Ticks);
                }
            };
        }
        GptCheck.IsChecked = _gptEnabled;
        _log.Info("Chat", "Réponses « @gpt » ou « @chatgpt » activées (Local, Région, Looking For Group, Trade ; réponse dans le même canal ; 1 par minute au plus ; phrases par thème, smiley 30 %, faute volontaire 5 %).");
        var folder = System.IO.Path.Combine(AppContext.BaseDirectory, ChatPhraseBook.FolderName);
        _gptPhrases = ChatPhraseBook.Load(folder, out var overridden);
        var phrases = _gptPhrases.Themes.Sum(t => t.Phrases.Values.Sum(l => l.Count));
        _log.Info("Chat", $"Base de phrases : {_gptPhrases.Themes.Count} thèmes, {phrases} phrases"
            + (overridden.Count > 0 ? $" (fichier(s) {string.Join(", ", overridden.Select(l => l + ".txt"))} du dossier « {ChatPhraseBook.FolderName} » utilisé(s))." : " (phrases intégrées au .exe)."));
        foreach (var error in _gptPhrases.Errors)
        {
            _log.Alerte("Chat", $"Base de phrases : {error}.");
        }
        _gptLists = ChatLists.Load(folder);
        _gptStats = new ChatStats(folder);
        _log.Info("Chat", $"Amis : {_gptLists.Friends.Count} ; liste noire : {_gptLists.Blocked.Count} (fichiers {ChatLists.FriendsFile} et {ChatLists.BlockedFile}). "
            + $"Plafonds : {ChatQuota.PerHour} réponses par heure, {ChatQuota.PerDay} par jour, {ChatQuota.PerPlayerPerHour} par joueur et par heure. "
            + $"Pause de {GptChatPicker.PauseAfterOwnMessage.TotalMinutes:0} min quand tu écris toi-même dans le chat.");
        ShowGptTab();
        ShowGptState("en attente du serveur");
        ShowGptTimer(null);
    }

    // ── Onglet CHAT (v0.4.10) : historique, statistiques, test sans envoi ──
    private void AddGptHistory(ChatMessage m, string theme, string reply, string status)
    {
        _gptStats.Add(new ChatHistoryEntry(DateTimeOffset.Now, ChatChannels.Name(m.Channel), m.Username, m.Text, theme, reply, status));
        ShowGptTab();
    }

    private void ShowGptTab()
    {
        var now = DateTimeOffset.UtcNow;
        var q = _gptPicker?.Quota;
        ChatTabStatus.Text = (_gptEnabled ? "Chatbot activé." : "Chatbot coupé.")
            + $" Réponses : {q?.LastHour(now) ?? 0}/{ChatQuota.PerHour} cette heure, {q?.LastDay(now) ?? 0}/{ChatQuota.PerDay} sur 24 h."
            + (_gptPicker is { } p && now < p.PausedUntil ? $" En pause jusqu'à {p.PausedUntil.ToLocalTime():HH:mm:ss} (tu as écrit dans le chat)." : "");
        ChatTabStats.Text = _gptStats.Summary();
        ChatHistoryBox.Text = string.Join(Environment.NewLine, _gptStats.History.Reverse().Select(h =>
            $"{h.At.LocalDateTime:HH:mm:ss}  [{h.Channel}] {h.User} : « {Short(h.Message)} »"
            + (h.Reply.Length > 0 ? Environment.NewLine + $"      → {(h.Theme.Length > 0 ? h.Theme + " : " : "")}« {h.Reply} »" : "")
            + $"  ({h.Status})"));
    }

    // Bouton « Tester une phrase » : montre le thème et la réponse, sans rien écrire dans le jeu ni compter dans les statistiques.
    private void ChatTestButton_Click(object sender, RoutedEventArgs e)
    {
        var text = ChatTestBox.Text.Trim();
        if (text.Length == 0)
        {
            ChatTestResult.Text = "Écris d'abord un message de joueur (par exemple « @gpt salut, tu coupes des arbres ? »).";
            return;
        }
        var channel = (int)ChatChannel.Region;
        var message = new ChatMessage(0, 0, "Testeur", "", channel, text, DateTimeOffset.UtcNow);
        var reply = ChatReplies.For(message, _gptPhrases, _gptRandom, info: _gptInfo, hour: DateTime.Now.Hour);
        ChatTestResult.Text = $"Thème : {reply.Theme}. Réponse : « {reply.Text} »"
            + (reply.Typo is { } typo ? $" (faute volontaire : {typo})" : "")
            + (reply.Correction is { } fix ? $" puis « {fix} »" : "")
            + ". Rien n'a été envoyé.";
        _gptStats.Add(new ChatHistoryEntry(DateTimeOffset.Now, "test", "Testeur", text, reply.Theme, reply.Text, "test, rien envoyé"));
        ShowGptTab();
    }

    // v0.4.16 : le chatbot (statistiques, test, historique) est replié sous l'archive du chat.
    private void ChatBotToggle_Click(object sender, RoutedEventArgs e)
    {
        var show = ChatBotPanel.Visibility != Visibility.Visible;
        ChatBotPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        ChatBotToggle.Content = show ? "Masquer le chatbot @gpt" : "Afficher le chatbot @gpt";
    }

    // Minuteur de la carte CHAT : temps avant la prochaine réponse possible (v0.4.7, demande de Julien).
    private void ShowGptTimer(TimeSpan? cooldown)
    {
        GptTimerText.Text = cooldown switch
        {
            null => "",
            { TotalSeconds: < 1 } => "Prochaine réponse possible : maintenant.",
            var c => $"Prochaine réponse possible dans {Math.Ceiling(c.Value.TotalSeconds):0} s.",
        };
    }

    private void GptCheck_Click(object sender, RoutedEventArgs e)
    {
        _gptEnabled = GptCheck.IsChecked == true;
        _gptPending = null;
        ShowGptTab();
        _log.Info("Chat", _gptEnabled ? "Réponses « @GPT » activées (case cochée)." : "Réponses « @GPT » coupées (case décochée).");
        ShowGptState(_gptEnabled ? "activé" : "coupé");
    }

    // Appelé toutes les 500 ms avec la dernière lecture du serveur (liaison active).
    private void RunGpt(ServerReader reader, PlayerSnapshot s)
    {
        if (!_gptEnabled)
        {
            ShowGptState("coupé");
            ShowGptTimer(null);
            return;
        }
        if (reader.State != ReaderState.Connecte || s.EntityId is not { } me)
        {
            ShowGptState("en attente du serveur");
            ShowGptTimer(null);
            return;
        }
        if (_gptPicker is null || _gptPickerFor != me)
        {
            _gptPicker = new GptChatPicker(me) { Lists = _gptLists };
            _gptPickerFor = me;
        }
        _gptInfo = new ChatInfo(string.IsNullOrWhiteSpace(s.RegionName) ? null : s.RegionName, s.Skills is { Count: > 0 } sk ? sk.Max(x => x.Level) : null);
        var now = DateTimeOffset.UtcNow;
        if (!GameIsForeground())
        {
            _gptGameInactiveAt = DateTime.Now;
        }
        ShowGptTimer(_gptPicker.Cooldown(now));

        // 1. Un nouveau message « @gpt » ?
        // Julien écrit lui-même dans le chat : pause (v0.4.10).
        var chat = reader.ChatMessages();
        if (!_gptSending)
        {
            _gptPicker.ObserveOwn(chat);
            if (_gptPending is { } waiting && now < _gptPicker.PausedUntil)
            {
                _log.Info("Chat", $"Réponse à {waiting.Message.Username} abandonnée : tu as écrit dans le chat, le chatbot fait une pause.");
                AddGptHistory(waiting.Message, waiting.Theme, waiting.Text, "abandonnée (pause : tu as écrit)");
                _gptPending = null;
            }
        }
        var picked = _gptPending is null && !_gptSending ? _gptPicker.Pick(chat, now) : null;
        foreach (var (skipped, reason) in _gptPicker.TakeSkipped())
        {
            _log.Info("Chat", $"« @gpt » de {skipped.Username} ({ChatChannels.Name(skipped.Channel)}) sans réponse : {reason}.");
            if (skipped.OwnerEntityId != me)
            {
                AddGptHistory(skipped, "", "", "sans réponse : " + reason);
            }
        }
        foreach (var bot in _gptPicker.TakeIgnored())
        {
            _log.Info("Chat", $"« @gpt » de {bot.Username} ignoré : ressemble à un autre bot (« {Short(bot.Text)} »).");
        }
        if (picked is { } m)
        {
            var friend = _gptLists.IsFriend(m.Username);
            var reply = ChatReplies.For(m, _gptPhrases, _gptRandom, info: _gptInfo, hour: DateTime.Now.Hour, friend: friend);
            var text = reply.Text;
            _gptPending = new GptReply(m, text, now + GptChatPicker.ReadingTime(m.Text, _gptRandom), reply.Correction, reply.Theme);
            _gptStats.Count(new ChatHistoryEntry(DateTimeOffset.Now, ChatChannels.Name(m.Channel), m.Username, m.Text, reply.Theme, text, "reçu"));
            ShowGptTab();
            _log.Info("Chat", $"« @gpt » de {m.Username} ({ChatChannels.Name(m.Channel)}) : « {Short(m.Text)} ». Thème : {reply.Theme}.{(friend ? " Ami : ton familier." : "")} Réponse prévue dans {ChatChannels.Name(m.Channel)} ({ChatChannels.Command(m.Channel)}) : « {text} »."
                + (reply.Typo is { } typo ? $" Faute volontaire : {typo}." : "")
                + (reply.Correction is { } fix ? $" Correction prévue juste après : « {fix} »." : ""));
            ShowGptTimer(_gptPicker.Cooldown(now));
        }
        if (_gptPending is not { } p || _gptSending)
        {
            ShowGptState(_gptSending ? "écrit dans le chat…" : $"à l'écoute ({_gptSent} réponse(s) envoyée(s))");
            return;
        }

        // 2. Trop tard : le message est trop vieux.
        if (now - p.Message.SentAt > GptChatPicker.MaxAge)
        {
            _log.Info("Chat", $"Réponse à {p.Message.Username} abandonnée : pas pu l'écrire à temps ({_gptLastState}).");
            AddGptHistory(p.Message, p.Theme, p.Text, "abandonnée (pas écrite à temps)");
            _gptPending = null;
            return;
        }

        // 3. Temps de lecture du message (comme quelqu'un qui lit, puis réfléchit).
        if (now < p.ReadyAt)
        {
            ShowGptState($"lit le message de {p.Message.Username} ({Math.Ceiling((p.ReadyAt - now).TotalSeconds):0} s)");
            return;
        }

        // 4. Le jeu est-il prêt ? (personnage connecté, pas de repas en cours, sécurités du contrôleur)
        var wait = s.SignedIn != true ? "personnage pas connecté dans le jeu"
            : _pendingMeal is not null ? "repas en cours"
            : _actions.WhyNot()
              ?? (DateTime.Now - _gptGameInactiveAt < GptCalmTime ? "le jeu vient de revenir au premier plan"
                  : DateTime.Now - GptLastClick < GptCalmTime ? "clic de souris récent"
                  : null);
        if (wait is not null)
        {
            ShowGptState($"réponse en attente : {wait}");
            return;
        }
        _ = SendGptAsync(p);
    }

    private async Task SendGptAsync(GptReply reply)
    {
        _gptSending = true;
        ShowGptState("écrit dans le chat…");
        try
        {
            var channel = reply.Message.Channel;
            var result = await _actions.ChatAsync("Réponse « @GPT »", reply.Text, ChatProbeForGame(channel), ChatChannels.Command(channel));
            if (result.Done)
            {
                _gptSent++;
                _gptPicker?.NoteSent(reply.Text);
                _log.Info("Chat", $"Réponse envoyée à {reply.Message.Username} dans {ChatChannels.Name(channel)} : « {reply.Text} ».");
                AddGptHistory(reply.Message, reply.Theme, reply.Text, "envoyée");
                if (reply.Correction is { } fix)
                {
                    await SendGptCorrectionAsync(reply.Message.Username, fix, channel);
                }
            }
            else
            {
                _log.Alerte("Chat", $"Réponse à {reply.Message.Username} pas envoyée : {result.Detail}.");
                AddGptHistory(reply.Message, reply.Theme, reply.Text, "pas envoyée : " + result.Detail);
            }
        }
        finally
        {
            // Une seule tentative par message : pas de boucle si le jeu refuse.
            _gptPending = null;
            _gptSending = false;
        }
    }

    // v0.4.9 (amélioration 2) : correction « *mot » 1,5 à 4 s après la réponse, comme un vrai joueur.
    // Mêmes sécurités ; si le jeu n'est pas prêt à ce moment-là, la correction est abandonnée (pas d'attente).
    private async Task SendGptCorrectionAsync(string user, string fix, int channel)
    {
        await Task.Delay(1500 + _gptRandom.Next(2500));
        var why = _actions.WhyNot() ?? (DateTime.Now - GptLastClick < GptCalmTime ? "clic de souris récent" : null);
        if (why is not null)
        {
            _log.Info("Chat", $"Correction « {fix} » pas envoyée : {why}.");
            return;
        }
        var result = await _actions.ChatAsync("Correction « @GPT »", fix, ChatProbeForGame(channel), ChatChannels.Command(channel));
        if (result.Done)
        {
            _gptPicker?.NoteSent(fix);
            _log.Info("Chat", $"Correction envoyée à {user} dans {ChatChannels.Name(channel)} : « {fix} ».");
        }
        else
        {
            _log.Alerte("Chat", $"Correction « {fix} » pas envoyée : {result.Detail}.");
        }
    }

    // Ce que l'envoi regarde à l'écran : image neuve du jeu (jamais d'une autre fenêtre de Julien).
    // channel : canal où répondre ; le bouton du chat doit afficher son nom (« LOCAL », « REGION »…).
    private ChatProbe ChatProbeForGame(int channel) => new()
    {
        IsOpen = () => Task.Run(() => CaptureGameNow() is { } frame ? ChatInput.IsOpen(frame) : (bool?)null),
        Channel = async () =>
        {
            if (_ocr is null || CaptureGameNow() is not { } frame)
            {
                return "";
            }
            var sx = frame.Width / 1920.0;
            var sy = frame.Height / 1080.0;
            var z = ChatInput.ChannelZone;
            var zone = new ScreenRect((int)(z.X * sx), (int)(z.Y * sy), (int)(z.Width * sx), (int)(z.Height * sy));
            return ChatInput.CleanChannel(await _ocr.ReadAsync(frame, zone, TextStyle.BrightText, 150));
        },
        ChannelOk = text => ChatInput.IsChannel(text, ChatChannels.ButtonWord(channel)),
        LastUserClick = () => GptLastClick,
    };

    private Frame? CaptureGameNow()
    {
        if (CurrentGame() is not { } game || !GameVisibility.IsUncovered(game))
        {
            return null;
        }
        try
        {
            return new ScreenCapturer().Capture(GameWindowLocator.GetClientArea(game.Handle));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    private void ShowGptState(string text)
    {
        GptStatusText.Text = text[..1].ToUpperInvariant() + text[1..] + ".";
        // Journal : seulement quand l'état change (sans les chiffres).
        var key = new string(text.Where(c => !char.IsDigit(c)).ToArray());
        if (key != _gptLastState)
        {
            _gptLastState = key;
            _log.Detail("Chat", text);
        }
    }

    private static string Short(string text)
    {
        var t = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return t.Length > ChatLogTextMax ? t[..ChatLogTextMax] + "…" : t;
    }

    // ── Chat ouvert par Julien (W tapé dans le chat, 23/11 à 22:32) ──
    // L'écran est regardé une fois par seconde ; entre deux regards, Entrée et Échap tapées par Julien font foi.
    private bool _chatByKeys;
    private DateTime _chatByKeysAt = DateTime.MinValue;

    // Appelé par le crochet clavier : Entrée ou Échap tapée par Julien, jeu au premier plan.
    private void OnChatKey(int scanCode)
    {
        if (!GameIsForeground())
        {
            return;
        }
        var now = DateTime.Now;
        if (scanCode == GameKeys.Enter)
        {
            _chatByKeys = !ChatLikelyOpen(now);
            _chatByKeysAt = now;
        }
        else if (scanCode == GameKeys.Escape)
        {
            _chatByKeys = false;
            _chatByKeysAt = now;
        }
    }

    // Le chat du jeu est-il (sans doute) ouvert ? Dernier signe le plus récent : touches de Julien ou écran.
    private bool ChatLikelyOpen(DateTime now)
    {
        var v = _view;
        if (v is null || _chatByKeysAt > v.At)
        {
            return _chatByKeys && now - _chatByKeysAt < TimeSpan.FromMinutes(5);
        }
        return v.ChatOpen && now - v.At < TimeSpan.FromSeconds(3);
    }
}
