using BitcraftBot.Perception.Capture;

namespace BitcraftBot.Perception.Hud;

// Mesure le remplissage des barres du HUD en regardant la couleur des pixels.
// Sert de vérification à la lecture du texte, et seule mesure possible pour la faim (pas de texte).
public static class BarReader
{
    public static bool IsHealthGreen(byte r, byte g, byte b) => g > 140 && r < 90 && b < 130 && g > r + 60;

    public static bool IsStaminaYellow(byte r, byte g, byte b) => r > 150 && g > 110 && b < 90 && r >= g;

    public static bool IsWhite(byte r, byte g, byte b) => r > 200 && g > 200 && b > 200;

    // Part remplie (0 à 1) d'une barre qui se vide de droite à gauche :
    // position du dernier pixel de la bonne couleur entre start et end.
    public static double? LeftAnchoredFill(Frame frame, int row, int start, int end, Func<byte, byte, byte, bool> isFilled)
    {
        if (!Fits(frame, row, end))
        {
            return null;
        }
        var last = -1;
        for (var x = start; x <= end; x++)
        {
            var (r, g, b) = Pixel(frame, x, row);
            if (isFilled(r, g, b))
            {
                last = x;
            }
        }
        return last < 0 ? 0 : Math.Clamp((last - start + 1) / (double)(end - start + 1), 0, 1);
    }

    // Part remplie (0 à 1) d'une ligne accrochée à droite qui raccourcit depuis la gauche (faim).
    public static double? RightAnchoredFill(Frame frame, int row, int start, int end, Func<byte, byte, byte, bool> isFilled)
    {
        if (!Fits(frame, row, end))
        {
            return null;
        }
        var first = -1;
        for (var x = start; x <= end; x++)
        {
            var (r, g, b) = Pixel(frame, x, row);
            if (isFilled(r, g, b))
            {
                first = x;
                break;
            }
        }
        return first < 0 ? 0 : Math.Clamp((end - first + 1) / (double)(end - start + 1), 0, 1);
    }

    // Vrai si le fond crème du panneau cible est visible sur tous les points de contrôle.
    public static bool IsTargetPanelVisible(Frame frame)
    {
        foreach (var (x, y) in HudLayout.TargetPanelProbes)
        {
            if (!Fits(frame, y, x))
            {
                return false;
            }
            var (r, g, b) = Pixel(frame, x, y);
            var isCream = r > 200 && g > 190 && b > 150 && r >= b + 10;
            if (!isCream)
            {
                return false;
            }
        }
        return true;
    }

    private static bool Fits(Frame frame, int row, int maxX) => row >= 0 && row < frame.Height && maxX < frame.Width;

    private static (byte R, byte G, byte B) Pixel(Frame frame, int x, int y)
    {
        var i = (y * frame.Width + x) * 4;
        return (frame.Pixels[i + 2], frame.Pixels[i + 1], frame.Pixels[i]);
    }
}
