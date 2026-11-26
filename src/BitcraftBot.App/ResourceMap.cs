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
    public const double RadiusUnits = 21; // v0.4.15 : zone lue d’environ 20 cases

    private static readonly Brush Background = Frozen(Color.FromRgb(10, 21, 36));
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
        Func<NearbyResource, bool> shown, ulong? best, ulong? screenTarget, ulong? selected)
    {
        _player = player;
        _destination = destination;
        _resources = resources;
        _shown = shown;
        _best = best;
        _screenTarget = screenTarget;
        _selected = selected;
        InvalidateVisual();
    }

    // Fond de carte (v0.4.18) : texte pour l'écran (null si aucun morceau).
    public string? TerrainText { get; private set; }

    // Refait l'image du fond seulement si les morceaux reçus ont changé. Renvoie true si l'image a été refaite.
    public bool SetTerrain(IReadOnlyCollection<TerrainChunk> chunks, ExploredChunks? explored)
    {
        var signature = TerrainImage.SignatureOf(chunks, explored);
        if (signature == _terrainSignature)
        {
            return false;
        }
        _terrainSignature = signature;
        _terrainImage = TerrainImage.Build(chunks, explored);
        if (_terrainImage is { } image)
        {
            _terrain = HexImage(image);
            TerrainText = $"fond de carte : {image.ChunkCount} morceaux de terrain" + (explored is null ? "" : ", zones non découvertes assombries");
        }
        else
        {
            _terrain = null;
            TerrainText = null;
        }
        InvalidateVisual();
        return true;
    }

    // Points d'image par grande case dans l'image des hexagones (assez pour des bords nets une fois agrandie).
    private const int HexPixels = 12;
    private static readonly Pen HexEdge = FrozenPen(Color.FromArgb(55, 0, 0, 0), 0.8);

    // v0.4.20 : chaque grande case (case du jeu) est dessinée en hexagone, comme dans le jeu : pointe en haut,
    // une ligne sur deux décalée d'une demi-case vers l'est (coordonnées décalées du serveur). Avant : un carré
    // d'un point par case, agrandi, donc très pixelisé. L'image est faite une seule fois par fond de carte reçu.
    private static BitmapSource HexImage(TerrainImage image)
    {
        var width = (int)Math.Ceiling((image.Width + 0.5) * HexPixels);
        var height = image.Height * HexPixels;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var brushes = new Dictionary<uint, Brush>();
            for (var row = 0; row < image.Height; row++)
            {
                var cellZ = image.NorthCell - row;
                var shift = (cellZ & 1) == 1 ? 0.5 : 0.0;
                for (var col = 0; col < image.Width; col++)
                {
                    var argb = image.Pixels[row * image.Width + col];
                    if (!brushes.TryGetValue(argb, out var brush))
                    {
                        brush = Frozen(Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
                        brushes[argb] = brush;
                    }
                    dc.DrawGeometry(brush, HexEdge, Hexagon((col + 0.5 + shift) * HexPixels, (row + 0.5) * HexPixels, HexPixels));
                }
            }
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    // Hexagone pointe en haut : largeur d'une case, hauteur 4/3 de case (les lignes se recouvrent d'un quart, comme un pavage).
    private static StreamGeometry Hexagon(double cx, double cy, double cell)
    {
        var half = cell / 2;
        var tall = cell * 2 / 3;
        var mid = cell / 3;
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(cx, cy - tall), true, true);
            ctx.PolyLineTo([
                new Point(cx + half, cy - mid), new Point(cx + half, cy + mid), new Point(cx, cy + tall),
                new Point(cx - half, cy + mid), new Point(cx - half, cy - mid)], true, true);
        }
        g.Freeze();
        return g;
    }

    private double Scale => (Math.Min(ActualWidth, ActualHeight) / 2 - 18) / RadiusUnits;

    private Point Center => new(ActualWidth / 2, ActualHeight / 2);

    private Point ToPoint(ServerPosition p, ServerPosition player) =>
        new(Center.X + (p.ScreenEast - player.ScreenEast) * Scale, Center.Y - (p.ScreenNorth - player.ScreenNorth) * Scale);

    protected override void OnRender(DrawingContext dc)
    {
        var size = new Rect(0, 0, ActualWidth, ActualHeight);
        dc.DrawRectangle(Background, null, size);
        var c = Center;
        var scale = Scale;
        if (scale <= 0)
        {
            return;
        }

        // Fond de carte (v0.4.18 ; v0.4.20 en hexagones) : une case du jeu = un hexagone, nord en haut, légèrement voilé pour que les points restent lisibles.
        if (_terrain is { } bitmap && _terrainImage is { } image && _player is { } at)
        {
            dc.PushClip(new RectangleGeometry(size));
            var left = c.X + (image.WestCell - 0.5 - at.ScreenEast) * scale;
            var top = c.Y - (image.NorthCell + 0.5 - at.ScreenNorth) * scale;
            dc.DrawImage(bitmap, new Rect(left, top, bitmap.PixelWidth / (double)HexPixels * scale, image.Height * scale));
            dc.DrawRectangle(TerrainVeil, null, size);
            dc.Pop();
        }

        // Repères : axes, cercles à 5 et 10 cases, points cardinaux.
        dc.DrawLine(AxisPen, new Point(c.X, c.Y - RadiusUnits * scale), new Point(c.X, c.Y + RadiusUnits * scale));
        dc.DrawLine(AxisPen, new Point(c.X - RadiusUnits * scale, c.Y), new Point(c.X + RadiusUnits * scale, c.Y));
        foreach (var ring in new[] { 5, 10, 15, 20 })
        {
            dc.DrawEllipse(null, RingPen, c, ring * scale, ring * scale);
            Text(dc, $"{ring}", new Point(c.X + ring * scale * 0.71 + 2, c.Y - ring * scale * 0.71 - 14), 11);
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

        // Ressources : les autres d'abord (dessous), puis les arbres, puis les repères de choix.
        foreach (var r in _resources)
        {
            if (!r.IsTree && _shown(r) && Visible(r))
            {
                dc.DrawEllipse(FamilyColors.Of(r.Family), DotOutline, ToPoint(r.Position, player), 3.5, 3.5);
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
            dc.DrawEllipse(brush, r.Health < r.MaxHealth ? DamagedPen : DotOutline, p, radius, radius);
        }
        foreach (var (id, pen, radius) in new[] { (_best, BestPen, 11.0), (_screenTarget, ScreenTargetPen, 14.0), (_selected, SelectedPen, 9.5) })
        {
            if (id is { } entity && _resources.FirstOrDefault(r => r.EntityId == entity) is { } r && Visible(r))
            {
                dc.DrawEllipse(null, pen, ToPoint(r.Position, player), radius, radius);
            }
        }

        // Destination (quand le personnage marche), puis le personnage.
        if (_destination is { } d && d.ScreenDistanceTo(player) > 0.3)
        {
            dc.DrawLine(DestinationPen, c, ToPoint(d, player));
        }
        dc.DrawEllipse(PlayerBrush, new Pen(Brushes.White, 1.5), c, 6, 6);
    }

    private bool Visible(NearbyResource r) => r.ScreenDistance <= RadiusUnits;

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
