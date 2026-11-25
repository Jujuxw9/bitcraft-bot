namespace BitcraftBot.Input.Keyboard;

// v0.4.14 : une touche tenue se répète (W tenu = 50 lignes dans le journal le 25/11). Seul le premier appui compte,
// jusqu'au relâchement. Relâchement perdu (verrouillage de Windows…) : la touche compte de nouveau après 1,2 s
// sans répétition (la répétition de Windows démarre au plus tard 1 s après l'appui).
public sealed class KeyRepeatFilter
{
    public const long ForgetAfterMs = 1200;

    private readonly Dictionary<(uint ScanCode, bool Extended), long> _held = [];

    // Touche enfoncée à l'instant nowMs : vrai si c'est un nouvel appui, faux si c'est une répétition.
    public bool IsFirstPress(uint scanCode, bool extended, long nowMs)
    {
        var key = (scanCode, extended);
        var repeat = _held.TryGetValue(key, out var last) && nowMs - last < ForgetAfterMs;
        _held[key] = nowMs;
        return !repeat;
    }

    public void Release(uint scanCode, bool extended) => _held.Remove((scanCode, extended));
}
