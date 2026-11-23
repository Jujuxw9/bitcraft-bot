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

    // Temps de lecture avant de répondre (v0.4.7) : 1,5 s, plus 60 ms par lettre du message (6 s au plus),
    // plus 0,5 à 2,5 s au hasard. Comme quelqu'un qui lit, puis réfléchit.
    public static TimeSpan ReadingTime(string text, Random random) =>
        TimeSpan.FromMilliseconds(1500 + Math.Min(6000, 60 * text.Length) + random.Next(500, 2500));
}

// Réponses toutes faites, sans IA (D-054 : pas de clé API pour le moment). Courtes, drôles, neutres.
// Jamais d'allusion au fait que c'est un bot (D-055).
public static class CannedReplies
{
    // Taille maximale d'un message accepté par le serveur.
    public const int MaxLength = 250;

    // Chance d'ajouter un smiley à la fin (v0.4.7, Julien : 30 %).
    public const double SmileyChance = 0.3;

    // Smileys en caractères simples (le jeu les affiche à coup sûr, contrairement aux emojis).
    public static readonly IReadOnlyList<string> Smileys = [":)", ":D", ";)", "xD", "^^", ":P", "o/", "<3", ":-)", "=)"];

    // {0} = pseudo du joueur.
    public static readonly IReadOnlyList<string> French =
    [
        "Bonne question, {0}. Je réfléchis… encore… voilà : coupe plus d'arbres.",
        "{0}, mon cœur dit oui. Mon chat dit non. Je suis l'avis du chat.",
        "Désolé {0}, j'étais en train de compter mes troncs. Tu disais ?",
        "{0}, la réponse est 42. Ou une pioche. Souvent une pioche.",
        "Message reçu, {0}. Transmis au conseil des arbres. Ils sont lents.",
        "{0}, je te répondrais bien, mais mon endurance est à 0 %.",
        "Excellente remarque, {0}. Je la note sur une feuille de chêne.",
        "{0}, j'ai consulté les étoiles de Snowrest : elles disent « va manger ».",
        "{0}, j'ai lu trop vite, mais bonne journée quand même !",
        "Hmm, {0}… laisse-moi finir ce tronc et je te dis.",
        "{0}, c'est noté. Enfin, c'est gravé dans l'écorce.",
        "Ah {0}, toujours là pour animer le chat !",
        "{0}, demande à la lavande, elle sait tout.",
        "Je dirais oui, {0}. Mais je dis oui à tout le monde aujourd'hui.",
        "{0}, j'ai une réponse, mais elle est restée dans mon sac.",
        "Pas maintenant {0}, je négocie avec un sanglier.",
        "{0}, la vraie question c'est : qui a mangé ma soupe ?",
        "Excellent {0}, tu viens de gagner un bâton imaginaire.",
        "{0}, j'y réfléchis en coupant du bois, c'est là que j'ai mes meilleures idées.",
        "Oui {0}. Non. Peut-être. Ça dépend de la météo.",
        "{0}, j'ai demandé à mon cheval, il a haussé les épaules.",
        "Si tu le dis {0}, alors c'est sûrement vrai.",
        "{0}, j'ai perdu le fil, mais j'ai trouvé une baie.",
        "Tu tombes bien {0}, je cherchais quelqu'un à qui dire bonjour.",
        "{0}, j'ai la tête dans les feuilles, repose ta question plus tard.",
        "Bien vu {0}. Je n'aurais pas dit mieux. Je n'aurais rien dit, en fait.",
        "{0}, j'ai demandé au forgeron, il a dit « plus de charbon ».",
        "C'est une question à trois haches, {0}.",
        "{0}, je médite sous un chêne, reviens dans dix minutes.",
        "Ah, {0} ! Justement, je pensais à toi. Enfin, au bois. Mais aussi à toi.",
        "{0}, la sagesse de la forêt dit : bois de l'eau.",
        "Merci {0}, ça m'a fait sourire.",
        "{0}, j'ai essayé de réfléchir, j'ai fait tomber ma hache.",
        "Grande question, {0}. Petite réponse : oui.",
        "{0}, je te réponds dès que mon pain est cuit.",
        "Bonne chance {0}, et que tes récoltes soient belles.",
        "{0}, entre nous, personne ne sait vraiment.",
        "Je note, {0}. Sur mon carnet de bûcheron.",
        "{0}, j'ai demandé à un écureuil, il est parti avec ma noisette.",
        "Tout ira bien, {0}. Les arbres repoussent toujours.",
    ];

    public static readonly IReadOnlyList<string> English =
    [
        "Great question, {0}. Thinking… still thinking… done: chop more trees.",
        "{0}, my heart says yes. My cat says no. I trust the cat.",
        "Sorry {0}, I was counting my logs. You were saying?",
        "{0}, the answer is 42. Or a pickaxe. Usually a pickaxe.",
        "Message received, {0}. Forwarded to the council of trees. They are slow.",
        "{0}, I'd answer, but my stamina is at 0%.",
        "Excellent point, {0}. Writing it down on an oak leaf.",
        "{0}, I asked the stars over Snowrest: they say \"go eat something\".",
        "{0}, I read that too fast, but have a nice day anyway!",
        "Hmm, {0}… let me finish this log and I'll tell you.",
        "{0}, noted. Well, carved into the bark.",
        "Ah {0}, always keeping the chat alive!",
        "{0}, ask the lavender, it knows everything.",
        "I'd say yes, {0}. But I'm saying yes to everyone today.",
        "{0}, I had an answer, but it's stuck in my backpack.",
        "Not now {0}, I'm negotiating with a boar.",
        "{0}, the real question is: who ate my soup?",
        "Nice {0}, you just won an imaginary stick.",
        "{0}, I'll think about it while chopping, that's where my best ideas come from.",
        "Yes {0}. No. Maybe. Depends on the weather.",
        "{0}, I asked my horse, he just shrugged.",
        "If you say so {0}, it must be true.",
        "{0}, I lost track, but I found a berry.",
        "Good timing {0}, I was looking for someone to say hi to.",
        "{0}, my head is in the leaves, ask me again later.",
        "Well said {0}. I couldn't have said it better. I wouldn't have said anything, really.",
        "{0}, I asked the blacksmith, he said \"more coal\".",
        "That's a three-axe question, {0}.",
        "{0}, I'm meditating under an oak, come back in ten minutes.",
        "Oh, {0}! I was just thinking about you. Well, about wood. But also you.",
        "{0}, the wisdom of the forest says: drink some water.",
        "Thanks {0}, that made me smile.",
        "{0}, I tried to think, I dropped my axe.",
        "Big question, {0}. Small answer: yes.",
        "{0}, I'll answer as soon as my bread is baked.",
        "Good luck {0}, may your harvests be plenty.",
        "{0}, between us, nobody really knows.",
        "Noted, {0}. In my lumberjack notebook.",
        "{0}, I asked a squirrel, it ran off with my acorn.",
        "It'll be fine, {0}. Trees always grow back.",
    ];

    // Phrase au hasard, dans la langue du joueur (français si « fr », anglais sinon). Prefix : marque éventuelle (« [GPT] », Q-027).
    // smileyChance : chance d'ajouter un smiley à la fin (0 = jamais).
    public static string For(ChatMessage message, Random random, string prefix = "", double smileyChance = 0)
    {
        var list = message.Language.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? French : English;
        var text = prefix + string.Format(System.Globalization.CultureInfo.InvariantCulture, list[random.Next(list.Count)], CleanName(message.Username));
        if (smileyChance > 0 && random.NextDouble() < smileyChance)
        {
            text += " " + Smileys[random.Next(Smileys.Count)];
        }
        return text.Length <= MaxLength ? text : text[..MaxLength];
    }

    // Pseudo sans caractères bizarres (lettres, chiffres, _ et - seulement), 24 caractères au plus.
    public static string CleanName(string name)
    {
        var clean = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-').Take(24).ToArray());
        return clean.Length > 0 ? clean : "aventurier";
    }
}
