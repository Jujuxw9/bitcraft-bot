using System.Text;
using System.Text.RegularExpressions;

namespace BitcraftBot.Perception.Hud;

// Transforme le texte lu par l'OCR en valeurs. Tolère les petites erreurs de lecture.
public static partial class HudParser
{
    // Corrige les confusions classiques de l'OCR dans une suite de chiffres (O → 0, l → 1…).
    public static string NormalizeDigits(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            builder.Append(c switch
            {
                'O' or 'o' or 'Q' or 'D' => '0',
                'l' or 'L' or 'I' or 'i' or '|' or '!' => '1',
                'Z' or 'z' => '2',
                'S' or 's' => '5',
                'B' => '8',
                'g' => '9',
                _ => c,
            });
        }
        return builder.ToString();
    }

    // « N 8927, E 6707 » → (8927, 6707).
    public static (int North, int East)? ParseCoordinates(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        var match = CoordinatesRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }
        var north = ParseNumber(match.Groups[1].Value);
        var east = ParseNumber(match.Groups[2].Value);
        if (north is null || east is null)
        {
            return null;
        }
        return (north.Value, east.Value);
    }

    // « 65/100 » → (65, 100). La valeur ne peut pas dépasser le maximum.
    public static (int Value, int Max)? ParseFraction(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        var match = FractionRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }
        var value = ParseNumber(match.Groups[1].Value);
        var max = ParseNumber(match.Groups[2].Value);
        if (value is null || max is null || max <= 0 || value > max)
        {
            return null;
        }
        return (value.Value, max.Value);
    }

    // « KELDAN (R18) » → (« Keldan », 18).
    public static (string Name, int Number)? ParseRegion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        var match = RegionRegex().Match(text.ToUpperInvariant());
        if (!match.Success)
        {
            return null;
        }
        var number = ParseNumber(match.Groups[2].Value);
        if (number is null)
        {
            return null;
        }
        return (TitleCase(MergeLooseLetters(match.Groups[1].Value)), number.Value);
    }

    // L'OCR coupe parfois un mot : « KELDA N » → « KELDAN ». Une lettre isolée est recollée au mot d'avant.
    public static string MergeLooseLetters(string text)
    {
        var words = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        for (var i = words.Count - 1; i > 0; i--)
        {
            if (words[i].Length == 1 && char.IsLetter(words[i][0]))
            {
                words[i - 1] += words[i];
                words.RemoveAt(i);
            }
        }
        return string.Join(' ', words);
    }

    // Nettoie un nom lu (zone, ressource) : espaces en trop, majuscules.
    public static string? CleanName(string? text, bool titleCase)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        var cleaned = SpacesRegex().Replace(text.Trim(), " ");
        cleaned = cleaned.Trim('.', ',', '-', '_', '\'', '"', ' ');
        if (cleaned.Length < 3)
        {
            return null;
        }
        return titleCase ? TitleCase(cleaned) : cleaned;
    }

    private static int? ParseNumber(string raw)
    {
        var digits = NormalizeDigits(raw).Replace(" ", string.Empty).Replace(".", string.Empty);
        return int.TryParse(digits, out var value) ? value : null;
    }

    private static string TitleCase(string text)
    {
        var words = text.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    // Lettre N, nombre, séparateur quelconque, lettre E, nombre. Les chiffres peuvent être mal lus.
    [GeneratedRegex(@"N\s*((?:[0-9OoQDlLIi|SsZzBg] ?){3,6})[^0-9A-Za-z]{0,4}\s*[E£€]\s*((?:[0-9OoQDlLIi|SsZzBg] ?){3,6})", RegexOptions.IgnoreCase)]
    private static partial Regex CoordinatesRegex();

    [GeneratedRegex(@"([0-9OoQDlIi|SsZzB]{1,6})\s*[/\\|]\s*([0-9OoQDlIi|SsZzB]{1,6})")]
    private static partial Regex FractionRegex();

    [GeneratedRegex(@"([A-Z][A-Z' ]{1,30}?)\s*\(\s*R\s*([0-9OQDILSZB|]{1,3})\s*\)")]
    private static partial Regex RegionRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpacesRegex();
}
