namespace BitcraftBot.Input.Human;

// Point de passage d'un trajet de souris : position écran et temps depuis le début du trajet.
public readonly record struct MouseStep(int X, int Y, double AtMs);

// Rectangle autorisé pour la souris (pixels écran).
public readonly record struct SafeArea(int Left, int Top, int Right, int Bottom)
{
    public bool Contains(int x, int y) => x >= Left && x <= Right && y >= Top && y <= Bottom;

    public (int X, int Y) Clamp(int x, int y) => (Math.Clamp(x, Left, Right), Math.Clamp(y, Top, Bottom));
}

// Fabrique des trajets de souris d'allure humaine :
// - courbe (pas de ligne droite), via une courbe de Bézier aux points de contrôle décalés ;
// - vitesse en cloche : départ doux, accélération, ralentissement à l'arrivée ;
// - durée selon la distance (loi de Fitts, comme un humain) ;
// - parfois un léger dépassement de la cible, puis une petite correction ;
// - petits tremblements.
public sealed class HumanMouse(Random random)
{
    // Intervalle entre deux positions envoyées (≈ 125 par seconde, comme une souris réelle).
    public const double StepMs = 8;

    public IReadOnlyList<MouseStep> Path(int fromX, int fromY, int toX, int toY, SafeArea area)
    {
        (toX, toY) = area.Clamp(toX, toY);
        var steps = new List<MouseStep>();
        var distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));
        if (distance < 2)
        {
            steps.Add(new MouseStep(toX, toY, 0));
            return steps;
        }

        // Dépassement dans 30 % des cas, pour les trajets assez longs.
        var overshoot = distance > 150 && random.NextDouble() < 0.3;
        double aimX = toX, aimY = toY;
        if (overshoot)
        {
            var factor = 0.03 + random.NextDouble() * 0.05;
            aimX = toX + (toX - fromX) * factor;
            aimY = toY + (toY - fromY) * factor;
            (var cx, var cy) = area.Clamp((int)aimX, (int)aimY);
            (aimX, aimY) = (cx, cy);
        }

        AddSegment(steps, fromX, fromY, aimX, aimY, 0, area);
        if (overshoot)
        {
            // Petite correction, plus lente proportionnellement.
            var last = steps[^1];
            AddSegment(steps, last.X, last.Y, toX, toY, last.AtMs + 40 + random.NextDouble() * 80, area);
        }
        // On termine exactement sur la cible.
        var end = steps[^1];
        if (end.X != toX || end.Y != toY)
        {
            steps.Add(new MouseStep(toX, toY, end.AtMs + StepMs));
        }
        return steps;
    }

    // Durée d'un geste selon la distance (loi de Fitts) ± 20 %.
    public double DurationMs(double distance)
    {
        var fitts = 120 + 110 * Math.Log2(distance / 20 + 1);
        return fitts * (0.8 + random.NextDouble() * 0.4);
    }

    private void AddSegment(List<MouseStep> steps, double x0, double y0, double x1, double y1, double startMs, SafeArea area)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var duration = DurationMs(distance);

        // Points de contrôle décalés sur le côté : la trajectoire s'arrondit.
        var nx = -dy / Math.Max(distance, 1);
        var ny = dx / Math.Max(distance, 1);
        var bend1 = (random.NextDouble() * 2 - 1) * 0.25 * distance;
        var bend2 = (random.NextDouble() * 2 - 1) * 0.15 * distance;
        var c1x = x0 + dx * 0.3 + nx * bend1;
        var c1y = y0 + dy * 0.3 + ny * bend1;
        var c2x = x0 + dx * 0.7 + nx * bend2;
        var c2y = y0 + dy * 0.7 + ny * bend2;

        var count = Math.Max(2, (int)(duration / StepMs));
        for (var i = 1; i <= count; i++)
        {
            var t = (double)i / count;
            // Profil de vitesse « minimum jerk » : départ et arrivée en douceur.
            var s = 10 * Math.Pow(t, 3) - 15 * Math.Pow(t, 4) + 6 * Math.Pow(t, 5);
            var u = 1 - s;
            var x = u * u * u * x0 + 3 * u * u * s * c1x + 3 * u * s * s * c2x + s * s * s * x1;
            var y = u * u * u * y0 + 3 * u * u * s * c1y + 3 * u * s * s * c2y + s * s * s * y1;
            // Tremblement léger, sauf sur le dernier point.
            if (i < count)
            {
                x += Gaussian() * 0.6;
                y += Gaussian() * 0.6;
            }
            var (px, py) = area.Clamp((int)Math.Round(x), (int)Math.Round(y));
            steps.Add(new MouseStep(px, py, startMs + t * duration));
        }
    }

    // Pause « humaine » entre deux gestes (ms).
    public double PauseMs() => 250 + random.NextDouble() * random.NextDouble() * 1500;

    // Cible au hasard dans la zone autorisée, pas trop près du point actuel.
    public (int X, int Y) RandomTarget(int fromX, int fromY, SafeArea area)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var x = random.Next(area.Left, area.Right + 1);
            var y = random.Next(area.Top, area.Bottom + 1);
            if (Math.Abs(x - fromX) + Math.Abs(y - fromY) > 80)
            {
                return (x, y);
            }
        }
        return ((area.Left + area.Right) / 2, (area.Top + area.Bottom) / 2);
    }

    private double Gaussian()
    {
        // Loi normale (Box-Muller).
        var u1 = 1.0 - random.NextDouble();
        var u2 = random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }
}
