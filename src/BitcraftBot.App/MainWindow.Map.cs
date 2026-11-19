using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Carte (H2) » : carte, filtres par famille et tableau des ressources proches.
public partial class MainWindow
{
    private const int MaxRowsShown = 80;

    private readonly ObservableCollection<ResourceRow> _resourceRows = [];
    private readonly Dictionary<string, CheckBox> _familyChecks = [];
    private readonly HashSet<string> _shownFamilies = [ResourceFamilies.Trees];
    private readonly ScreenTargetMatcher _targetMatcher = new();
    private TargetReading? _lastHudTarget;
    private DateTime _lastHudTargetAt;
    private ulong? _selectedResource;
    private ulong? _loggedScreenTarget;
    private bool _loggedScreenTargetSure;
    private ulong? _loggedBest;
    private bool _updatingRows;

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
        // Une case par famille, dans l'ordre, avec sa couleur. Le nombre lu s'ajoute à la connexion.
        foreach (var family in ResourceFamilies.All)
        {
            var check = new CheckBox
            {
                Content = family,
                IsChecked = _shownFamilies.Contains(family),
                Foreground = FamilyColors.Of(family),
                Margin = new Thickness(0, 2, 16, 2),
                Tag = family,
            };
            check.Click += (_, _) => SetFamily(family, check.IsChecked == true);
            _familyChecks[family] = check;
            FamilyChecks.Children.Add(check);
        }
    }

    private void SetFamily(string family, bool shown)
    {
        if (shown) _shownFamilies.Add(family);
        else _shownFamilies.Remove(family);
        _familyChecks[family].IsChecked = shown;
        _resourceRows.Clear();
    }

    private void FamilyAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var family in ResourceFamilies.All) SetFamily(family, true);
    }

    private void FamilyTrees_Click(object sender, RoutedEventArgs e)
    {
        foreach (var family in ResourceFamilies.All) SetFamily(family, family == ResourceFamilies.Trees);
    }

    private void SrvResourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingRows)
        {
            _selectedResource = (SrvResourceList.SelectedItem as ResourceRow)?.EntityId;
        }
    }

    private void ShowResources(ServerReader reader, PlayerSnapshot s)
    {
        MapPlayerText.Text = s.Position is { } me
            ? $"Vous : {me.ScreenText} (±1){(s.Walking == true ? ", en marche" : "")} — vie {s.Health:0}, endurance {s.Stamina:0}, satiété {s.Satiation:0} — {s.RegionName ?? "région ?"}"
            : "En attente de la position du personnage…";

        // Nombre de ressources lues par famille, sur chaque case.
        var counts = s.Resources.GroupBy(r => r.Family).ToDictionary(g => g.Key, g => g.Count());
        foreach (var (family, check) in _familyChecks)
        {
            check.Content = $"{family} ({counts.GetValueOrDefault(family)})";
        }

        bool Shown(NearbyResource r) => _shownFamilies.Contains(r.Family);
        var shown = s.Resources.Where(Shown).ToList();
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
            ? $"centre {new ServerPosition(a.CenterX, a.CenterZ).ScreenText}, rayon ≈ {ServerReader.ResourceRadius / 3} cases, relue à {at:HH:mm:ss} (relue dès que vous vous éloignez d'environ 7 cases)"
              + (reader.HealthRefused ? " — santé refusée par le serveur" : "")
            : "—";

        UpdateRows(shown.Take(MaxRowsShown).ToList(), best?.EntityId, match?.Resource.EntityId);
        SrvMap.Update(s.Position, s.Walking == true ? s.Destination : null, s.Resources, Shown, best?.EntityId, match?.Resource.EntityId, _selectedResource);
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
