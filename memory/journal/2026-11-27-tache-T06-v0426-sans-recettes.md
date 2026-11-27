# Tâche T-06 — v0.4.26 : onglet Recettes retiré

- **Date** : 2026-11-27, 02:00 à 02:15
- **Agent** : Claude Code (Opus 5.5, tâche planifiée de nuit)

## Carte des dépendances (avant de retirer)
| Élément | Verdict | Où il sert |
|---|---|---|
| Page RECETTES (`MainWindow.xaml`) | seulement l'onglet | — |
| `RecipeView.cs` (fiche en logigramme) | seulement l'onglet | — |
| Recherche, Retour, quantité, liste (`MainWindow.Recipes.cs`) | seulement l'onglet | — |
| Chargement du livre (`LoadRecipesInBackground`, `_recipes`) | sert ailleurs | Personnage (noms, tiers, images), tableau de bord (`CraftActions`, D-078), Voyageurs |
| Téléchargement des images (`StartIconDownload`, `ItemIcons`) | sert ailleurs | Personnage, Voyageurs |
| `IconImage`, `SetIconAsync` | sert ailleurs | Voyageurs (objets, portraits) |
| `WithIcon` | seulement l'onglet | — |
| `RecipeBook` | sert ailleurs | voir plus haut ; `Search` et `Summary` ne servent plus qu'aux tests (gardés) |
| `RecipePlanner` | sert ailleurs | `TravelerTasks` (effort) |
| `RawMaterials` | sert ailleurs | onglet Voyageurs (matières premières) |
| Tables `crafting_recipe_desc`, `building_type_desc`, `item_conversion_recipe_desc` | sert ailleurs | lues par le livre |
| Tests `RecipePlannerTests`, `RawMaterialsTests`, `V0314Tests` | gardés | parties gardées |
| Onglet ouvert au démarrage | pas dans les réglages | rien à changer |

## Fait
- Page RECETTES et `RecipeView.cs` retirés ; `MainWindow.Recipes.cs` renommé `MainWindow.ItemData.cs` (seulement ce qui sert ailleurs). D-083, architecture-suite §30.
- Tests `V0426Tests` (4). 653/653, build 0 avertissement.
- v0.4.26 publiée ; essai sans le jeu 55 s (02:04) : aucun « Abonnement refusé », aucune erreur, « Recettes chargées : 9322 objets », 5 inventaires reçus.

## Pas fait
- Onglets Personnage et Voyageurs pas regardés à l'écran (pas de clic dans la fenêtre du bot la nuit) : images et tiers « À vérifier » par Julien (le code qui les fournit n'a pas changé).
