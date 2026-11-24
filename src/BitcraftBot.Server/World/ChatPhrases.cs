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
    // Français en premier : l'ordre des thèmes du fichier français décide des égalités.
    public static ChatPhraseBook LoadEmbedded() => Parse(EmbeddedFiles());

    // D-055 : une phrase ne parle jamais de bot. Vérifié au chargement (même pour un fichier modifié à la main).
    public static readonly IReadOnlyList<string> ForbiddenWords =
        ["bot", "robot", " ia ", " ai ", "circuit", "bip", "beep", "program", "intelligence", "openai", "chatgpt"];

    public static bool HasForbiddenWord(string phrase)
    {
        var lower = " " + phrase.ToLowerInvariant() + " ";
        return ForbiddenWords.Any(f => lower.Contains(f, StringComparison.Ordinal));
    }

    // Dossier à côté du .exe (v0.4.9, Julien : amélioration 7) : un fichier « fr.txt » ou « en.txt » posé là
    // remplace celui intégré au .exe, sans nouvelle version. Les modèles « modele-fr.txt »… y sont réécrits à chaque démarrage.
    public const string FolderName = "phrases-chat";

    public static IReadOnlyList<(string Language, string Content)> EmbeddedFiles()
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
            files.Add((resource["BitcraftBot.Chat.".Length..^".txt".Length], reader.ReadToEnd()));
        }
        return files.OrderBy(f => f.Item1 == "fr" ? 0 : 1).ToList();
    }

    // Fichiers intégrés, remplacés langue par langue par ceux du dossier s'ils existent. Overridden : langues remplacées.
    public static ChatPhraseBook Load(string? folder, out IReadOnlyList<string> overridden)
    {
        var files = EmbeddedFiles().ToList();
        var replaced = new List<string>();
        if (folder is not null)
        {
            try
            {
                Directory.CreateDirectory(folder);
                foreach (var (language, content) in files)
                {
                    File.WriteAllText(Path.Combine(folder, $"modele-{language}.txt"), content, new UTF8Encoding(false));
                }
                for (var i = 0; i < files.Count; i++)
                {
                    var path = Path.Combine(folder, $"{files[i].Item1}.txt");
                    if (File.Exists(path))
                    {
                        files[i] = (files[i].Item1, File.ReadAllText(path, Encoding.UTF8));
                        replaced.Add(files[i].Item1);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Dossier inaccessible : les phrases intégrées suffisent.
            }
        }
        overridden = replaced;
        var book = Parse(files);
        // Fichier modifié qui ne donne rien d'utilisable : retour aux phrases intégrées.
        if (replaced.Count > 0 && (book.Theme(DefaultTheme) is not { } autre || autre.Phrases.Values.Sum(l => l.Count) == 0))
        {
            var fallback = Parse(EmbeddedFiles());
            fallback._errors.Insert(0, "fichier de phrases à côté du .exe inutilisable (pas de thème [autre]) : phrases intégrées gardées");
            overridden = [];
            return fallback;
        }
        return book;
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
                if (phrase.Length == 0 || StripKnown(phrase).Contains('{') || StripKnown(phrase).Contains('}'))
                {
                    _errors.Add($"{language}.txt ligne {number} : phrase vide ou accolade inconnue");
                    continue;
                }
                if (HasForbiddenWord(phrase))
                {
                    _errors.Add($"{language}.txt ligne {number} : phrase refusée, elle parle de bot (D-055)");
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
    public static string StripKnown(string phrase) =>
        phrase.Replace("{nom}", "").Replace("{region}", "").Replace("{niveau}", "");

    // info : vraies infos du jeu (v0.4.10) ; une phrase avec {region} ou {niveau} n'est choisie que si l'info est connue.
    public string? Pick(string themeName, string language, Random random, ChatInfo? info = null)
    {
        var list = Usable(Candidates(themeName, language), info) ?? Usable(Candidates(DefaultTheme, language), info);
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

    private static List<string>? Usable(List<string>? list, ChatInfo? info)
    {
        var ok = list?.Where(p => (info?.Region is not null || !p.Contains("{region}", StringComparison.Ordinal))
            && (info?.Level is not null || !p.Contains("{niveau}", StringComparison.Ordinal))).ToList();
        return ok is { Count: > 0 } ? ok : null;
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

// Réponse prête à taper : texte, thème reconnu, faute volontaire éventuelle (pour le journal),
// correction « *mot » à envoyer juste après (v0.4.9), langue choisie.
public sealed record ChatReply(string Text, string Theme, string? Typo, string? Correction = null, string Language = "en");

public static class ChatReplies
{
    // Chance d'une faute volontaire dans la réponse (v0.4.8, Julien : « genre 5 % du temps »).
    public const double TypoChance = 0.05;

    // Chance de corriger la faute par un 2e message « *mot » (v0.4.9, Julien : amélioration 2).
    public const double CorrectionChance = 0.5;

    // Langue de la réponse (v0.4.9) : « fr/ » devant le pseudo, ou message écrit en français.
    public static string LanguageOf(ChatMessage message) => ChatStyle.LanguageOf(message);

    // v0.4.10 : smiley et style relâché selon le canal et selon l'ami (null = valeur du canal) ;
    // info : vraies infos du jeu ; hour : heure locale (bonjour / bonsoir), -1 = ne rien changer.
    public static ChatReply For(ChatMessage message, ChatPhraseBook book, Random random,
        double? smileyChance = null, double typoChance = TypoChance, double? casualChance = null,
        ChatInfo? info = null, int hour = -1, bool friend = false)
    {
        var tone = ChatTone.For(message.Channel, friend);
        var smiley = smileyChance ?? tone.Smiley;
        var casual = casualChance ?? tone.Casual;
        var theme = book.DetectTheme(message.Text);
        var language = LanguageOf(message);
        var name = CannedReplies.CleanName(message.Username);
        var phrase = book.Pick(theme, language, random, info) ?? (language == "fr" ? "Coucou {nom} !" : "Hey {nom}!");
        var text = phrase.Replace("{nom}", name, StringComparison.Ordinal)
            .Replace("{region}", info?.Region ?? "", StringComparison.Ordinal)
            .Replace("{niveau}", info?.Level?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", StringComparison.Ordinal);
        text = ChatTone.AdaptToTime(text, language, hour);
        if (casual > 0 && random.NextDouble() < casual)
        {
            text = ChatStyle.Casual(text, language, name, random);
        }
        string? typo = null;
        string? correction = null;
        if (typoChance > 0 && random.NextDouble() < typoChance && ChatTypos.Apply(text, language, name, random) is { } faulty)
        {
            typo = faulty.Description;
            text = faulty.Text;
            if (random.NextDouble() < CorrectionChance)
            {
                correction = "*" + faulty.Right;
            }
        }
        if (smiley > 0 && random.NextDouble() < smiley)
        {
            text += " " + CannedReplies.Smileys[random.Next(CannedReplies.Smileys.Count)];
        }
        return new ChatReply(text.Length <= CannedReplies.MaxLength ? text : text[..CannedReplies.MaxLength], theme, typo, correction, language);
    }
}

// Vraies infos du jeu que le bot peut dire (v0.4.10) : région et plus haut niveau de métier. Rien de privé.
public sealed record ChatInfo(string? Region, int? Level);
