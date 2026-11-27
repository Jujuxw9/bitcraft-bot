using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using BitcraftBot.Input.Actions;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// v0.4.32 (T-14) : « Aller à cette ressource » et « Récolter jusqu'à épuisement » (D-089).
// Menu sur une ressource de la carte, sur une ligne du tableau, et bouton « Y aller » de la ressource recherchée
// la plus proche. Le bot va sur une case voisine (ResourceApproach) avec « Se déplacer ici ». Pour « Récolter »,
// il s'arrête à l'arrivée : Julien donne le premier coup (clic sur la ressource), la reprise des récoltes relance
// ensuite à chaque baisse d'endurance, et HarvestWatch dit quand la ressource est épuisée.
public partial class MainWindow
{
    private const string HarvestLog = "Récolte guidée";
    private HarvestWatch? _harvestWatch;
    private double? _harvestLastHealth;
    private bool _harvestFleeing;
    private NearbyResource? _nearestSought;

    // Entrées de menu pour une ressource (rangées au début du menu donné).
    private void AddResourceItems(ContextMenu menu, NearbyResource r)
    {
        var label = $"{r.Name}{(r.Tier is { } t ? $" (T{t})" : "")}";
        var go = new MenuItem { Header = $"Aller à cette ressource : {label}" };
        var harvest = new MenuItem { Header = "Récolter jusqu'à épuisement" };
        var why = WhyNoResourceTrip(r, out var cell);
        if (why is not null)
        {
            go.IsEnabled = false;
            harvest.IsEnabled = false;
            menu.Items.Insert(0, new MenuItem { Header = $"Refusé : {why}", IsEnabled = false });
        }
        else
        {
            go.Click += (_, _) => _ = GoToResourceAsync(r, harvest: false);
            harvest.Click += (_, _) => _ = GoToResourceAsync(r, harvest: true);
        }
        menu.Items.Insert(0, harvest);
        menu.Items.Insert(0, go);
        if (menu.Items.Count > 3)
        {
            menu.Items.Insert(why is null ? 2 : 3, new Separator());
        }
    }

    // null = trajet possible (cell = case voisine choisie).
    private string? WhyNoResourceTrip(NearbyResource r, out (double East, double North)? cell)
    {
        cell = null;
        if (!_settings.TravelEnabled)
        {
            return "déplacements coupés (OPTIONS)";
        }
        if (_travelBusy)
        {
            return "un déplacement est déjà en cours";
        }
        if (_lastSnapshot?.Position is not { } p)
        {
            return "position du personnage inconnue";
        }
        var choice = ResourceApproach.Choose(r.Position.ScreenEast, r.Position.ScreenNorth, p.ScreenEast, p.ScreenNorth, CheckTravel);
        if (choice.Cell is null && choice.Detail != "déjà à côté de la ressource")
        {
            return choice.Detail;
        }
        cell = choice.Cell;
        return null;
    }

    private ContextMenu BuildResourceMenu(NearbyResource r)
    {
        var menu = new ContextMenu();
        AddResourceItems(menu, r);
        return menu;
    }

    private async Task GoToResourceAsync(NearbyResource r, bool harvest)
    {
        if (WhyNoResourceTrip(r, out var cell) is { } why)
        {
            _log.Info(HarvestLog, $"« {r.Name} » ({r.Position.ScreenText}) : refusé, {why}.");
            TravelStatusText.Text = $"Refusé : {why}.";
            return;
        }
        _harvestWatch = null;
        _log.Info(HarvestLog, $"{(harvest ? "Récolter jusqu'à épuisement" : "Aller à")} « {r.Name} » ({r.Position.ScreenText})"
                              + (cell is { } c ? $" : case voisine N {c.North:0}, E {c.East:0}." : " : déjà à côté, pas de trajet."));
        var arrived = cell is not { } target || await TravelToAsync(target.East, target.North, flee: false);
        if (!arrived)
        {
            _log.Info(HarvestLog, $"Pas arrivé près de « {r.Name} » : rien d'autre n'est fait.");
            return;
        }
        if (!harvest)
        {
            TravelStatusText.Text = $"Arrivé près de « {r.Name} ».";
            return;
        }
        _harvestWatch = new HarvestWatch(r.EntityId, r.Name, DateTimeOffset.UtcNow);
        _harvestLastHealth = _lastSnapshot?.Health;
        var resumeNote = _settings.AutoResumeEnabled
            ? "le bot continuera jusqu'à épuisement (reprise des récoltes)."
            : "attention, la reprise des récoltes est coupée (OPTIONS) : le bot ne relancera pas.";
        TravelStatusText.Text = $"Arrivé : cliquez une fois sur « {r.Name} » ; {resumeNote}";
        _log.Info(HarvestLog, $"Arrivé près de « {r.Name} ». Cliquez une fois sur la ressource : {resumeNote}");
    }

    // Appelé toutes les 500 ms (après la reprise des récoltes).
    private void RunHarvestWatch(PlayerSnapshot s)
    {
        if (_harvestWatch is not { Over: false } w)
        {
            return;
        }
        var attacked = s.Health is { } h && _harvestLastHealth is { } before && h < before - 0.01 && h > 0;
        if (s.Health is not null)
        {
            _harvestLastHealth = s.Health;
        }
        var line = w.Update(new HarvestWatchInput
        {
            Now = DateTimeOffset.UtcNow,
            Position = s.Position,
            Resources = s.Resources,
            LastExtractTarget = s.LastExtract?.Target,
            ActionTarget = s.Action?.Target,
            UnderAttack = attacked,
        });
        if (line is null)
        {
            return;
        }
        if (w.State == HarvestWatchState.Abandon && attacked) _log.Alerte(HarvestLog, line);
        else _log.Info(HarvestLog, line);
        TravelStatusText.Text = line;
        if (attacked && w.LastKnown is { } at && s.Position is { } p && !_harvestFleeing)
        {
            _ = FleeFromHarvestAsync(p, at);
        }
    }

    // §3 de la nuit (D-072) : vie qui baisse = Sprint (4) puis fuite 10 cases à l'opposé de la ressource, puis arrêt.
    private async Task FleeFromHarvestAsync(ServerPosition p, ServerPosition resource)
    {
        _harvestFleeing = true;
        try
        {
            await ActAsync(() => _actions.PressAsync("Récolte guidée : Sprint (fuite)", GameKeys.Sprint));
            var (fe, fn) = TravelGuard.FleePoint(p.ScreenEast, p.ScreenNorth, resource.ScreenEast, resource.ScreenNorth);
            await TravelToAsync(fe, fn, flee: true);
            _log.Info(HarvestLog, "Après la fuite : arrêt, rien n'est repris tout seul.");
        }
        finally
        {
            _harvestFleeing = false;
        }
    }

    // Clic droit sur une ligne du tableau : la ligne est choisie, puis le menu de la ressource.
    private void SrvResourceList_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var item = FindParent<ListViewItem>(e.OriginalSource as DependencyObject);
        if (item?.DataContext is not ResourceRow row)
        {
            return;
        }
        SrvResourceList.SelectedItem = row;
        if (_lastSnapshot?.Resources.FirstOrDefault(r => r.EntityId == row.EntityId) is not { } res)
        {
            return;
        }
        var menu = BuildResourceMenu(res);
        menu.PlacementTarget = item;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
        e.Handled = true;
    }

    // Bouton « Y aller » de la ressource recherchée la plus proche.
    private void NearestGoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_nearestSought is not { } r)
        {
            TravelStatusText.Text = "Aucune ressource recherchée dans la zone.";
            return;
        }
        var menu = BuildResourceMenu(r);
        menu.PlacementTarget = NearestGoButton;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private static T? FindParent<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d is not null and not T)
        {
            d = d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return d as T;
    }
}
