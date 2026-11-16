namespace BitcraftBot.Input.Keyboard;

// Touche d'arrêt d'urgence (D-022) : Num 5 du pavé numérique.
// Reconnue par sa position physique (scancode), que le Verr. Num soit actif ou non,
// et quelle que soit la disposition du clavier (AZERTY / QWERTY).
public static class EmergencyKey
{
    // Scancode de la touche 5 du pavé numérique.
    public const int Numpad5ScanCode = 0x4C;

    public const string DisplayName = "Num 5";

    // La touche « 5 » du pavé et aucune autre : les touches « étendues » (flèches, etc.) sont exclues.
    public static bool IsEmergencyStop(int scanCode, bool isExtended) =>
        scanCode == Numpad5ScanCode && !isExtended;
}
