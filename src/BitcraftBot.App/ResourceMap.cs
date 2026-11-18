using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Carte des arbres autour du personnage (H2), dessinée d'après le serveur.
// Nord en haut, est à droite, comme les coordonnées N et E du jeu. Le personnage est au centre.
public sealed class ResourceMap : FrameworkElement
{
    // Rayon montré, en unités de l'écran (N, E). Les ressources sont lues sur environ 13 unités.
    public const double RadiusUnits = 14;

    private static readonly Brush Background = Frozen(Color.FromRgb(20, 21, 23));
    private static readonly Pen RingPen = FrozenPen(Color.FromRgb(60, 63, 68), 1);
    private static readonly Pen AxisPen = FrozenPen(Color.FromRgb(45, 47, 51), 1);
    private static readonly Brush TextBrush = Frozen(Color.FromRgb(154, 160, 166));
    private static readonly Brush PlayerBrush = Frozen(Color.FromRgb(138, 180, 248));
    private static readonly Pen DestinationPen = FrozenPen(Color.FromRgb(138, 180, 248), 1.5, dashed: true);
    private static readonly Brush MatureBrush = Frozen(Color.FromRgb(52, 168, 83));
    private static readonly Brush YoungBrush = Frozen(Color.FromRgb(168, 218, 181));
    private static readonly Brush DeadBrush = Frozen(Color.FromRgb(141, 110, 99));
    private static readonly Brush OtherTreeBrush = Frozen(Color.FromRgb(129, 201, 149));
    private static readonly Brush OtherBrush = Frozen(Color.FromRgb(95, 99, 104));
    private static readonly Pen DamagedPen = FrozenPen(Color.FromRgb(251, 140, 0), 2);
    private static readonly Pen BestPen = FrozenPen(Color.FromRgb(253, 214, 99), 2.5);
    private static readonly Pen ScreenTargetPen = FrozenPen(Color.FromRgb(242, 139, 245), 2.5);
    private static readonly Pen SelectedPen = FrozenPen(Colors.White, 2);
    private static readonly Typeface Font = new("Segoe UI");

    private ServerPosition? _player;
    private ServerPosition? _destination;
    private IReadOnlyList<NearbyResource> _resources = [];
    private bool _treesOnly = true;
    private ulong? _best;
    private ulong? _screenTarget;
    private ulong? _selected;

    // Clic sur la carte : la ressource la plus proche du clic (null si rien autour).
    public event Action<ulong?>? ResourceClicked;

    public void Update(ServerPosition? player, ServerPosition? destination, IReadOnlyList<NearbyResource> resources,
        bool treesOnly, ulong? best, ulong? screenTarget, ulong? selected)
    {
        _player = player;
        _destination = destination;
        _resources = resources;
        _treesOnly = treesOnly;
        _best = best;
        _screenTarget = screenTarget;
        _selected = selected;
        InvalidateVisual();
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

        // Repères : axes, cercles à 5 et 10 cases, points cardinaux.
        dc.DrawLine(AxisPen, new Point(c.X, c.Y - RadiusUnits * scale), new Point(c.X, c.Y + RadiusUnits * scale));
        dc.DrawLine(AxisPen, new Point(c.X - RadiusUnits * scale, c.Y), new Point(c.X + RadiusUnits * scale, c.Y));
        foreach (var ring in new[] { 5, 10 })
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
            if (!r.IsTree && !_treesOnly && Visible(r))
            {
                dc.DrawEllipse(OtherBrush, null, ToPoint(r.Position, player), 2.5, 2.5);
            }
        }
        foreach (var r in _resources.Where(r => r.IsTree && Visible(r)))
        {
            var p = ToPoint(r.Position, player);
            var (brush, radius) = r.Age switch
            {
                "Mûr" => (MatureBrush, 7.0),
                "Jeune" => (YoungBrush, 4.0),
                "Mort" => (DeadBrush, 4.0),
                _ => (OtherTreeBrush, 4.0),
            };
            dc.DrawEllipse(brush, r.Health < r.MaxHealth ? DamagedPen : null, p, radius, radius);
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
            .Where(r => (r.IsTree || !_treesOnly) && Visible(r))
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
