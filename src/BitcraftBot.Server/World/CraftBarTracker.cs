namespace BitcraftBot.Server.World;

// v0.4.31 (T-13) : part faite de la fabrication suivie, pour la barre du tableau de bord.
// Le serveur garde la fabrication à 100 % (progressive_action_state) jusqu'au ramassage des objets
// (code serveur : craft.rs, craft_collect.rs). Deux cas faisaient s'arrêter la barre avant la fin :
// - à la fin, l'action du joueur disparaît, avec elle la recette visée : s'il y a d'autres fabrications
//   en cours dans d'autres ateliers, on ne savait plus laquelle suivre ;
// - objets ramassés tout de suite : la fabrication disparaît, il ne restait rien à afficher.
// On retient donc la recette suivie et la dernière part lue, jusqu'à Reset.
public sealed class CraftBarTracker
{
    private int? _recipeId;
    private double? _last;

    public double? Last => _last;

    // actionsPerItem : gestes par objet de chaque recette (données du jeu). null si inconnu.
    public double? Update(ActionInfo? action, IReadOnlyList<CraftProgress> crafts, IReadOnlyDictionary<int, int> actionsPerItem)
    {
        if (action is { Type: "Craft", RecipeId: { } id })
        {
            if (_recipeId != id)
            {
                _last = null;
            }
            _recipeId = id;
        }
        var recipe = action?.RecipeId ?? _recipeId;
        double? Done(CraftProgress c) =>
            actionsPerItem.TryGetValue(c.RecipeId, out var perItem) && perItem > 0 ? c.Done(perItem) : null;

        CraftProgress? craft = null;
        if (recipe is { } r)
        {
            // Même recette dans deux ateliers : la moins avancée est celle qu'on fait (l'autre attend d'être ramassée).
            craft = crafts.Where(c => c.RecipeId == r).OrderBy(c => Done(c) ?? 1).FirstOrDefault();
        }
        craft ??= crafts.Count == 1 ? crafts[0] : null;
        if (craft is not null && Done(craft) is { } done)
        {
            _last = done;
            return done;
        }
        // Fabrication disparue (objets ramassés) : la dernière part lue reste affichée.
        return action is null ? _last : null;
    }

    public void Reset()
    {
        _recipeId = null;
        _last = null;
    }
}
