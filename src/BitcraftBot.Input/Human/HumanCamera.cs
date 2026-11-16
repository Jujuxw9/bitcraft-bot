namespace BitcraftBot.Input.Human;

// Touches de caméra autorisées (D-024). Positions physiques du clavier AZERTY de Julien (scancodes).
public enum CameraKey
{
    Haut,       // Z : tourner vers le haut
    Gauche,     // Q : tourner à gauche
    Bas,        // S : tourner vers le bas
    Droite,     // D : tourner à droite
    Recentrer,  // W : recentrer la caméra
}

// Une action de caméra : maintenir une touche pendant HoldMs, puis attendre PauseMs.
public readonly record struct CameraAction(CameraKey Key, double HoldMs, double PauseMs);

public static class CameraKeys
{
    // Scancodes (position physique), indépendants de la disposition du clavier :
    // Z AZERTY = 0x11, Q AZERTY = 0x1E, S = 0x1F, D = 0x20, W AZERTY = 0x2C.
    public static ushort ScanCode(CameraKey key) => key switch
    {
        CameraKey.Haut => 0x11,
        CameraKey.Gauche => 0x1E,
        CameraKey.Bas => 0x1F,
        CameraKey.Droite => 0x20,
        CameraKey.Recentrer => 0x2C,
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };

    public static string Label(CameraKey key) => key switch
    {
        CameraKey.Haut => "Z (haut)",
        CameraKey.Gauche => "Q (gauche)",
        CameraKey.Bas => "S (bas)",
        CameraKey.Droite => "D (droite)",
        CameraKey.Recentrer => "W (recentrer)",
        _ => key.ToString(),
    };
}

// Fabrique une suite de mouvements de caméra d'allure humaine :
// surtout des rotations gauche / droite, parfois haut / bas, un recentrage de temps en temps,
// appuis et pauses de durée variable.
public sealed class HumanCamera(Random random)
{
    public CameraAction Next(int index)
    {
        // Un recentrage environ toutes les 5 actions.
        if (index > 0 && random.NextDouble() < 0.2)
        {
            return new CameraAction(CameraKey.Recentrer, 60 + random.NextDouble() * 70, 500 + random.NextDouble() * 900);
        }
        var roll = random.NextDouble();
        var key = roll < 0.4 ? CameraKey.Gauche
            : roll < 0.8 ? CameraKey.Droite
            : roll < 0.9 ? CameraKey.Haut
            : CameraKey.Bas;
        // Rotations gauche / droite plus longues que haut / bas.
        var hold = key is CameraKey.Gauche or CameraKey.Droite
            ? 150 + random.NextDouble() * 750
            : 100 + random.NextDouble() * 300;
        var pause = 350 + random.NextDouble() * random.NextDouble() * 1800;
        return new CameraAction(key, hold, pause);
    }
}
