using System.Text.RegularExpressions;
using BitcraftBot.Perception.Capture;

namespace BitcraftBot.Perception.Hud;

// Un caractère trouvé à l'écran : sa boîte et son « empreinte » (encre moyenne sur une grille fixe).
public sealed record Glyph(int X, int Y, int Width, int Height, double[] Features, bool IsComma);

// Lecture des coordonnées par comparaison à des modèles de caractères (Q-016, méthode B).
// Le texte du HUD est toujours blanc, dans la même police et la même taille. Méthode :
// 1. repérer les taches blanches de la taille d'un caractère (le décor blanc, plus grand ou plus petit, est ignoré) ;
// 2. les regrouper en lignes (même bas de ligne, caractères proches) ;
// 3. comparer chaque caractère aux modèles 0-9, N et E.
public static partial class GlyphReader
{
    // Taille de la grille d'empreinte : chaque caractère est ramené à 7 colonnes × 10 lignes.
    public const int GridColumns = 7;
    public const int GridRows = 10;

    // Pixel « encre » : les trois couleurs très hautes (texte blanc). Mesuré : texte ≥ 240, fonds clairs < 225.
    private const int StrongInk = 225;
    // Seuil plus large pour relier les pixels d'un même caractère (bords lissés).
    private const int LinkInk = 200;
    private const int SoftInkLow = 170;
    private const int SoftInkHigh = 240;

    // Taille d'un caractère des coordonnées en 1920×1080 : 9 à 10 pixels de haut.
    private const int MinCharHeight = 7;
    private const int MaxCharHeight = 12;
    private const int MaxCharWidth = 16;
    // Écart maximal entre deux caractères d'une même ligne (l'espace après la virgule fait ~20 px).
    private const int MaxCharGap = 30;

    // Au-delà de cet écart moyen, le caractère est jugé inconnu (lecture refusée).
    public const double MaxDistance = 0.22;

    // Lit « N 8945, E 6704 » dans la zone donnée. Renvoie null si rien de sûr n'est trouvé.
    // text : le texte reconnu (pour le journal), même en cas d'échec.
    public static (int North, int East)? ReadCoordinates(Frame frame, ScreenRect area, out string text)
    {
        text = string.Empty;
        foreach (var line in FindLines(frame, area))
        {
            var lineText = Classify(line, GlyphTemplates.Coordinates);
            if (lineText is null)
            {
                continue;
            }
            text = lineText;
            var match = CoordinatesRegex().Match(lineText);
            if (match.Success)
            {
                return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
            }
        }
        return null;
    }

    // Lit un nombre entier (ex. niveau de métier « 25 ») avec les modèles des chiffres. Null si pas sûr.
    public static int? ReadNumber(Frame frame, ScreenRect area, out string text)
    {
        text = string.Empty;
        var digits = GlyphTemplates.Coordinates.Where(p => char.IsDigit(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        foreach (var line in FindLines(frame, area, minChars: 1))
        {
            if (line.Any(g => g.IsComma))
            {
                continue;
            }
            var lineText = Classify(line, digits);
            if (lineText is not null && int.TryParse(lineText, out var value))
            {
                text = lineText;
                return value;
            }
        }
        return null;
    }

    // Toutes les lignes de texte blanc de la zone, de haut en bas, chacune de gauche à droite.
    public static List<List<Glyph>> FindLines(Frame frame, ScreenRect area, int minChars = 3)
    {
        var blobs = FindBlobs(frame, Clip(frame, area));
        var chars = blobs.Where(b => b.Height is >= MinCharHeight and <= MaxCharHeight && b.Width <= MaxCharWidth).ToList();
        var commas = blobs.Where(b => b.Height is >= 2 and <= 5 && b.Width <= 4).ToList();

        // Regroupement par bas de ligne (à 2 pixels près).
        var lines = new List<List<Blob>>();
        foreach (var blob in chars.OrderBy(b => b.Bottom).ThenBy(b => b.X))
        {
            var line = lines.FirstOrDefault(l => Math.Abs(l[0].Bottom - blob.Bottom) <= 2);
            if (line is null)
            {
                lines.Add([blob]);
            }
            else
            {
                line.Add(blob);
            }
        }

        var result = new List<List<Glyph>>();
        foreach (var line in lines.OrderBy(l => l[0].Bottom))
        {
            var top = line.Min(b => b.Y);
            var bottom = line.Max(b => b.Bottom);
            // Virgules : petites taches dont le haut est dans la moitié basse de la ligne.
            var members = line.Concat(commas.Where(c => c.Y > (top + bottom) / 2 && c.Y <= bottom + 2)).OrderBy(b => b.X).ToList();
            members = MergeOverlapping(members);

            // Découpe en morceaux quand deux caractères sont trop éloignés (textes différents sur la même hauteur).
            var run = new List<Blob>();
            foreach (var blob in members)
            {
                if (run.Count > 0 && blob.X - run[^1].Right > MaxCharGap)
                {
                    AddRun(frame, run, top, bottom, minChars, result);
                    run = [];
                }
                run.Add(blob);
            }
            AddRun(frame, run, top, bottom, minChars, result);
        }
        return result;
    }

    // Compare chaque caractère aux modèles. Renvoie null dès qu'un caractère n'est pas reconnu.
    public static string? Classify(IReadOnlyList<Glyph> glyphs, IReadOnlyDictionary<char, double[]> templates)
    {
        if (glyphs.Count == 0 || templates.Count == 0)
        {
            return null;
        }
        var chars = new char[glyphs.Count];
        for (var i = 0; i < glyphs.Count; i++)
        {
            if (glyphs[i].IsComma)
            {
                chars[i] = ',';
                continue;
            }
            var (best, distance) = BestTemplate(glyphs[i].Features, templates);
            if (distance > MaxDistance)
            {
                return null;
            }
            chars[i] = best;
        }
        return new string(chars);
    }

    public static (char Character, double Distance) BestTemplate(double[] features, IReadOnlyDictionary<char, double[]> templates)
    {
        var best = '?';
        var bestDistance = double.MaxValue;
        foreach (var (character, template) in templates)
        {
            var distance = Distance(features, template);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = character;
            }
        }
        return (best, bestDistance);
    }

    // Écart moyen entre deux empreintes (0 = identiques, 1 = opposées).
    public static double Distance(double[] a, double[] b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            sum += Math.Abs(a[i] - b[i]);
        }
        return sum / a.Length;
    }

    // Empreinte : la boîte du caractère est ramenée à une grille fixe, chaque case = encre moyenne (0 à 1).
    // La dernière valeur est la largeur relative (distingue le « 1 », étroit, des autres chiffres).
    public static double[] Features(Frame frame, int x0, int y0, int width, int height)
    {
        var features = new double[GridColumns * GridRows + 1];
        for (var gy = 0; gy < GridRows; gy++)
        {
            for (var gx = 0; gx < GridColumns; gx++)
            {
                // Zone de l'image couverte par cette case (en pixels fractionnaires).
                var sx0 = x0 + (double)gx * width / GridColumns;
                var sx1 = x0 + (double)(gx + 1) * width / GridColumns;
                var sy0 = y0 + (double)gy * height / GridRows;
                var sy1 = y0 + (double)(gy + 1) * height / GridRows;
                features[gy * GridColumns + gx] = AverageInk(frame, sx0, sx1, sy0, sy1);
            }
        }
        features[^1] = Math.Clamp(width / (double)height, 0, 1);
        return features;
    }

    // Tache blanche : boîte englobante des pixels reliés entre eux.
    private readonly record struct Blob(int X, int Y, int Width, int Height, int Strong)
    {
        public int Right => X + Width - 1;
        public int Bottom => Y + Height - 1;
    }

    private static void AddRun(Frame frame, List<Blob> run, int top, int bottom, int minChars, List<List<Glyph>> result)
    {
        // Coordonnées : au moins 3 caractères (« N 0, E 0 » en a 6). Un niveau de métier peut n'en avoir qu'un.
        if (run.Count(b => b.Height >= MinCharHeight) < minChars)
        {
            return;
        }
        // Tous les caractères ont la même hauteur : on prend le haut et le bas médians de la ligne.
        // Ainsi un caractère dont un bout est mal vu (fond clair, nuit) garde la bonne empreinte.
        var tops = run.Where(b => b.Height >= MinCharHeight).Select(b => b.Y).Order().ToList();
        var bottoms = run.Where(b => b.Height >= MinCharHeight).Select(b => b.Bottom).Order().ToList();
        var lineTop = tops[tops.Count / 2];
        var lineBottom = bottoms[bottoms.Count / 2];
        var glyphs = run.Select(b =>
        {
            var isComma = b.Height < MinCharHeight;
            return isComma
                ? new Glyph(b.X, b.Y, b.Width, b.Height, [], true)
                : new Glyph(b.X, lineTop, b.Width, lineBottom - lineTop + 1, Features(frame, b.X, lineTop, b.Width, lineBottom - lineTop + 1), false);
        }).ToList();
        result.Add(glyphs);
    }

    // Un caractère peut être coupé en deux taches (bord lissé trop sombre) : on fusionne les taches qui se chevauchent en largeur.
    private static List<Blob> MergeOverlapping(List<Blob> blobs)
    {
        var merged = new List<Blob>();
        foreach (var blob in blobs)
        {
            if (merged.Count > 0 && blob.X <= merged[^1].Right)
            {
                var last = merged[^1];
                var x = Math.Min(last.X, blob.X);
                var y = Math.Min(last.Y, blob.Y);
                var right = Math.Max(last.Right, blob.Right);
                var bottom = Math.Max(last.Bottom, blob.Bottom);
                merged[^1] = new Blob(x, y, right - x + 1, bottom - y + 1, last.Strong + blob.Strong);
            }
            else
            {
                merged.Add(blob);
            }
        }
        return merged;
    }

    // Repère les taches de pixels clairs reliés (8 voisins), avec au moins 3 pixels blancs francs.
    private static List<Blob> FindBlobs(Frame frame, ScreenRect area)
    {
        var width = area.Width;
        var height = area.Height;
        var visited = new bool[width * height];
        var blobs = new List<Blob>();
        var stack = new Stack<int>();
        for (var start = 0; start < visited.Length; start++)
        {
            if (visited[start] || MinChannel(frame, area.X + start % width, area.Y + start / width) < LinkInk)
            {
                continue;
            }
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1, strong = 0, size = 0;
            visited[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                int px = p % width, py = p / width;
                size++;
                if (MinChannel(frame, area.X + px, area.Y + py) >= StrongInk)
                {
                    strong++;
                }
                minX = Math.Min(minX, px);
                maxX = Math.Max(maxX, px);
                minY = Math.Min(minY, py);
                maxY = Math.Max(maxY, py);
                // Une tache énorme est du décor : on arrête de l'explorer (elle sera rejetée par sa taille).
                if (size > 400)
                {
                    continue;
                }
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        int nx = px + dx, ny = py + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        {
                            continue;
                        }
                        var n = ny * width + nx;
                        if (!visited[n] && MinChannel(frame, area.X + nx, area.Y + ny) >= LinkInk)
                        {
                            visited[n] = true;
                            stack.Push(n);
                        }
                    }
                }
            }
            if (strong >= 3 || (strong >= 1 && maxY - minY <= 5))
            {
                blobs.Add(new Blob(area.X + minX, area.Y + minY, maxX - minX + 1, maxY - minY + 1, strong));
            }
        }
        return blobs;
    }

    private static ScreenRect Clip(Frame frame, ScreenRect area)
    {
        var x = Math.Max(area.X, 0);
        var y = Math.Max(area.Y, 0);
        var right = Math.Min(area.X + area.Width, frame.Width);
        var bottom = Math.Min(area.Y + area.Height, frame.Height);
        return new ScreenRect(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y));
    }

    private static double AverageInk(Frame frame, double sx0, double sx1, double sy0, double sy1)
    {
        double total = 0, weight = 0;
        for (var y = (int)Math.Floor(sy0); y < (int)Math.Ceiling(sy1); y++)
        {
            var wy = Math.Min(sy1, y + 1) - Math.Max(sy0, y);
            for (var x = (int)Math.Floor(sx0); x < (int)Math.Ceiling(sx1); x++)
            {
                var wx = Math.Min(sx1, x + 1) - Math.Max(sx0, x);
                var w = wx * wy;
                if (w <= 0 || x < 0 || y < 0 || x >= frame.Width || y >= frame.Height)
                {
                    continue;
                }
                total += w * Ink(frame, x, y);
                weight += w;
            }
        }
        return weight > 0 ? total / weight : 0;
    }

    // Encre « douce » : 0 pour un fond, 1 pour un texte blanc franc, entre les deux sur les bords lissés.
    private static double Ink(Frame frame, int x, int y) =>
        Math.Clamp((MinChannel(frame, x, y) - SoftInkLow) / (double)(SoftInkHigh - SoftInkLow), 0, 1);

    private static int MinChannel(Frame frame, int x, int y)
    {
        var i = (y * frame.Width + x) * 4;
        return Math.Min(frame.Pixels[i], Math.Min(frame.Pixels[i + 1], frame.Pixels[i + 2]));
    }

    [GeneratedRegex(@"^N(\d{1,5}),E(\d{1,5})$")]
    private static partial Regex CoordinatesRegex();
}
