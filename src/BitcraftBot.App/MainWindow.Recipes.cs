using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using BitcraftBot.GameData;

namespace BitcraftBot.App;

// Onglet « Recettes » (demande de Julien, 2026-11-19) : chercher un objet, voir sa description,
// comment l'obtenir (fabrication, récolte), avec quoi (atelier, outils, métier) et ses conditions.
public partial class MainWindow
{
    private const int MaxRecipesShown = 15;
    private const int MaxUsesShown = 25;

    private RecipeBook? _recipes;
    private readonly ItemIcons _icons = new();
    private bool _recipesLoading;
    private DispatcherTimer? _recipeSearchTimer;

    private static readonly Brush TitleBrush = Frozen(232, 234, 237);
    private static readonly Brush DimBrush = Frozen(154, 160, 166);
    private static readonly Brush AccentBrush = Frozen(138, 180, 248);
    private static readonly Brush CardBrush = Frozen(37, 39, 43);

    // Chargé en tâche de fond après les données du jeu (≈ 12 Mo de recettes).
    private void LoadRecipesInBackground()
    {
        if (_recipesLoading || !RecipeBook.CanLoad(GameDataLoader.Folder))
        {
            return;
        }
        _recipesLoading = true;
        _ = Task.Run(() =>
        {
            try
            {
                var book = RecipeBook.Load(GameDataLoader.Folder);
                Dispatcher.BeginInvoke(() =>
                {
                    _recipes = book;
                    _log.Info("Données", $"Recettes chargées : {book.Entries.Count} objets et cargaisons, {book.Recipes.Count} recettes (fabrication et récolte).");
                    RecipeSearchInfo.Text = $"{book.Entries.Count} objets connus.";
                    RunRecipeSearch();
                });
            }
            catch (Exception ex)
            {
                _log.Erreur("Données", $"Recettes illisibles : {ex.Message}");
            }
            finally
            {
                _recipesLoading = false;
            }
        });
    }

    private void RecipeSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Petite attente : on cherche quand Julien a fini de taper.
        _recipeSearchTimer ??= CreateSearchTimer();
        _recipeSearchTimer.Stop();
        _recipeSearchTimer.Start();
    }

    private DispatcherTimer CreateSearchTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            RunRecipeSearch();
        };
        return timer;
    }

    private void RunRecipeSearch()
    {
        if (_recipes is null)
        {
            RecipeSearchInfo.Text = _recipesLoading || !RecipeBook.CanLoad(GameDataLoader.Folder)
                ? "Données en cours de chargement…"
                : "Données des recettes absentes.";
            LoadRecipesInBackground();
            return;
        }
        var found = _recipes.Search(RecipeSearchBox.Text);
        RecipeResults.ItemsSource = found.Select(e => new ListBoxItem
        {
            Content = WithIcon(e.Key, 24, $"{e.Name}   (palier {e.Tier}{(e.IsCargo ? ", cargaison" : "")})"),
            Tag = e.Key,
        }).ToList();
        RecipeSearchInfo.Text = RecipeSearchBox.Text.Trim().Length == 0
            ? $"{_recipes.Entries.Count} objets connus."
            : $"{found.Count} résultat(s){(found.Count >= 200 ? " (200 premiers)" : "")}.";
        if (found.Count > 0)
        {
            RecipeResults.SelectedIndex = 0;
        }
    }

    private void RecipeResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecipeResults.SelectedItem is ListBoxItem { Tag: string key })
        {
            ShowRecipeDetails(key);
        }
    }

    // Ouvre la fiche d'un autre objet (clic sur un ingrédient).
    private void OpenEntry(string key)
    {
        if (_recipes is null || !_recipes.Entries.TryGetValue(key, out var entry))
        {
            return;
        }
        RecipeSearchBox.Text = entry.Name; // relance la recherche ; la fiche exacte arrive en tête
    }

    private void ShowRecipeDetails(string key)
    {
        var book = _recipes;
        if (book is null || !book.Entries.TryGetValue(key, out var entry))
        {
            return;
        }
        var panel = RecipeDetails.Children;
        panel.Clear();
        var title = new StackPanel { Orientation = Orientation.Horizontal };
        title.Children.Add(IconImage(entry.Key, 64, hover: false));
        var name = MakeText(entry.Name, 22, FontWeights.Bold, TitleBrush);
        name.VerticalAlignment = VerticalAlignment.Center;
        name.Margin = new Thickness(10, 0, 0, 0);
        title.Children.Add(name);
        panel.Add(title);
        panel.Add(MakeText($"{(entry.IsCargo ? "Cargaison" : "Objet")} · palier {entry.Tier} · catégorie « {entry.Tag} » · rareté {entry.Rarity}", 13, FontWeights.Normal, DimBrush));
        if (entry.Description.Length > 0)
        {
            panel.Add(MakeText(entry.Description, 14, FontWeights.Normal, TitleBrush, italic: true, top: 6));
        }

        var producing = book.Producing(key);
        panel.Add(Section($"COMMENT L'OBTENIR ({producing.Count} façon{(producing.Count > 1 ? "s" : "")})"));
        if (producing.Count == 0)
        {
            panel.Add(MakeText("Aucune recette ni récolte connue : objet obtenu autrement (butin, coffre, quête, commerce…).", 13, FontWeights.Normal, DimBrush));
        }
        // Les plus simples d'abord : récoltes, puis fabrications avec le moins d'ingrédients.
        foreach (var recipe in producing.OrderBy(r => r.Kind == "Récolte" ? 0 : 1).ThenBy(r => r.Inputs.Count).Take(MaxRecipesShown))
        {
            panel.Add(RecipeCard(book, recipe, key));
        }
        if (producing.Count > MaxRecipesShown)
        {
            panel.Add(MakeText($"… et {producing.Count - MaxRecipesShown} autres.", 13, FontWeights.Normal, DimBrush));
        }

        var uses = book.Using(key);
        panel.Add(Section($"SERT À ({uses.Count})"));
        if (uses.Count == 0)
        {
            panel.Add(MakeText("N'entre dans aucune recette connue.", 13, FontWeights.Normal, DimBrush));
        }
        foreach (var use in uses.Take(MaxUsesShown))
        {
            var line = new TextBlock { FontSize = 13, Foreground = TitleBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) };
            line.Inlines.Add(new Run($"{use.Kind} « {use.Name} » → "));
            AddLinks(book, line, use.Outputs);
            panel.Add(line);
        }
        if (uses.Count > MaxUsesShown)
        {
            panel.Add(MakeText($"… et {uses.Count - MaxUsesShown} autres.", 13, FontWeights.Normal, DimBrush));
        }
    }

    private Border RecipeCard(RecipeBook book, Recipe r, string wanted)
    {
        var card = new StackPanel();
        card.Children.Add(MakeText($"{r.Kind} : {r.Name}", 15, FontWeights.SemiBold, AccentBrush));
        card.Children.Add(InfoLine(r.Kind == "Récolte" ? "Où : sur la ressource « " + r.Place + " »" : "Atelier : " + r.Place));

        if (r.Inputs.Count > 0)
        {
            var inputs = new TextBlock { FontSize = 13, Foreground = TitleBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) };
            inputs.Inlines.Add(new Run(r.Kind == "Récolte" ? "Consomme : " : "Ingrédients : "));
            AddLinks(book, inputs, r.Inputs);
            card.Children.Add(inputs);
        }
        var outputs = new TextBlock { FontSize = 13, Foreground = TitleBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) };
        outputs.Inlines.Add(new Run("Donne : "));
        AddLinks(book, outputs, r.Outputs, wanted);
        card.Children.Add(outputs);

        var skills = r.Skills.Count == 0 ? "aucun" : string.Join(", ", r.Skills.Select(s => $"{s.Name} niveau {s.Level}"));
        var tools = r.Tools.Count == 0 ? (r.HandsAllowed ? "aucun (à la main)" : "aucun") : string.Join(", ", r.Tools.Select(t => $"{t.Name} niveau {t.Level}")) + (r.HandsAllowed ? " (ou à la main)" : "");
        card.Children.Add(InfoLine($"Métier : {skills}   ·   Outil : {tools}"));

        var effort = r.Actions > 0
            ? $"{r.Actions} actions de {r.SecondsPerAction:0.##} s (≈ {Duration(r.Actions * r.SecondsPerAction)}), endurance {r.StaminaPerAction:0.##} par action"
            : $"{r.SecondsPerAction:0.##} s par action, endurance {r.StaminaPerAction:0.##} par action";
        card.Children.Add(InfoLine("Effort : " + effort));

        var conditions = new List<string>();
        if (r.NeedsKnowledge) conditions.Add("une connaissance à découvrir avant (recette cachée au début)");
        if (r.NeedsClaimTech) conditions.Add("une technologie de colonie (claim) à débloquer");
        if (conditions.Count > 0)
        {
            card.Children.Add(MakeText("Conditions : " + string.Join(" ; ", conditions), 13, FontWeights.Normal, Frozen(253, 214, 99)));
        }
        return new Border { Background = CardBrush, CornerRadius = new CornerRadius(4), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 4, 0, 4), Child = card };
    }

    // Noms d'objets cliquables : un clic ouvre la fiche de l'objet.
    private void AddLinks(RecipeBook book, TextBlock target, IReadOnlyList<EntryAmount> amounts, string? highlight = null)
    {
        for (var i = 0; i < amounts.Count; i++)
        {
            var a = amounts[i];
            if (i > 0) target.Inlines.Add(new Run(", "));
            target.Inlines.Add(new Run($"{a.Quantity} × "));
            target.Inlines.Add(new InlineUIContainer(IconImage(a.Key, 18)) { BaselineAlignment = BaselineAlignment.Center });
            var link = new Hyperlink(new Run(book.NameOf(a.Key))) { Foreground = a.Key == highlight ? Frozen(129, 201, 149) : AccentBrush };
            link.ToolTip = BigIcon(a.Key, book.NameOf(a.Key));
            ToolTipService.SetInitialShowDelay(link, 150);
            var key = a.Key;
            link.Click += (_, _) => OpenEntry(key);
            target.Inlines.Add(link);
            if (a.Probability is > 0 and < 1)
            {
                target.Inlines.Add(new Run($" ({a.Probability:P0} de chance)"));
            }
            else if (a.Probability > 1)
            {
                target.Inlines.Add(new Run($" (poids {a.Probability:0.##})"));
            }
        }
    }

    // Image d'un objet (vide le temps du téléchargement, ou si l'image n'existe pas).
    // hover : une grande image apparaît quand la souris passe dessus.
    private Image IconImage(string key, double size, bool hover = true)
    {
        var image = new Image { Width = size, Height = size, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        if (_recipes?.Entries.TryGetValue(key, out var entry) == true && entry.Icon.Length > 0)
        {
            _ = SetIconAsync(image, entry.Icon);
            if (hover)
            {
                image.ToolTip = BigIcon(key, entry.Name);
                ToolTipService.SetInitialShowDelay(image, 150);
            }
        }
        return image;
    }

    private async Task SetIconAsync(Image image, string icon)
    {
        var source = await _icons.GetAsync(icon);
        if (source is not null)
        {
            image.Source = source;
        }
    }

    // Info-bulle : grande image et nom de l'objet.
    private ToolTip BigIcon(string key, string name)
    {
        var content = new StackPanel();
        var image = new Image { Width = 96, Height = 96, HorizontalAlignment = HorizontalAlignment.Center };
        if (_recipes?.Entries.TryGetValue(key, out var entry) == true && entry.Icon.Length > 0)
        {
            _ = SetIconAsync(image, entry.Icon);
        }
        content.Children.Add(image);
        content.Children.Add(new TextBlock { Text = name, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
        return new ToolTip { Content = content };
    }

    private StackPanel WithIcon(string key, double size, string text)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(IconImage(key, size, hover: false));
        row.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
        return row;
    }

    private static string Duration(double seconds) =>
        seconds < 90 ? $"{seconds:0} s" : seconds < 5400 ? $"{seconds / 60:0.#} min" : $"{seconds / 3600:0.#} h";

    private static TextBlock Section(string title) =>
        MakeText(title, 12, FontWeights.SemiBold, DimBrush, top: 14);

    private static TextBlock InfoLine(string text) => MakeText(text, 13, FontWeights.Normal, TitleBrush, top: 1);

    private static TextBlock MakeText(string text, double size, FontWeight weight, Brush color, bool italic = false, double top = 0) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = weight,
        Foreground = color,
        FontStyle = italic ? FontStyles.Italic : FontStyles.Normal,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, top, 0, 2),
    };
}
