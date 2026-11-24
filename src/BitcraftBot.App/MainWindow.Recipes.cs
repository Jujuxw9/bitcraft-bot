using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BitcraftBot.GameData;

namespace BitcraftBot.App;

// Onglet « Recettes » (demande de Julien, 2026-11-19) : chercher un objet, voir sa description,
// comment l'obtenir (fabrication, récolte), avec quoi (atelier, outils, métier) et ses conditions.
// v0.4.6 : fiche en logigramme (RecipeView), boutons Rechercher, Effacer et Retour.
public partial class MainWindow
{
    private RecipeBook? _recipes;
    private RecipeView? _recipeView;
    private readonly ItemIcons _icons = new();
    private bool _recipesLoading;
    private DispatcherTimer? _recipeSearchTimer;

    // Fiches ouvertes avant (bouton Retour), et la fiche affichée.
    private readonly Stack<string> _recipeHistory = new();
    private string? _recipeShown;
    private bool _recipeGoingBack;

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
                    _recipeView = new RecipeView(book, _icons, OpenEntry);
                    _log.Info("Données", $"Recettes chargées : {book.Entries.Count} objets et cargaisons, {book.Recipes.Count} recettes (fabrication et récolte).");
                    RecipeSearchInfo.Text = $"{book.Entries.Count} objets";
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
        var hasText = RecipeSearchBox.Text.Trim().Length > 0;
        RecipeSearchButton.IsEnabled = hasText;
        RecipeClearButton.IsEnabled = RecipeSearchBox.Text.Length > 0;
        // Petite attente : on cherche quand Julien a fini de taper.
        _recipeSearchTimer ??= CreateSearchTimer();
        _recipeSearchTimer.Stop();
        _recipeSearchTimer.Start();
    }

    // Entrée dans la zone de recherche = bouton Rechercher ; Échap = Effacer.
    private void RecipeSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            RecipeSearchNow();
        }
        else if (e.Key == Key.Escape && RecipeSearchBox.Text.Length > 0)
        {
            e.Handled = true;
            RecipeClear();
        }
    }

    private void RecipeSearchButton_Click(object sender, RoutedEventArgs e) => RecipeSearchNow();

    private void RecipeClearButton_Click(object sender, RoutedEventArgs e) => RecipeClear();

    private void RecipeBackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_recipeHistory.Count == 0)
        {
            return;
        }
        _recipeGoingBack = true;
        try
        {
            OpenEntry(_recipeHistory.Pop());
        }
        finally
        {
            _recipeGoingBack = false;
        }
        ShowRecipeBackState();
    }

    private void RecipeSearchNow()
    {
        _recipeSearchTimer?.Stop();
        RunRecipeSearch();
    }

    private void RecipeClear()
    {
        _recipeSearchTimer?.Stop();
        RecipeSearchBox.Text = "";
        RunRecipeSearch();
        RecipeSearchBox.Focus();
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

    // Ouvre la fiche d'un autre objet (clic sur une tuile du logigramme).
    private void OpenEntry(string key)
    {
        if (_recipes is null || !_recipes.Entries.TryGetValue(key, out var entry))
        {
            return;
        }
        _recipeSearchTimer?.Stop();
        RecipeSearchBox.Text = entry.Name;
        _recipeSearchTimer?.Stop();
        RunRecipeSearch();
        // Plusieurs objets peuvent porter le même nom : sélectionner exactement celui cliqué.
        var item = RecipeResults.Items.OfType<ListBoxItem>().FirstOrDefault(i => (string)i.Tag == key);
        if (item is not null)
        {
            RecipeResults.SelectedItem = item;
            RecipeResults.ScrollIntoView(item);
        }
        else
        {
            ShowRecipeDetails(key);
        }
    }

    private void ShowRecipeDetails(string key)
    {
        if (_recipeView is null || key == _recipeShown)
        {
            return;
        }
        if (_recipeShown is not null && !_recipeGoingBack)
        {
            _recipeHistory.Push(_recipeShown);
        }
        _recipeShown = key;
        RecipeDetails.Children.Clear();
        RecipeDetails.Children.Add(_recipeView.Build(key, RecipeQuantity()));
        RecipeDetailsScroll.ScrollToTop();
        ShowRecipeBackState();
    }

    // Quantité voulue (zone « Quantité ») : entier de 1 à 99 999, 1 si le texte n'est pas un nombre.
    private int RecipeQuantity() => int.TryParse(RecipeQuantityBox.Text.Trim(), out var n) ? Math.Clamp(n, 1, 99_999) : 1;

    // Quantité changée : la fiche affichée est refaite (sans toucher à l'historique du bouton Retour).
    private void RecipeQuantityBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_recipeView is null || _recipeShown is null)
        {
            return;
        }
        var offset = RecipeDetailsScroll.VerticalOffset;
        RecipeDetails.Children.Clear();
        RecipeDetails.Children.Add(_recipeView.Build(_recipeShown, RecipeQuantity()));
        RecipeDetailsScroll.ScrollToVerticalOffset(offset);
    }

    private void ShowRecipeBackState()
    {
        RecipeBackButton.IsEnabled = _recipeHistory.Count > 0;
        RecipeBackButton.ToolTip = _recipeHistory.Count > 0 && _recipes is not null
            ? $"Revenir à « {_recipes.NameOf(_recipeHistory.Peek())} »"
            : "Aucune fiche précédente";
    }

    // Image d'un objet dans la liste des résultats (vide le temps du téléchargement, ou si l'image n'existe pas).
    private Image IconImage(string key, double size)
    {
        var image = new Image { Width = size, Height = size, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        if (_recipes?.Entries.TryGetValue(key, out var entry) == true && entry.Icon.Length > 0)
        {
            _ = SetIconAsync(image, entry.Icon);
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

    private StackPanel WithIcon(string key, double size, string text)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(IconImage(key, size));
        row.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
        return row;
    }
}
