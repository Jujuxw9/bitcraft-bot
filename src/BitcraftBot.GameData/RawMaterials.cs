namespace BitcraftBot.GameData;

// Une matière première à récupérer : Quantity × Key, obtenue par Harvest (récolte), ou sans source connue (null : butin, commerce…).
public sealed record RawNeed(string Key, int Quantity, Recipe? Harvest);

// v0.4.15 (demande de Julien, D-066) : pour une liste d'objets manquants, les matières premières à récupérer, c'est-à-dire
// le plus bas de l'arbre des recettes (ce qui se récolte). Le stock est déduit à chaque niveau : un produit intermédiaire
// déjà en stock (des briques) évite de descendre plus bas (l'argile).
public static class RawMaterials
{
    private const int MaxDepth = 12;

    // needs : objets manquants (clé, quantité) ; stock : ce qui reste disponible (clé, quantité), consommé au fil du calcul.
    public static IReadOnlyList<RawNeed> For(RecipeBook book, IEnumerable<(string Key, int Quantity)> needs, IReadOnlyDictionary<string, int> stock)
    {
        var left = new Dictionary<string, int>(stock);
        var raw = new Dictionary<string, RawNeed>();
        var order = new List<string>();
        foreach (var (key, quantity) in needs)
        {
            Need(book, key, quantity, left, raw, order, [], 0, top: true);
        }
        return order.Select(k => raw[k]).ToList();
    }

    private static void Need(RecipeBook book, string key, int quantity, Dictionary<string, int> left, Dictionary<string, RawNeed> raw,
        List<string> order, HashSet<string> path, int depth, bool top)
    {
        if (quantity <= 0)
        {
            return;
        }
        // Les objets demandés eux-mêmes sont déjà comptés « manquants » : le stock ne se déduit qu'en dessous.
        if (!top && left.TryGetValue(key, out var have) && have > 0)
        {
            var take = Math.Min(have, quantity);
            left[key] = have - take;
            quantity -= take;
            if (quantity == 0)
            {
                return;
            }
        }
        var recipe = depth >= MaxDepth || path.Contains(key) ? null : RecipePlanner.Best(book, key);
        if (recipe is null || recipe.Kind == "Récolte")
        {
            if (raw.TryGetValue(key, out var existing))
            {
                raw[key] = existing with { Quantity = existing.Quantity + quantity };
            }
            else
            {
                raw[key] = new RawNeed(key, quantity, recipe);
                order.Add(key);
            }
            return;
        }
        var crafts = (int)Math.Ceiling(quantity / (double)RecipePlanner.YieldOf(recipe, key));
        path.Add(key);
        foreach (var input in recipe.Inputs)
        {
            Need(book, input.Key, input.Quantity * crafts, left, raw, order, path, depth + 1, top: false);
        }
        path.Remove(key);
    }
}
