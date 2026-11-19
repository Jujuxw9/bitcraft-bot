using System.Windows.Controls;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Personnage », carte « Sac et banques » : ce que le serveur dit de vos inventaires (lecture seule).
public partial class MainWindow
{
    private static readonly TimeSpan InventoryInterval = TimeSpan.FromSeconds(2);

    private DateTime _inventoryShownAt = DateTime.MinValue;
    private string _inventorySignature = "";
    private List<(string Where, ItemStackInfo Stack)> _inventoryStacks = [];
    private bool _updatingPicker;

    private sealed record InventoryRow(string Name, string Quantity, string Kind, string Tier, string Where);

    // Appelé par la minuterie de l'onglet (toutes les 500 ms) ; la liste est refaite au plus toutes les 2 s, et seulement si elle a changé.
    private void ShowInventories(ServerReader reader, PlayerSnapshot s)
    {
        if (DateTime.Now - _inventoryShownAt < InventoryInterval)
        {
            return;
        }
        _inventoryShownAt = DateTime.Now;
        if (reader.InventoryRefused)
        {
            InventorySummary.Text = "Le serveur a refusé la lecture du sac et des banques (voir le journal).";
            return;
        }
        var inventories = reader.Inventories();
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

        // Choix « où » : tout, ou un inventaire précis (on garde le choix courant).
        var places = stacks.Select(x => x.Where).Distinct().ToList();
        var current = InventoryPicker.SelectedItem as string;
        _updatingPicker = true;
        InventoryPicker.ItemsSource = new[] { "Tout" }.Concat(places).ToList();
        InventoryPicker.SelectedItem = current is not null && (current == "Tout" || places.Contains(current)) ? current : "Tout";
        _updatingPicker = false;

        var bankCount = inventories.Count(i => i.Owner != s.EntityId);
        InventorySummary.Text = inventories.Count == 0
            ? "Aucun inventaire reçu pour l'instant."
            : $"{inventories.Count} inventaires lus ({bankCount} de banque), {stacks.Count} piles d'objets. Mis à jour à {DateTime.Now:HH:mm:ss}.";
        FillInventoryList();
    }

    private void InventoryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingPicker)
        {
            FillInventoryList();
        }
    }

    private void InventorySearchBox_TextChanged(object sender, TextChangedEventArgs e) => FillInventoryList();

    // Regroupe les piles du même objet au même endroit, filtre, trie par endroit puis par nom.
    private void FillInventoryList()
    {
        var place = InventoryPicker.SelectedItem as string ?? "Tout";
        var search = InventorySearchBox.Text.Trim();
        var store = _gameData.Current;
        var rows = _inventoryStacks
            .Where(x => place == "Tout" || x.Where == place)
            .GroupBy(x => (x.Where, x.Stack.ItemId, x.Stack.IsCargo))
            .Select(g =>
            {
                var (where, id, cargo) = g.Key;
                var key = (cargo ? "C" : "I") + id;
                var name = (cargo ? store?.Cargos.GetValueOrDefault(id) : store?.Items.GetValueOrDefault(id))
                           ?? (_recipes?.Entries.TryGetValue(key, out var e1) == true ? e1.Name : $"objet n° {id}");
                var tier = _recipes?.Entries.TryGetValue(key, out var e2) == true ? e2.Tier.ToString() : "?";
                return new InventoryRow(name, g.Sum(x => x.Stack.Quantity).ToString("N0"), cargo ? "Cargaison" : "Objet", tier, where);
            })
            .Where(r => search.Length == 0 || r.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Where)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        InventoryList.ItemsSource = rows;
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
            // Pas de distance dans le nom : il doit rester stable pendant que vous marchez (choix de la liste).
            return $"Banque à {at.ScreenText}";
        }
        return $"Coffre (bâtiment n° {inv.Owner})";
    }
}
