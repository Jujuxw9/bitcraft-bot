# Architecture serveur — suite (sections 40 et suivantes)

Suite de [architecture-serveur.md](architecture-serveur.md) (découpée pour rester lisible). Session « tâches », 2026-11-24.

## 40. v0.4.11 — onglet Voyageurs (session tâches, 2026-11-24)

- Données : `GameData/TravelerTaskBook` lit `traveler_task_desc` et `npc_desc` (ajoutées aux tables téléchargées).
- Serveur : abonnement à `traveler_task_state` du personnage ; refusé, le bot réessaie sans (premier essai de la chaîne de repli). `World/TravelerTasks` : lecture et comparaison au stock (`TravelerTaskPlanner`) : objets comparés par numéro et type (un autre palier n'est jamais confondu), stock réparti une seule fois entre les tâches (sac d'abord, dans l'ordre des tâches).
- Verdicts : faisable maintenant (tout dans le sac), à aller chercher (ceinture ou banque, avec l'endroit), partiel (manque exact), impossible, déjà rendue.
- Onglet VOYAGEURS (après PERSONNAGE), redessiné toutes les 2 s s'il est affiché et si quelque chose a changé.
- Correction : les onglets sont repérés par leur nom, plus par un numéro fixe (l'onglet CHAT de la v0.4.10 avait décalé PERSONNAGE et CARTE).


## 41. v0.4.12 — recettes simples, plan d'actions, voyageurs en trois listes (session tâches, 2026-11-24)

- **Recettes** : `RecipeBook.Simplest` écarte les paquets et les recettes qui consomment l'objet. `RecipePlanner` (GameData) : arborescence (`Tree`), liste d'actions dans l'ordre avec quantités fusionnées (`Steps`), effort estimé (`EffortOf` : sources inconnues, fabrications, secondes). Fiche : `RecipeView.Build(clé, quantité)` ; zone Quantité de l'onglet.
- **Voyageurs** : `TaskCheck.Progress` (part du stock) et `TravelerTaskPlanner.EffortToFinish`. `MainWindow.Travelers.cs` dessine trois sections de cartes (image des objets, portrait ou initiale du voyageur).
- **Images** : `ItemIcons.Candidates` essaie « GeneratedIcons/Other|Items|Cargo » quand le chemin du jeu est absent du CDN.
- **Personnage** : trois ListView (sac, ceinture, banques), tri par clic sur l'en-tête (`InventoryHeader_Click`). **Bot** : carte des bonus actifs sous le tableau de bord, carte « Options » (style `SwitchCheck` dans le thème). **Paramètres** : 4 cartes.

## 42. v0.4.13 — écrans épurés, arbre en rectangles, images, overlay, colonies (session tâches, 2026-11-24)

- **Recettes** : `RecipeView.FlowTree` (ingrédients → `Station` → objet, récursif) ; conversions d'objets lues dans `RecipeBook.Load`.
- **Images** : `ItemIcons.EnsureFileAsync` (chemins possibles, liste `introuvables.txt`), `DownloadAllAsync` une fois par installation (marqueur `tout-telecharge.txt`), lancé par `MainWindow.Recipes.StartIconDownload`.
- **Bot** : `MainWindow.Buffs.cs` (pastilles, logos par famille) ; éléments retirés placés dans le `StackPanel Parking` caché du XAML ; image du jeu dimensionnée dans `ApplyLayout`.
- **Overlay** : `OverlayWindow.PlaceBottomCenter` (76 % de la hauteur du jeu), trois voyants.
- **Colonies** : `Server/World/Claims.cs` (`claim_state` + `claim_local_state`, abonnement dans `Queries`, `ServerReader.Claims()`), pas d'affichage.
