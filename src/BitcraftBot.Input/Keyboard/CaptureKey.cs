namespace BitcraftBot.Input.Keyboard;

// Touche de capture d'écran du jeu : Num 0 du pavé numérique (demande de Julien, 2 écrans).
// Même principe que la touche d'urgence : reconnue par sa position physique, touche « Inser » (étendue) exclue.
public static class CaptureKey
{
    public const int Numpad0ScanCode = 0x52;

    public const string DisplayName = "Num 0";

    public static bool IsCapture(int scanCode, bool isExtended) =>
        scanCode == Numpad0ScanCode && !isExtended;
}
