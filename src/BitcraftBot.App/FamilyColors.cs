using System.Windows.Media;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Couleur de chaque famille de ressources : la même sur la carte et sur sa case à cocher.
public static class FamilyColors
{
    private static readonly Dictionary<string, Brush> Brushes = new()
    {
        [ResourceFamilies.Trees] = Frozen(52, 168, 83),
        [ResourceFamilies.Rocks] = Frozen(158, 158, 158),
        [ResourceFamilies.Ores] = Frozen(255, 138, 101),
        [ResourceFamilies.Flowers] = Frozen(240, 98, 146),
        [ResourceFamilies.Fibers] = Frozen(220, 231, 117),
        [ResourceFamilies.Berries] = Frozen(171, 71, 188),
        [ResourceFamilies.Mushrooms] = Frozen(215, 204, 200),
        [ResourceFamilies.Crops] = Frozen(255, 213, 79),
        [ResourceFamilies.Soils] = Frozen(188, 170, 164),
        [ResourceFamilies.Fishing] = Frozen(79, 195, 247),
        [ResourceFamilies.Obstacles] = Frozen(161, 136, 127),
        [ResourceFamilies.Others] = Frozen(120, 144, 156),
    };

    public static Brush Of(string family) => Brushes.TryGetValue(family, out var b) ? b : Brushes[ResourceFamilies.Others];

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
