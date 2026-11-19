using BitcraftBot.Perception.Native;

namespace BitcraftBot.Perception.Capture;

// Le jeu est-il vraiment visible à l'écran ? La capture copie l'écran : si une autre fenêtre
// (Bloc-notes, navigateur…) passe devant le jeu, c'est elle qui serait capturée et lue.
// Constat de l'essai v0.3.12 (2026-11-19) : captures et images de diagnostic du Bloc-notes de Julien.
// On regarde quelle fenêtre est au-dessus à quelques points clés du HUD (coordonnées, barres, cible, centre).
// Cliquer sur la fenêtre du bot (2ᵉ écran) ne gêne pas : le jeu reste visible.
public static class GameVisibility
{
    // Points en proportion de la zone du jeu : coordonnées (haut droite), cible (haut milieu),
    // barres de vie et d'endurance (bas), centre de l'écran.
    private static readonly (double X, double Y)[] Probes =
    [
        (0.913, 0.29), (0.52, 0.05), (0.40, 0.89), (0.60, 0.89), (0.50, 0.50),
    ];

    public static bool IsUncovered(GameWindow game)
    {
        var area = game.ClientArea;
        if (area.IsEmpty || Win32.IsIconic(game.Handle))
        {
            return false;
        }
        foreach (var (px, py) in Probes)
        {
            var point = new Win32.POINT { X = area.X + (int)(area.Width * px), Y = area.Y + (int)(area.Height * py) };
            var top = Win32.WindowFromPoint(point);
            var root = top == IntPtr.Zero ? IntPtr.Zero : Win32.GetAncestor(top, Win32.GA_ROOT);
            if (root != game.Handle)
            {
                return false;
            }
        }
        return true;
    }
}
