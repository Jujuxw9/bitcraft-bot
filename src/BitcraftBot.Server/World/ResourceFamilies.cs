namespace BitcraftBot.Server.World;

// Familles de ressources pour les filtres de la carte (H2). Les ~80 catégories (« tag ») des données
// du jeu sont regroupées en une douzaine de familles lisibles.
public static class ResourceFamilies
{
    public const string Trees = "Arbres";
    public const string Rocks = "Rochers et pierre";
    public const string Ores = "Minerais";
    public const string Flowers = "Fleurs";
    public const string Fibers = "Plantes à fibres";
    public const string Berries = "Baies et fruits";
    public const string Mushrooms = "Champignons";
    public const string Crops = "Céréales et légumes";
    public const string Soils = "Sable, argile, terre";
    public const string Fishing = "Pêche";
    public const string Obstacles = "Obstacles";
    public const string Others = "Autres";

    // Ordre d'affichage des cases à cocher.
    public static IReadOnlyList<string> All { get; } =
        [Trees, Rocks, Ores, Flowers, Fibers, Berries, Mushrooms, Crops, Soils, Fishing, Obstacles, Others];

    private static readonly Dictionary<string, string> ByTag = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tree"] = Trees,
        ["Hexite Infused Tree"] = Trees,
        ["Fruit Tree"] = Trees,
        ["Blossom Tree"] = Trees,
        ["Sapling"] = Trees,
        ["Stump"] = Trees,
        ["Wood Logs"] = Trees,
        ["Wood"] = Trees,
        ["Stick"] = Trees,
        ["Rock"] = Rocks,
        ["Rock Boulder"] = Rocks,
        ["Rock Outcrop"] = Rocks,
        ["Stone"] = Rocks,
        ["Ore Vein"] = Ores,
        ["Hardened Ore Node"] = Ores,
        ["Weakened Ore"] = Ores,
        ["Metal Outcrop"] = Ores,
        ["Hexite"] = Ores,
        ["Hexite Meteor"] = Ores,
        ["Flower"] = Flowers,
        ["Fiber Plant"] = Fibers,
        ["Berry"] = Berries,
        ["Bountiful Berry Bush"] = Berries,
        ["Fruit"] = Berries,
        ["Coconut"] = Berries,
        ["Mushroom"] = Mushrooms,
        ["Rare Mushroom"] = Mushrooms,
        ["Wild Grain"] = Crops,
        ["Wild Vegetable"] = Crops,
        ["Foraging Resource"] = Crops,
        ["Sand"] = Soils,
        ["Clay"] = Soils,
        ["Dirt"] = Soils,
        ["Salt"] = Soils,
        ["Baitfish"] = Fishing,
        ["Mollusks"] = Fishing,
        ["Obstacle"] = Obstacles,
        ["Dungeon Obstacle"] = Obstacles,
        ["Lava"] = Obstacles,
    };

    public static string Of(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return Others;
        }
        if (ByTag.TryGetValue(tag, out var family))
        {
            return family;
        }
        return tag.Contains("Fish", StringComparison.OrdinalIgnoreCase) ? Fishing : Others;
    }
}
