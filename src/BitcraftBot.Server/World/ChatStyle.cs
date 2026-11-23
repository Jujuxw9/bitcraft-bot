namespace BitcraftBot.Server.World;

// Langue du message et style relâché des réponses (v0.4.9, Julien : améliorations 1 et 3).
public static class ChatStyle
{
    // Chance d'un style relâché (pas de majuscule au début, pas de point à la fin, abréviations).
    public const double CasualChance = 0.25;

    private static readonly HashSet<string> FrenchWords =
    [
        "je", "tu", "il", "elle", "on", "nous", "vous", "est", "es", "suis", "le", "la", "les", "des", "un", "une", "du",
        "pour", "avec", "dans", "sur", "pas", "oui", "non", "salut", "bonjour", "bonsoir", "coucou", "merci", "quoi",
        "comment", "pourquoi", "ou", "qui", "quel", "quelle", "mais", "et", "ca", "c", "t", "j", "moi", "toi", "mon", "ton",
        "ma", "ta", "mes", "tes", "bien", "tres", "trop", "aussi", "ici", "vas", "fais", "peux", "veux", "sais", "mdr",
        "ptdr", "stp", "svp", "jsp", "tkt", "wesh", "slt", "cc", "bois", "arbre", "faim", "aide",
    ];

    private static readonly HashSet<string> EnglishWords =
    [
        "i", "you", "u", "he", "she", "we", "they", "is", "are", "am", "the", "a", "an", "and", "to", "of", "for", "with",
        "in", "on", "not", "yes", "no", "hi", "hello", "hey", "thanks", "thank", "what", "how", "why", "where", "who",
        "which", "but", "it", "me", "my", "your", "can", "do", "does", "good", "very", "too", "here", "lol", "pls", "idk",
        "wood", "tree", "help",
    ];

    // Langue de la réponse : « fr/ » devant le pseudo, ou message écrit en français (plus de mots français qu'anglais).
    public static string LanguageOf(ChatMessage message)
    {
        if (message.Language.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
        {
            return "fr";
        }
        return LooksFrench(message.Text) ? "fr" : "en";
    }

    public static bool LooksFrench(string text)
    {
        var tokens = ChatPhraseBook.Tokens(text).Where(t => t != "gpt" && t != "chatgpt").ToList();
        var fr = tokens.Count(FrenchWords.Contains);
        var en = tokens.Count(EnglishWords.Contains);
        // Les accents français comptent aussi.
        fr += text.Count(c => "éèêàâùûîïôç".Contains(char.ToLowerInvariant(c)));
        return fr > en;
    }

    // Abréviations du chat, sur des morceaux de phrase (minuscules).
    private static readonly (string From, string To)[] FrenchShort =
    [
        ("je ne sais pas", "jsp"), ("t'inquiète", "tkt"), ("s'il te plaît", "stp"), ("beaucoup", "bcp"),
        ("quelque chose", "qqch"), ("pourquoi", "pk"), ("aujourd'hui", "ajd"), ("pas de souci", "pas de soucis"),
        ("d'accord", "dac"), ("je suis", "chui"), ("toujours", "tjrs"), ("rien", "r1"), ("c'est", "c"),
    ];

    private static readonly (string From, string To)[] EnglishShort =
    [
        ("no problem", "np"), ("i don't know", "idk"), ("to be honest", "tbh"), ("because", "bc"), ("please", "pls"),
        ("thanks", "thx"), ("okay", "ok"), ("you are", "ur"), ("you", "u"), ("really", "rly"), ("people", "ppl"),
    ];

    // Version relâchée du texte, ou le texte tel quel. protectedName : le pseudo, jamais touché.
    public static string Casual(string text, string language, string protectedName, Random random)
    {
        var result = text;
        // Une abréviation au plus (la première trouvée dans un ordre au hasard), une fois sur deux.
        if (random.NextDouble() < 0.5)
        {
            var list = language == "fr" ? FrenchShort : EnglishShort;
            foreach (var (from, to) in list.OrderBy(_ => random.Next()))
            {
                var i = IndexOfWord(result, from);
                if (i >= 0)
                {
                    result = result[..i] + to + result[(i + from.Length)..];
                    break;
                }
            }
        }
        // Pas de point final (garde « ! », « ? », « … »).
        if (result.EndsWith('.') && !result.EndsWith("..", StringComparison.Ordinal))
        {
            result = result[..^1];
        }
        // Pas de majuscule au début (sauf si la phrase commence par le pseudo).
        if (result.Length > 0 && char.IsUpper(result[0]) && !(protectedName.Length > 0 && result.StartsWith(protectedName, StringComparison.Ordinal)))
        {
            result = char.ToLowerInvariant(result[0]) + result[1..];
        }
        return result;
    }

    // Position du morceau, mot entier, sans tenir compte des majuscules ; -1 si absent.
    private static int IndexOfWord(string text, string part)
    {
        var start = 0;
        while (true)
        {
            var i = text.IndexOf(part, start, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
            {
                return -1;
            }
            var before = i == 0 || !char.IsLetter(text[i - 1]);
            var end = i + part.Length;
            var after = end >= text.Length || !char.IsLetter(text[end]);
            if (before && after)
            {
                return i;
            }
            start = i + 1;
        }
    }
}
