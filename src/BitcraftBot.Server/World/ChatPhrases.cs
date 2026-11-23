using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BitcraftBot.Server.World;

// Base de phrases du chat « @gpt », rangées par thème (v0.4.8, demande de Julien).
// Les phrases sont dans des fichiers texte simples, un par langue : World/Phrases/fr.txt, en.txt (intégrés au .exe).
// Format (expliqué en tête de chaque fichier) :
//   [theme]          début d'un thème
//   mots: a, b*, c d  mots qui font reconnaître le thème (« b* » = tout mot qui commence par b ; « c d » = les deux à la suite)
//   - phrase         une réponse possible ; {nom} = pseudo du joueur
// Le thème du message = celui qui a le plus de mots trouvés (égalité : le plus haut dans le fichier).
// Aucun mot trouvé : thème [autre]. Les mots de toutes les langues comptent (un joueur « fr/ » peut écrire en anglais).
// Ajouter une langue = ajouter un fichier (« es.txt ») ; ajouter un thème = ajouter un bloc [nom] dans les fichiers.
public sealed class ChatTheme
{
    public required string Name { get; init; }

    // Mots déjà normalisés (minuscules, sans accents), chacun en suite de morceaux.
    public List<string[]> Keywords { get; } = [];

    // Phrases par langue (« fr », « en »).
    public Dictionary<string, List<string>> Phrases { get; } = [];
}

public sealed class ChatPhraseBook
{
    public const string DefaultTheme = "autre";
    public const string DefaultLanguage = "en";

    // Une phrase dite il y a peu n'est pas redite (les N dernières, toutes langues confondues).
    public const int HistorySize = 12;

    private static readonly Regex TriggerPattern = new(@"@(chat)?gpt\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly List<ChatTheme> _themes = [];
    private readonly List<string> _errors = [];
    private readonly Queue<string> _recent = new();

    public IReadOnlyList<ChatTheme> Themes => _themes;

    // Lignes du fichier pas comprises (le test vérifie qu'il n'y en a aucune).
    public IReadOnlyList<string> Errors => _errors;

    public ChatTheme? Theme(string name) => _themes.FirstOrDefault(t => t.Name == name);

    // Fichiers intégrés au .exe (World/Phrases/*.txt).
    public static ChatPhraseBook LoadEmbedded()
    {
        var assembly = typeof(ChatPhraseBook).Assembly;
        var files = new List<(string, string)>();
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith("BitcraftBot.Chat.", StringComparison.Ordinal)).Order())
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                continue;
            }
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var language = resource["BitcraftBot.Chat.".Length..^".txt".Length];
            files.Add((language, reader.ReadToEnd()));
        }
        // Français en premier : l'ordre des thèmes du fichier français décide des égalités.
        return Parse(files.OrderBy(f => f.Item1 == "fr" ? 0 : 1).ToList());
    }

    public static ChatPhraseBook Parse(IReadOnlyList<(string Language, string Content)> files)
    {
        var book = new ChatPhraseBook();
        foreach (var (language, content) in files)
        {
            book.ParseFile(language, content);
        }
        return book;
    }

    private void ParseFile(string language, string content)
    {
        ChatTheme? current = null;
        var number = 0;
        foreach (var raw in content.Split('\n'))
        {
            number++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var name = line[1..^1].Trim().ToLowerInvariant();
                if (name.Length == 0 || name.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-'))
                {
                    _errors.Add($"{language}.txt ligne {number} : nom de thème pas valable « {line} »");
                    current = null;
                    continue;
                }
                current = Theme(name);
                if (current is null)
                {
                    current = new ChatTheme { Name = name };
                    _themes.Add(current);
                }
                continue;
            }
            if (current is null)
            {
                _errors.Add($"{language}.txt ligne {number} : ligne avant le premier [thème]");
                continue;
            }
            if (line.StartsWith("mots:", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var word in line[5..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var parts = Keyword(word);
                    if (parts.Length == 0)
                    {
                        _errors.Add($"{language}.txt ligne {number} : mot vide « {word} »");
                    }
                    else if (!current.Keywords.Any(k => k.SequenceEqual(parts)))
                    {
                        current.Keywords.Add(parts);
                    }
                }
                continue;
            }
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                var phrase = line[2..].Trim();
                if (phrase.Length == 0 || phrase.Replace("{nom}", "").Contains('{') || phrase.Replace("{nom}", "").Contains('}'))
                {
                    _errors.Add($"{language}.txt ligne {number} : phrase vide ou accolade inconnue");
                    continue;
                }
                if (!current.Phrases.TryGetValue(language, out var list))
                {
                    current.Phrases[language] = list = [];
                }
                list.Add(phrase);
                continue;
            }
            _errors.Add($"{language}.txt ligne {number} : ligne pas comprise « {line} »");
        }
    }

    // Mot du fichier → morceaux normalisés. Un mot sans lettre ni chiffre (« ? », « <3 ») est gardé tel quel.
    private static string[] Keyword(string word)
    {
        var lower = word.ToLowerInvariant();
        if (!lower.Any(char.IsLetterOrDigit))
        {
            return [lower];
        }
        var star = lower.EndsWith('*');
        var parts = Tokens(star ? lower[..^1] : lower);
        if (star && parts.Length > 0)
        {
            parts[^1] += "*";
        }
        return parts;
    }

    // Minuscules, sans accents, découpé sur tout ce qui n'est ni lettre ni chiffre.
    public static string[] Tokens(string text)
    {
        var decomposed = text.ToLowerInvariant().Replace("œ", "oe", StringComparison.Ordinal).Replace("æ", "ae", StringComparison.Ordinal)
            .Replace('’', '\'').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    // Nombre de mots du thème trouvés dans le message (chaque mot compte une fois).
    public static int Score(ChatTheme theme, string text)
    {
        var lower = text.ToLowerInvariant();
        var tokens = Tokens(text);
        var score = 0;
        foreach (var k in theme.Keywords)
        {
            if (k.Length == 1 && !k[0].Any(char.IsLetterOrDigit))
            {
                score += lower.Contains(k[0], StringComparison.Ordinal) ? 1 : 0;
                continue;
            }
            for (var i = 0; i + k.Length <= tokens.Length; i++)
            {
                var ok = true;
                for (var j = 0; j < k.Length && ok; j++)
                {
                    var part = k[j];
                    ok = part.EndsWith('*') ? tokens[i + j].StartsWith(part[..^1], StringComparison.Ordinal) : tokens[i + j] == part;
                }
                if (ok)
                {
                    score++;
                    break;
                }
            }
        }
        return score;
    }

    // Thème du message (sans le « @gpt » lui-même). Aucun mot trouvé : [autre].
    public string DetectTheme(string message)
    {
        var text = TriggerPattern.Replace(message, " ");
        var best = DefaultTheme;
        var bestScore = 0;
        foreach (var theme in _themes)
        {
            if (theme.Name == DefaultTheme)
            {
                continue;
            }
            var score = Score(theme, text);
            if (score > bestScore)
            {
                best = theme.Name;
                bestScore = score;
            }
        }
        return best;
    }

    // Phrase au hasard du thème, dans la langue voulue (sinon anglais, sinon thème [autre]).
    // Une phrase dite parmi les dernières n'est pas reprise tant qu'il en reste d'autres.
    public string? Pick(string themeName, string language, Random random)
    {
        var list = Candidates(themeName, language) ?? Candidates(DefaultTheme, language);
        if (list is null || list.Count == 0)
        {
            return null;
        }
        var fresh = list.Where(p => !_recent.Contains(p)).ToList();
        var choice = (fresh.Count > 0 ? fresh : list)[random.Next(fresh.Count > 0 ? fresh.Count : list.Count)];
        _recent.Enqueue(choice);
        while (_recent.Count > HistorySize)
        {
            _recent.Dequeue();
        }
        return choice;
    }

    private List<string>? Candidates(string themeName, string language)
    {
        if (Theme(themeName) is not { } theme)
        {
            return null;
        }
        return theme.Phrases.TryGetValue(language, out var list) && list.Count > 0 ? list
            : theme.Phrases.TryGetValue(DefaultLanguage, out var en) && en.Count > 0 ? en
            : null;
    }
}

// Réponse prête à taper : texte, thème reconnu, faute volontaire éventuelle (pour le journal).
public sealed record ChatReply(string Text, string Theme, string? Typo);

public static class ChatReplies
{
    // Chance d'une faute volontaire dans la réponse (v0.4.8, Julien : « genre 5 % du temps »).
    public const double TypoChance = 0.05;

    // Langue de la réponse : français si le joueur a le jeu en français (« fr/ »), anglais sinon.
    public static string LanguageOf(ChatMessage message) =>
        message.Language.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? "fr" : "en";

    public static ChatReply For(ChatMessage message, ChatPhraseBook book, Random random,
        double smileyChance = CannedReplies.SmileyChance, double typoChance = TypoChance)
    {
        var theme = book.DetectTheme(message.Text);
        var language = LanguageOf(message);
        var name = CannedReplies.CleanName(message.Username);
        var phrase = book.Pick(theme, language, random) ?? (language == "fr" ? "Coucou {nom} !" : "Hey {nom}!");
        var text = phrase.Replace("{nom}", name, StringComparison.Ordinal);
        string? typo = null;
        if (typoChance > 0 && random.NextDouble() < typoChance && ChatTypos.Apply(text, language, name, random) is { } faulty)
        {
            typo = faulty.Description;
            text = faulty.Text;
        }
        if (smileyChance > 0 && random.NextDouble() < smileyChance)
        {
            text += " " + CannedReplies.Smileys[random.Next(CannedReplies.Smileys.Count)];
        }
        return new ChatReply(text.Length <= CannedReplies.MaxLength ? text : text[..CannedReplies.MaxLength], theme, typo);
    }
}
