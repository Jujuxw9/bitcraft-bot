using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;

namespace BitcraftBot.Perception.Screen;

// Direction de la caméra lue sur la mini-carte (v0.4.3, D-052).
// Bearing : cap en degrés, 0 = nord, 90 = est, 180 = sud, 270 = ouest (la mini-carte reste orientée nord, réglage
// LockMinimap). Confidence : part de la lumière du cône trouvée dans la direction choisie (0 à 1).
public sealed record CameraHeading(double Bearing, double Confidence, int Pixels)
{
    public string French => $"{Compass(Bearing)} ({Bearing:0}°)";

    // « vers le nord-est (45°) », « vers l'est (90°) ».
    public string Toward => (Compass(Bearing)[0] is 'e' or 'o' ? "vers l'" : "vers le ") + French;

    public static string Compass(double bearing)
    {
        string[] names = ["nord", "nord-est", "est", "sud-est", "sud", "sud-ouest", "ouest", "nord-ouest"];
        return names[(int)Math.Round(((bearing % 360) + 360) % 360 / 45.0) % 8];
    }

    // Écart d'angle le plus court entre deux caps (0 à 180).
    public static double Gap(double a, double b)
    {
        var d = Math.Abs(((a - b) % 360 + 360) % 360);
        return d > 180 ? 360 - d : d;
    }
}

// Lecture du cône de la caméra sur la mini-carte (captures du 2026-11-22) :
// - le personnage (goutte bleue cerclée de blanc) est toujours au centre de la mini-carte, en (1752, 163) ;
// - le fond de la mini-carte est uni (vert olive 98, 106, 80) ;
// - le cône part du personnage : deux bords blancs à ~90° l'un de l'autre, rempli d'un voile clair.
// Méthode : on garde les pixels clairs et gris autour du personnage (rayon 13 à 38 px), sauf ceux qui touchent
// du bleu (contours blancs des autres repères), puis on cherche la direction qui contient le plus de lumière dans
// un angle de 96° et le moins juste à côté. Pas de réponse si le cône n'est pas assez net (mieux vaut rien que faux).
public static class MinimapCone
{
    public const int CenterX = 1752;
    public const int CenterY = 163;
    private const double InnerRadius = 13;
    private const double OuterRadius = 38;
    public const double MinConfidence = 0.6;
    public const int MinPixels = 80;

    // Raison du dernier refus (pour le journal et les tests).
    public static CameraHeading? Read(Frame frame) => Read(frame, out _);

    public static CameraHeading? Read(Frame frame, out string why)
    {
        var k = frame.Height / (double)HudLayout.ReferenceHeight;
        var cx = (int)Math.Round(CenterX * frame.Width / (double)HudLayout.ReferenceWidth);
        var cy = (int)Math.Round(CenterY * k);
        var outer = OuterRadius * k;
        var inner = InnerRadius * k;
        var reach = (int)Math.Ceiling(outer) + 3;
        if (cx - reach < 0 || cy - reach < 0 || cx + reach >= frame.Width || cy + reach >= frame.Height)
        {
            why = "image trop petite";
            return null;
        }
        if (!PlayerMarkerVisible(frame, cx, cy, k))
        {
            why = "personnage pas vu au centre de la mini-carte (fenêtre ou carte ouverte ?)";
            return null;
        }

        // 1. Tout ce qui n'est ni le fond olive ni le voile du cône (repères bleus, disques gris des bâtiments,
        //    zones non explorées, triangles rouges), élargi de 2 px : leurs contours blancs imitent les bords du cône.
        var size = 2 * reach + 1;
        var blue = new bool[size * size];
        for (var dy = -reach; dy <= reach; dy++)
        {
            for (var dx = -reach; dx <= reach; dx++)
            {
                var i = ((cy + dy) * frame.Width + cx + dx) * 4;
                int b = frame.Pixels[i], g = frame.Pixels[i + 1], r = frame.Pixels[i + 2];
                if (!OliveOrCone(r, g, b))
                {
                    for (var ey = -2; ey <= 2; ey++)
                    {
                        for (var ex = -2; ex <= 2; ex++)
                        {
                            var x = dx + ex + reach;
                            var y = dy + ey + reach;
                            if (x >= 0 && y >= 0 && x < size && y < size)
                            {
                                blue[y * size + x] = true;
                            }
                        }
                    }
                }
            }
        }

        // 2. Pixels clairs autour du personnage : voile (fill) et blanc des bords (bright, les plus sûrs).
        var bright = new List<(double X, double Y)>();
        var fill = new double[360];
        var count = 0;
        for (var dy = -reach; dy <= reach; dy++)
        {
            for (var dx = -reach; dx <= reach; dx++)
            {
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < inner || dist > outer || blue[(dy + reach) * size + dx + reach])
                {
                    continue;
                }
                var i = ((cy + dy) * frame.Width + cx + dx) * 4;
                int b = frame.Pixels[i], g = frame.Pixels[i + 1], r = frame.Pixels[i + 2];
                var spread = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
                var lum = 0.299 * r + 0.587 * g + 0.114 * b;
                if (spread >= 25 || lum < 112)
                {
                    continue;
                }
                count++;
                if (lum >= 150)
                {
                    bright.Add((dx / k, dy / k));
                }
                else
                {
                    fill[(int)((Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360) % 360] += 1;
                }
            }
        }
        if (count < MinPixels * k * k)
        {
            why = $"cône pas vu ({count} pixels clairs)";
            return null;
        }

        // 3. Bords : pour chaque rayon (1°), nombre de pixels blancs à moins de 1,2 px du rayon.
        var line = new double[360];
        for (var a = 0; a < 360; a++)
        {
            var ux = Math.Cos(a * Math.PI / 180);
            var uy = Math.Sin(a * Math.PI / 180);
            foreach (var (x, y) in bright)
            {
                var along = x * ux + y * uy;
                if (along > 0 && Math.Abs(-x * uy + y * ux) <= 1.2)
                {
                    line[a] += 1;
                }
            }
        }

        // 4. Deux bords écartés de 86 à 110°, du voile entre eux, rien juste à l'extérieur.
        //    Si un repère cache un bord, l'autre bord et le voile placent le cône.
        double bestScore = double.MinValue;
        int bestStart = 0, bestWidth = 0;
        for (var a = 0; a < 360; a++)
        {
            for (var w = 86; w <= 110; w += 2)
            {
                var score = EdgeWeight * (line[a] + line[(a + w) % 360]);
                for (var j = 6; j <= w - 6; j++)
                {
                    score += fill[(a + j) % 360];
                }
                for (var j = 4; j <= 30; j++)
                {
                    score -= fill[(a - j + 360) % 360] + fill[(a + w + j) % 360] + line[(a - j + 360) % 360] + line[(a + w + j) % 360];
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestStart = a;
                    bestWidth = w;
                }
            }
        }
        var edgeA = line[bestStart];
        var edgeB = line[(bestStart + bestWidth) % 360];
        // Un bord net au moins (≥ 10 pixels blancs alignés) : sinon ce n'est pas le cône.
        if (Math.Max(edgeA, edgeB) < 10)
        {
            why = $"bords du cône pas vus ({Math.Max(edgeA, edgeB):0} pixels blancs alignés)";
            return null;
        }
        // Confiance : 1 si les deux bords sont nets, 0,5 si un seul.
        var confidence = Math.Min(1, (Math.Min(1, edgeA / 15) + Math.Min(1, edgeB / 15)) / 2);
        if (confidence < MinConfidence * 0.8)
        {
            why = $"un seul bord du cône à moitié vu";
            return null;
        }
        // Angle à l'écran (0 = droite, 90 = bas) → cap (0 = nord = haut de la mini-carte).
        var screenAngle = bestStart + bestWidth / 2.0;
        var bearing = ((screenAngle + 90) % 360 + 360) % 360;
        why = "";
        return new CameraHeading(bearing, confidence, count);
    }

    // Un bord blanc pèse plus que le voile : c'est une ligne nette, rarement imitée.
    private const double EdgeWeight = 4;

    // Fond de la mini-carte (olive 98, 106, 80, à ± 14 près) ou voile clair et gris du cône.
    private static bool OliveOrCone(int r, int g, int b)
    {
        if (Math.Abs(r - 98) <= 14 && Math.Abs(g - 106) <= 14 && Math.Abs(b - 80) <= 14)
        {
            return true;
        }
        var spread = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
        return spread < 25 && 0.299 * r + 0.587 * g + 0.114 * b >= 105;
    }

    // Goutte du personnage : bleu (21, 86, 126) au centre.
    private static bool PlayerMarkerVisible(Frame frame, int cx, int cy, double k)
    {
        var radius = Math.Max(2, (int)Math.Round(5 * k));
        var hits = 0;
        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                var i = ((cy + dy) * frame.Width + cx + dx) * 4;
                int b = frame.Pixels[i], g = frame.Pixels[i + 1], r = frame.Pixels[i + 2];
                if (Math.Abs(r - 21) <= 22 && Math.Abs(g - 86) <= 22 && Math.Abs(b - 126) <= 22)
                {
                    hits++;
                }
            }
        }
        return hits >= (2 * radius + 1) * (2 * radius + 1) / 4;
    }
}
