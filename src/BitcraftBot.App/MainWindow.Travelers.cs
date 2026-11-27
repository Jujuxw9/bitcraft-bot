using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitcraftBot.GameData;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Voyageurs » (v0.4.11, refait en v0.4.12 sur demande de Julien) : les tâches de voyageur comparées au stock
// (sac, ceinture, banques), en trois listes : à rendre maintenant, les 10 plus avancées, les plus faciles à farmer.
// Lecture seule du serveur.
public partial class MainWindow
{
    private static readonly TimeSpan TravelerInterval = TimeSpan.FromSeconds(2);
    private const int TravelerTopCount = 10;

    private DateTime _travelerShownAt = DateTime.MinValue;
    private string _travelerSignature = "";

    private readonly TravelerPortraits _portraits = new();

    private Brush TravBrush(string key) => (Brush)FindResource(key);

    // Bot pas relié au serveur : rien à comparer.
    private void ShowTravelersOffline()
    {
        _travelerSignature = "";
        TravelerMessage("Serveur pas encore lu.");
    }

    private void TravelerMessage(string text)
    {
        TravelerNowCount.Text = TravelerFetchCount.Text = TravelerPartialCount.Text = TravelerImpossibleCount.Text = "—";
        TravelerSummary.Text = text;
        TravelerPanel.Children.Clear();
    }

    private void ShowTravelers(ServerReader reader, PlayerSnapshot s, IReadOnlyList<InventoryInfo> inventories)
    {
        if (DateTime.Now - _travelerShownAt < TravelerInterval)
        {
            return;
        }
        _travelerShownAt = DateTime.Now;
        if (reader.TasksRefused)
        {
            TravelerMessage("Le serveur a refusé la lecture des tâches de voyageur (journal).");
            return;
        }
        if (reader.InventoryRefused || s.EntityId is null)
        {
            TravelerMessage(reader.InventoryRefused ? "Stock non lu : le serveur a refusé la lecture du sac et des banques (journal)." : "Personnage pas encore trouvé.");
            return;
        }
        var store = _gameData.Current;
        if (store is null || store.TravelerTasks.Count == 0)
        {
            TravelerMessage("Données du jeu sur les tâches pas encore téléchargées (redémarrer le bot avec Internet).");
            return;
        }

        var banks = reader.BankPositions();
        var stock = inventories
            .SelectMany(inv =>
            {
                var where = Describe(inv, s, banks);
                var inBag = inv.Owner == s.EntityId && inv.Index == 0;
                return inv.Stacks.Select(st => new StockStack(where, inBag, st));
            })
            .ToList();
        var tasks = reader.TravelerTasks();
        var signature = string.Join("|", tasks.Select(t => $"{t.EntityId}:{t.TaskId}:{t.Completed}"))
                        + "#" + string.Join("|", stock.Select(x => $"{x.Where}:{x.Stack.ItemId}:{x.Stack.IsCargo}:{x.Stack.Quantity}"))
                        + "#" + (_recipes is null ? 0 : 1);
        if (signature == _travelerSignature)
        {
            return;
        }
        _travelerSignature = signature;

        if (tasks.Count == 0)
        {
            TravelerMessage($"Aucune tâche de voyageur reçue du serveur. Mis à jour à {DateTime.Now:HH:mm:ss}.");
            return;
        }
        var checks = TravelerTaskPlanner.Check(tasks, store.TravelerTasks, stock);
        var totals = stock.GroupBy(x => (x.Stack.IsCargo ? "C" : "I") + x.Stack.ItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Stack.Quantity));
        RenderTravelers(checks, store, totals);
    }

    private void RenderTravelers(IReadOnlyList<TaskCheck> checks, GameDataStore store, IReadOnlyDictionary<string, int> totals)
    {
        var active = checks.Where(c => c.Verdict is not (TaskVerdict.DejaRendue or TaskVerdict.Inconnue)).ToList();
        int Count(TaskVerdict v) => active.Count(c => c.Verdict == v);
        TravelerNowCount.Text = Count(TaskVerdict.FaisableMaintenant).ToString();
        TravelerFetchCount.Text = Count(TaskVerdict.FaisableApresBanque).ToString();
        TravelerPartialCount.Text = Count(TaskVerdict.Partiel).ToString();
        TravelerImpossibleCount.Text = Count(TaskVerdict.Impossible).ToString();
        TravelerSummary.Text = $"{active.Count} tâches · {DateTime.Now:HH:mm}";

        var effort = new Dictionary<TaskCheck, Effort>();
        foreach (var c in active.Where(c => c.Items.Any(i => i.Missing > 0)))
        {
            effort[c] = _recipes is null ? Effort.None : TravelerTaskPlanner.EffortToFinish(c, _recipes);
        }

        var panel = TravelerPanel;
        panel.Children.Clear();

        var now = active.Where(c => c.Verdict is TaskVerdict.FaisableMaintenant or TaskVerdict.FaisableApresBanque)
            .OrderBy(c => c.Verdict).ThenBy(c => c.Task.TravelerId).ToList();
        AddTravelerSection(panel, "À RENDRE SANS FARMER", now.Count);
        foreach (var c in now)
        {
            panel.Children.Add(TaskCard(c, store, null, totals));
        }

        var top = active.Where(c => c.Progress is > 0 and < 1).OrderByDescending(c => c.Progress)
            .ThenBy(c => effort.GetValueOrDefault(c, Effort.None).SortKey).Take(TravelerTopCount).ToList();
        AddTravelerSection(panel, $"LES {TravelerTopCount} PLUS AVANCÉES", top.Count);
        foreach (var c in top)
        {
            panel.Children.Add(TaskCard(c, store, effort.GetValueOrDefault(c), totals));
        }

        var easy = effort.Keys.OrderBy(c => effort[c].SortKey).ThenByDescending(c => c.Progress).ToList();
        AddTravelerSection(panel, "LES PLUS FACILES À FARMER", easy.Count);
        foreach (var c in easy)
        {
            panel.Children.Add(TaskCard(c, store, effort[c], totals));
        }
    }

    private void AddTravelerSection(Panel panel, string title, int count)
    {
        var head = new StackPanel { Margin = new Thickness(8, 14, 8, 2) };
        var line = new StackPanel { Orientation = Orientation.Horizontal };
        line.Children.Add(new TextBlock { Text = title, Style = (Style)FindResource("CardTitle"), Margin = new Thickness(0, 0, 10, 0) });
        line.Children.Add(new TextBlock { Text = $"{count} tâche{(count > 1 ? "s" : "")}", Foreground = TravBrush("Muted"), VerticalAlignment = VerticalAlignment.Center });
        head.Children.Add(line);
        panel.Children.Add(head);
        if (count == 0)
        {
            panel.Children.Add(new TextBlock { Text = "Aucune.", Foreground = TravBrush("Muted"), Margin = new Thickness(10, 4, 0, 0) });
        }
    }

    // Une tâche (v0.4.15 : moins de texte) : portrait, nom du voyageur (la phrase de la tâche est au survol),
    // objets demandés avec leur image, matières premières à récolter, avancement.
    private Border TaskCard(TaskCheck c, GameDataStore store, Effort? effort, IReadOnlyDictionary<string, int> totals)
    {
        var traveler = store.Travelers.GetValueOrDefault(c.Task.TravelerId) ?? $"Voyageur n° {c.Task.TravelerId}";
        var card = new Border { Style = (Style)FindResource("Card"), Padding = new Thickness(12, 8, 12, 8) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        card.Child = grid;

        grid.Children.Add(Portrait(traveler));

        var middle = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        middle.Children.Add(new TextBlock
        {
            Text = traveler,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = TravBrush("Gold"),
            ToolTip = c.Desc?.Description is { Length: > 0 } d ? d : $"Tâche n° {c.Task.TaskId}",
        });
        var items = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        foreach (var i in c.Items)
        {
            items.Children.Add(ItemChip(i));
        }
        middle.Children.Add(items);
        // Matières premières (D-066) : le bas de l'arbre des recettes pour ce qui manque, stock déduit à chaque niveau.
        var missing = c.Items.Where(i => i.Missing > 0).Select(i => ((i.Item.IsCargo ? "C" : "I") + i.Item.ItemId, i.Missing)).ToList();
        if (missing.Count > 0 && _recipes is { } book)
        {
            var raw = RawMaterials.For(book, missing, totals);
            if (raw.Count > 0)
            {
                var line = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
                line.Children.Add(new TextBlock { Text = "À récolter", Foreground = TravBrush("Muted"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 4) });
                foreach (var r in raw)
                {
                    line.Children.Add(RawChip(r, book));
                }
                middle.Children.Add(line);
            }
        }
        Grid.SetColumn(middle, 1);
        grid.Children.Add(middle);

        var right = new StackPanel { Width = 130, VerticalAlignment = VerticalAlignment.Center };
        var verdictBrush = c.Verdict switch
        {
            TaskVerdict.FaisableMaintenant => TravBrush("Good"),
            TaskVerdict.FaisableApresBanque => TravBrush("Gold"),
            TaskVerdict.Partiel => TravBrush("Warn"),
            _ => TravBrush("Bad"),
        };
        right.Children.Add(new TextBlock { Text = $"{c.Progress:P0}", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = verdictBrush, ToolTip = TravelerTaskPlanner.Text(c.Verdict) });
        right.Children.Add(new ProgressBar { Value = c.Progress * 100, Maximum = 100, Height = 8, Foreground = verdictBrush, Margin = new Thickness(0, 2, 0, 4) });
        if (effort is { } e && e != Effort.None)
        {
            right.Children.Add(new TextBlock { Text = e.Text, Foreground = TravBrush("Muted"), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        }
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        return card;
    }

    // Une matière première : image, quantité, nom ; au survol, où la récolter.
    private Border RawChip(RawNeed r, RecipeBook book)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(IconImage(r.Key, 22));
        var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
        text.Inlines.Add(new System.Windows.Documents.Run($"{r.Quantity:N0}  ") { FontWeight = FontWeights.Bold, Foreground = TravBrush("Warn") });
        text.Inlines.Add(new System.Windows.Documents.Run(book.NameOf(r.Key)));
        row.Children.Add(text);
        return new Border
        {
            Background = TravBrush("Track"),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(3, 1, 8, 1),
            Margin = new Thickness(0, 0, 6, 4),
            Child = row,
            ToolTip = r.Harvest is { } h ? $"Récolte sur « {h.Place} »" : "Source inconnue (butin, commerce…)",
        };
    }

    // Portrait du voyageur : wiki du jeu (TravelerPortraits) ou image du jeu (Svim) ; sinon un rond doré avec l'initiale.
    private UIElement Portrait(string traveler)
    {
        var holder = new Grid { Width = 52, Height = 52, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
        holder.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(26),
            Background = TravBrush("GoldDim"),
            Child = new TextBlock { Text = traveler[..1], FontSize = 24, FontWeight = FontWeights.Bold, Foreground = TravBrush("Bg"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        });
        var image = new Image { Stretch = Stretch.UniformToFill, Clip = new EllipseGeometry(new Point(26, 26), 26, 26) };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        holder.Children.Add(image);
        holder.Children.Add(new Border { CornerRadius = new CornerRadius(26), BorderBrush = TravBrush("GoldDim"), BorderThickness = new Thickness(2) });
        if (TravelerPortraits.Has(traveler))
        {
            _ = SetPortraitAsync(image, traveler);
        }
        else
        {
            _ = SetIconAsync(image, "UI/TravelerTasks" + traveler.Replace(" ", "") + "Portrait");
        }
        return holder;
    }

    private async Task SetPortraitAsync(Image image, string traveler)
    {
        if (await _portraits.GetAsync(traveler) is { } source)
        {
            image.Source = source;
        }
    }

    // Un objet demandé : image, « en stock / demandé », nom. Vert : complet ; jaune : partiel ; rouge : rien.
    private Border ItemChip(TaskItemCheck i)
    {
        var key = (i.Item.IsCargo ? "C" : "I") + i.Item.ItemId;
        var have = Math.Min(i.Found, i.Item.Quantity);
        var brush = i.Missing == 0 ? TravBrush("Good") : have > 0 ? TravBrush("Warn") : TravBrush("Bad");
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(IconImage(key, 28));
        var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        text.Inlines.Add(new System.Windows.Documents.Run($"{have:N0}/{i.Item.Quantity:N0}  ") { Foreground = brush, FontWeight = FontWeights.Bold });
        text.Inlines.Add(new System.Windows.Documents.Run(ItemName(i.Item.ItemId, i.Item.IsCargo)));
        row.Children.Add(text);
        var tip = $"Dans le sac : {i.InBag:N0}" + string.Concat(i.Elsewhere.Select(e => $"\n{e.Where} : {e.Quantity:N0}"))
                  + (i.Missing > 0 ? $"\nIl manque : {i.Missing:N0}" : "") + (i.Shared ? "\n(aussi demandé par une autre tâche)" : "");
        return new Border
        {
            Background = TravBrush("Track"),
            BorderBrush = TravBrush("CardBorder"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(4, 2, 8, 2),
            Margin = new Thickness(0, 0, 6, 4),
            Child = row,
            ToolTip = tip,
        };
    }
}
