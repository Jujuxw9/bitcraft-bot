using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Carte des arbres autour du personnage (H2), dessinée d'après le serveur.
// Nord en haut, est à droite, comme les coordonnées N et E du jeu. Le personnage est au centre.
public sealed class ResourceMap : FrameworkElement
{
    // Rayon montré, en unités de l'écran (N, E). Les ressources sont lues sur environ 13 unités.
    // v0.4.15 : zone lue d’environ 20 cases ; v0.4.21 : 25 cases ; v0.4.22 : 30 cases (un seul cercle, celui de la zone lue).
    public const double DataRadius = 30;
    public const double RadiusUnits = DataRadius + 1;

    // v0.4.22 : zoom ; v0.4.23 (Julien) : de 10 à 100 cases de rayon, la zone lue (30) au milieu.
    public static readonly double[] ViewSteps = [10, 15, 20, 30, 45, 65, 100, 140, 200];
    private int _zoomIndex = 3;

    // Rayon montré, en grandes cases (une case de marge autour du dernier palier).
    public double ViewRadius => ViewSteps[_zoomIndex] + 1;

    // Rayon annoncé à Julien (« vue : N cases »).
    public double ViewCases => ViewSteps[_zoomIndex];

    public event Action? ZoomChanged;

    // v0.4.28 : vue choisie directement (essai de la carte) : le cran le plus proche.
    public void ZoomTo(double cases)
    {
        var index = Array.FindIndex(ViewSteps, v => v >= cases);
        ZoomBy((index < 0 ? ViewSteps.Length - 1 : index) - _zoomIndex);
    }

    // v0.4.23 : dans le jeu, une ligne de cases (N) est plus courte qu'une colonne (E) : les cases sont des hexagones
    // réguliers, pointe en haut, donc une ligne monte de √3/2 de la largeur d'une case. Mesuré sur la carte du jeu
    // (morceaux de 72 points de large pour 62,5 de haut). Avant, la carte du bot était étirée vers le haut.
    public static readonly double RowFactor = Math.Sqrt(3) / 2;

    // Pas de zoom : -1 = rapprocher (moins de cases), +1 = éloigner (plus de cases).
    public void ZoomBy(int step)
    {
        var next = Math.Clamp(_zoomIndex + step, 0, ViewSteps.Length - 1);
        if (next != _zoomIndex)
        {
            _zoomIndex = next;
            InvalidateVisual();
            ZoomChanged?.Invoke();
        }
    }

    // v0.4.27 : même couleur que les zones inconnues du fond de carte (avant : bleu nuit presque noir).
    private static readonly Brush Background = Frozen(Color.FromRgb(0x18, 0x22, 0x2E));
    private static readonly Pen RingPen = FrozenPen(Color.FromRgb(43, 76, 112), 1);
    private static readonly Pen AxisPen = FrozenPen(Color.FromRgb(26, 51, 80), 1);
    private static readonly Brush TextBrush = Frozen(Color.FromRgb(147, 168, 191));
    private static readonly Brush PlayerBrush = Frozen(Color.FromRgb(95, 168, 224));
    private static readonly Pen DestinationPen = FrozenPen(Color.FromRgb(138, 180, 248), 1.5, dashed: true);
    private static readonly Brush MatureBrush = Frozen(Color.FromRgb(52, 168, 83));
    private static readonly Brush YoungBrush = Frozen(Color.FromRgb(168, 218, 181));
    private static readonly Brush DeadBrush = Frozen(Color.FromRgb(141, 110, 99));
    private static readonly Brush OtherTreeBrush = Frozen(Color.FromRgb(129, 201, 149));
    private static readonly Pen DamagedPen = FrozenPen(Color.FromRgb(251, 140, 0), 2);
    private static readonly Pen BestPen = FrozenPen(Color.FromRgb(210, 182, 93), 2.5);
    private static readonly Pen ScreenTargetPen = FrozenPen(Color.FromRgb(242, 139, 245), 2.5);
    private static readonly Pen SelectedPen = FrozenPen(Colors.White, 2);
    private static readonly Typeface Font = new("Segoe UI");

    private ServerPosition? _player;
    private ServerPosition? _destination;
    private IReadOnlyList<NearbyResource> _resources = [];
    // v0.4.22 : ressources vues avant, hors de la zone lue en direct (dessinées plus pâles).
    private IReadOnlyList<NearbyResource> _remembered = [];
    private Func<NearbyResource, bool> _shown = r => r.IsTree;
    private ulong? _best;
    private ulong? _screenTarget;
    private ulong? _selected;
    private BitmapSource? _terrain;
    private TerrainImage? _terrainImage;
    private string? _terrainSignature;
    private static readonly Pen DotOutline = FrozenPen(Color.FromArgb(200, 10, 16, 26), 1);
    private static readonly Brush TerrainVeil = Frozen(Color.FromArgb(70, 10, 21, 36));

    // Clic sur la carte : la ressource la plus proche du clic (null si rien autour).
    public event Action<ulong?>? ResourceClicked;

    public void Update(ServerPosition? player, ServerPosition? destination, IReadOnlyList<NearbyResource> resources,
        Func<NearbyResource, bool> shown, ulong? best, ulong? screenTarget, ulong? selected, IReadOnlyList<NearbyResource>? remembered = null)
    {
        _player = player;
        _destination = destination;
        _resources = resources;
        _remembered = remembered ?? [];
        _shown = shown;
        _best = best;
        _screenTarget = screenTarget;
        _selected = selected;
        InvalidateVisual();
    }

    // Fond de carte (v0.4.18) : texte pour l'écran (null si aucun morceau).
    public string? TerrainText { get; private set; }

    // Refait l'image du fond seulement si les morceaux reçus ont changé. Renvoie true si un nouveau calcul a commencé.
    // v0.4.28 : le calcul (assemblage des morceaux puis hexagones point par point) se fait en arrière-plan ;
    // l'image est posée sur la carte quand elle est prête (TerrainReady). Un calcul dépassé est jeté.
    public bool SetTerrain(IReadOnlyCollection<TerrainChunk> chunks, ExploredChunks? explored)
    {
        // v0.4.28 : l'image est calculée à la taille où elle sera vue (points par case à l'écran) : pas de bords
        // d'hexagones écrasés quand on dézoome (fond granuleux), et moins de calcul.
        var wanted = WantedHexPixels();
        var signature = TerrainImage.SignatureOf(chunks, explored) + "|" + wanted;
        if (signature == _terrainSignature)
        {
            return false;
        }
        _terrainSignature = signature;
        var job = ++_terrainJob;
        var list = chunks.ToList();
        _ = Task.Run(() =>
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var image = TerrainImage.Build(list, explored);
            BitmapSource? bitmap = null;
            var hexPixels = 12;
            if (image is not null)
            {
                hexPixels = Math.Max(2, Math.Min(wanted, HexRaster.PixelsPerCell(image)));
                var pixels = HexRaster.Render(image, hexPixels, out var width, out var height);
                bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, width * 4);
                bitmap.Freeze();
            }
            var ms = watch.ElapsedMilliseconds;
            Dispatcher.BeginInvoke(() =>
            {
                if (job != _terrainJob)
                {
                    return;
                }
                _terrainImage = image;
                _terrain = bitmap;
                _hexPixels = hexPixels;
                TerrainText = image is null ? null
                    : $"fond de carte : {image.ChunkCount} morceaux de terrain" + (explored is null ? "" : ", zones non découvertes assombries");
                InvalidateVisual();
                TerrainReady?.Invoke(new TerrainBuild(image?.ChunkCount ?? 0, bitmap?.PixelWidth ?? 0, bitmap?.PixelHeight ?? 0, ms, ViewCases));
            });
        });
        return true;
    }

    private int _terrainJob;

    // Points par case à l'écran (arrondi au-dessus), entre 2 et 12.
    private int WantedHexPixels()
    {
        var side = ActualWidth > 0 ? Math.Min(ActualWidth, ActualHeight) : 560;
        var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        return Math.Clamp((int)Math.Ceiling((side / 2 - 18) / ViewRadius * dpi), 2, 12);
    }

    // Fond prêt : nombre de morceaux, taille de l'image, durée du calcul en arrière-plan (ms), vue (cases).
    public event Action<TerrainBuild>? TerrainReady;

    // Points d'image par grande case dans l'image des hexagones (12 = bords nets ; moins quand la vue est large).
    private int _hexPixels = 12;

    // v0.4.28 : carte carrée, aussi grande que la place donnée (avant : 560 × 560 fixe, agrandie par un Viewbox).
    protected override Size MeasureOverride(Size availableSize)
    {
        var side = Math.Min(availableSize.Width, availableSize.Height);
        if (double.IsInfinity(side))
        {
            side = double.IsInfinity(availableSize.Width) ? availableSize.Height : availableSize.Width;
        }
        if (double.IsInfinity(side) || double.IsNaN(side))
        {
            side = 560;
        }
        side = Math.Max(MinSide, side);
        return new Size(side, side);
    }

    private const double MinSide = 240;

    // Durée du dernier dessin sur le fil de l'interface (ms), pour les mesures de l'essai.
    public double LastRenderMs { get; private set; }

    private double Scale => (Math.Min(ActualWidth, ActualHeight) / 2 - 18) / ViewRadius;

    private Point Center => new(ActualWidth / 2, ActualHeight / 2);

    private Point ToPoint(ServerPosition p, ServerPosition player) =>
        new(Center.X + (p.ScreenEast - player.ScreenEast) * Scale, Center.Y - (p.ScreenNorth - player.ScreenNorth) * Scale * RowFactor);

    // Point de la carte → position (cases E, N), pour « Se déplacer ici ».
    public (double East, double North)? ToCell(Point at)
    {
        if (_player is not { } player || Scale <= 0)
        {
            return null;
        }
        return (player.ScreenEast + (at.X - Center.X) / Scale, player.ScreenNorth - (at.Y - Center.Y) / (Scale * RowFactor));
    }

    protected override void OnRender(DrawingContext dc)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var size = new Rect(0, 0, ActualWidth, ActualHeight);
        dc.DrawRectangle(Background, null, size);
        var c = Center;
        var scale = Scale;
        if (scale <= 0)
        {
            return;
        }
        // v0.4.23 : tout est coupé au bord de la carte (avant, le cercle de 30 cases débordait quand on zoomait).
        dc.PushClip(new RectangleGeometry(size));
        try
        {
            RenderInside(dc, size, c, scale);
        }
        finally
        {
            dc.Pop();
            LastRenderMs = watch.Elapsed.TotalMilliseconds;
        }
    }

    private void RenderInside(DrawingContext dc, Rect size, Point c, double scale)
    {

        // Fond de carte (v0.4.18 ; v0.4.20 en hexagones) : une case du jeu = un hexagone, nord en haut, légèrement voilé pour que les points restent lisibles.
        if (_terrain is { } bitmap && _terrainImage is { } image && _player is { } at)
        {
            var left = c.X + (image.WestCell - 0.5 - at.ScreenEast) * scale;
            var top = c.Y - (image.NorthCell + 0.5 - at.ScreenNorth) * scale * RowFactor;
            dc.DrawImage(bitmap, new Rect(left, top, bitmap.PixelWidth / (double)_hexPixels * scale, bitmap.PixelHeight / (double)_hexPixels * scale));
            dc.DrawRectangle(TerrainVeil, null, size);
        }

        // Repères : axes et un seul cercle, la limite de la zone lue en direct (v0.4.22, demande de Julien).
        var view = ViewRadius;
        dc.DrawLine(AxisPen, new Point(c.X, 0), new Point(c.X, ActualHeight));
        dc.DrawLine(AxisPen, new Point(0, c.Y), new Point(ActualWidth, c.Y));
        // Limite de la zone lue : un ovale, puisque les lignes N sont plus courtes que les colonnes E.
        dc.DrawEllipse(null, RingPen, c, DataRadius * scale, DataRadius * scale * RowFactor);
        if (DataRadius < view)
        {
            Text(dc, $"{DataRadius:0}", new Point(c.X + DataRadius * scale * 0.71 + 2, c.Y - DataRadius * scale * RowFactor * 0.71 - 14), 11);
        }
        Text(dc, "N", new Point(c.X - 4, 2), 13);
        Text(dc, "S", new Point(c.X - 4, ActualHeight - 18), 13);
        Text(dc, "E", new Point(ActualWidth - 14, c.Y - 9), 13);
        Text(dc, "O", new Point(4, c.Y - 9), 13);

        if (_player is not { } player)
        {
            Text(dc, "En attente de la position du personnage…", new Point(10, ActualHeight - 40), 12);
            return;
        }

        // v0.4.28 : points plus petits quand on dézoome (à 140-200 cases ils se chevauchaient), sans contour au-delà de 65 cases.
        var dot = DotScale(ViewCases);
        var outline = dot < 1 ? null : DotOutline;

        // Ressources vues avant, hors de la zone lue en direct (v0.4.22) : pâles, dessous.
        if (_remembered.Count > 0)
        {
            dc.PushOpacity(0.45);
            foreach (var r in _remembered)
            {
                if (_shown(r) && Visible(r))
                {
                    dc.DrawEllipse(r.IsTree ? OtherTreeBrush : FamilyColors.Of(r.Family), outline, ToPoint(r.Position, player), 3 * dot, 3 * dot);
                }
            }
            dc.Pop();
        }

        // Ressources : les autres d'abord (dessous), puis les arbres, puis les repères de choix.
        foreach (var r in _resources)
        {
            if (!r.IsTree && _shown(r) && Visible(r))
            {
                dc.DrawEllipse(FamilyColors.Of(r.Family), outline, ToPoint(r.Position, player), 3.5 * dot, 3.5 * dot);
            }
        }
        foreach (var r in _resources.Where(r => r.IsTree && _shown(r) && Visible(r)))
        {
            var p = ToPoint(r.Position, player);
            var (brush, radius) = r.Age switch
            {
                "Mûr" => (MatureBrush, 7.0),
                "Jeune" => (YoungBrush, 4.0),
                "Mort" => (DeadBrush, 4.0),
                _ => (OtherTreeBrush, 4.0),
            };
            dc.DrawEllipse(brush, r.Health < r.MaxHealth ? DamagedPen : outline, p, radius * dot, radius * dot);
        }
        foreach (var (id, pen, radius) in new[] { (_best, BestPen, 11.0), (_screenTarget, ScreenTargetPen, 14.0), (_selected, SelectedPen, 9.5) })
        {
            // v0.4.23 : la ressource recherchée peut être une ressource gardée en mémoire (hors de la zone lue).
            if (id is { } entity && (_resources.FirstOrDefault(r => r.EntityId == entity) ?? _remembered.FirstOrDefault(r => r.EntityId == entity)) is { } r && Visible(r))
            {
                dc.DrawEllipse(null, pen, ToPoint(r.Position, player), radius, radius);
            }
        }

        // Destination (quand le personnage marche), puis le personnage.
        if (_destination is { } d && d.ScreenDistanceTo(player) > 0.3)
        {
            dc.DrawLine(DestinationPen, c, ToPoint(d, player));
        }
        // Le personnage reste bien visible à toutes les vues (taille fixe, contour blanc).
        dc.DrawEllipse(PlayerBrush, PlayerPen, c, 6, 6);
    }

    private static readonly Pen PlayerPen = FrozenPen(Colors.White, 1.5);

    // Taille des points selon la vue : 1 jusqu'à 45 cases, puis plus petits (0,45 à 200 cases).
    public static double DotScale(double viewCases) => viewCases switch
    {
        <= 45 => 1.0,
        <= 65 => 0.85,
        <= 100 => 0.7,
        <= 140 => 0.55,
        _ => 0.45,
    };

    // Dans la vue : distance au personnage recalculée (les ressources gardées ont une distance ancienne).
    private bool Visible(NearbyResource r) => _player is { } p && r.Position.ScreenDistanceTo(p) <= ViewRadius;

    // v0.4.27 : texte « vue il y a N h » d'une ressource gardée en mémoire (donné par la fenêtre).
    public Func<ulong, string?>? SeenText { get; set; }

    private ulong? _hovered;

    // Survol d'une ressource gardée (pâle) : son nom et depuis quand elle n'a pas été revue.
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_player is not { } player)
        {
            return;
        }
        var at = e.GetPosition(this);
        var near = _remembered
            .Where(r => _shown(r) && Visible(r))
            .Select(r => (r, d: (ToPoint(r.Position, player) - at).Length))
            .Where(x => x.d <= 8)
            .OrderBy(x => x.d)
            .Select(x => x.r)
            .FirstOrDefault();
        if (near?.EntityId == _hovered)
        {
            return;
        }
        _hovered = near?.EntityId;
        ToolTip = near is null ? null : $"{near.Name}{(near.Tier is { } t ? $" (T{t})" : "")} — {SeenText?.Invoke(near.EntityId) ?? "gardée en mémoire"}";
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (_player is not { } player)
        {
            return;
        }
        var click = e.GetPosition(this);
        var nearest = _resources
            .Where(r => _shown(r) && Visible(r))
            .Select(r => (r, d: (ToPoint(r.Position, player) - click).Length))
            .Where(x => x.d <= 14)
            .OrderBy(x => x.d)
            .Select(x => (ulong?)x.r.EntityId)
            .FirstOrDefault();
        ResourceClicked?.Invoke(nearest);
    }

    // v0.4.28 : clic droit sur la carte = la case visée (E, N), pour « Se déplacer ici » (T-10). Pas encore de menu.
    public event Action<double, double>? CellRightClicked;

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        if (ToCell(e.GetPosition(this)) is { } cell)
        {
            CellRightClicked?.Invoke(cell.East, cell.North);
            e.Handled = true;
        }
    }

    private void Text(DrawingContext dc, string text, Point at, double size)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Font, size, TextBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, at);
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen FrozenPen(Color color, double thickness, bool dashed = false)
    {
        var pen = new Pen(Frozen(color), thickness);
        if (dashed)
        {
            pen.DashStyle = DashStyles.Dash;
        }
        pen.Freeze();
        return pen;
    }
}

// v0.4.28 : résultat d'un calcul du fond de carte (pour le journal et les mesures).
public sealed record TerrainBuild(int Chunks, int Width, int Height, long Milliseconds, double ViewCases);
