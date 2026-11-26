using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Carte » : fond de carte (terrain lu sur le serveur, v0.4.18), filtres en pastilles et tableau des ressources proches.
public partial class MainWindow
{
    private const int MaxRowsShown = 80;

    private readonly ObservableCollection<ResourceRow> _resourceRows = [];
    // v0.4.18 (D-070) : filtres en pastilles cliquables, retenus dans les réglages (catégories et paliers).
    private MapFilter _mapFilter = new();
    private readonly Dictionary<string, MapPill> _categoryPills = [];
    private readonly Dictionary<int, MapPill> _tierPills = [];
    private readonly ScreenTargetMatcher _targetMatcher = new();
    private TargetReading? _lastHudTarget;
    private DateTime _lastHudTargetAt;
    private ulong? _selectedResource;
    private ulong? _loggedScreenTarget;
    private bool _loggedScreenTargetSure;
    private ulong? _loggedBest;
    private bool _updatingRows;
    private bool _terrainShownLogged;

    private void InitResourceMap()
    {
        SrvResourceList.ItemsSource = _resourceRows;
        SrvMap.ResourceClicked += id =>
        {
            _selectedResource = id;
            var row = _resourceRows.FirstOrDefault(r => r.EntityId == id);
            SrvResourceList.SelectedItem = row;
            if (row is not null)
            {
                SrvResourceList.ScrollIntoView(row);
            }
        };
        _mapFilter = new MapFilter(_settings.MapShownCategories, _settings.MapHiddenTiers);

        // Une pastille par catégorie (couleur des points de la carte) ; le nombre lu s'ajoute à la connexion.
        foreach (var category in MapCategories.All)
        {
            var pill = new MapPill(FamilyColors.Of(category.Families[0]), $"{category.Name} : {category.Hint}. Clic : afficher ou masquer.");
            pill.Clicked += right =>
            {
                _mapFilter.ToggleCategory(category.Name);
                MapFilterChanged();
            };
            _categoryPills[category.Name] = pill;
            FamilyChecks.Children.Add(pill);
        }
        // Une pastille par palier, T1 à T10, toutes de la même couleur (v0.4.20 : plus simple). Clic : allumer ou éteindre ; clic droit : ce palier seul.
        foreach (var tier in MapFilter.TierPills)
        {

            var pill = new MapPill(TierColors.Pill, $"Tier {tier}. Clic : afficher ou masquer. Clic droit : ce tier seul (ou tout).");
            pill.Clicked += right =>
            {
                if (right) _mapFilter.OnlyTier(tier);
                else _mapFilter.ToggleTier(tier);
                MapFilterChanged();
            };
            _tierPills[tier] = pill;
            TierChecks.Children.Add(pill);
        }
        ShowPills([]);
        InitMapMemory();
    }

    private void MapFilterChanged()
    {
        _settings.MapShownCategories = [.. _mapFilter.ShownCategories];
        _settings.MapHiddenTiers = [.. _mapFilter.HiddenTiers];
        _settings.Save(LocalSettings.DefaultPath);
        _resourceRows.Clear();
        ShowPills(_lastMapResources);
    }


    private IReadOnlyList<NearbyResource> _lastMapResources = [];

    // Pastilles : allumées ou éteintes selon le filtre, avec le nombre de ressources de la zone lue.
    private void ShowPills(IReadOnlyList<NearbyResource> resources)
    {
        _lastMapResources = resources;
        var byCategory = _mapFilter.CategoryCounts(resources);
        foreach (var (name, pill) in _categoryPills)
        {
            pill.Set(name, byCategory.GetValueOrDefault(name), _mapFilter.IsShown(name));
        }
        var byTier = _mapFilter.TierCounts(resources);
        foreach (var (tier, pill) in _tierPills)
        {
            pill.Set($"T{tier}", byTier.GetValueOrDefault(tier), _mapFilter.IsShown(tier));
        }
    }

    private void SrvResourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingRows)
        {
            _selectedResource = (SrvResourceList.SelectedItem as ResourceRow)?.EntityId;
        }
    }

    // visible : l'onglet Carte est affiché. Sinon, seul le journal (cible à l'écran, meilleur arbre) est tenu à jour.
    private void ShowResources(ServerReader reader, PlayerSnapshot s, bool visible)
    {
        if (!visible)
        {
            var bestHidden = s.Resources.FirstOrDefault(ResourceNames.IsHarvestTarget);
            var targetHidden = CurrentHudTarget();
            LogMatches(bestHidden, _targetMatcher.Match(targetHidden?.Name, targetHidden?.Health, s.Resources), targetHidden);
            return;
        }
        MapPlayerText.Text = s.Position is { } me
            ? $"Vous : {me.ScreenText} (±1){(s.Walking == true ? ", en marche" : "")} — vie {s.Health:0}, endurance {s.Stamina:0}, satiété {s.Satiation:0} — {s.RegionName ?? "région ?"}"
            : "En attente de la position du personnage…";

        ShowPills(s.Resources);
        var filter = _mapFilter;
        bool Shown(NearbyResource r) => filter.Shows(r);
        var shown = s.Resources.Where(Shown).ToList();
        ShowTerrain(reader, s.Position);
        SrvResourceCount.Text = $"{shown.Count} / {s.Resources.Count} ressources · {s.Resources.Count(ResourceNames.IsHarvestTarget)} arbres mûrs";

        var best = s.Resources.FirstOrDefault(ResourceNames.IsHarvestTarget);
        var target = CurrentHudTarget();
        var match = _targetMatcher.Match(target?.Name, target?.Health, s.Resources);

        SrvBestTree.Text = best is null ? "aucun arbre mûr dans la zone lue" : Describe(best);
        SrvScreenTarget.Text = target is not { Name: { } targetName }
            ? "aucune cible lue à l'écran"
            : match is null
                ? $"« {targetName} » {ResourceRow.FormatHealth(target.Health, target.MaxHealth)} : introuvable à moins de {ScreenTargetMatcher.MaxDistance:0} cases"
                : $"{Describe(match.Resource)}{(match.Sure ? " — c'est bien elle (même santé)" : " — probable (même nom)")}";
        // La zone est relue quand le personnage s'éloigne de la moitié du rayon (20 petites cases ≈ 7 cases du jeu).
        SrvArea.Text = reader.ResourceArea is { } a && reader.ResourceAreaUpdated is { } at
            ? $"centre {new ServerPosition(a.CenterX, a.CenterZ).ScreenText}, rayon ≈ {ServerReader.ResourceRadius / 3} cases, relue à {at:HH:mm:ss} (relue dès que vous vous éloignez d’environ 15 cases)"
              + (reader.HealthRefused ? " — santé refusée par le serveur" : "")
              + (reader.TerrainRefused ? " — fond de carte refusé par le serveur" : SrvMap.TerrainText is { } t ? $" — {t}" : "")
            : "—";

        UpdateRows(shown.Take(MaxRowsShown).ToList(), best?.EntityId, match?.Resource.EntityId);
        var remembered = _resourceMemory.Update(s.Resources, reader.ResourceArea);
        SrvMap.Update(s.Position, s.Walking == true ? s.Destination : null, s.Resources, Shown, best?.EntityId, match?.Resource.EntityId, _selectedResource, remembered);
        LogMatches(best, match, target);
    }

    // Cible lue sur l'écran, oubliée si la lecture de l'écran s'est arrêtée.
    private TargetReading? CurrentHudTarget() =>
        DateTime.Now - _lastHudTargetAt > TimeSpan.FromSeconds(5) ? null : _lastHudTarget;

    private static string Describe(NearbyResource r) =>
        $"{r.Name}, {r.Position.ScreenText}, à {r.ScreenDistance:0.0} cases ({r.Direction}), santé {ResourceRow.FormatHealth(r.Health, r.MaxHealth)}";

    // Met à jour le tableau sur place quand l'ordre ne change pas (la sélection et le défilement restent).
    private void UpdateRows(IReadOnlyList<NearbyResource> shown, ulong? best, ulong? screenTarget)
    {
        static string Mark(ulong id, ulong? best, ulong? target) =>
            (id == best ? "★" : "") + (id == target ? "◎" : "");

        _updatingRows = true;
        try
        {
            var sameOrder = shown.Count == _resourceRows.Count && shown.Select(r => r.EntityId).SequenceEqual(_resourceRows.Select(r => r.EntityId));
            if (sameOrder)
            {
                for (var i = 0; i < shown.Count; i++)
                {
                    _resourceRows[i].Refresh(shown[i], Mark(shown[i].EntityId, best, screenTarget));
                }
                return;
            }
            _resourceRows.Clear();
            foreach (var r in shown)
            {
                var row = new ResourceRow(r);
                row.Refresh(r, Mark(r.EntityId, best, screenTarget));
                _resourceRows.Add(row);
            }
            SrvResourceList.SelectedItem = _resourceRows.FirstOrDefault(r => r.EntityId == _selectedResource);
        }
        finally
        {
            _updatingRows = false;
        }
    }

    // Journal : quand la cible à l'écran change (ou devient sûre), et quand le meilleur arbre change.
    private void LogMatches(NearbyResource? best, ScreenTargetMatch? match, TargetReading? target)
    {
        if (match is not null && target is not null
            && (match.Resource.EntityId != _loggedScreenTarget || match.Sure != _loggedScreenTargetSure))
        {
            _log.Info("Serveur", $"Cible à l'écran « {target.Name} » {ResourceRow.FormatHealth(target.Health, target.MaxHealth)} = {Describe(match.Resource)} " +
                                 $"({(match.Sure ? "sûr : seule ressource proche avec cette santé" : "probable : même nom")}).");
        }
        _loggedScreenTarget = match?.Resource.EntityId;
        _loggedScreenTargetSure = match?.Sure ?? false;
        if (best?.EntityId != _loggedBest && best is not null)
        {
            _log.Detail("Serveur", $"Meilleur arbre : {Describe(best)}.");
        }
        _loggedBest = best?.EntityId;
    }
}

// Fond de carte et pastilles des filtres (v0.4.18, D-070).
public partial class MainWindow
{
    // v0.4.22 : mémoire du fond de carte (sur le disque, à côté du .exe) et des ressources déjà vues.
    private TerrainStore? _terrainStore;
    private readonly ResourceMemory _resourceMemory = new();

    private void InitMapMemory()
    {
        var store = new TerrainStore(System.IO.Path.Combine(AppContext.BaseDirectory, TerrainStore.FolderName));
        _ = Task.Run(() =>
        {
            var read = store.LoadAll();
            _terrainStore = store;
            _log.Info("Carte", $"Mémoire de la carte : {read} morceaux de terrain gardés (dossier « {TerrainStore.FolderName} » à côté du .exe).");
        });
        SrvMap.ZoomChanged += () =>
        {
            MapZoomText.Text = $"vue : {SrvMap.ViewRadius - 1:0} cases";
            _terrainArea = null;
        };
    }

    private void MapZoomIn_Click(object sender, RoutedEventArgs e) => SrvMap.ZoomBy(-1);

    private void MapZoomOut_Click(object sender, RoutedEventArgs e) => SrvMap.ZoomBy(1);

    // Morceau de carte et vue du dernier fond dessiné : on ne recalcule que si l'un change.
    private (int X, int Z, double View)? _terrainArea;

    // Fond de carte : refait seulement quand les morceaux à montrer (ou les zones découvertes) changent.
    // Les morceaux reçus en direct vont dans la mémoire ; on dessine ceux de la mémoire autour du personnage.
    private void ShowTerrain(ServerReader reader, ServerPosition? position)
    {
        var live = reader.TerrainChunks();
        var explored = reader.Explored();
        IReadOnlyCollection<TerrainChunk> chunks = live;
        if (_terrainStore is { } store && position is { } p)
        {
            var changed = false;
            foreach (var chunk in live)
            {
                changed |= store.Put(chunk);
            }
            var area = (TerrainChunk.ChunkOf(p.ScreenEast, p.ScreenNorth).X, TerrainChunk.ChunkOf(p.ScreenEast, p.ScreenNorth).Z, SrvMap.ViewRadius);
            if (!changed && _terrainArea == area)
            {
                return;
            }
            _terrainArea = area;
            chunks = store.Around(p.ScreenEast, p.ScreenNorth, SrvMap.ViewRadius + 4);
        }
        if (SrvMap.SetTerrain(chunks, explored) && !_terrainShownLogged && chunks.Count > 0)
        {
            _terrainShownLogged = true;
            _log.Info("Carte", $"Fond de carte dessiné : {chunks.Count} morceaux de terrain, zones découvertes {(explored is null ? "pas lues" : $"lues ({explored.Count} morceaux découverts)")}.");
            if (Environment.GetCommandLineArgs().Any(a => a.Equals("--sans-lancer-jeu", StringComparison.OrdinalIgnoreCase)))
            {
                // Essai de Claude (jeu jamais lancé) : image de la fenêtre enregistrée par le bot lui-même,
                // car la capture de l'écran sort blanche quand la session Windows est verrouillée.
                Dispatcher.BeginInvoke(SaveTestCapture, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        }
    }

    private void SaveTestCapture()
    {
        try
        {
            if (Content is not FrameworkElement root || root.ActualWidth < 10)
            {
                return;
            }
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, root.ActualWidth, root.ActualHeight));
            }
            bitmap.Render(visual);
            var folder = System.IO.Path.Combine(AppContext.BaseDirectory, "captures-essai");
            System.IO.Directory.CreateDirectory(folder);
            var file = System.IO.Path.Combine(folder, $"carte-{DateTime.Now:HHmmss}.png");
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var stream = System.IO.File.Create(file);
            encoder.Save(stream);
            _log.Info("Carte", $"Essai : image de la fenêtre enregistrée ({file}).");
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _log.Info("Carte", $"Essai : image de la fenêtre impossible ({ex.Message}).");
        }
    }
}

// Couleur des pastilles de tier (v0.4.20) : une seule couleur pour tous, plus simple à lire que 10 couleurs.
public static class TierColors
{
    public static Brush Pill { get; } = Make(Color.FromRgb(138, 180, 248));

    private static Brush Make(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

// Pastille cliquable (filtre de la carte) : allumée = fond coloré ; éteinte = contour seul, texte pâle.
public sealed class MapPill : Border
{
    private readonly Brush _color;
    private readonly TextBlock _dot = new() { Text = "●", Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _label = new() { VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock _count = new() { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Opacity = 0.85 };
    private static readonly Brush OffBackground = Frozen(Color.FromArgb(40, 255, 255, 255));
    private static readonly Brush DarkText = Frozen(Color.FromRgb(16, 24, 36));
    private static readonly Brush OffText = Frozen(Color.FromRgb(120, 136, 156));

    // true = clic droit.
    public event Action<bool>? Clicked;

    public MapPill(Brush color, string tip)
    {
        _color = color;
        CornerRadius = new CornerRadius(12);
        BorderThickness = new Thickness(1.5);
        Padding = new Thickness(10, 3, 10, 3);
        Margin = new Thickness(0, 3, 6, 3);
        Cursor = Cursors.Hand;
        ToolTip = tip;
        Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { _dot, _label, _count } };
        MouseLeftButtonUp += (_, e) => { Clicked?.Invoke(false); e.Handled = true; };
        MouseRightButtonUp += (_, e) => { Clicked?.Invoke(true); e.Handled = true; };
    }

    public bool On { get; private set; }

    public void Set(string label, int count, bool on)
    {
        On = on;
        _label.Text = label;
        _count.Text = count.ToString(System.Globalization.CultureInfo.CurrentCulture);
        BorderBrush = _color;
        Background = on ? _color : OffBackground;
        _label.Foreground = on ? DarkText : OffText;
        _count.Foreground = on ? DarkText : OffText;
        _dot.Foreground = on ? DarkText : _color;
        Opacity = count == 0 && on ? 0.55 : 1;
    }

    private static Brush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
