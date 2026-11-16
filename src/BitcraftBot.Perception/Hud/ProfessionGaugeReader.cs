using BitcraftBot.Perception.Capture;

namespace BitcraftBot.Perception.Hud;

// Jauge de métier, à gauche de la minicarte : visible seulement pendant une action (coupe, minage…).
// Profession : reconnue par son icône (null si icône inconnue). Level : nombre sous l'icône. Fill : 0 à 1.
public sealed record ProfessionGaugeReading(string? Profession, int? Level, double Fill, string RawLevel)
{
    public override string ToString() =>
        $"{Profession ?? "métier ?"} niv. {(Level is { } l ? l.ToString() : "?")}, {Fill * 100:0} %";
}

// Lecture de la jauge de métier (demande de Julien, 2026-11-16).
// Positions mesurées le 2026-11-16 sur des captures exactes du bot (1920×1080), jauge Forestry niveau 25.
//
//   y = 22  ─┐ pointe haute = 100 %
//            │ barre verte qui monte, traits blancs à 75 % (y 78), 50 % (y 134), 25 % (y 190)
//   y = 245 ─┘ pointe basse = 0 %
//   y 255-292 : icône du métier (hache = Forestry)
//   y 298-310 : niveau (« 25 »), même police que les coordonnées
public static class ProfessionGaugeReader
{
    // Coin de référence de la jauge en 1920×1080 : tout le reste est mesuré à partir de ce point.
    public static readonly (int X, int Y) ScreenOrigin = (1480, 0);

    // Positions relatives au coin de référence.
    private const int BarLeft = 57;      // x 1537
    private const int BarRight = 68;     // x 1548
    private const int BarTop = 22;       // 100 %
    private const int BarBottom = 245;   // 0 %
    private const int BorderSearchLeft = 50; // x 1530 : bord blanc gauche attendu vers x 1534 (± 4)
    private static readonly ScreenRect IconArea = new(38, 252, 54, 42);
    private static readonly ScreenRect LevelArea = new(35, 294, 60, 22);

    // Zones à dessiner dans la fenêtre de supervision (en coordonnées écran 1920×1080).
    public static ScreenRect ScreenArea => new(ScreenOrigin.X + BarLeft - 6, BarTop, BarRight - BarLeft + 13, LevelArea.Y + LevelArea.Height - BarTop);

    // origin : coin de référence dans l'image (ScreenOrigin pour une capture complète, (0, 0) pour une découpe).
    public static ProfessionGaugeReading? Read(Frame frame, (int X, int Y) origin)
    {
        if (!IsVisible(frame, origin))
        {
            return null;
        }
        var fill = MeasureFill(frame, origin);
        var level = GlyphReader.ReadNumber(frame, Offset(LevelArea, origin), out var rawLevel);
        var profession = RecognizeIcon(frame, Offset(IconArea, origin));
        return new ProfessionGaugeReading(profession, level, fill, rawLevel);
    }

    public static ProfessionGaugeReading? Read(Frame frame) => Read(frame, ScreenOrigin);

    // Traits blancs des quarts (75 %, 50 %, 25 %), à travers tout l'intérieur de la barre.
    private static readonly int[] TickRows = [78, 134, 190];

    // Visible si : les deux bords clairs du cadre sont présents sur la plus grande partie de la hauteur,
    // et au moins 2 des 3 traits des quarts traversent la barre.
    // L'intérieur vide est semi-transparent (sombre le jour, bleu la nuit, clair devant le ciel) : on ne s'y fie pas.
    public static bool IsVisible(Frame frame, (int X, int Y) origin)
    {
        if (origin.X + BarRight + 8 >= frame.Width || origin.Y + BarBottom >= frame.Height)
        {
            return false;
        }
        int rows = 0, framed = 0;
        for (var y = origin.Y + 40; y <= origin.Y + 230; y += 2)
        {
            rows++;
            if (HasWhite(frame, origin.X + BorderSearchLeft, origin.X + BarLeft - 1, y) &&
                HasWhite(frame, origin.X + BarRight + 1, origin.X + BarRight + 7, y))
            {
                framed++;
            }
        }
        var ticks = TickRows.Count(t => HasTick(frame, origin, t));
        return framed >= rows * 0.8 && ticks >= 2;
    }

    // Un trait : sur l'une des 3 lignes autour de la position prévue, 75 % de l'intérieur est blanc ou blanc-vert.
    private static bool HasTick(Frame frame, (int X, int Y) origin, int row)
    {
        for (var y = origin.Y + row - 1; y <= origin.Y + row + 1; y++)
        {
            int white = 0, total = 0;
            for (var x = origin.X + BarLeft + 1; x <= origin.X + BarRight - 1; x++)
            {
                total++;
                if (IsNeutralWhite(frame, x, y))
                {
                    white++;
                }
            }
            if (white * 4 >= total * 3)
            {
                return true;
            }
        }
        return false;
    }

    // Remplissage : ligne la plus haute où l'intérieur de la barre est vert.
    public static double MeasureFill(Frame frame, (int X, int Y) origin)
    {
        for (var y = origin.Y + BarTop; y <= origin.Y + BarBottom; y++)
        {
            int green = 0, total = 0;
            for (var x = origin.X + BarLeft + 2; x <= origin.X + BarRight - 2; x++)
            {
                total++;
                if (IsGaugeGreen(frame, x, y))
                {
                    green++;
                }
            }
            if (green * 2 > total)
            {
                return Math.Clamp((origin.Y + BarBottom - y) / (double)(BarBottom - BarTop), 0, 1);
            }
        }
        return 0;
    }

    // Vert de la jauge mesuré : (17, 182, 74).
    public static bool IsGaugeGreen(Frame frame, int x, int y)
    {
        var i = (y * frame.Width + x) * 4;
        int b = frame.Pixels[i], g = frame.Pixels[i + 1], r = frame.Pixels[i + 2];
        return g > 140 && r < 90 && g - r > 80 && g - b > 50;
    }

    // Icône : empreinte des pixels blancs, comparée aux icônes connues (ProfessionIconTemplates).
    public static string? RecognizeIcon(Frame frame, ScreenRect area)
    {
        var features = IconFeatures(frame, area);
        if (features is null)
        {
            return null;
        }
        string? best = null;
        var bestDistance = double.MaxValue;
        foreach (var (name, template) in ProfessionIconTemplates.Icons)
        {
            var distance = GlyphReader.Distance(features, template);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = name;
            }
        }
        return bestDistance <= MaxIconDistance ? best : null;
    }

    public const double MaxIconDistance = 0.2;

    // Boîte englobante des pixels blancs de la zone, ramenée à une grille 7 × 10 (comme les chiffres).
    public static double[]? IconFeatures(Frame frame, ScreenRect area)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1, count = 0;
        for (var y = area.Y; y < area.Y + area.Height && y < frame.Height; y++)
        {
            for (var x = area.X; x < area.X + area.Width && x < frame.Width; x++)
            {
                if (MinChannel(frame, x, y) >= 225)
                {
                    count++;
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }
        // Une icône fait ~30 × 30 pixels : en dessous de 60 pixels blancs, il n'y a pas d'icône.
        if (count < 60)
        {
            return null;
        }
        return GlyphReader.Features(frame, minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // Cadre de la jauge : gris clair neutre (188, 189, 189).
    private static bool HasWhite(Frame frame, int x0, int x1, int y)
    {
        for (var x = x0; x <= x1; x++)
        {
            if (IsNeutralWhite(frame, x, y))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsNeutralWhite(Frame frame, int x, int y)
    {
        var i = (y * frame.Width + x) * 4;
        int b = frame.Pixels[i], g = frame.Pixels[i + 1], r = frame.Pixels[i + 2];
        return Math.Min(r, Math.Min(g, b)) >= 150 && Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) <= 40;
    }

    private static ScreenRect Offset(ScreenRect r, (int X, int Y) origin) => new(r.X + origin.X, r.Y + origin.Y, r.Width, r.Height);

    private static int MinChannel(Frame frame, int x, int y)
    {
        var i = (y * frame.Width + x) * 4;
        return Math.Min(frame.Pixels[i], Math.Min(frame.Pixels[i + 1], frame.Pixels[i + 2]));
    }
}
