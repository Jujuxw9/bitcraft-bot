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
