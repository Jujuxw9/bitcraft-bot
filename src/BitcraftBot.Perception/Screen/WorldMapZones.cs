using BitcraftBot.Perception.Capture;

namespace BitcraftBot.Perception.Screen;

// Que faire quand la touche de la carte vient d'être appuyée (v0.4.30, T-12).
public enum MapOpenOutcome
{
    // « Set destination » lu dans la barre du bas : la carte est ouverte.
    Ouverte,
    // Barre pas lue mais la carte grise est vue : la refermer avec la même touche.
    ARefermer,
    // Rien vu : ne rien rappuyer (un second appui ouvrirait la carte pour rien).
    RienOuvert,
}

// v0.4.30 (T-12) : zones de la grande carte du jeu, partagées par le bot et les tests sur captures.
public static class WorldMapZones
{
    // Barre du bas de la carte (« Région | (N, E)  Set destination … ») : x 30-69 %, y 91-97,5 %, mesurée sur la capture du 16/11.
    public static ScreenRect BarZone(int width, int height) =>
        new((int)(width * 0.30), (int)(height * 0.91), (int)(width * 0.39), (int)(height * 0.065));

    public static ScreenRect BarZone(Frame f) => BarZone(f.Width, f.Height);

    public static bool SaysSetDestination(string? barText) =>
        barText is not null && barText.Contains("destination", StringComparison.OrdinalIgnoreCase);

    public static MapOpenOutcome AfterKey(string? barText, bool worldMapSeen) =>
        SaysSetDestination(barText) ? MapOpenOutcome.Ouverte
        : worldMapSeen ? MapOpenOutcome.ARefermer
        : MapOpenOutcome.RienOuvert;
}
