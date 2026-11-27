using System.Globalization;
using System.Text.RegularExpressions;

namespace BitcraftBot.Server.World;

// v0.4.29 (T-10, étude memory/technique/deplacement-etude.md) : « Se déplacer ici ».
// Classes pures (sans écran ni clavier), testées dans V0429Tests.

// Lecture de la barre du bas de la grande carte : « Keldan (R18) | (8918, 6700) » = coordonnées sous la souris.
// Calibration par 3 lectures (centre, 200 px à droite, 200 px en bas) : ordre des nombres (E/N) et échelle.
public sealed record MapScale(bool FirstIsNorth, double CellsPerPixelX, double CellsPerPixelY, int CenterX, int CenterY,
    double CenterEast, double CenterNorth)
{
    public const int MaxCoordinate = 30_000;
    public const double MinCellsPerPixel = 0.01;
    public const double MaxCellsPerPixel = 0.5;

    private static readonly Regex Pair = new(@"\(\s*(\d{1,5})\s*[,.;]\s*(\d{1,5})\s*\)", RegexOptions.CultureInvariant);

    // Les deux nombres de la barre, dans l'ordre affiché. null si illisible ou hors bornes.
    public static (int A, int B)? ParseBar(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        var matches = Pair.Matches(text);
        if (matches.Count == 0)
        {
            return null;
        }
        var m = matches[^1];
        var a = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var b = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        if (a <= 0 || b <= 0 || a > MaxCoordinate || b > MaxCoordinate)
        {
            return null;
        }
        return (a, b);
    }

    // Calibration à partir de trois lectures : au centre, dx pixels à droite, dy pixels plus bas.
    // Vers la droite, E doit croître ; vers le bas, N doit décroître. Rend l'échelle ou la raison du refus.
    public static (MapScale? Scale, string? Refusal) Calibrate((int A, int B) center, (int A, int B) right, (int A, int B) down,
        int centerX, int centerY, int dx, int dy, ServerPosition? player = null)
    {
        if (dx <= 0 || dy <= 0)
        {
            return (null, "décalages de calibration invalides");
        }
        var rightDa = right.A - center.A;
        var rightDb = right.B - center.B;
        var downDa = down.A - center.A;
        var downDb = down.B - center.B;
        bool firstIsNorth;
        double eastPerPx;
        double northPerPx;
        // Cas 1 : (N, E) : à droite seul B change (croît), en bas seul A change (décroît).
        if (rightDb > 0 && downDa < 0 && Math.Abs(rightDa) <= Math.Max(1, rightDb / 10) && Math.Abs(downDb) <= Math.Max(1, -downDa / 10))
        {
            firstIsNorth = true;
            eastPerPx = rightDb / (double)dx;
            northPerPx = -downDa / (double)dy;
        }
        // Cas 2 : (E, N) : à droite seul A croît, en bas seul B décroît.
        else if (rightDa > 0 && downDb < 0 && Math.Abs(rightDb) <= Math.Max(1, rightDa / 10) && Math.Abs(downDa) <= Math.Max(1, -downDb / 10))
        {
            firstIsNorth = false;
            eastPerPx = rightDa / (double)dx;
            northPerPx = -downDb / (double)dy;
        }
        else
        {
            return (null, "lectures de la barre incohérentes (ordre des nombres non trouvé)");
        }
        if (Math.Abs(eastPerPx - northPerPx) > 0.15 * Math.Max(eastPerPx, northPerPx))
        {
            return (null, $"échelles différentes en largeur et en hauteur ({eastPerPx:0.000} et {northPerPx:0.000} case/pixel)");
        }
        var scale = (eastPerPx + northPerPx) / 2;
        if (scale > MaxCellsPerPixel)
        {
            return (null, "carte trop dézoomée : zoom de la carte à régler à la main");
        }
        if (scale < MinCellsPerPixel)
        {
            return (null, "carte trop zoomée : zoom de la carte à régler à la main");
        }
        var east = firstIsNorth ? center.B : center.A;
        var north = firstIsNorth ? center.A : center.B;
        if (player is { } p && (Math.Abs(p.ScreenEast - east) > 3 || Math.Abs(p.ScreenNorth - north) > 3))
        {
            return (null, $"centre de la carte (E {east}, N {north}) loin de la position du serveur ({p.ScreenText})");
        }
        return (new MapScale(firstIsNorth, eastPerPx, northPerPx, centerX, centerY, east, north), null);
    }

    // Lecture de la barre → (E, N).
    public (int East, int North) ToEastNorth((int A, int B) read) => FirstIsNorth ? (read.B, read.A) : (read.A, read.B);

    // Case visée → point de l'écran.
    public (int X, int Y) ToScreen(double east, double north) =>
        ((int)Math.Round(CenterX + (east - CenterEast) / CellsPerPixelX),
         (int)Math.Round(CenterY - (north - CenterNorth) / CellsPerPixelY));

    // Correction après une lecture : nouveau point à partir du point actuel et de l'écart lu.
    public (int X, int Y) Correct((int X, int Y) current, (int East, int North) read, double east, double north) =>
        ((int)Math.Round(current.X + (east - read.East) / CellsPerPixelX),
         (int)Math.Round(current.Y - (north - read.North) / CellsPerPixelY));

    // Visée acceptée si l'écart est d'une case au plus dans les deux sens.
    public static bool OnTarget((int East, int North) read, double east, double north) =>
        Math.Abs(read.East - east) <= 1 && Math.Abs(read.North - north) <= 1;
}

// Vérification du trajet en ligne droite sur le relief gardé (règles du joueur, code serveur) :
// |écart de hauteur| ≥ 5 entre deux cases voisines = escalade (refusée) ; eau ≥ 5 de profondeur = nage (refusée).
public static class PathCheck
{
    public const int ClimbStep = 5;
    public const int SwimDepth = 5;
    public const double MaxDistance = 30;

    public sealed record Result(bool Allowed, string Detail, int ShallowWaterCells = 0)
    {
        public static Result Ok(int shallow) => new(true, shallow > 0 ? $"permis ({shallow} case(s) d'eau peu profonde)" : "permis", shallow);
    }

    // cell(E, N) rend (sol, eau) d'une grande case, null si son relief n'est pas connu.
    public static Result Check(double fromEast, double fromNorth, double toEast, double toNorth,
        Func<int, int, (short Elevation, short Water)?> cell, double maxDistance = MaxDistance)
    {
        var distance = Math.Sqrt((toEast - fromEast) * (toEast - fromEast) + (toNorth - fromNorth) * (toNorth - fromNorth));
        if (distance > maxDistance)
        {
            return new Result(false, $"trop loin pour l'instant ({distance:0} cases, {maxDistance:0} au plus)");
        }
        if (distance < 1)
        {
            return new Result(false, "déjà sur place");
        }
        // Ligne droite + deux parallèles à ± 1 case (le personnage dévie un peu) : les trois doivent passer.
        var nx = -(toNorth - fromNorth) / distance;
        var ny = (toEast - fromEast) / distance;
        var shallow = 0;
        foreach (var offset in new[] { 0.0, 1.0, -1.0 })
        {
            var r = CheckLine(fromEast + nx * offset, fromNorth + ny * offset, toEast + nx * offset, toNorth + ny * offset, cell);
            if (!r.Allowed)
            {
                return offset == 0 ? r : r with { Detail = r.Detail + " (à une case du trajet)" };
            }
            shallow = Math.Max(shallow, r.ShallowWaterCells);
        }
        return Result.Ok(shallow);
    }

    private static Result CheckLine(double fromEast, double fromNorth, double toEast, double toNorth,
        Func<int, int, (short Elevation, short Water)?> cell)
    {
        var distance = Math.Sqrt((toEast - fromEast) * (toEast - fromEast) + (toNorth - fromNorth) * (toNorth - fromNorth));
        var steps = Math.Max(1, (int)Math.Ceiling(distance / 0.5));
        (int E, int N)? previous = null;
        int? previousHeight = null;
        var shallow = 0;
        var seen = new HashSet<(int, int)>();
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            var e = (int)Math.Floor(fromEast + (toEast - fromEast) * t);
            var n = (int)Math.Floor(fromNorth + (toNorth - fromNorth) * t);
            if (previous == (e, n))
            {
                continue;
            }
            if (cell(e, n) is not { } c)
            {
                return new Result(false, $"relief inconnu (E {e}, N {n})");
            }
            var depth = c.Water - c.Elevation;
            if (depth >= SwimDepth)
            {
                return new Result(false, $"eau profonde, nage (E {e}, N {n})");
            }
            if (depth > 0 && seen.Add((e, n)))
            {
                shallow++;
            }
            // Hauteur pour le joueur : le sol (l'eau peu profonde ne porte pas).
            int height = c.Elevation;
            if (previousHeight is { } ph && Math.Abs(height - ph) >= ClimbStep)
            {
                return new Result(false, $"escalade nécessaire (écart de {Math.Abs(height - ph)} en E {e}, N {n})");
            }
            previous = (e, n);
            previousHeight = height;
        }
        return Result.Ok(shallow);
    }

    // Lecture d'une case dans le relief gardé.
    public static Func<int, int, (short Elevation, short Water)?> FromChunks(IEnumerable<TerrainChunk> chunks)
    {
        var byPos = chunks.Where(c => c.Dimension == TerrainChunk.Overworld).GroupBy(c => (c.ChunkX, c.ChunkZ)).ToDictionary(g => g.Key, g => g.First());
        return (e, n) =>
        {
            var (cx, cz) = TerrainChunk.ChunkOf(e, n);
            if (!byPos.TryGetValue((cx, cz), out var chunk))
            {
                return null;
            }
            var i = chunk.CellIndex(e - cx * TerrainChunk.Size, n - cz * TerrainChunk.Size);
            return (chunk.Elevations[i], chunk.WaterLevels[i]);
        };
    }
}

public enum TravelState
{
    EnRoute,
    Arrive,
    Bloque,
    TropLong,
    Danger,
    ATerre,
    LiaisonPerdue,
}

// Suivi d'un trajet à partir des instantanés du serveur : arrivée, blocage, temps maximum, danger (vie qui baisse), à terre.
public sealed class TravelGuard
{
    public const double ArrivalCells = 1.5;
    public static readonly TimeSpan StillForArrival = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan StuckWindow = TimeSpan.FromSeconds(5);
    public const double StuckProgress = 0.5;
    public static readonly TimeSpan ServerSilence = TimeSpan.FromSeconds(5);

    private readonly double _targetEast;
    private readonly double _targetNorth;
    private readonly DateTime _start;
    private readonly List<(DateTime At, double Distance)> _history = [];
    private double? _lastHealth;
    private DateTime? _stillSince;
    private (double E, double N)? _lastPos;

    public TravelGuard(double targetEast, double targetNorth, double distance, DateTime start)
    {
        _targetEast = targetEast;
        _targetNorth = targetNorth;
        _start = start;
        MaxDuration = TimeSpan.FromSeconds(10 + 2 * distance);
    }

    public TimeSpan MaxDuration { get; }

    public string Detail { get; private set; } = "";

    public double DistanceLeft(double east, double north) =>
        Math.Sqrt((east - _targetEast) * (east - _targetEast) + (north - _targetNorth) * (north - _targetNorth));

    // Un nouvel instantané (now = heure de lecture ; lastUpdate = dernière nouvelle du serveur).
    public TravelState Update(DateTime now, double east, double north, double? health, DateTime lastUpdate)
    {
        if (now - lastUpdate > ServerSilence)
        {
            Detail = "plus de nouvelles du serveur depuis 5 s";
            return TravelState.LiaisonPerdue;
        }
        if (health is { } h && h <= 0)
        {
            Detail = "vie à 0 (à terre)";
            return TravelState.ATerre;
        }
        if (health is { } h2 && _lastHealth is { } before && h2 < before - 0.01)
        {
            Detail = $"vie qui baisse ({before:0} → {h2:0})";
            _lastHealth = h2;
            return TravelState.Danger;
        }
        if (health is not null)
        {
            _lastHealth = health;
        }
        var left = DistanceLeft(east, north);
        if (_lastPos is { } p && Math.Abs(p.E - east) < 0.05 && Math.Abs(p.N - north) < 0.05)
        {
            _stillSince ??= now;
        }
        else
        {
            _stillSince = null;
        }
        _lastPos = (east, north);
        if (left <= ArrivalCells && _stillSince is { } s && now - s >= StillForArrival)
        {
            Detail = $"arrivé à {left:0.0} case de la cible";
            return TravelState.Arrive;
        }
        if (now - _start > MaxDuration)
        {
            Detail = $"trajet trop long (plus de {MaxDuration.TotalSeconds:0} s)";
            return TravelState.TropLong;
        }
        _history.Add((now, left));
        _history.RemoveAll(x => now - x.At > StuckWindow + TimeSpan.FromSeconds(1));
        var old = _history.FirstOrDefault(x => now - x.At >= StuckWindow);
        if (old != default && old.Distance - left < StuckProgress && left > ArrivalCells)
        {
            Detail = $"bloqué : {left:0.0} cases restantes, moins de 0,5 case gagnée en 5 s";
            return TravelState.Bloque;
        }
        Detail = $"{left:0.0} cases restantes";
        return TravelState.EnRoute;
    }

    // Point de fuite : 10 cases à l'opposé d'une menace (ou du sens de marche).
    public static (double East, double North) FleePoint(double east, double north, double fromEast, double fromNorth, double cells = 10)
    {
        var dx = east - fromEast;
        var dy = north - fromNorth;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 0.01)
        {
            return (east, north - cells);
        }
        return (east + dx / len * cells, north + dy / len * cells);
    }
}

// Fenêtre « à terre » : Get Up seulement, jamais Go Home (règles de la nuit §3).
public static class GetUpChooser
{
    public sealed record Word(string Text, int X, int Y, int Width, int Height)
    {
        public int CenterX => X + Width / 2;
        public int CenterY => Y + Height / 2;
    }

    // Rend le centre du texte « Get Up » SEULEMENT si « Go Home » est lu ailleurs, à plus de 100 px. Sinon null (ne rien cliquer).
    public static (int X, int Y)? Choose(IReadOnlyList<Word> lines)
    {
        var getUp = lines.Where(w => Normalize(w.Text).Contains("getup")).ToList();
        var goHome = lines.Where(w => Normalize(w.Text).Contains("gohome")).ToList();
        if (getUp.Count != 1 || goHome.Count != 1)
        {
            return null;
        }
        var g = getUp[0];
        var h = goHome[0];
        if (Normalize(g.Text).Contains("gohome"))
        {
            return null;
        }
        var dx = g.CenterX - h.CenterX;
        var dy = g.CenterY - h.CenterY;
        if (Math.Sqrt(dx * dx + dy * dy) <= 100)
        {
            return null;
        }
        return (g.CenterX, g.CenterY);
    }

    private static string Normalize(string s) => new(s.Where(char.IsLetter).Select(char.ToLowerInvariant).ToArray());
}
