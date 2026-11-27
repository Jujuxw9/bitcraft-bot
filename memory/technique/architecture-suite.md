# Architecture — suite (sections 18 et suivantes)

Suite de [architecture.md](architecture.md), découpée le 2026-11-26 (T-01) pour rester sous 300 lignes. Même règle : comportement et organisation, pas de code (D-023).

## 18. v0.4.14 : fenêtre du bot (D-065)

- Barres : une petite aide d'animation (`App/BarAnimation.cs`) fait glisser chaque barre vers sa nouvelle valeur.
- Clavier : `Input/Keyboard/KeyRepeatFilter.cs` ne laisse passer que le premier appui d'une touche tenue ; l'écoute du clavier l'utilise pour tout le bot.
- PERSONNAGE : les banques sont regroupées par objet, les lieux sont dans l'info-bulle de la ligne ; la colonne Objet s'ajuste à la largeur.
- CARTE : filtre par palier en plus des familles (cases recréées quand les paliers présents changent).
- PARAMÈTRES : connexion, caméra, petites cartes (sécurité, affichage, système), compte replié.
- Overlay : ancré au bord bas de la zone du jeu, au centre.

## 19. v0.4.15 : démarrage en séquence, voyageurs, personnage (D-066)

- Démarrage : `App/MainWindow.Startup.cs` enchaîne connexion au serveur → attente du personnage et des inventaires → lecture de l'écran → lancement du jeu par Steam (`steam://rungameid/3454650`). Jeu déjà lancé : rien ne démarre. Les boutons désactivés sont cachés. Options d'essai : `--sans-demarrage-auto`, `--sans-lancer-jeu`.
- Matières premières : `GameData/RawMaterials.cs` descend l'arbre des recettes jusqu'à la récolte, en déduisant le stock à chaque niveau.
- Portraits des voyageurs : `App/TravelerPortraits.cs` (wiki du jeu, gardés dans `donnees-jeu/icones/voyageurs`).
- Personnage : lignes avec image (chargée après coup), logos des trois listes, tris par défaut.
- Serveur : rayon des ressources 60 petites cases (≈ 20 cases du jeu).

## 20. v0.4.16 : archive du chat en local (D-068)

- **Base** : `Server/World/ChatArchive.cs`, base SQLite (paquet `Microsoft.Data.Sqlite`, intégré au .exe) dans `archive-chat\chat.db` **à côté du .exe** (ignorée par Git). Une table `messages` : sorte (chat public ou privé), identifiant du serveur, heure, canal, joueur (nom + entité), langue, texte, région, « écrit par le bot », destinataire (messages privés), heure d'archivage. Un même message (sorte + identifiant) n'est jamais enregistré deux fois.
- **Chat public** : chaque passage de la boucle serveur (500 ms), `MainWindow.ChatArchive.cs` repère les messages pas encore vus (Local, Région, LFG, Trade : ceux que l'abonnement du chatbot reçoit déjà) et les donne à **un fil d'écriture à part** (file d'attente) : la fenêtre n'attend jamais la base. Les messages de Julien attendent 10 s, le temps de savoir si c'est une réponse du bot (`GptChatPicker.WrittenByBot`).
- **Messages privés** : pendant la découverte (liaison courte au module global, jeu fermé, déjà faite au démarrage), un 2e abonnement demande `direct_message_state` (le serveur ne donne que les messages envoyés ou reçus par Julien). Pas de liaison de plus. Refus, silence (5 s) ou module global fermé : découverte gardée, pas de messages privés, une ligne dans le journal (« Messages privés (archive du chat) : … »). **À vérifier** : format réel (jamais reçu, le module global a refusé la liaison pendant l'essai du 26/11).
- **Onglet CHAT** (de nouveau visible) : en haut, recherche par joueur (morceau du nom) et filtre par canal (dont « Privé »), nombre total ; au milieu, tableau Heure / Canal / Joueur / Texte, le plus récent en haut, 500 lignes au plus (texte complet et région au survol) ; en bas, l'état du chatbot et un bouton « Afficher le chatbot @gpt » (statistiques, test d'une phrase, historique, repliés par défaut). Le tableau est relu au plus toutes les 2 s, seulement quand l'onglet est affiché.
- **Journal** : au démarrage « Archive du chat : N messages, fichier … », puis « premiers messages enregistrés ».

## 21. v0.4.17 : capacités automatiques (D-069)

- **Données** : `GameData/AbilityBook.cs` lit `ability_custom_desc`, `buff_desc` et `skill_desc`. Pour chaque capacité : endurance, recharge, bonus et **métier déduit des effets du bonus** (le champ métier du jeu vaut toujours « ANY »). Tableau des capacités : [capacites.md](capacites.md).
- **Décision** (sans fenêtre, testée) : `Server/World/AbilityPlanner.cs`. Entrées : interrupteurs, action en cours (récolte « Extract ») et métier de la ressource visée, première barre d'action (serveur), bonus actifs, endurance. Sortie : la case à lancer maintenant (ou rien) et l'état de chaque case pour l'affichage. Règles : recharge finie, bonus fini, endurance après coût ≥ 30 %, une touche toutes les 2 s, capacités sans métier (Sprint) jamais lancées.
- **Fenêtre** : `App/MainWindow.Abilities.cs`, appelé par la boucle serveur (500 ms) juste après le repas automatique. Appui par le contrôleur d'actions (mêmes sécurités que le repas) ; attente de la confirmation par le serveur (6 s) ; deux échecs de suite = capacité en pause 5 min. Réglages gardés dans `config.local.json` (interrupteur général, choix par capacité).
- **Touches** : `GameKeys.ActionBarKeys` (rangée du haut, position physique) ajoutées aux touches autorisées du contrôleur.
- **Journal** : liste de la barre avec l'état de chaque capacité quand la barre change ; « Capacité : Hewing Strikes (touche 7), coupe de bois » à chaque appui.

## 22. v0.4.18 : onglet CARTE refait (D-070)

- **Lecture** (sans fenêtre, testée) : `Server/World/TerrainMap.cs`. `TerrainChunk` (une ligne de `terrain_chunk_state` : biomes, part des biomes, hauteurs du sol et de l'eau, 32 × 32 cases ; indice `(dimension-1)*1000000 + z*1000 + x + 1`), `ChunksAround` (morceaux à lire autour du personnage : 24 cases de rayon), `ExploredChunks` (bitmap de `exploration_chunks_state_v2`, un bit par morceau du monde), `TerrainColors` (biome, eau, ombre, assombrissement), `TerrainImage` (assemblage des morceaux en une image, nord en haut, signature pour ne la refaire que si les morceaux changent). Grande case x ≈ E du HUD, z ≈ N.
- **Abonnement** : `Queries.Terrain` ajouté à `FollowPlayer` quand la zone des ressources est connue (`withTerrain`) ; relu avec la zone (tous les ~10 cases). `ServerReader` : repli « sans le fond de carte » en premier, `TerrainChunks()` (une ligne n'est convertie qu'une fois), `Explored()`, journal « Fond de carte : N morceaux… Exemple brut » et « Zones découvertes » une fois par connexion, poids de chaque abonnement en Ko.
- **Filtres** (testés) : `Server/World/MapFilter.cs` : `MapCategories` (12 familles → 7 catégories), `MapFilter` (catégories affichées, tiers masqués, ce tier seul, nombres croisés : un bouton de tier compte selon les catégories allumées et inversement).
- **Fenêtre** : `MainWindow.Map.cs` : pastilles `MapPill` (fond coloré allumée, contour éteinte, nombre), couleurs `TierColors` ; filtres gardés dans `config.local.json` (`MapShownCategories`, `MapHiddenTiers`). `ResourceMap.SetTerrain` dessine l'image sous les cercles, les points ont un contour sombre.
- **Essai de Claude** : avec `--sans-lancer-jeu`, le bot enregistre une image de sa fenêtre dans `captures-essai\` à côté du .exe au premier dessin du fond (la capture de l'écran sort blanche quand la session Windows est verrouillée).

## 23. v0.4.19 : requêtes de zone par morceau de carte (D-071)

- **Pourquoi** : depuis le 26/11 au soir, le serveur refuse un abonnement dont l'estimation dépasse 20 millions de lignes ; un carré de coordonnées seul sur `location_state` est estimé trop large.
- **Comment** : chaque requête de zone (ressources, santé, sacs au sol) porte `chunk_index = N` (colonne indexée) en plus du carré ; une requête par morceau de 32 × 32 cases touché par la zone. Les morceaux du fond de carte restent ceux de la v0.4.18.
- **Repli** : en cas de refus, une seule partie est retirée à la fois, des plus lourdes (zone autour du personnage) aux plus légères (sac, banques). Sac, banques, chat, barre d'action et voyageurs ne partent qu'en dernier.

## 24. v0.4.20 : retours sur la v0.4.19 (D-073)

- **Fond de carte** : l'image par case (un point par grande case) sert seulement de source de couleurs. Elle est redessinée une fois par fond reçu en hexagones (12 points d'image par case, pointe en haut, lignes impaires décalées d'une demi-case vers l'est, comme les coordonnées décalées du serveur), puis affichée à l'échelle de la carte.
- **Filtres** : 8 catégories (pierre et minerai séparés), tiers T1 à T10 d'une couleur ; les ressources sans tier ne sont cachées que si un tier est masqué ; les anciens réglages sont convertis au chargement.
- **Chat** : tableau tenu à jour par ajout en bas (pas refait à chaque fois), relu toutes les secondes quand l'onglet est ouvert ; refait seulement si le filtre change.
- **Activité** : dernière action gardée 2,5 s après sa fin.

## 25. v0.4.21 : zone de 25 cases (D-075)

- Ressources lues sur 75 petites cases autour du personnage (9 morceaux de carte au plus), fond de carte sur 29 cases, carte du bot à 26 cases de rayon avec un cercle « 25 ».
- Boutons de tier T1 à T5 ; un tier plus haut masqué dans d'anciens réglages est réaffiché au chargement, sauf en mode « ce tier seul ».

## 26. v0.4.22 : carte gardée, zoom, fabrication, RAM (D-078)

- **Mémoire du terrain** (`TerrainStore`) : morceaux reçus gardés en mémoire et sur le disque (`carte/<numéro>.bin`, format binaire maison « BCT1 »), réécrits seulement s'ils changent. Le fond dessiné = morceaux gardés autour du personnage dans le rayon de la vue ; recalculé quand le personnage change de morceau, quand le zoom change ou quand un morceau change.
- **Mémoire des ressources** (`ResourceMemory`) : 40 000 au plus, en mémoire seulement ; oubliées si absentes de la lecture en direct dans la zone lue.
- **Zoom** : 5 crans (0,5 à 2 fois la zone lue) ; image des hexagones plafonnée à environ 2 400 points de côté.
- **Fabrication** : abonnement à `progressive_action_state` du personnage (index sur le propriétaire) ; nombre de gestes par recette tiré de `crafting_recipe_desc`.
- **RAM** : niveau d'alerte changé après 3 mesures de suite ; ramasse-miettes forcé (avec tassement) après un message de plus de 500 000 caractères, au plus une fois par minute.

## 27. v0.4.23 : carte, ressource recherchée, overlay (D-079)

- **Vue** : 7 crans (10 à 100 cases). Ordonnées multipliées par √3/2 (`ResourceMap.RowFactor`) pour le fond, les points et la limite de la zone lue ; tout est coupé au cadre. `ToCell` donne la case sous un point de la carte (préparation de « Se déplacer ici »).
- **Zones découvertes** : `ServerReader.TerrainMemory` = la mémoire de la carte. À chaque abonnement possible (5 s au moins entre deux), le lecteur cherche les morceaux découverts (bitmap `exploration_chunks_state_v2`) à moins de 130 cases, absents de la mémoire et jamais demandés pendant cette connexion ; il en ajoute 24 au plus aux requêtes du fond de carte. Ils sont rangés dans la mémoire dès l'abonnement accepté, puis ne sont plus redemandés.
- **Mémoire du terrain** : numéro de version (`TerrainStore.Version`) ; la carte se redessine quand il change.
- **Ressource recherchée** (`MainWindow.Sought.cs`) : liste des ressources des données du jeu filtrée sur les mots tapés ; choix gardé dans les réglages (`MapSoughtResource`) ; la plus proche parmi la zone lue et la mémoire.
- **Overlay** : ligne système (`SystemSummary`, dans Core) reprise de la dernière mesure CPU/RAM.

## 28. v0.4.24 : reprise des récoltes (D-080)

- `HarvestResume` (Server/World, sans écran) : machine à états récolte → attente de l'endurance → sélection (Tab) → confirmation. Entrées : action en cours, dernier coup, position, endurance, cible sélectionnée (`target_state`, nouvel abonnement), ressources lues.
- `MainWindow.Resume.cs` : appel toutes les 500 ms ; touches envoyées par le contrôleur d'actions (mêmes sécurités que les capacités). Touches ajoutées à la liste blanche : Tab (0x0F) et Interact = M AZERTY (0x27).
- Journal : catégorie « Reprise » (états, cible sélectionnée dans le jeu, alertes).

## 29. v0.4.25 : reprise à la souris, overlay étroit (D-082)

- `HarvestResume` : plus d'étape Tab. Phases récolte → attente de l'endurance → relance → confirmation. Nouvelles entrées : dernier clic de Julien (`ClickPoint` : point écran, heure, position du personnage, compteur de caméra) et compteur de caméra actuel. `PointProblem` dit si le point est encore sur la ressource. `NotSent()` : geste refusé par le contrôleur, refait sans compter d'échec.
- `MainWindow.Camera.cs` : chaque clic (gauche ou droit) dans la fenêtre du jeu est retenu ; molette, bouton du milieu, Q, D et W augmentent le compteur de caméra.
- `MainWindow.Resume.cs` : `MoveAsync` (souris seule), 150 ms, mesure des pixels verts dans un carré de 48 px autour du point (journal), vérification que la souris n'a pas bougé, puis M.
- `ActionController.MoveAsync` : même chemin humain et même arrêt d'urgence que `ClickAsync`, sans clic.
- `SystemSummary.Overlay` : ligne courte ; `OverlayWindow` : police et marges réduites, `MaxWidth` 300.


## 30. v0.4.26 : onglet Recettes retiré (D-083)

- Page RECETTES retirée de `MainWindow.xaml` ; `RecipeView.cs` supprimé.
- `MainWindow.Recipes.cs` devient `MainWindow.ItemData.cs` : chargement du livre des recettes en tâche de fond (`_recipes`), téléchargement unique des images, `IconImage` / `SetIconAsync` (onglet Voyageurs). Lus aussi par `MainWindow.Inventory.cs` (noms, tiers, images), `MainWindow.Dashboard.cs` (`CraftActions`) et `MainWindow.Travelers.cs` (planificateur, matières premières).
- `GameData` inchangé (`RecipeBook`, `RecipePlanner`, `RawMaterials`, tables téléchargées).

## 31. v0.4.27 : carte, données (D-084)

- `ResourceMemory` (Server/World) : mémoire des ressources vues, avec date « vue le » (horloge injectable pour les tests), 200 000 au plus, `Cleanup()` (72 h), `Save` / `Load` (fichier BCR1 `carte/ressources/ressources.bin`), contrôle `Valid` (position dans le monde), compteurs de rejets et doublons, `SeenText` pour le survol.
- `MainWindow.Map.cs` : chargement en arrière-plan au démarrage puis nettoyage ; minuterie d'une minute (`MaintainResourceMemory`) : nettoyage chaque heure, écriture toutes les 2 min si changé ; écriture aussi à la fermeture (`OnClosing`). La mémoire suit même onglet Carte caché.
- `ResourceMap` : crans de zoom jusqu'à 200 ; survol d'une ressource pâle = nom et « vue il y a N h » ; fond gris-bleu (#18222E), comme `TerrainColors.Unknown`.
- `ServerReader.DiscoveredRadius` = 230 ; `TerrainFreshFor` = 24 h : `Queries.Terrain(..., skip)` ne redemande pas un morceau frais de `TerrainStore` (`IsFresh`, date de réception gardée, relue depuis le fichier), sauf les 3 × 3 autour du personnage.
- Journal : « Abonnement accepté (N Ko reçus ; K abonnements en M min, X par minute, Y Mo en tout) » ; « Mémoire de la carte : … morceau(x) illisible(s) rejeté(s) ».

## 32. v0.4.28 : carte, affichage (D-085)

- `HexRaster` (Server/World, sans WPF) : image des hexagones calculée point par point (centre de case le plus proche = pavage exact), en parallèle ; bords sombres si ≥ 8 points par case ; `PixelsPerCell` limite l'image à ≈ 2 400 points.
- `ResourceMap.SetTerrain` : assemblage (`TerrainImage.Build`) et image en arrière-plan (`Task.Run`), posée par le fil de l'interface ; un calcul dépassé est jeté (numéro de calcul). Taille demandée = points par case à l'écran (2 à 12), comprise dans la signature. Événement `TerrainReady(TerrainBuild)` (morceaux, taille, ms, vue).
- `ResourceMap` se mesure carrée (`MeasureOverride` : min largeur / hauteur) ; plus de `Viewbox`. `DotScale(vue)` réduit les points au dézoom. `CellRightClicked(east, north)` au clic droit (pour T-10). `ZoomTo(cases)`. `LastRenderMs`.
- Disposition (`MainWindow.Layout.cs`) : colonne de la carte en taille automatique, `MapCard.MaxWidth` = 69 % de la largeur (`LayoutRules.MapShare`).
- Tableau : 6 colonnes ; `ResourceRow.Details` au survol de la ligne.
- `MainWindow.Map.cs` : `OnTerrainReady` (journal), `RunMapTest` (option `--essai-carte` avec `--sans-lancer-jeu` : vues 30, 100, 200, 1280 × 800 ; mesures, images `captures-essai/vueNN-*.png`, taille de la fenêtre remise).


## 33. v0.4.29 : « Se déplacer ici » (D-086)

- `Server/World/Travel.cs` (classes pures, testées dans `V0429Tests`) :
  - `MapScale` : lecture de la barre de la grande carte (`ParseBar` : deux nombres entre parenthèses, 1 à 30 000), `Calibrate` (trois lectures : centre, droite, bas → ordre (N, E) ou (E, N), cases par pixel, refus si zoom hors 0,01-0,5, échelles différentes de plus de 15 %, centre à plus de 3 cases du serveur), `ToScreen`, `Correct`, `OnTarget` (≤ 1 case).
  - `PathCheck` : ligne droite + parallèles à ± 1 case, pas de 0,5 case, sur le relief gardé (`FromChunks` lit `TerrainStore`) ; refus : > 30 cases, relief inconnu, écart de sol ≥ 5 (escalade), eau ≥ 5 (nage).
  - `TravelGuard` : état du trajet à chaque instantané (en route, arrivé, bloqué, trop long, danger = vie qui baisse, à terre = vie 0, liaison perdue) ; `FleePoint` (10 cases à l'opposé).
  - `GetUpChooser` : centre de « Get Up » seulement si « Go Home » est lu une fois, ailleurs (> 100 px).
- `Input/Actions/ActionController.cs` : `GameKeys.Forbidden` (0x0C, 0x0D, V 0x2F, B 0x30), vérifiée dans `PressAsync` avant `Allowed` ; ces touches ne sont plus dans `Allowed`. Nouvelles touches permises : `MapKey` 0x34, `MapKeyNumpad` 0x53 ; `Sprint` = 0x05 (case 4).
- `Perception/Ocr/OcrReader.ReadLinesAsync` : lignes lues avec leur rectangle (ramené à l'image par `ToFrame`).
- `App/MainWindow.Travel.cs` : menu du clic droit (`OnMapRightClick`, branché sur `ResourceMap.CellRightClicked`), `TravelToAsync` (contrôle, geste, départ, suivi), `SetDestinationAsync` (ouvrir, mesurer, viser, cliquer, fermer), `GetUpAsync`, captures de la fenêtre du jeu (`images/analyse/` si trouvé en remontant depuis le .exe, sinon `captures-essai/deplacement/`), modes `--essai-deplacement` et `--essai-menu-deplacement`.
- `MainWindow.Actions.cs` : la grande carte ouverte par le bot (`_travelMapAllowedUntil`) ne bloque pas ses propres gestes (D-052 garde toutes les autres fenêtres).
- `LocalSettings` : `TravelEnabled` (vrai), `MapKeyScanCode` (0x34). OPTIONS : interrupteur « Déplacements ».
- Zones de l'écran (mesurées sur `images/analyse/20261116_analyse_carte-monde_001.png`) : barre du bas x 30-69 %, y 91-97,5 % ; souris permise x de 40 px à 80 %, y de 40 px à 88 % (hors « Map Options », boussole, barre).

## 34. v0.4.30 : corrections de « Se déplacer ici » (D-087)

- `Perception/Screen/WorldMapZones.cs` : zone de la barre de la grande carte (`BarZone`, déplacée depuis `MainWindow.Travel.cs`), `SaysSetDestination`, `AfterKey` (carte ouverte / à refermer / rien d'ouvert : ne rien rappuyer).
- `Input/Actions/ActionReadiness.cs` : `WaitAsync` (attend que le contrôleur permette les actions, 20 s au plus pour le trajet ; s'arrête tout de suite si les actions sont bloquées ou le jeu introuvable, ou sur demande d'arrêt) ; `UntilAsync` (vérification répétée, pour la carte qui se referme).
- `App/MainWindow.Travel.cs` : `TravelToAsync` attend le retour sur le jeu avant le premier geste (le clic du menu laisse le bot au premier plan) ; `OpenMapAsync` lit la barre trois fois et ne rappuie que si la carte est vue ; `CloseMapAsync` regarde trois fois avant l'alerte.
- Tests : `V0430Tests` (barre lue par OCR sur `images/reference-ui/fenetres/carte-du-monde_001.png`, attente du premier plan avec un faux clavier, limites de l'attente, fermeture lente).

## 35. v0.4.31 : petits défauts (D-088)

- `Perception/Hud/HudLayout.cs` : `DarkTargetPanelProbes` (4 points et leur couleur : bord gauche, cadre du haut, fond) pour le panneau cible sombre du jeu (depuis le 23/11/2026) ; zones de texte inchangées.
- `Perception/Hud/BarReader.cs` : `GetTargetPanelStyle` (absent, crème, sombre ; écart permis 8 par canal) ; `IsTargetPanelVisible` s'en sert.
- `Perception/Hud/HudReader.cs` : texte foncé sur le panneau crème, clair sur le sombre ; « Tier I » corrigé en « Tier 1 ».
- `Server/World/CraftBarTracker.cs` : part faite de la fabrication suivie (recette retenue après la fin de l'action, la moins avancée si la même recette tourne dans deux ateliers, dernière part gardée si les objets sont ramassés) ; `App/MainWindow.Dashboard.cs` l'utilise et la remet à zéro à la fin de l'affichage ou sur une autre action.
- `Server/World/TaskJournal.cs` : `TaskInput.Abilities` (capacités du serveur), suivi des débuts de recharge pendant la tâche, `SkillAbilities` (capacités du métier sur la barre, fourni par `App/MainWindow.Tasks.cs`), événement « Conseil » après la fin d'une tâche de minage ou de pêche (`AdvisedSkills`, au moins 20 s).
- Tests : `V0431Tests` (captures `images/reference-ui/cible-sombre/`, barre de fabrication, conseil).
