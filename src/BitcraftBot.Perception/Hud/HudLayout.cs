using BitcraftBot.Perception.Capture;

namespace BitcraftBot.Perception.Hud;

// Positions des éléments du HUD, en pixels, pour le jeu en 1920×1080 et échelle d'interface par défaut (D-006).
// Mesurées le 2026-11-16 sur une vraie capture (jeu en train de couper un Mature Oak Tree).
public static class HudLayout
{
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    // Sous la minicarte, en haut à droite (D-018).
    public static readonly ScreenRect Coordinates = new(1665, 298, 180, 26);
    // Zone plus large pour la lecture par modèles : le texte est centré, sa largeur change avec le nombre de chiffres.
    // S'arrête au-dessus du nom de la zone (y ≈ 334).
    public static readonly ScreenRect CoordinatesSearch = new(1610, 294, 290, 34);
    public static readonly ScreenRect ZoneName = new(1650, 326, 210, 24);
    public static readonly ScreenRect Region = new(1665, 354, 180, 24);

    // Panneau de la cible, en haut au centre (nom, catégorie, santé de la ressource).
    public static readonly ScreenRect TargetName = new(745, 22, 330, 24);
    public static readonly ScreenRect TargetCategory = new(1080, 22, 175, 24);
    public static readonly ScreenRect TargetHealth = new(880, 49, 240, 18);

    // Points où le fond crème du panneau cible est visible quand le panneau est affiché.
    public static readonly (int X, int Y)[] TargetPanelProbes = [(720, 40), (1100, 90), (1240, 90)];

    // Depuis une mise à jour du jeu (vers le 23/11/2026), le panneau cible est sombre, texte clair, aux mêmes places.
    // Points de contrôle et couleur attendue : bord gauche, cadre du haut, puis le fond du panneau.
    // Le bord et le cadre, plus clairs que le fond, distinguent le panneau des menus plein écran (fond uni).
    public static readonly (int X, int Y, byte R, byte G, byte B)[] DarkTargetPanelProbes =
    [
        (720, 40, 46, 43, 59),
        (1000, 20, 60, 57, 74),
        (1100, 90, 38, 36, 48),
        (1240, 90, 38, 36, 48),
    ];

    // Zones affichées en surimpression dans la fenêtre de supervision (v0.3.20 : la cible seulement ;
    // position, zone et région restent mesurées ici pour les tests et un éventuel retour).
    public static IReadOnlyList<(string Name, ScreenRect Rect)> AllZones { get; } =
    [
        ("Cible", TargetName),
        ("Catégorie", TargetCategory),
        ("Santé cible", TargetHealth),
    ];
}
