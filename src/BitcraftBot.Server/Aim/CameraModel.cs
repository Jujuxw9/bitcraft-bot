namespace BitcraftBot.Server.Aim;

// Une paire apprise (R1) : « Julien a cliqué ici » et « le serveur dit qu'il a frappé la ressource là ».
// Ground : position de la ressource par rapport au personnage, en unités de l'écran du jeu (E vers l'est, N vers le nord).
// Pixel : point cliqué dans la zone utile de la fenêtre du jeu (0, 0 = coin haut gauche), et taille de cette zone.
public sealed record CameraPair
{
    public DateTime At { get; init; }
    public double East { get; init; }
    public double North { get; init; }
    public int PixelX { get; init; }
    public int PixelY { get; init; }
    public int ClientWidth { get; init; }
    public int ClientHeight { get; init; }
    public string Resource { get; init; } = "";
    public string? Tag { get; init; }
    public ulong EntityId { get; init; }

    // Point cliqué, ramené au centre de l'image et divisé par la hauteur : indépendant de la taille de la fenêtre
    // (la caméra du jeu garde le même angle de vue vertical quand la fenêtre change de taille, à vérifier).
    public double U => ClientHeight <= 0 ? 0 : (PixelX - ClientWidth / 2.0) / ClientHeight;

    public double V => ClientHeight <= 0 ? 0 : (PixelY - ClientHeight / 2.0) / ClientHeight;
}

public enum CameraFormula
{
    // Formule simple : la caméra vue comme « plate » (6 nombres).
    Plate,

    // Formule avec perspective : les choses lointaines sont plus petites (8 nombres).
    Perspective,
}

// Formule de la caméra : position au sol (E, N) par rapport au personnage → point de l'écran (U, V).
// Valable caméra recentrée (W) : orientation et zoom de référence (D-049).
public sealed class CameraFit
{
    // Ground divisé par cette échelle avant le calcul (meilleure précision numérique).
    private const double Scale = 10;

    private readonly double[] _p;

    private CameraFit(CameraFormula formula, double[] p)
    {
        Formula = formula;
        _p = p;
    }

    public CameraFormula Formula { get; }

    // Nombre de paires gardées pour le calcul, et paires écartées (trop loin des autres : indices faux).
    public int Used { get; private set; }

    public int Rejected { get; private set; }

    // Précision honnête : pour chaque paire, formule calculée SANS elle, puis écart avec le vrai clic.
    // Médiane et 90 % des écarts, en pixels pour une image de hauteur ReferenceHeight. null si trop peu de paires.
    public double? MedianErrorPx { get; private set; }

    public double? P90ErrorPx { get; private set; }

    public int ReferenceHeight { get; private set; }

    // Formule digne de confiance : au moins 8 clics gardés, et au plus un tiers des clics écartés.
    // Essai R1 du 2026-11-22 : « précision 18 px » affichée avec 4 clics gardés sur 17 : trompeur.
    public bool Reliable => MedianErrorPx is not null && Used >= 8 && Rejected * 2 <= Used;

    public IReadOnlyList<CameraPair> Inliers { get; private set; } = [];

    public static int MinimumPairs(CameraFormula f) => f == CameraFormula.Plate ? 3 : 4;

    // Point de l'écran (U, V) d'une position au sol, ou null si derrière la caméra.
    public (double U, double V)? Project(double east, double north)
    {
        var a = east / Scale;
        var b = north / Scale;
        if (Formula == CameraFormula.Plate)
        {
            return (_p[0] * a + _p[1] * b + _p[2], _p[3] * a + _p[4] * b + _p[5]);
        }
        var w = _p[6] * a + _p[7] * b + 1;
        if (w <= 1e-6)
        {
            return null;
        }
        return ((_p[0] * a + _p[1] * b + _p[2]) / w, (_p[3] * a + _p[4] * b + _p[5]) / w);
    }

    // Pixel de la zone utile du jeu pour une position au sol.
    public (int X, int Y)? ToPixel(double east, double north, int clientWidth, int clientHeight)
    {
        if (Project(east, north) is not { } uv)
        {
            return null;
        }
        return ((int)Math.Round(uv.U * clientHeight + clientWidth / 2.0), (int)Math.Round(uv.V * clientHeight + clientHeight / 2.0));
    }

    // Écart en pixels (à la hauteur de la paire) entre le clic de la paire et la formule.
    public double? ErrorPx(CameraPair pair) =>
        Project(pair.East, pair.North) is { } uv ? Math.Sqrt(Sq(uv.U - pair.U) + Sq(uv.V - pair.V)) * pair.ClientHeight : null;

    // Calcul de la formule à partir des paires (indices, pas vérité : D-049).
    // 1. Consensus : on essaie des petits groupes de paires et on garde la formule qui s'accorde avec le plus de paires
    //    (écart < thresholdPx). Les autres sont écartées.
    // 2. Calcul final sur les paires gardées.
    // 3. Si assez de paires : formule « perspective » essayée aussi, gardée si elle prédit nettement mieux.
    public static CameraFit? Compute(IReadOnlyList<CameraPair> pairs, double thresholdPx = 35, int seed = 1)
    {
        var usable = pairs.Where(p => p.ClientHeight > 0).ToList();
        var plate = Robust(usable, CameraFormula.Plate, thresholdPx, seed);
        if (plate is null)
        {
            return null;
        }
        if (usable.Count >= 7)
        {
            // La formule plate écarte parfois de bons clics lointains (la perspective les déforme) : la perspective
            // est gardée si elle accepte plus de clics, ou autant mais avec une précision nettement meilleure.
            var perspective = Robust(usable, CameraFormula.Perspective, thresholdPx, seed);
            if (perspective is { MedianErrorPx: { } pe } && perspective.Used >= 7
                && (perspective.Used > plate.Used || (perspective.Used == plate.Used && plate.MedianErrorPx is { } ae && pe < ae * 0.9)))
            {
                return perspective;
            }
        }
        return plate;
    }

    private static CameraFit? Robust(List<CameraPair> pairs, CameraFormula formula, double thresholdPx, int seed)
    {
        var min = MinimumPairs(formula);
        if (pairs.Count < min)
        {
            return null;
        }
        var random = new Random(seed);
        List<CameraPair>? best = null;
        var bestError = double.MaxValue;
        var tries = pairs.Count == min ? 1 : Math.Min(400, Combinations(pairs.Count, min));
        for (var t = 0; t < tries; t++)
        {
            var sample = pairs.Count == min ? pairs : Sample(pairs, min, random);
            if (Solve(sample, formula) is not { } fit)
            {
                continue;
            }
            var inliers = pairs.Where(p => fit.ErrorPx(p) is { } e && e <= thresholdPx).ToList();
            var error = inliers.Sum(p => fit.ErrorPx(p) ?? 0);
            if (best is null || inliers.Count > best.Count || (inliers.Count == best.Count && error < bestError))
            {
                best = inliers;
                bestError = error;
            }
        }
        if (best is null || best.Count < min)
        {
            return null;
        }
        // Calcul final, puis une passe pour reprendre les paires qui s'accordent avec la formule finale.
        var final = Solve(best, formula);
        if (final is null)
        {
            return null;
        }
        var kept = pairs.Where(p => final.ErrorPx(p) is { } e && e <= thresholdPx).ToList();
        if (kept.Count >= min && Solve(kept, formula) is { } refined)
        {
            final = refined;
            best = kept;
        }
        final.Used = best.Count;
        final.Rejected = pairs.Count - best.Count;
        final.Inliers = best;
        final.ReferenceHeight = (int)Median(best.Select(p => (double)p.ClientHeight).ToList());
        final.MeasureLeaveOneOut(best);
        return final;
    }

    private void MeasureLeaveOneOut(List<CameraPair> inliers)
    {
        if (inliers.Count <= MinimumPairs(Formula))
        {
            return;
        }
        var errors = new List<double>();
        for (var i = 0; i < inliers.Count; i++)
        {
            var others = inliers.Where((_, j) => j != i).ToList();
            if (Solve(others, Formula) is { } fit && fit.Project(inliers[i].East, inliers[i].North) is { } uv)
            {
                errors.Add(Math.Sqrt(Sq(uv.U - inliers[i].U) + Sq(uv.V - inliers[i].V)) * ReferenceHeight);
            }
        }
        if (errors.Count == 0)
        {
            return;
        }
        errors.Sort();
        MedianErrorPx = Median(errors);
        P90ErrorPx = errors[Math.Min(errors.Count - 1, (int)Math.Ceiling(errors.Count * 0.9) - 1)];
    }

    // Moindres carrés sur toutes les paires données.
    public static CameraFit? Solve(IReadOnlyList<CameraPair> pairs, CameraFormula formula)
    {
        if (pairs.Count < MinimumPairs(formula))
        {
            return null;
        }
        var n = formula == CameraFormula.Plate ? 3 : 8;
        if (formula == CameraFormula.Plate)
        {
            var rows = pairs.Select(p => new[] { p.East / Scale, p.North / Scale, 1 }).ToList();
            var pu = LeastSquares(rows, pairs.Select(p => p.U).ToList(), n);
            var pv = LeastSquares(rows, pairs.Select(p => p.V).ToList(), n);
            return pu is null || pv is null ? null : new CameraFit(formula, [.. pu, .. pv]);
        }
        // Perspective : u (g a + h b + 1) = p0 a + p1 b + p2, idem pour v (calcul linéaire classique).
        var hr = new List<double[]>();
        var hy = new List<double>();
        foreach (var p in pairs)
        {
            var a = p.East / Scale;
            var b = p.North / Scale;
            hr.Add([a, b, 1, 0, 0, 0, -p.U * a, -p.U * b]);
            hy.Add(p.U);
            hr.Add([0, 0, 0, a, b, 1, -p.V * a, -p.V * b]);
            hy.Add(p.V);
        }
        var h = LeastSquares(hr, hy, n);
        return h is null ? null : new CameraFit(formula, h);
    }

    // Les nombres de la formule, pour l'enregistrer ou l'écrire dans le journal.
    public IReadOnlyList<double> Parameters => _p;

    private static double[]? LeastSquares(List<double[]> rows, List<double> y, int n)
    {
        var m = new double[n, n + 1];
        for (var r = 0; r < rows.Count; r++)
        {
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    m[i, j] += rows[r][i] * rows[r][j];
                }
                m[i, n] += rows[r][i] * y[r];
            }
        }
        // Élimination de Gauss avec choix du pivot.
        for (var c = 0; c < n; c++)
        {
            var pivot = c;
            for (var r = c + 1; r < n; r++)
            {
                if (Math.Abs(m[r, c]) > Math.Abs(m[pivot, c]))
                {
                    pivot = r;
                }
            }
            if (Math.Abs(m[pivot, c]) < 1e-10)
            {
                // Paires alignées ou en double : pas de formule possible.
                return null;
            }
            if (pivot != c)
            {
                for (var j = 0; j <= n; j++)
                {
                    (m[c, j], m[pivot, j]) = (m[pivot, j], m[c, j]);
                }
            }
            for (var r = 0; r < n; r++)
            {
                if (r == c)
                {
                    continue;
                }
                var f = m[r, c] / m[c, c];
                for (var j = c; j <= n; j++)
                {
                    m[r, j] -= f * m[c, j];
                }
            }
        }
        var x = new double[n];
        for (var i = 0; i < n; i++)
        {
            x[i] = m[i, n] / m[i, i];
            if (double.IsNaN(x[i]) || double.IsInfinity(x[i]))
            {
                return null;
            }
        }
        return x;
    }

    private static List<CameraPair> Sample(List<CameraPair> pairs, int count, Random random)
    {
        var picked = new HashSet<int>();
        while (picked.Count < count)
        {
            picked.Add(random.Next(pairs.Count));
        }
        return picked.Select(i => pairs[i]).ToList();
    }

    private static int Combinations(int n, int k)
    {
        double c = 1;
        for (var i = 0; i < k; i++)
        {
            c = c * (n - i) / (i + 1);
        }
        return (int)Math.Min(c, int.MaxValue);
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    private static double Sq(double v) => v * v;
}
