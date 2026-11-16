namespace BitcraftBot.GameData;

// Noms exacts vus dans le jeu (captures du 2026-11-16, interface en anglais, D-015).
// Servent à corriger les petites erreurs de lecture (OCR) : « Maturc Oak Tree » → « Mature Oak Tree ».
// À compléter quand une source de données sera choisie (Q-004).
public static class KnownNames
{
    public static IReadOnlyList<string> Resources { get; } =
    [
        // Arbres (Forestry)
        "Mature Oak Tree",
        "Young Oak Tree",
        "Young Beech Tree",
        "Sticks",
        // Minerai et pierre (Mining)
        "Large Ferralith Vein",
        "Ferralith Vein",
        "Limestone Rock Formation",
        "Flint Pile",
        // Cueillette (Foraging)
        "Lavender",
        "Daisy",
        "Strawberry Bush",
        "Wild Grains",
        "Wild Starbulb Plant",
        "Mud Mound",
        // Pêche
        "Briny Guppi",
        // Objets déplaçables
        "Rough Wood Trunk",
    ];

    public static IReadOnlyList<string> Zones { get; } = ["Safe Meadows"];
}
