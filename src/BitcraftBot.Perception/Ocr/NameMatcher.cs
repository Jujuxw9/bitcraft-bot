namespace BitcraftBot.Perception.Ocr;

// Rapproche un texte lu par l'OCR du nom connu le plus proche (distance d'édition).
public static class NameMatcher
{
    // Renvoie le nom connu si au plus maxErrors lettres diffèrent (et au plus 20 % du nom), sinon null.
    public static string? BestMatch(string? text, IEnumerable<string> candidates, int maxErrors = 3)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        var cleaned = text.Trim().ToLowerInvariant();
        string? best = null;
        var bestDistance = int.MaxValue;
        foreach (var candidate in candidates)
        {
            var distance = Distance(cleaned, candidate.ToLowerInvariant());
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }
        if (best is null)
        {
            return null;
        }
        var allowed = Math.Min(maxErrors, Math.Max(1, best.Length / 5));
        return bestDistance <= allowed ? best : null;
    }

    // Distance de Levenshtein : nombre minimal de lettres à changer, ajouter ou retirer.
    public static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
}
