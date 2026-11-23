using System.Text.Json;
using System.Text.RegularExpressions;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Module « @GPT » (D-054) : lire le chat (lecture seule) et choisir à quel message répondre.
// Ce fichier ne fait que lire et choisir. Il n'envoie rien : ni au serveur (interdit, D-039), ni au jeu.
// L'écriture dans le chat passe par le contrôleur d'actions (D-045), au clavier.

// Canaux du chat (code serveur, enum ChatChannel). Seuls Local, Région, Recherche de groupe et Commerce acceptent les joueurs.
public enum ChatChannel
{
    Systeme = 0,
    Local = 2,
    Region = 3, // « General » dans le jeu
    RechercheGroupe = 7, // « Looking For Group »
    Commerce = 8, // « Trade »
}

// Un message du chat (table chat_message_state du module de région, publique).
public sealed record ChatMessage(ulong EntityId, ulong OwnerEntityId, string Username, string Language, int Channel, string Text,
    DateTimeOffset SentAt);

// Canaux où le bot répond, et la commande du jeu pour écrire dans chacun (menu du bouton du canal, capture du 23/11) :
// Local « /s », Région « /y », Looking For Group « /lfg », Trade « /t ».
public static class ChatChannels
{
    public static IReadOnlyList<ChatChannel> Answered { get; } =
        [ChatChannel.Local, ChatChannel.Region, ChatChannel.RechercheGroupe, ChatChannel.Commerce];

    public static bool IsAnswered(int channel) => Answered.Any(c => (int)c == channel);

    public static string? Command(int channel) => (ChatChannel)channel switch
    {
        ChatChannel.Local => "/s",
        ChatChannel.Region => "/y",
        ChatChannel.RechercheGroupe => "/lfg",
        ChatChannel.Commerce => "/t",
        _ => null,
    };

    // Mot lu sur le bouton du canal du chat quand ce canal est choisi (« GROUP » : « Looking For Group » peut être coupé).
    public static string ButtonWord(int channel) => (ChatChannel)channel switch
    {
        ChatChannel.Local => "LOCAL",
        ChatChannel.Region => "REGION",
        ChatChannel.RechercheGroupe => "GROUP",
        ChatChannel.Commerce => "TRADE",
        _ => "?",
    };

    public static string Name(int channel) => (ChatChannel)channel switch
    {
        ChatChannel.Local => "Local",
        ChatChannel.Region => "Région",
        ChatChannel.RechercheGroupe => "Looking For Group",
        ChatChannel.Commerce => "Trade",
        _ => $"canal {channel}",
    };
}

public static class ChatMessages
{
    // Requête d'abonnement : le canal Région seulement (D-054).
    public static string RegionQuery => $"SELECT * FROM chat_message_state WHERE channel_id = {(int)ChatChannel.Region}";

    // Seulement les messages récents (v0.4.5) : le serveur garde 2 jours de chat, inutile de tout recevoir.
    public static string RegionQuerySince(long unixSeconds) => QuerySince(ChatChannel.Region, unixSeconds);

    public static string QuerySince(ChatChannel channel, long unixSeconds) =>
        $"SELECT * FROM chat_message_state WHERE channel_id = {(int)channel} AND timestamp > {unixSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    // v0.4.7 : une requête simple par canal répondu (pas de « OR », plus sûr pour le serveur).
    public static IReadOnlyList<string> AnsweredQueriesSince(long unixSeconds) =>
        ChatChannels.Answered.Select(c => QuerySince(c, unixSeconds)).ToList();

    public static IReadOnlyList<ChatMessage> Read(TableCache cache)
    {
        var result = new List<ChatMessage>();
        foreach (var row in cache.Rows("chat_message_state"))
        {
            if (Parse(row) is { } m)
            {
                result.Add(m);
            }
        }
        return result.OrderBy(m => m.SentAt).ThenBy(m => m.EntityId).ToList();
    }

    public static ChatMessage? Parse(Row row)
    {
        if (row.U64("entity_id") is not { } id || row.U64("owner_entity_id") is not { } owner
            || row.I32("channel_id") is not { } channel || row.I64("timestamp") is not { } ts)
        {
            return null;
        }
        var text = Text(row, "text") ?? "";
        var (language, name) = SplitUsername(Text(row, "username") ?? "");
        return new ChatMessage(id, owner, name, language, channel, text, DateTimeOffset.FromUnixTimeSeconds(ts));
    }

    // Le serveur range le pseudo sous la forme « fr/Pseudo » (code de langue, barre, pseudo).
    public static (string Language, string Name) SplitUsername(string raw)
    {
        var slash = raw.IndexOf('/');
        return slash is > 0 and <= 8 ? (raw[..slash], raw[(slash + 1)..]) : ("", raw);
    }

    private static string? Text(Row row, string column) =>
        row.Get(column) is { ValueKind: JsonValueKind.String } v ? v.GetString() : null;
}

// Choix du message auquel répondre (règles de D-054, élargies en v0.4.7).
public sealed class GptChatPicker
{
    // Déclencheurs (v0.4.7, Julien) : « @gpt » ou « @chatgpt », majuscules ou minuscules, pas collé à un autre mot.
    private static readonly Regex TriggerPattern = new(@"(?<![\w@])@(chat)?gpt(?![\w])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Au plus une réponse par minute.
    public static readonly TimeSpan MinDelay = TimeSpan.FromMinutes(1);

    // Un message plus vieux que ça n'attend plus de réponse (par exemple au démarrage du bot).
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(2);

    private readonly ulong _me;
    private readonly HashSet<ulong> _handled = [];
    private DateTimeOffset? _lastReply;

    public GptChatPicker(ulong myEntityId) => _me = myEntityId;

    public DateTimeOffset? LastReply => _lastReply;

    public static bool IsTrigger(string text) => TriggerPattern.IsMatch(text);

    // Temps à attendre avant de pouvoir répondre de nouveau (zéro si c'est possible tout de suite).
    public TimeSpan Cooldown(DateTimeOffset now) =>
        _lastReply is { } last && now - last < MinDelay ? MinDelay - (now - last) : TimeSpan.Zero;

    // Messages qui attendent une réponse : canal répondu, déclencheur, pas de Julien, pas trop vieux, pas déjà traités.
    public IReadOnlyList<ChatMessage> Pending(IEnumerable<ChatMessage> messages, DateTimeOffset now) =>
        messages.Where(m => ChatChannels.IsAnswered(m.Channel) && m.OwnerEntityId != _me && IsTrigger(m.Text)
                && !_handled.Contains(m.EntityId) && now - m.SentAt <= MaxAge)
            .OrderBy(m => m.SentAt).ThenBy(m => m.EntityId).ToList();

    // Le message auquel répondre maintenant, ou null (rien en attente, ou moins d'une minute depuis la dernière réponse).
    // Plusieurs en attente : le plus récent seulement, les autres sont oubliés.
    public ChatMessage? Pick(IEnumerable<ChatMessage> messages, DateTimeOffset now)
    {
        if (Cooldown(now) > TimeSpan.Zero)
        {
            return null;
        }
        var all = Pending(messages, now);
        // v0.4.9 : un message qui ressemble à un autre bot est oublié (pas de boucle entre deux bots).
        foreach (var m in all.Where(LooksAutomated))
        {
            _handled.Add(m.EntityId);
            _ignored.Add(m);
        }
        var pending = all.Where(m => !LooksAutomated(m)).ToList();
        if (pending.Count == 0)
        {
            return null;
        }
        foreach (var m in pending)
        {
            _handled.Add(m.EntityId);
        }
        _lastReply = now;
        return pending[^1];
    }

    private readonly List<ChatMessage> _ignored = [];

    // Messages oubliés parce qu'ils ressemblent à un autre bot, depuis le dernier appel (pour le journal).
    public IReadOnlyList<ChatMessage> TakeIgnored()
    {
        var list = _ignored.ToList();
        _ignored.Clear();
        return list;
    }

    // Dernières réponses envoyées par le bot (un message qui les répète est un écho d'un autre bot).
    private readonly Queue<string> _sent = new();

    public void NoteSent(string text)
    {
        _sent.Enqueue(Normalize(text));
        while (_sent.Count > 10)
        {
            _sent.Dequeue();
        }
    }

    private static readonly string[] BotMarkers =
    [
        "[bot]", "(bot)", "[ai]", "[ia]", "[gpt]", "auto-reply", "autoreply", "automated message", "message automatique",
        "réponse automatique", "beep boop", "bip boup", "i am a bot", "i'm a bot", "je suis un bot", "as an ai", "en tant qu'ia",
        "as a language model", "en tant que modèle",
    ];

    // Ressemble à un autre bot : pseudo avec « bot », marque de bot dans le texte, ou écho d'une réponse du bot.
    public bool LooksAutomated(ChatMessage m)
    {
        if (m.Username.Contains("bot", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        var lower = m.Text.ToLowerInvariant();
        if (BotMarkers.Any(b => lower.Contains(b, StringComparison.Ordinal)))
        {
            return true;
        }
        var text = Normalize(TriggerPattern.Replace(m.Text, " "));
        return text.Length >= 12 && _sent.Any(s => s.Contains(text, StringComparison.Ordinal) || text.Contains(s, StringComparison.Ordinal));
    }

    private static string Normalize(string text) => string.Join(' ', ChatPhraseBook.Tokens(text));

    // Temps de lecture avant de répondre (v0.4.7) : 1,5 s, plus 60 ms par lettre du message (6 s au plus),
    // plus 0,5 à 2,5 s au hasard. Comme quelqu'un qui lit, puis réfléchit.
    public static TimeSpan ReadingTime(string text, Random random) =>
        TimeSpan.FromMilliseconds(1500 + Math.Min(6000, 60 * text.Length) + random.Next(500, 2500));
}

// Réglages communs des réponses (v0.4.8 : les phrases sont dans World/Phrases/*.txt, voir ChatPhrases.cs).
// Jamais d'allusion au fait que c'est un bot (D-055).
public static class CannedReplies
{
    // Taille maximale d'un message accepté par le serveur.
    public const int MaxLength = 250;

    // Chance d'ajouter un smiley à la fin (v0.4.7, Julien : 30 %).
    public const double SmileyChance = 0.3;

    // Smileys en caractères simples (le jeu les affiche à coup sûr, contrairement aux emojis).
    public static readonly IReadOnlyList<string> Smileys = [":)", ":D", ";)", "xD", "^^", ":P", "o/", "<3", ":-)", "=)"];

    // Pseudo sans caractères bizarres (lettres, chiffres, _ et - seulement), 24 caractères au plus.
    public static string CleanName(string name)
    {
        var clean = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-').Take(24).ToArray());
        return clean.Length > 0 ? clean : "aventurier";
    }
}
