namespace BitcraftBot.Server.World;

// Textes en français pour l'affichage des ressources (âge d'un arbre, direction).
public static class ResourceNames
{
    // Âge d'un arbre, d'après son nom dans les données du jeu (ex. « Mature Oak Tree » → « Mûr »).
    public static string Age(string name)
    {
        if (name.StartsWith("Young ", StringComparison.OrdinalIgnoreCase)) return "Jeune";
        if (name.StartsWith("Mature ", StringComparison.OrdinalIgnoreCase)) return "Mûr";
        if (name.StartsWith("Dead ", StringComparison.OrdinalIgnoreCase)) return "Mort";
        if (name.StartsWith("Planted ", StringComparison.OrdinalIgnoreCase)) return "Planté";
        if (name.StartsWith("Fully Grown ", StringComparison.OrdinalIgnoreCase)) return "Planté, adulte";
        if (name.Contains("Stump", StringComparison.OrdinalIgnoreCase)) return "Souche";
        return "—";
    }

    // Le « Maker's Tree » ne doit jamais être coupé (constat du 2026-11-16).
    public static bool IsForbidden(string name) => name.Contains("Maker", StringComparison.OrdinalIgnoreCase);

    // Arbre que le bot visera plus tard (D-036) : un arbre mûr, pas interdit.
    public static bool IsHarvestTarget(NearbyResource r) => r.IsTree && r.Age == "Mûr" && !IsForbidden(r.Name);

    // Direction à 8 points, vue du personnage. dEast > 0 = vers l'est, dNorth > 0 = vers le nord.
    public static string Direction(double dEast, double dNorth)
    {
        if (Math.Abs(dEast) < 0.25 && Math.Abs(dNorth) < 0.25)
        {
            return "ici";
        }
        // Angle depuis le nord, dans le sens des aiguilles d'une montre.
        var angle = (Math.Atan2(dEast, dNorth) * 180 / Math.PI + 360) % 360;
        string[] names = ["N", "NE", "E", "SE", "S", "SO", "O", "NO"];
        return names[(int)Math.Round(angle / 45) % 8];
    }
}
