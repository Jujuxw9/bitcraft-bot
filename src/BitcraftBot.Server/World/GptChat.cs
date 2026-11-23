using System.Text.Json;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server.World;

// Module « @GPT » (D-054) : lire le chat Région (lecture seule) et choisir à quel message répondre.
// Ce fichier ne fait que lire et choisir. Il n'envoie rien : ni au serveur (interdit, D-039), ni au jeu.
// L'écriture dans le chat passera plus tard par le contrôleur d'actions (D-045), au clavier.

// Canaux du chat (code serveur, enum ChatChannel). Seuls Local, Région, Groupe et Commerce acceptent les joueurs.
public enum ChatChannel
{
    Systeme = 0,
    Local = 2,
    Region = 3, // « General » dans le jeu
    Groupe = 7,
    Commerce = 8,
}

// Un message du chat (table chat_message_state du module de région, publique).
public sealed record ChatMessage(ulong EntityId, ulong OwnerEntityId, string Username, string Language, int Channel, string Text,
    DateTimeOffset SentAt);

public static class ChatMessages
{
    // Requête d'abonnement : le canal Région seulement (D-054).
    public static string RegionQuery => $"SELECT * FROM chat_message_state WHERE channel_id = {(int)ChatChannel.Region}";

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

// Choix du message auquel répondre (règles de D-054).
public sealed class GptChatPicker
{
    // Déclencheur : « @GPT » en majuscules exactement.
    public const string Trigger = "@GPT";

    // Au plus une réponse par minute.
    public static readonly TimeSpan MinDelay = TimeSpan.FromMinutes(1);

    // Un message plus vieux que ça n'attend plus de réponse (par exemple au démarrage du bot).
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(2);

    private readonly ulong _me;
    private readonly HashSet<ulong> _handled = [];
    private DateTimeOffset? _lastReply;

    public GptChatPicker(ulong myEntityId) => _me = myEntityId;

    public DateTimeOffset? LastReply => _lastReply;

    public static bool IsTrigger(string text) => text.Contains(Trigger, StringComparison.Ordinal);

    // Messages qui attendent une réponse : canal Région, « @GPT », pas de Julien, pas trop vieux, pas déjà traités.
    public IReadOnlyList<ChatMessage> Pending(IEnumerable<ChatMessage> messages, DateTimeOffset now) =>
        messages.Where(m => m.Channel == (int)ChatChannel.Region && m.OwnerEntityId != _me && IsTrigger(m.Text)
                && !_handled.Contains(m.EntityId) && now - m.SentAt <= MaxAge)
            .OrderBy(m => m.SentAt).ThenBy(m => m.EntityId).ToList();

    // Le message auquel répondre maintenant, ou null (rien en attente, ou moins d'une minute depuis la dernière réponse).
    // Plusieurs en attente : le plus récent seulement, les autres sont oubliés.
    public ChatMessage? Pick(IEnumerable<ChatMessage> messages, DateTimeOffset now)
    {
        if (_lastReply is { } last && now - last < MinDelay)
        {
            return null;
        }
        var pending = Pending(messages, now);
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
}

// Réponses toutes faites, sans IA (D-054 : pas de clé API pour le moment). Courtes, drôles, neutres.
public static class CannedReplies
{
    // Taille maximale d'un message accepté par le serveur.
    public const int MaxLength = 250;

    // {0} = pseudo du joueur.
    public static readonly IReadOnlyList<string> French =
    [
        "Bonne question, {0}. Je réfléchis… encore… voilà : coupe plus d'arbres.",
        "{0}, mes circuits disent oui. Mon chat dit non. Je suis l'avis du chat.",
        "Je suis une IA de bûcheron, {0} : ma réponse est « écorce ».",
        "Désolé {0}, j'étais en train de compter mes troncs. Tu disais ?",
        "{0}, la réponse est 42. Ou une pioche. Souvent une pioche.",
        "Message reçu, {0}. Transmis au conseil des arbres. Ils sont lents.",
        "{0}, je te répondrais bien, mais mon endurance est à 0 %.",
        "Excellente remarque, {0}. Je la note sur une feuille de chêne.",
        "{0}, j'ai consulté les étoiles de Snowrest : elles disent « va manger ».",
        "Bip boup, {0}. Traduction : bonne journée !",
    ];

    public static readonly IReadOnlyList<string> English =
    [
        "Great question, {0}. Thinking… still thinking… done: chop more trees.",
        "{0}, my circuits say yes. My cat says no. I trust the cat.",
        "I'm a lumberjack AI, {0}: my answer is \"bark\".",
        "Sorry {0}, I was counting my logs. You were saying?",
        "{0}, the answer is 42. Or a pickaxe. Usually a pickaxe.",
        "Message received, {0}. Forwarded to the council of trees. They are slow.",
        "{0}, I'd answer, but my stamina is at 0%.",
        "Excellent point, {0}. Writing it down on an oak leaf.",
        "{0}, I asked the stars over Snowrest: they say \"go eat something\".",
        "Beep boop, {0}. Translation: have a nice day!",
    ];

    // Phrase au hasard, dans la langue du joueur (français si « fr », anglais sinon). Prefix : marque éventuelle (« [GPT] », Q-027).
    public static string For(ChatMessage message, Random random, string prefix = "")
    {
        var list = message.Language.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? French : English;
        var text = prefix + string.Format(System.Globalization.CultureInfo.InvariantCulture, list[random.Next(list.Count)], CleanName(message.Username));
        return text.Length <= MaxLength ? text : text[..MaxLength];
    }

    // Pseudo sans caractères bizarres (lettres, chiffres, _ et - seulement), 24 caractères au plus.
    public static string CleanName(string name)
    {
        var clean = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-').Take(24).ToArray());
        return clean.Length > 0 ? clean : "aventurier";
    }
}
