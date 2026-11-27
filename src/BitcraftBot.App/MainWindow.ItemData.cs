using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitcraftBot.GameData;

namespace BitcraftBot.App;

// Données des objets (v0.4.26, D-083) : livre des recettes et images des objets.
// L'onglet « Recettes » est retiré (les recettes ont leur propre site, D-077), mais ce qui suit sert ailleurs :
// noms et tiers des objets (onglet Personnage), barre de fabrication (tableau de bord, D-078),
// tâches de voyageur et matières premières (onglet Voyageurs), images des objets (Personnage, Voyageurs).
public partial class MainWindow
{
    private RecipeBook? _recipes;
    private readonly ItemIcons _icons = new();
    private bool _recipesLoading;

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
                    StartIconDownload(book);
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

    // Téléchargement complet des images, une seule fois (v0.4.13, demande de Julien : tout avoir en local).
    // Fait en tâche de fond, 4 à la fois ; le marqueur « tout-telecharge.txt » évite de le refaire (le supprimer pour le refaire).
    private bool _iconDownloadStarted;

    private void StartIconDownload(RecipeBook book)
    {
        if (_iconDownloadStarted || File.Exists(ItemIcons.DoneMarker))
        {
            return;
        }
        _iconDownloadStarted = true;
        var icons = book.Entries.Values.Select(e => e.Icon).Where(i => i.Length > 0).ToList();
        _log.Info("Images", $"Téléchargement de toutes les images du jeu (une seule fois) : {icons.Count} à vérifier, dans {ItemIcons.Folder}.");
        _ = Task.Run(async () =>
        {
            try
            {
                var (done, missing, errors) = await _icons.DownloadAllAsync(icons, (n, total) => _log.Info("Images", $"Images : {n} / {total} vérifiées."), CancellationToken.None);
                _log.Info("Images", $"Images terminées : {done} vérifiées, {missing} introuvables sur le site (notées dans introuvables.txt), {errors} erreurs réseau.");
                if (errors == 0)
                {
                    Directory.CreateDirectory(ItemIcons.Folder);
                    File.WriteAllText(ItemIcons.DoneMarker, $"Terminé le {DateTime.Now:yyyy-MM-dd HH:mm}, {done} vérifiées, {missing} introuvables.");
                }
                else
                {
                    _log.Alerte("Images", "Quelques images n'ont pas pu être téléchargées (réseau) : nouvel essai au prochain lancement.");
                }
            }
            catch (Exception ex)
            {
                _log.Erreur("Images", $"Téléchargement des images interrompu : {ex.Message}");
            }
        });
    }

    // Image d'un objet (onglet Voyageurs) : vide le temps du téléchargement, ou si l'image n'existe pas.
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
}
