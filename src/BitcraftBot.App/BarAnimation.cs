using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace BitcraftBot.App;

// v0.4.14 (demande de Julien) : les barres (vie, endurance, faim, action, métier, sac) glissent vers leur nouvelle valeur
// au lieu de sauter. La durée est celle du rafraîchissement du tableau de bord (500 ms) : une barre qui avance sans arrêt
// (action en cours) avance donc d'un mouvement continu.
internal static class BarAnimation
{
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(500);

    public static void Smooth(this ProgressBar bar, double value)
    {
        // La valeur visée est gardée dans Tag : Value donne la valeur en cours d'animation.
        if (bar.Tag is double target && Math.Abs(target - value) < 1e-6)
        {
            return;
        }
        bar.Tag = value;
        var animation = new DoubleAnimation(value, Duration) { FillBehavior = FillBehavior.HoldEnd };
        bar.BeginAnimation(RangeBase.ValueProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }
}
