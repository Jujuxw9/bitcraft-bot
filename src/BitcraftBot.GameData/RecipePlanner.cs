namespace BitcraftBot.GameData;

// Un nœud de l'arborescence : « il me faut Quantity × Key », obtenu par Recipe (null : source inconnue ou récolte directe
// sans ingrédient), en Crafts fois (coups de récolte ou fabrications), à partir des Children.
public sealed record PlanNode(string Key, int Quantity, Recipe? Recipe, int Crafts, IReadOnlyList<PlanNode> Children);

// Une action du plan, dans l'ordre : ce qu'il faut avoir Quantity × Key, en faisant Recipe Crafts fois.
// Recipe null : l'objet n'a aucune source connue (butin, commerce…), il faut l'avoir.
public sealed record PlanStep(string Key, int Quantity, Recipe? Recipe, int Crafts);

// Effort estimé : sources inconnues, nombre d'étapes de fabrication, secondes de travail (récolte et fabrication).
public sealed record Effort(int Unknown, int CraftSteps, double Seconds)
{
    public static readonly Effort None = new(0, 0, 0);

    public static Effort operator +(Effort a, Effort b) => new(a.Unknown + b.Unknown, a.CraftSteps + b.CraftSteps, a.Seconds + b.Seconds);

    // Pour trier du plus facile au plus dur : sources inconnues en dernier, puis moins d'étapes de fabrication, puis moins de temps.
    public (int, int, double) SortKey => (Unknown > 0 ? 1 : 0, CraftSteps, Seconds);

    public static string Duration(double seconds) =>
        seconds < 90 ? $"{seconds:0} s" : seconds < 5400 ? $"{seconds / 60:0.#} min" : $"{seconds / 3600:0.#} h";

    public string Text => Unknown > 0 ? "source inconnue (butin ou commerce)"
        : CraftSteps == 0 ? (Seconds > 0 ? $"récolte seulement, ≈ {Duration(Seconds)}" : "rien à faire")
        : $"{CraftSteps} fabrication{(CraftSteps > 1 ? "s" : "")}, ≈ {Duration(Seconds)}";
}

// Plan pour obtenir une quantité d'un objet (v0.4.12, demande de Julien) : on choisit pour chaque objet la façon la plus simple
// (RecipeBook.Simplest : récolte d'abord, sans paquets), puis on remonte les ingrédients jusqu'aux ressources de base.
public static class RecipePlanner
{
    private const int MaxDepth = 12;

    public static Recipe? Best(RecipeBook book, string key) => book.Simplest(key, 1).FirstOrDefault();

    // Quantité obtenue par action pour cet objet (au moins 1).
    public static int YieldOf(Recipe recipe, string key) => Math.Max(1, recipe.Outputs.Where(o => o.Key == key).Sum(o => o.Quantity));

    // Arborescence : A ← 5 × B, 5 × C ; B ← récolte ; C ← fabrication avec D…
    public static PlanNode Tree(RecipeBook book, string key, int quantity) => Node(book, key, Math.Max(1, quantity), [], 0);

    private static PlanNode Node(RecipeBook book, string key, int quantity, HashSet<string> path, int depth)
    {
        var recipe = depth >= MaxDepth || path.Contains(key) ? null : Best(book, key);
        if (recipe is null)
        {
            return new PlanNode(key, quantity, null, 0, []);
        }
        var crafts = (int)Math.Ceiling(quantity / (double)YieldOf(recipe, key));
        path.Add(key);
        var children = recipe.Inputs.Select(i => Node(book, i.Key, i.Quantity * crafts, path, depth + 1)).ToList();
        path.Remove(key);
        return new PlanNode(key, quantity, recipe, crafts, children);
    }

    // Liste d'actions dans l'ordre : d'abord les ressources de base, puis les fabrications, l'objet voulu en dernier.
    // Un même objet demandé à plusieurs endroits de l'arbre n'apparaît qu'une fois, avec la quantité totale.
    public static IReadOnlyList<PlanStep> Steps(RecipeBook book, string key, int quantity)
    {
        var chosen = new Dictionary<string, Recipe?>();
        var order = new List<string>();
        var state = new Dictionary<string, int>(); // 1 = en cours, 2 = fini
        Visit(book, key, chosen, order, state, 0);

        var need = new Dictionary<string, int> { [key] = Math.Max(1, quantity) };
        var crafts = new Dictionary<string, int>();
        var done = new HashSet<string>();
        // Ordre inverse de la visite : un objet est traité après tous ceux qui l'utilisent.
        foreach (var k in Enumerable.Reverse(order))
        {
            done.Add(k);
            var n = need.GetValueOrDefault(k);
            var recipe = chosen[k];
            if (recipe is null || n <= 0)
            {
                continue;
            }
            var c = (int)Math.Ceiling(n / (double)YieldOf(recipe, k));
            crafts[k] = c;
            foreach (var input in recipe.Inputs)
            {
                if (!done.Contains(input.Key))
                {
                    need[input.Key] = need.GetValueOrDefault(input.Key) + input.Quantity * c;
                }
            }
        }
        return order.Where(k => need.GetValueOrDefault(k) > 0)
            .Select(k => new PlanStep(k, need[k], chosen[k], crafts.GetValueOrDefault(k)))
            .ToList();
    }

    private static void Visit(RecipeBook book, string key, Dictionary<string, Recipe?> chosen, List<string> order, Dictionary<string, int> state, int depth)
    {
        state[key] = 1;
        var recipe = depth >= MaxDepth ? null : Best(book, key);
        chosen[key] = recipe;
        if (recipe is not null)
        {
            foreach (var input in recipe.Inputs)
            {
                if (!state.ContainsKey(input.Key))
                {
                    Visit(book, input.Key, chosen, order, state, depth + 1);
                }
            }
        }
        state[key] = 2;
        order.Add(key);
    }

    // Effort pour obtenir Quantity × Key en partant de rien.
    public static Effort EffortOf(RecipeBook book, string key, int quantity)
    {
        var effort = Effort.None;
        foreach (var step in Steps(book, key, quantity))
        {
            if (step.Recipe is null)
            {
                // Objet de base sans recette : inconnu seulement s'il n'est pas déjà ce qu'on récolte (ressource de base).
                effort += new Effort(1, 0, 0);
                continue;
            }
            var seconds = step.Crafts * (step.Recipe.Actions > 0 ? step.Recipe.Actions * step.Recipe.SecondsPerAction : step.Recipe.SecondsPerAction);
            effort += new Effort(0, step.Recipe.Kind == "Fabrication" ? 1 : 0, seconds);
        }
        return effort;
    }
}
