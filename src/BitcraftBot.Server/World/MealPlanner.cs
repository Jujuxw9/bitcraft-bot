using BitcraftBot.GameData;

namespace BitcraftBot.Server.World;

// Ce que le repas automatique sait à un instant donné.
// FoodRegenSeconds : secondes restantes du bonus de nourriture ; null si les bonus ne sont pas lus, 0 si plus de bonus.
// SacRead : le sac est lu. NextFood : l'aliment que Maj+E mangera (premier aliment du sac), null si aucun.
// Blocker : objet du sac qui passerait avant la nourriture (potion, thé…) : on ne mange pas.
public sealed record MealSituation(
    double? Satiation,
    double? MaxSatiation,
    int? FoodRegenSeconds,
    bool SacRead,
    FoodInfo? NextFood,
    FoodInfo? Blocker,
    bool AlreadyEating);

public enum MealAction
{
    Rien,
    Manger,
    Bloque,
}

// Raison : phrase pour le journal et l'écran. StartsMeal : ce repas ouvre une série (faim basse).
public sealed record MealDecision(MealAction Action, string Reason, bool ContinuesSeries = false);

// Règles du repas automatique (D-042, Julien, 2026-11-19) :
// 1. faim sous 30 % du maximum → manger, un aliment à la fois, tant que l'aliment ne dépasse pas le manque ;
// 2. plus de bonus de nourriture (« Food Regen ») → manger un aliment, même si la faim est haute.
public static class MealPlanner
{
    public const double HungerThreshold = 0.30;

    // Valeur supposée d'un aliment quand le sac n'est pas lu (aliment de palier 1 : 25).
    public const double UnknownFoodHunger = 25;

    public const double DefaultMaxSatiation = 100;

    public static MealDecision Decide(MealSituation s)
    {
        if (s.Satiation is not { } satiation)
        {
            return new(MealAction.Rien, "faim inconnue (serveur pas encore lu)");
        }
        var max = s.MaxSatiation is > 0 ? s.MaxSatiation.Value : DefaultMaxSatiation;
        var missing = Math.Max(0, max - satiation);
        var hungry = satiation < max * HungerThreshold;
        var noBonus = s.FoodRegenSeconds == 0;

        if (!hungry && !noBonus && !s.AlreadyEating)
        {
            return new(MealAction.Rien, $"faim {satiation:0}/{max:0}, bonus de nourriture actif");
        }
        if (s.Blocker is { } blocker)
        {
            return new(MealAction.Bloque, $"« {blocker.Name} » est avant la nourriture dans le sac : Maj+E le mangerait. Rangez la nourriture en premier.");
        }
        if (s.SacRead && s.NextFood is null)
        {
            return new(MealAction.Bloque, "plus de nourriture dans le sac");
        }
        var value = s.NextFood?.Hunger ?? UnknownFoodHunger;
        var name = s.NextFood?.Name ?? "premier aliment du sac (sac non lu)";

        // Série en cours (ou faim basse) : on mange tant que l'aliment « rentre » dans le manque, sans gaspiller.
        if (hungry || s.AlreadyEating)
        {
            if (missing >= value)
            {
                return new(MealAction.Manger, $"faim {satiation:0}/{max:0} (manque {missing:0}) : « {name} » rend {value:0}", ContinuesSeries: true);
            }
            if (!noBonus)
            {
                return new(MealAction.Rien, $"repas fini : faim {satiation:0}/{max:0}, « {name} » ({value:0}) dépasserait le manque ({missing:0})");
            }
        }
        // Plus de bonus : un seul aliment, pour le bonus.
        return new(MealAction.Manger, $"plus de bonus de nourriture : « {name} » (faim {satiation:0}/{max:0})");
    }

    // Secondes restantes du bonus de nourriture, d'après les bonus actifs. null si rien n'est lu.
    public static int? FoodRegenSeconds(IReadOnlyList<ActiveBuffInfo> buffs, bool buffsRead, IReadOnlySet<int> regenIds, DateTimeOffset now)
    {
        if (!buffsRead)
        {
            return null;
        }
        var left = buffs.Where(b => regenIds.Contains(b.BuffId)).Select(b => b.RemainingSeconds(now)).DefaultIfEmpty(0).ToList();
        return left.Any(l => l < 0) ? int.MaxValue : left.Max();
    }

    // Ce que Maj+E mangera : le premier aliment du sac (dans l'ordre des poches). S'il n'est pas un repas
    // (potion, thé, élixir), il bloque. À vérifier en jeu : « premier » = premier aliment du sac.
    public static (FoodInfo? Next, FoodInfo? Blocker) FirstFood(IEnumerable<ItemStackInfo> sac, IReadOnlyDictionary<int, FoodInfo> foods)
    {
        foreach (var stack in sac)
        {
            if (stack.IsCargo || !foods.TryGetValue(stack.ItemId, out var food))
            {
                continue;
            }
            return food.IsMeal ? (food, null) : (null, food);
        }
        return (null, null);
    }
}
