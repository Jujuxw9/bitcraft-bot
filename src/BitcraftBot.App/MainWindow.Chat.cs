using System.Windows;
using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Module « @GPT » (v0.4.5, D-054, D-055) : quand un joueur écrit « @GPT » dans le chat Région, le bot répond
// une phrase simple, au clavier, comme Julien (jamais par le serveur, D-039).
// Règles : 1 réponse par minute au plus, au message le plus récent ; jamais aux messages de Julien ;
// pas d'IA (phrases toutes faites) ; le chat est toujours refermé et vérifié à l'écran.
// Sécurités du contrôleur d'actions : jeu actif et visible, en jeu, aucune fenêtre du jeu ouverte,
// chat fermé avant de commencer, Julien ne tape pas (une touche de Julien pendant la frappe = arrêt).
public partial class MainWindow
{
    private const int ChatLogTextMax = 80;

    private readonly Random _gptRandom = new();
    private bool _gptEnabled = true;
    private GptChatPicker? _gptPicker;
    private ulong _gptPickerFor;
    private GptReply? _gptPending;
    private volatile bool _gptSending;
    private string _gptLastState = "";
    private int _gptSent;

    private sealed record GptReply(ChatMessage Message, string Text);

    private void InitGpt()
    {
        GptCheck.IsChecked = _gptEnabled;
        _log.Info("Chat", "Réponses « @GPT » activées (canal Région, 1 par minute au plus, phrases toutes faites).");
        ShowGptState("en attente du serveur");
    }

    private void GptCheck_Click(object sender, RoutedEventArgs e)
    {
        _gptEnabled = GptCheck.IsChecked == true;
        _gptPending = null;
        _log.Info("Chat", _gptEnabled ? "Réponses « @GPT » activées (case cochée)." : "Réponses « @GPT » coupées (case décochée).");
        ShowGptState(_gptEnabled ? "activé" : "coupé");
    }

    // Appelé toutes les 500 ms avec la dernière lecture du serveur (liaison active).
    private void RunGpt(ServerReader reader, PlayerSnapshot s)
    {
        if (!_gptEnabled)
        {
            ShowGptState("coupé");
            return;
        }
        if (reader.State != ReaderState.Connecte || s.EntityId is not { } me)
        {
            ShowGptState("en attente du serveur");
            return;
        }
        if (_gptPicker is null || _gptPickerFor != me)
        {
            _gptPicker = new GptChatPicker(me);
            _gptPickerFor = me;
        }
        var now = DateTimeOffset.UtcNow;

        // 1. Un nouveau message « @GPT » ?
        if (_gptPending is null && !_gptSending && _gptPicker.Pick(reader.ChatMessages(), now) is { } m)
        {
            _gptPending = new GptReply(m, CannedReplies.For(m, _gptRandom));
            _log.Info("Chat", $"« @GPT » de {m.Username} : « {Short(m.Text)} ». Réponse prévue : « {_gptPending.Text} ».");
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
            _gptPending = null;
            return;
        }

        // 3. Le jeu est-il prêt ? (personnage connecté, pas de repas en cours, sécurités du contrôleur)
        var wait = s.SignedIn != true ? "personnage pas connecté dans le jeu"
            : _pendingMeal is not null ? "repas en cours"
            : _actions.WhyNot();
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
            var result = await _actions.ChatAsync("Réponse « @GPT »", reply.Text, ChatProbeForGame());
            if (result.Done)
            {
                _gptSent++;
                _log.Info("Chat", $"Réponse envoyée à {reply.Message.Username} : « {reply.Text} ».");
            }
            else
            {
                _log.Alerte("Chat", $"Réponse à {reply.Message.Username} pas envoyée : {result.Detail}.");
            }
        }
        finally
        {
            // Une seule tentative par message : pas de boucle si le jeu refuse.
            _gptPending = null;
            _gptSending = false;
        }
    }

    // Ce que l'envoi regarde à l'écran : image neuve du jeu (jamais d'une autre fenêtre de Julien).
    private ChatProbe ChatProbeForGame() => new()
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
        ChannelOk = ChatInput.IsRegion,
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
