using System.Collections.ObjectModel;
using System.Windows.Controls;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Serveur » : carte des arbres et tableau des ressources proches (H2).
public partial class MainWindow
{
    private const int MaxRowsShown = 60;

    private readonly ObservableCollection<ResourceRow> _resourceRows = [];
    private TargetReading? _lastHudTarget;
    private DateTime _lastHudTargetAt;
    private ulong? _selectedResource;
    private ulong? _loggedScreenTarget;
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
        TreesOnlyCheck.Click += (_, _) => _resourceRows.Clear();
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
        var treesOnly = TreesOnlyCheck.IsChecked == true;
        var shown = s.Resources.Where(r => !treesOnly || r.IsTree).ToList();
        var trees = s.Resources.Count(r => r.IsTree);
        SrvResourceCount.Text = $"{trees} arbres ({s.Resources.Count(ResourceNames.IsHarvestTarget)} mûrs) parmi {s.Resources.Count} ressources lues.";

        var best = s.Resources.FirstOrDefault(ResourceNames.IsHarvestTarget);
        var screenTarget = FindScreenTarget(s.Resources, out var sure);

        SrvBestTree.Text = best is null ? "aucun arbre mûr dans la zone lue" : Describe(best);
        SrvScreenTarget.Text = _lastHudTarget is not { Name: { } targetName } target
            ? "aucune cible lue à l'écran"
            : screenTarget is null
                ? $"« {targetName} » {ResourceRow.FormatHealth(target.Health, target.MaxHealth)} : introuvable dans la zone lue"
                : $"{Describe(screenTarget)}{(sure ? " (même santé : c'est bien lui)" : " (même nom, santé différente : probable)")}";
        SrvArea.Text = reader.ResourceArea is { } a && reader.ResourceAreaUpdated is { } at
            ? $"centre {new ServerPosition(a.CenterX, a.CenterZ).ScreenText}, rayon ≈ {ServerReader.ResourceRadius / 3} cases, relue à {at:HH:mm:ss} (relue dès que vous vous éloignez de {ServerReader.ResourceRadius / 6} cases)"
              + (reader.HealthRefused ? " — santé des arbres refusée par le serveur" : "")
            : "—";

        UpdateRows(shown.Take(MaxRowsShown).ToList(), best?.EntityId, screenTarget?.EntityId);
        SrvMap.Update(s.Position, s.Walking == true ? s.Destination : null, s.Resources, treesOnly, best?.EntityId, screenTarget?.EntityId, _selectedResource);
        LogMatches(best, screenTarget, sure);
    }

    // Arbre ciblé dans le jeu (lu à l'écran) : même nom et même santé ; sinon, même nom, le plus proche.
    private NearbyResource? FindScreenTarget(IReadOnlyList<NearbyResource> resources, out bool sure)
    {
        sure = false;
        if (DateTime.Now - _lastHudTargetAt > TimeSpan.FromSeconds(5))
        {
            _lastHudTarget = null; // lecture de l'écran arrêtée : plus de cible
        }
        if (_lastHudTarget is not { Name: { } name } target)
        {
            return null;
        }
        var sameName = resources.Where(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        var exact = sameName.FirstOrDefault(r => r.Health is not null && r.Health == target.Health);
        if (exact is not null)
        {
            sure = true;
            return exact;
        }
        return sameName.FirstOrDefault(r => r.ScreenDistance <= 4);
    }

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

    // Journal : quand la cible à l'écran ou le meilleur arbre change (pour vérifier H2 après coup).
    private void LogMatches(NearbyResource? best, NearbyResource? screenTarget, bool sure)
    {
        if (screenTarget?.EntityId != _loggedScreenTarget && screenTarget is not null && _lastHudTarget is { } t)
        {
            _log.Info("Serveur", $"Cible à l'écran « {t.Name} » {ResourceRow.FormatHealth(t.Health, t.MaxHealth)} = arbre du serveur {Describe(screenTarget)} " +
                                 $"({(sure ? "même santé : sûr" : "même nom seulement : probable")}).");
        }
        _loggedScreenTarget = screenTarget?.EntityId;
        if (best?.EntityId != _loggedBest && best is not null)
        {
            _log.Detail("Serveur", $"Meilleur arbre : {Describe(best)}.");
        }
        _loggedBest = best?.EntityId;
    }
}
