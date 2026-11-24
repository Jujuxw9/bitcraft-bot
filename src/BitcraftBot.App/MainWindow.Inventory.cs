using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Personnage » : ce que le serveur dit de vos inventaires (lecture seule).
// v0.4.12 (demande de Julien) : trois listes côte à côte (sac, ceinture d'outils, banques) au lieu d'un choix,
// et colonnes triables d'un clic sur l'en-tête (alphabétique, quantité, type, palier).
public partial class MainWindow
{
    private static readonly TimeSpan InventoryInterval = TimeSpan.FromSeconds(2);

    private DateTime _inventoryShownAt = DateTime.MinValue;
    private string _inventorySignature = "";
    private List<(string Where, ItemStackInfo Stack)> _inventoryStacks = [];

    // Quantity et TierSort servent au tri (nombres) ; TierText est ce qu'on voit.
    private sealed record InventoryRow(string Name, int Quantity, string Kind, int TierSort, string TierText, string Where);

    // Tri de chaque liste : colonne (propriété de InventoryRow) et sens. Par défaut : nom, A à Z.
    private readonly Dictionary<ListView, (string Property, ListSortDirection Direction)> _inventorySort = [];

    private static readonly Dictionary<string, string> InventorySortColumns = new()
    {
        ["Objet"] = nameof(InventoryRow.Name),
        ["Quantité"] = nameof(InventoryRow.Quantity),
        ["Type"] = nameof(InventoryRow.Kind),
        ["Palier"] = nameof(InventoryRow.TierSort),
        ["Où"] = nameof(InventoryRow.Where),
    };

    // Appelé par la minuterie de l'onglet (toutes les 500 ms) ; les listes sont refaites au plus toutes les 2 s, et seulement si elles ont changé.
    private void ShowInventories(ServerReader reader, PlayerSnapshot s, IReadOnlyList<InventoryInfo> inventories)
    {
        if (DateTime.Now - _inventoryShownAt < InventoryInterval)
        {
            return;
        }
        _inventoryShownAt = DateTime.Now;
        if (reader.InventoryRefused)
        {
            InventorySummary.Text = "Lecture refusée (journal).";
            return;
        }
        var banks = reader.BankPositions();
        var stacks = new List<(string Where, ItemStackInfo Stack)>();
        foreach (var inv in inventories)
        {
            var where = Describe(inv, s, banks);
            stacks.AddRange(inv.Stacks.Select(st => (where, st)));
        }
        var signature = string.Join("|", stacks.Select(x => $"{x.Where}:{x.Stack.ItemId}:{x.Stack.IsCargo}:{x.Stack.Quantity}"));
        if (signature == _inventorySignature)
        {
            return;
        }
        _inventorySignature = signature;
        _inventoryStacks = stacks;

        var bankCount = inventories.Count(i => i.Owner != s.EntityId);
        InventorySummary.Text = inventories.Count == 0
            ? "Aucun inventaire reçu pour l'instant."
            : $"{inventories.Count} inventaires lus ({bankCount} de banque), {stacks.Count} piles d'objets. Mis à jour à {DateTime.Now:HH:mm:ss}.";
        FillInventoryList();
    }

    private void InventorySearchBox_TextChanged(object sender, TextChangedEventArgs e) => FillInventoryList();

    // Les trois listes : sac, ceinture d'outils, et tout le reste (banques, coffres).
    private void FillInventoryList()
    {
        var search = InventorySearchBox.Text.Trim();
        var rows = _inventoryStacks
            .GroupBy(x => (x.Where, x.Stack.ItemId, x.Stack.IsCargo))
            .Select(g =>
            {
                var (where, id, cargo) = g.Key;
                var key = (cargo ? "C" : "I") + id;
                var tier = _recipes?.Entries.TryGetValue(key, out var e2) == true ? e2.Tier : -1;
                return new InventoryRow(ItemName(id, cargo), g.Sum(x => x.Stack.Quantity), cargo ? "Cargaison" : "Objet", tier, tier >= 0 ? tier.ToString() : "?", where);
            })
            .Where(r => search.Length == 0 || r.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Show(BagList, BagTitle, "SAC", rows.Where(r => r.Where == "Sac"));
        Show(BeltList, BeltTitle, "CEINTURE D'OUTILS", rows.Where(r => r.Where == "Ceinture d'outils"));
        Show(BankList, BankTitle, "BANQUES", rows.Where(r => r.Where != "Sac" && r.Where != "Ceinture d'outils"));
    }

    private void Show(ListView list, TextBlock title, string name, IEnumerable<InventoryRow> rows)
    {
        var items = rows.ToList();
        title.Text = $"{name}  ·  {items.Count} objet{(items.Count > 1 ? "s" : "")}";
        list.ItemsSource = items;
        ApplyInventorySort(list);
    }

    // Clic sur un en-tête de colonne : tri par cette colonne ; deuxième clic : sens inverse.
    private void InventoryHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ListView list || e.OriginalSource is not GridViewColumnHeader { Column: { } column, Role: not GridViewColumnHeaderRole.Padding })
        {
            return;
        }
        var label = column.Header as string ?? "";
        label = label.TrimEnd(' ', '▲', '▼');
        if (!InventorySortColumns.TryGetValue(label, out var property))
        {
            return;
        }
        var direction = _inventorySort.TryGetValue(list, out var current) && current.Property == property && current.Direction == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        _inventorySort[list] = (property, direction);
        ApplyInventorySort(list);
    }

    private void ApplyInventorySort(ListView list)
    {
        var (property, direction) = _inventorySort.TryGetValue(list, out var s) ? s : (nameof(InventoryRow.Name), ListSortDirection.Ascending);
        list.Items.SortDescriptions.Clear();
        list.Items.SortDescriptions.Add(new SortDescription(property, direction));
        if (property != nameof(InventoryRow.Name))
        {
            list.Items.SortDescriptions.Add(new SortDescription(nameof(InventoryRow.Name), ListSortDirection.Ascending));
        }
        // Flèche sur l'en-tête de la colonne triée.
        if (list.View is GridView view)
        {
            foreach (var column in view.Columns)
            {
                var label = (column.Header as string ?? "").TrimEnd(' ', '▲', '▼');
                column.Header = InventorySortColumns.GetValueOrDefault(label) == property
                    ? label + (direction == ListSortDirection.Ascending ? " ▲" : " ▼")
                    : label;
            }
        }
    }

    // Nom lisible d'un inventaire : sac, ceinture, banque régionale, ou banque d'un bâtiment (avec sa position).
    private static string Describe(InventoryInfo inv, PlayerSnapshot s, IReadOnlyDictionary<ulong, ServerPosition> banks)
    {
        if (inv.Owner == s.EntityId)
        {
            return inv.Index switch
            {
                0 => "Sac",
                1 => "Ceinture d'outils",
                _ => $"Inventaire personnel n° {inv.Index}",
            };
        }
        if (inv.IsRegionalBank)
        {
            return "Banque régionale";
        }
        if (banks.TryGetValue(inv.Owner, out var at))
        {
            // Pas de distance dans le nom : il doit rester stable pendant que vous marchez.
            return $"Banque à {at.ScreenText}";
        }
        return $"Coffre (bâtiment n° {inv.Owner})";
    }

    // Nom d'un objet ou d'une cargaison d'après les données du jeu.
    private string ItemName(int id, bool cargo)
    {
        var store = _gameData.Current;
        return (cargo ? store?.Cargos.GetValueOrDefault(id) : store?.Items.GetValueOrDefault(id))
               ?? (_recipes?.Entries.TryGetValue((cargo ? "C" : "I") + id, out var e) == true ? e.Name : $"{(cargo ? "cargaison" : "objet")} n° {id}");
    }
}
