# Recherches pour le site des recettes (2026-11-26)

## Sites existants
| Site | Ce qu'il fait | Ce qu'on en retient |
|---|---|---|
| bccodex.pages.dev (BitCraft Codex) | Calculateur de recettes : vue tableau et vue arbre, suivi de l'inventaire en se connectant au compte du jeu | Le plus proche de l'idée de Julien. Défauts vus : arbre très large et peu lisible ; recette choisie pas la plus simple (Luminite Machete : 2 haches + 60 Tool Scrap, 591 267 objets au total) ; demande de connecter son compte |
| bitjita.com/items | Base de données : fiche par objet (onglets Crafting, Uses, Extraction, Cargo, Market) | Complet mais pas de calcul d'arbre ni de liste à cocher. Images servies par bitjita (`/GeneratedIcons/...webp`, mêmes noms que `icon_asset_name`), y compris les bâtiments |
| brico.app (Brico's Toolbox, AGPL-3.0) | Base de données des objets, recettes, créatures, structures | Source des images utilisées par le bot (`cdn.brico.app/sprites/`) |
| everycalculators.com, west-games.com | Calculateurs génériques | Chiffres présentés comme estimations, peu fiables |
| bitcraft.wiki.gg | Wiki communautaire | Page des objets incomplète |

## Données du jeu
- Dépôt public `BitCraftToolBox/BitCraft_GameData`, branche `cereal/cs`, dossier `static/` : toutes les tables `*_desc.json`, mises à jour chaque jour (déjà utilisé par le bot, D-033).
- Chiffres (commit du 2026-11-25) : 8 645 objets, 677 cargos, 7 921 recettes de fabrication, 786 recettes de récolte.
- **Supplies du claim** : table `building_repairs_desc` (64 cargos : Timber, Brick Slab, Cloth Tarp, Leather Sheeting, Frames, Experimental Compounds, sacs de Hex Coins), avec la valeur en supplies. Confirmé par le code serveur (`claim_resupply.rs` : seuls ces cargos remplissent le claim).
- Les outils existent en plusieurs raretés (ex. 5 entrées « Emarium Axe » : Common, Uncommon, Rare) : regrouper par nom dans la recherche.
- Les noms de recettes sont des modèles (« Assemble {0} ») : le nom affiché doit venir de l'objet fabriqué.
- Biome ou position des ressources : **absents** des tables publiques. « À vérifier ».

## Images
- Aucune source **officielle** publique trouvée : ni dans `BitCraftPublic` (seulement les logos), ni dans les dépôts de BitCraftToolBox (licences : aucune sur les données, AGPL-3.0 pour brico/cereal).
- Sources communautaires : `cdn.brico.app/sprites/` et `bitjita.com/GeneratedIcons/`. Toutes deux suivent `icon_asset_name`.
- Extraire les images des fichiers du jeu installé : écarté (proche des interdits du projet, à ne pas faire sans décision).
- Décision de Julien (2026-11-26) : on garde brico.app pour l'instant et on les affiche sur le site.
