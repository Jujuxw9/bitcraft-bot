namespace BitcraftBot.Server.World;

// Catégories de la carte (v0.4.18, D-070) : les 12 familles de ressources regroupées en 7 boutons simples.
public sealed record MapCategory(string Name, string Hint, IReadOnlyList<string> Families);

public static class MapCategories
{
    public const string Wood = "Bois";
    public const string Stone = "Pierre et minerai";
    public const string Plants = "Plantes";
    public const string Food = "Nourriture";
    public const string Soil = "Sable et argile";
    public const string Fish = "Pêche";
    public const string Other = "Autres";

    public static IReadOnlyList<MapCategory> All { get; } =
    [
        new(Wood, "arbres, souches, bûches", [ResourceFamilies.Trees]),
        new(Stone, "rochers, pierre, filons de minerai", [ResourceFamilies.Rocks, ResourceFamilies.Ores]),
        new(Plants, "fleurs, plantes à fibres", [ResourceFamilies.Flowers, ResourceFamilies.Fibers]),
        new(Food, "baies, fruits, champignons, céréales, légumes", [ResourceFamilies.Berries, ResourceFamilies.Mushrooms, ResourceFamilies.Crops]),
        new(Soil, "sable, argile, terre, sel", [ResourceFamilies.Soils]),
        new(Fish, "coins de pêche", [ResourceFamilies.Fishing]),
        new(Other, "obstacles et le reste", [ResourceFamilies.Obstacles, ResourceFamilies.Others]),
    ];

    public static string Of(string family) =>
        All.FirstOrDefault(c => c.Families.Contains(family))?.Name ?? Other;

    public static string Of(NearbyResource r) => Of(r.Family);
}

// Filtre de la carte : catégories affichées et paliers (tiers) masqués. Retenu dans les réglages du bot.
public sealed class MapFilter
{
    public const int MaxTier = 10;
    public const int UnknownTier = -1;

    private readonly HashSet<string> _categories;
    private readonly HashSet<int> _hiddenTiers;

    public MapFilter(IEnumerable<string>? shownCategories = null, IEnumerable<int>? hiddenTiers = null)
    {
        var known = MapCategories.All.Select(c => c.Name).ToHashSet();
        _categories = shownCategories is null ? known : shownCategories.Where(known.Contains).ToHashSet();
        _hiddenTiers = hiddenTiers?.ToHashSet() ?? [];
    }

    // Paliers montrés en boutons : T1 à T10, puis « ? » (palier inconnu).
    public static IReadOnlyList<int> Tiers { get; } = [.. Enumerable.Range(1, MaxTier), UnknownTier];

    public static int TierOf(NearbyResource r) => r.Tier is { } t && t >= 1 && t <= MaxTier ? t : UnknownTier;

    public IReadOnlyCollection<string> ShownCategories => _categories;

    public IReadOnlyCollection<int> HiddenTiers => _hiddenTiers;

    public bool IsShown(string category) => _categories.Contains(category);

    public bool IsShown(int tier) => !_hiddenTiers.Contains(tier);

    public bool Shows(NearbyResource r) => _categories.Contains(MapCategories.Of(r)) && !_hiddenTiers.Contains(TierOf(r));

    public bool AllShown => _hiddenTiers.Count == 0 && _categories.Count == MapCategories.All.Count;

    public void ToggleCategory(string category)
    {
        if (!_categories.Remove(category))
        {
            _categories.Add(category);
        }
    }

    public void ToggleTier(int tier)
    {
        if (!_hiddenTiers.Remove(tier))
        {
            _hiddenTiers.Add(tier);
        }
    }

    // Clic droit sur un palier : n'afficher que lui (ou tout réafficher s'il était déjà seul).
    public void OnlyTier(int tier)
    {
        var alone = _hiddenTiers.Count == Tiers.Count - 1 && !_hiddenTiers.Contains(tier);
        _hiddenTiers.Clear();
        if (!alone)
        {
            foreach (var t in Tiers.Where(t => t != tier))
            {
                _hiddenTiers.Add(t);
            }
        }
    }

    public void ShowAll()
    {
        _hiddenTiers.Clear();
        foreach (var c in MapCategories.All)
        {
            _categories.Add(c.Name);
        }
    }

    // Nombre par palier, pour les catégories affichées (le bouton d'un palier montre ce qu'on verrait en l'allumant).
    public IReadOnlyDictionary<int, int> TierCounts(IEnumerable<NearbyResource> resources) =>
        resources.Where(r => _categories.Contains(MapCategories.Of(r))).GroupBy(TierOf).ToDictionary(g => g.Key, g => g.Count());

    // Nombre par catégorie, pour les paliers affichés.
    public IReadOnlyDictionary<string, int> CategoryCounts(IEnumerable<NearbyResource> resources) =>
        resources.Where(r => !_hiddenTiers.Contains(TierOf(r))).GroupBy(MapCategories.Of).ToDictionary(g => g.Key, g => g.Count());
}
