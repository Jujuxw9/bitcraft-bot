using Microsoft.Win32;

namespace BitcraftBot.Perception.Screen;

// Un réglage du jeu dont dépend le bot, la valeur lue et la valeur attendue.
public sealed record GameSettingCheck(string Name, string French, string? Value, string Expected, bool Ok, bool Required, string Why);

// Réglages du jeu BitCraft (v0.4.2, demande de Julien) : le jeu les range dans le registre de Windows,
// HKCU\Software\ClockworkLabs\BitCraft (réglages Unity). LECTURE SEULE, et seulement une liste blanche de noms :
// le bot ne lit jamais le jeton ni l'adresse e-mail rangés au même endroit. Rien n'est jamais écrit.
public static class GameSettings
{
    public const string KeyPath = @"Software\ClockworkLabs\BitCraft";

    // Nom du réglage (sans le suffixe « _hNNNN » d'Unity) → attendu. Required : le bot ne marche pas bien sans.
    private static readonly (string Name, string French, string Expected, bool Required, string Why)[] Wanted =
    [
        ("EarlyAccess:Language", "langue", "en", true, "lecture des textes en anglais (D-015)"),
        ("EarlyAccess:UIScale", "taille de l'interface", "1", true, "zones du HUD mesurées à l'échelle 1 (D-006)"),
        ("Screenmanager Resolution Width", "largeur", "1920", true, "zones du HUD mesurées en 1920 × 1080 (D-006)"),
        ("Screenmanager Resolution Height", "hauteur", "1080", true, "zones du HUD mesurées en 1920 × 1080 (D-006)"),
        ("EarlyAccess:LockMinimap", "mini-carte orientée nord", "1", true, "lecture de la direction de la caméra sur la mini-carte"),
        ("EarlyAccess:MinimapRadius", "zoom de la mini-carte", "500", false, "taille du cône de la caméra sur la mini-carte"),
        ("EarlyAccess:CameraObjectCollision", "collision de la caméra", "0", false, "avec 1, la caméra avance quand un arbre la gêne : vue imprévisible (À vérifier)"),
        ("EarlyAccess:CameraPanSmoothing", "caméra adoucie", "0", false, "avec 1, W ramène la caméra en douceur (1 s, D-050)"),
        ("EarlyAccess:AutoOpenStoragesInRange", "ouvrir les coffres proches", "0", false, "avec 1, des fenêtres s'ouvrent seules près d'un coffre et cachent la vue"),
        ("EarlyAccess:GrassDensity", "herbe", "0", false, "l'herbe haute cache le pied des arbres et le contour vert du survol"),
    ];

    public static IReadOnlyList<GameSettingCheck> Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (key is null)
        {
            return [];
        }
        var names = key.GetValueNames();
        return Check(name =>
        {
            var full = names.FirstOrDefault(n => n == name || (n.StartsWith(name + "_h", StringComparison.Ordinal) && n.Length > name.Length + 2));
            return full is null ? null : Format(key.GetValue(full));
        });
    }

    // Comparaison séparée de la lecture du registre (testée sans le jeu).
    public static IReadOnlyList<GameSettingCheck> Check(Func<string, string?> read) =>
        Wanted.Select(w =>
        {
            var value = read(w.Name);
            return new GameSettingCheck(w.Name, w.French, value, w.Expected, value == w.Expected, w.Required, w.Why);
        }).ToList();

    // Unity range les entiers en DWORD, les nombres à virgule en double (64 bits), les textes en octets terminés par 0.
    public static string? Format(object? raw) => raw switch
    {
        int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
        long l when Math.Abs(l) > 1_000_000 => Number(BitConverter.Int64BitsToDouble(l)),
        long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
        byte[] b => System.Text.Encoding.UTF8.GetString(b).TrimEnd('\0'),
        null => null,
        _ => raw.ToString(),
    };

    private static string Number(double d) => d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
