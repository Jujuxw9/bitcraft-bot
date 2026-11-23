using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;

namespace BitcraftBot.Perception.Screen;

// Zone de saisie du chat (v0.4.5, module « @GPT » et W tapé dans le chat).
// Mesuré sur la capture du 2026-11-23 à 22:32:17 (chat ouvert, 1920 × 1080) :
// - cadre blanc (254, 254, 254) de la zone de saisie : de x = 182 à x = 476, bord du haut vers y = 968, du bas vers y = 996 ;
// - à gauche, le bouton du canal (« REGION ») : de x = 54 à x = 172, même hauteur ;
// - chat fermé : ni cadre ni bouton (seulement les derniers messages, sans fond).
public static class ChatInput
{
    // Cadre de la zone de saisie (bords inclus), en pixels 1920 × 1080.
    public const int BoxLeft = 182;
    public const int BoxRight = 476;
    public const int TopY = 968;
    public const int BottomY = 996;

    // Bouton du canal, lu par OCR (« REGION »).
    public static readonly ScreenRect ChannelZone = new(56, 971, 114, 24);

    // Le chat est-il ouvert (zone de saisie visible) ?
    public static bool IsOpen(Frame frame) => Score(frame) >= 0.85;

    // Part des colonnes où les bords blancs du haut ET du bas sont vus (0 à 1).
    public static double Score(Frame frame)
    {
        var sx = frame.Width / (double)HudLayout.ReferenceWidth;
        var sy = frame.Height / (double)HudLayout.ReferenceHeight;
        var seen = 0;
        var hits = 0;
        for (var x = BoxLeft + 16; x <= BoxRight - 16; x += 4)
        {
            seen++;
            var px = (int)Math.Round(x * sx);
            if (WhiteNear(frame, px, (int)Math.Round(TopY * sy)) && WhiteNear(frame, px, (int)Math.Round(BottomY * sy)))
            {
                hits++;
            }
        }
        return seen == 0 ? 0 : hits / (double)seen;
    }

    // Un pixel presque blanc à 3 lignes près (le bord fait 2 px, l'image peut être décalée d'un pixel).
    private static bool WhiteNear(Frame frame, int x, int y)
    {
        if (x < 0 || x >= frame.Width)
        {
            return false;
        }
        for (var dy = -3; dy <= 3; dy++)
        {
            var yy = y + dy;
            if (yy < 0 || yy >= frame.Height)
            {
                continue;
            }
            var i = (yy * frame.Width + x) * 4;
            if (frame.Pixels[i] >= 235 && frame.Pixels[i + 1] >= 235 && frame.Pixels[i + 2] >= 235)
            {
                return true;
            }
        }
        return false;
    }

    // Texte lu sur le bouton du canal, nettoyé (majuscules, lettres seulement). Ex. « REGION ».
    // Chiffres souvent lus à la place des lettres : 1 → I, 0 → O, 5 → S.
    public static string CleanChannel(string text) =>
        new(text.ToUpperInvariant().Select(c => c switch { '1' => 'I', '0' => 'O', '5' => 'S', _ => c }).Where(char.IsLetter).ToArray());

    // Le canal affiché est-il « Région » ? Tolérant à une lettre mal lue (REG10N, RECION…).
    public static bool IsRegion(string text) => IsChannel(text, "REGION");

    // Le canal affiché est-il celui attendu (« LOCAL », « REGION », « TRADE », « GROUP »…) ? (v0.4.7)
    // Tolérant à une lettre mal lue, à condition que le mot ait au moins 5 lettres.
    public static bool IsChannel(string text, string target)
    {
        var t = CleanChannel(text);
        if (t.Contains(target, StringComparison.Ordinal))
        {
            return true;
        }
        if (target.Length < 5 || t.Length < target.Length - 1 || t.Length > target.Length + 1)
        {
            return false;
        }
        var same = 0;
        for (var i = 0; i < Math.Min(t.Length, target.Length); i++)
        {
            if (t[i] == target[i])
            {
                same++;
            }
        }
        return same >= target.Length - 1;
    }
}
