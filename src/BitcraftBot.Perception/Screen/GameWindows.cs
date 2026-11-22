using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Perception.Ocr;

namespace BitcraftBot.Perception.Screen;

public enum WindowKind
{
    // Fenêtre du jeu : corps crème, bandeau bleu avec le titre (inventaire, chariot, banque…).
    Fenetre,
    // Carte du monde : tout l'écran gris foncé uni.
    Carte,
}

// Fenêtre du jeu vue à l'écran. Title : titre lu (« INVENTORY »), vide si pas lu. Area : zone crème, en pixels de l'image.
public sealed record OpenWindow(WindowKind Kind, ScreenRect Area, string Title = "")
{
    public string French => Kind == WindowKind.Carte
        ? "carte du monde"
        : string.IsNullOrWhiteSpace(Title) ? $"fenêtre en ({Area.X}, {Area.Y})" : $"« {Title} »";
}

// Fenêtres du jeu ouvertes (v0.4.3, D-052 : « détecter les fenêtres du jeu ouvertes avant toute action »).
// Mesuré sur les captures du 2026-11-16 (inventaire et chariot ouverts, carte du monde) :
// - corps des fenêtres crème (233, 223, 196), cases un peu plus foncées (225, 215, 189) ;
// - carte du monde : fond gris uni (45, 45, 45) sur tout l'écran.
// Le HUD normal a aussi du crème (panneau de la cible en haut, panneau d'action en bas, objets ramassés à gauche) :
// ces zones sont ignorées, et une fenêtre doit être grande (au moins 150 × 150 px pleins de crème).
public static class GameWindows
{
    private const int Cell = 24;

    // Zones du HUD où le crème est normal (1920 × 1080).
    private static readonly ScreenRect[] HudZones =
    [
        new(630, 0, 660, 110),     // panneau de la cible
        new(600, 800, 720, 280),   // panneau d'action, jauges, barre d'action
        new(1590, 0, 330, 390),    // mini-carte, heure, coordonnées
        new(0, 990, 1920, 90),     // icônes du bas
    ];

    public static IReadOnlyList<OpenWindow> Detect(Frame frame)
    {
        if (IsWorldMap(frame))
        {
            return [new OpenWindow(WindowKind.Carte, new ScreenRect(0, 0, frame.Width, frame.Height))];
        }
        var sx = frame.Width / (double)HudLayout.ReferenceWidth;
        var sy = frame.Height / (double)HudLayout.ReferenceHeight;
        var cols = frame.Width / Cell;
        var rows = frame.Height / Cell;
        var cream = new bool[cols * rows];
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var x = col * Cell;
                var y = row * Cell;
                if (InHud(x / sx, y / sy) || InHud((x + Cell) / sx, (y + Cell) / sy))
                {
                    continue;
                }
                var hits = 0;
                var seen = 0;
                for (var py = y + 1; py < y + Cell; py += 3)
                {
                    for (var px = x + 1; px < x + Cell; px += 3)
                    {
                        seen++;
                        if (IsCream(frame, px, py))
                        {
                            hits++;
                        }
                    }
                }
                cream[row * cols + col] = hits * 10 >= seen * 6;
            }
        }

        // Groupes de cases crème voisines.
        var found = new List<OpenWindow>();
        var done = new bool[cream.Length];
        for (var start = 0; start < cream.Length; start++)
        {
            if (!cream[start] || done[start])
            {
                continue;
            }
            int minC = int.MaxValue, minR = int.MaxValue, maxC = 0, maxR = 0, n = 0;
            var stack = new Stack<int>();
            stack.Push(start);
            done[start] = true;
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                var col = c % cols;
                var row = c / cols;
                n++;
                minC = Math.Min(minC, col);
                maxC = Math.Max(maxC, col);
                minR = Math.Min(minR, row);
                maxR = Math.Max(maxR, row);
                foreach (var next in new[] { col > 0 ? c - 1 : -1, col < cols - 1 ? c + 1 : -1, row > 0 ? c - cols : -1, row < rows - 1 ? c + cols : -1 })
                {
                    if (next >= 0 && cream[next] && !done[next])
                    {
                        done[next] = true;
                        stack.Push(next);
                    }
                }
            }
            var width = (maxC - minC + 1) * Cell;
            var height = (maxR - minR + 1) * Cell;
            var boxCells = (maxC - minC + 1) * (maxR - minR + 1);
            // Grande et assez remplie (au moins 40 cases et 30 % du rectangle : deux fenêtres superposées font un « L ») :
            // une fenêtre. Petite ou éparse : objet ramassé, sable, ciel clair…
            if (width >= 150 * sx && height >= 150 * sy && n >= 40 && n * 10 >= boxCells * 3)
            {
                found.Add(new OpenWindow(WindowKind.Fenetre, new ScreenRect(minC * Cell, minR * Cell, width, height)));
            }
        }
        return found;
    }

    // Titre lu dans le bandeau bleu au-dessus du corps crème (texte clair).
    public static async Task<OpenWindow> ReadTitleAsync(OcrReader ocr, Frame frame, OpenWindow window)
    {
        if (window.Kind != WindowKind.Fenetre)
        {
            return window;
        }
        var top = Math.Max(0, window.Area.Y - 110);
        var band = new ScreenRect(window.Area.X, top, Math.Min(window.Area.Width, frame.Width - window.Area.X), Math.Max(1, window.Area.Y + 30 - top));
        var text = await ocr.ReadAsync(frame, band, TextStyle.BrightText, 200, scale: 2);
        return window with { Title = Clean(text) };
    }

    private static string Clean(string text)
    {
        var t = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
        return t.Length > 40 ? t[..40] : t;
    }

    // Carte du monde : au moins 45 % de points gris foncé uni (± 4 entre les couleurs, 35 à 60 de luminosité).
    public static bool IsWorldMap(Frame frame)
    {
        var hits = 0;
        var seen = 0;
        for (var y = frame.Height / 12; y < frame.Height * 11 / 12; y += frame.Height / 24)
        {
            for (var x = frame.Width / 20; x < frame.Width * 4 / 5; x += frame.Width / 40)
            {
                seen++;
                var i = (y * frame.Width + x) * 4;
                int b = frame.Pixels[i], g = frame.Pixels[i + 1], r = frame.Pixels[i + 2];
                if (Math.Abs(r - g) <= 4 && Math.Abs(g - b) <= 4 && r is >= 35 and <= 60)
                {
                    hits++;
                }
            }
        }
        return hits * 100 >= seen * 45;
    }

    private static bool IsCream(Frame frame, int x, int y)
    {
        var i = (y * frame.Width + x) * 4;
        int b = frame.Pixels[i], g = frame.Pixels[i + 1], r = frame.Pixels[i + 2];
        return r is >= 205 and <= 245 && g is >= 195 and <= 235 && b is >= 168 and <= 212
               && r - g is >= 4 and <= 18 && g - b is >= 18 and <= 36;
    }

    private static bool InHud(double x, double y) =>
        HudZones.Any(z => x >= z.X && x < z.X + z.Width && y >= z.Y && y < z.Y + z.Height);
}
