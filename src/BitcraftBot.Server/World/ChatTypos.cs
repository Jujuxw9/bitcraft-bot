namespace BitcraftBot.Server.World;

// Fautes volontaires dans les réponses du chat (v0.4.8, Julien : « genre 5 % du temps, pour rendre le truc plus humain »).
// Une seule faute par message, sur un seul mot. Jamais sur le pseudo du joueur, jamais sur un smiley.
// Deux familles :
// - faute de grammaire ou d'accord courante (« a » / « à », « et » / « est », « your » / « you're »…), si le message en offre une ;
// - faute de frappe : deux lettres inversées, une lettre oubliée, une lettre doublée, la touche d'à côté,
//   ou (français) un accent oublié.
public static class ChatTypos
{
    // Right : le mot juste (pour la correction « *mot » envoyée juste après, v0.4.9).
    public sealed record Typo(string Text, string Description, string Right);

    // Chance de choisir une faute de grammaire quand le message en permet une.
    public const double GrammarShare = 0.4;

    // Fautes de grammaire et d'accord courantes, mot entier (minuscules).
    private static readonly Dictionary<string, string[]> FrenchSwaps = new()
    {
        ["a"] = ["à"],
        ["à"] = ["a"],
        ["et"] = ["est"],
        ["est"] = ["et", "ai"],
        ["ces"] = ["ses"],
        ["ses"] = ["ces"],
        ["on"] = ["ont"],
        ["ont"] = ["on"],
        ["ou"] = ["où"],
        ["où"] = ["ou"],
        ["c'est"] = ["s'est"],
        ["sais"] = ["sait"],
        ["fais"] = ["fait"],
        ["vais"] = ["vait"],
        ["peux"] = ["peut"],
        ["veux"] = ["veut"],
        ["serai"] = ["serais"],
        ["aurai"] = ["aurais"],
        ["leur"] = ["leurs"],
        ["quand"] = ["quant"],
        ["davantage"] = ["d'avantage"],
        ["malgré"] = ["malgrés"],
    };

    private static readonly Dictionary<string, string[]> EnglishSwaps = new()
    {
        ["your"] = ["you're", "ur"],
        ["you're"] = ["your"],
        ["its"] = ["it's"],
        ["it's"] = ["its"],
        ["there"] = ["their", "they're"],
        ["their"] = ["there"],
        ["then"] = ["than"],
        ["than"] = ["then"],
        ["to"] = ["too"],
        ["too"] = ["to"],
        ["lose"] = ["loose"],
        ["were"] = ["where"],
        ["definitely"] = ["definately"],
        ["could"] = ["could of"],
        ["receive"] = ["recieve"],
        ["weird"] = ["wierd"],
    };

    // Terminaisons françaises souvent confondues (é / er / ez, ais / ait).
    private static readonly (string From, string To)[] FrenchEndings =
    [
        ("é", "er"), ("er", "é"), ("ez", "er"), ("ais", "ait"), ("ait", "ais"), ("és", "é"),
    ];

    private static readonly string[] AzertyRows = ["azertyuiop", "qsdfghjklm", "wxcvbn"];
    private static readonly string[] QwertyRows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"];

    // Le texte avec une faute, ou null si aucun mot ne s'y prête.
    public static Typo? Apply(string text, string language, string protectedName, Random random)
    {
        var words = text.Split(' ');
        var candidates = new List<int>();
        for (var i = 0; i < words.Length; i++)
        {
            var core = Core(words[i]).Core;
            if (core.Length >= 1 && core.All(c => char.IsLetter(c) || c == '\'')
                && !(protectedName.Length > 0 && words[i].Contains(protectedName, StringComparison.OrdinalIgnoreCase)))
            {
                candidates.Add(i);
            }
        }
        if (candidates.Count == 0)
        {
            return null;
        }
        if (random.NextDouble() < GrammarShare && Grammar(words, candidates, language, random) is { } grammar)
        {
            return grammar;
        }
        var letters = candidates.Where(i => Core(words[i]).Core.Count(char.IsLetter) >= 3).ToList();
        if (letters.Count == 0)
        {
            return Grammar(words, candidates, language, random);
        }
        var index = letters[random.Next(letters.Count)];
        var (before, word, after) = Core(words[index]);
        var (faulty, kind) = Keyboard(word, language, random);
        if (faulty == word)
        {
            return null;
        }
        words[index] = before + faulty + after;
        return new Typo(string.Join(' ', words), $"« {faulty} » au lieu de « {word} » ({kind})", word);
    }

    private static Typo? Grammar(string[] words, List<int> candidates, string language, Random random)
    {
        var swaps = language == "fr" ? FrenchSwaps : EnglishSwaps;
        var options = new List<(int Index, string To)>();
        foreach (var i in candidates)
        {
            var word = Core(words[i]).Core;
            var lower = word.ToLowerInvariant();
            if (swaps.TryGetValue(lower, out var to))
            {
                options.AddRange(to.Select(t => (i, t)));
            }
            if (language == "fr" && lower.Length >= 5)
            {
                foreach (var (from, end) in FrenchEndings)
                {
                    if (lower.EndsWith(from, StringComparison.Ordinal))
                    {
                        options.Add((i, word[..^from.Length] + end));
                    }
                }
            }
        }
        // « a lot » (deux mots) en anglais.
        if (language != "fr")
        {
            for (var i = 0; i + 1 < words.Length; i++)
            {
                if (words[i].Equals("a", StringComparison.OrdinalIgnoreCase) && Core(words[i + 1]).Core.Equals("lot", StringComparison.OrdinalIgnoreCase))
                {
                    var joined = words.ToList();
                    var tail = Core(words[i + 1]).After;
                    joined[i] = "alot" + tail;
                    joined.RemoveAt(i + 1);
                    if (random.Next(options.Count + 1) == 0)
                    {
                        return new Typo(string.Join(' ', joined), "« alot » au lieu de « a lot » (grammaire)", "a lot");
                    }
                }
            }
        }
        if (options.Count == 0)
        {
            return null;
        }
        var (index, replacement) = options[random.Next(options.Count)];
        var (before, original, after) = Core(words[index]);
        if (char.IsUpper(original[0]))
        {
            replacement = char.ToUpperInvariant(replacement[0]) + replacement[1..];
        }
        words[index] = before + replacement + after;
        return new Typo(string.Join(' ', words), $"« {replacement} » au lieu de « {original} » (grammaire)", original);
    }

    private static (string Text, string Kind) Keyboard(string word, string language, Random random)
    {
        var kinds = new List<int> { 0, 1, 2, 3 };
        if (language == "fr" && word.Any(c => "éèêàâùûîïôç".Contains(char.ToLowerInvariant(c))))
        {
            kinds.Add(4);
            kinds.Add(4);
        }
        // Jamais la première lettre (on la rate rarement).
        var pos = 1 + random.Next(word.Length - 1);
        switch (kinds[random.Next(kinds.Count)])
        {
            case 0 when pos < word.Length - 1 && word[pos] != word[pos + 1]:
                return (word[..pos] + word[pos + 1] + word[pos] + word[(pos + 2)..], "lettres inversées");
            case 1:
                return (word.Remove(pos, 1), "lettre oubliée");
            case 2:
                return (word.Insert(pos, word[pos].ToString()), "lettre doublée");
            case 3 when Neighbor(word[pos], language, random) is { } n:
                return (word[..pos] + n + word[(pos + 1)..], "touche d'à côté");
            case 4:
                return (new string(word.Select(StripAccent).ToArray()), "accent oublié");
            default:
                return (word.Insert(pos, word[pos].ToString()), "lettre doublée");
        }
    }

    private static char? Neighbor(char c, string language, Random random)
    {
        var lower = char.ToLowerInvariant(c);
        var rows = language == "fr" ? AzertyRows : QwertyRows;
        foreach (var row in rows)
        {
            var i = row.IndexOf(lower);
            if (i < 0)
            {
                continue;
            }
            var near = new List<char>();
            if (i > 0)
            {
                near.Add(row[i - 1]);
            }
            if (i < row.Length - 1)
            {
                near.Add(row[i + 1]);
            }
            var n = near[random.Next(near.Count)];
            return char.IsUpper(c) ? char.ToUpperInvariant(n) : n;
        }
        return null;
    }

    private static char StripAccent(char c) => char.ToLowerInvariant(c) switch
    {
        'é' or 'è' or 'ê' or 'ë' => char.IsUpper(c) ? 'E' : 'e',
        'à' or 'â' => char.IsUpper(c) ? 'A' : 'a',
        'ù' or 'û' => char.IsUpper(c) ? 'U' : 'u',
        'î' or 'ï' => char.IsUpper(c) ? 'I' : 'i',
        'ô' => char.IsUpper(c) ? 'O' : 'o',
        'ç' => char.IsUpper(c) ? 'C' : 'c',
        _ => c,
    };

    // Mot sans la ponctuation collée devant et derrière (« (salut, » → « ( », « salut », « , »).
    private static (string Before, string Core, string After) Core(string word)
    {
        var start = 0;
        while (start < word.Length && !char.IsLetterOrDigit(word[start]))
        {
            start++;
        }
        var end = word.Length;
        while (end > start && !char.IsLetterOrDigit(word[end - 1]))
        {
            end--;
        }
        return (word[..start], word[start..end], word[end..]);
    }
}
