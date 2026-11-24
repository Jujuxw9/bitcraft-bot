# Architecture — brique Server et voie hybride (H1)

Suite de [../architecture.md](../architecture.md) (sections 1 à 16 : vision, HUD, données du jeu). Découpé le 2026-11-18 (fichier trop long).

> Chaque section décrit l'état de **sa** version (journal des versions). Une fonction a pu changer depuis : la section la plus récente fait foi (ex. Num 5 supprimée en section 32).

## 17. Voie hybride (D-039, 2026-11-17) : nouvelle brique Server

- Sixième brique : **Server** (`src/BitcraftBot.Server`). Rôle : lire en direct l'état du jeu sur le serveur officiel, **en lecture seule**, avec le jeton de Julien.
- Elle remplacera peu à peu la **Perception** comme source principale (position, arbres, vie, endurance, faim). La Perception reste pour la sécurité et le contrôle croisé.
- La brique n'envoie **aucune action** au serveur. Les actions restent dans **Input** (souris, clavier).
- Le jeton est chiffré avec la protection de Windows (lié à la session de Julien), jamais écrit dans le journal ni dans Git.
- État : **v0.3.10**, lecture vérifiée en réel (sections 18 à 23).

## 18. v0.3.5 — H1 : lecture du serveur (2026-11-18)

Nouvel onglet **« Serveur (H1) »**. Le bot lit le serveur officiel en direct, sans rien envoyer d'autre que des demandes de lecture.

```
  Julien : e-mail ──► service de connexion BitCraft ──► code par e-mail ──► jeton
                                                                             │ chiffré par Windows
                                                                             ▼  (%LOCALAPPDATA%\BitcraftBot\jeton.bin)
  Brique Server :  schéma public ─► liaison chiffrée avec bitcraft-live-18 ─► « qui suis-je ? »
                   ─► abonnement 1 : mon personnage (numéro d'entité)
                   ─► abonnement 2 : position, vie, endurance, faim, connecté ou non,
                                     + ressources dans un carré de 80 × 80 cases autour de moi
                   ─► le serveur envoie ensuite chaque changement ; copie locale à jour
```

| Élément | Rôle |
|---|---|
| Connexion au compte | Demande un code pour l'e-mail, échange le code contre le jeton. Le jeton n'est jamais dans le journal. |
| Coffre du jeton | Chiffrement Windows lié au compte de Julien. Bouton « Oublier le jeton ». |
| Liaison | WebSocket chiffré, protocole JSON de SpacetimeDB. **Ne sait pas** envoyer d'action (un test automatique le vérifie). |
| Schéma | Donne le nom de chaque colonne : les lignes arrivent en simples listes de valeurs. |
| Copie locale | Tables reçues, mises à jour à chaque changement (suppression puis ajout). |
| Instantané | Résumé du personnage + arbres triés par distance, noms et paliers tirés de BitCraft_GameData. |

Règles de sécurité de la liaison :
- **Ordre** : se connecter avant de lancer le jeu. Si le jeu tourne déjà, le bot demande confirmation (risque de déconnexion du personnage à la fermeture du bot).
- **Pas de reconnexion automatique** si la liaison tombe : elle ferait du bot la connexion principale.
- Le carré des ressources est redemandé quand le personnage s'est éloigné de 20 cases (au plus une fois toutes les 5 s).
- Si le serveur refuse la requête des ressources, le bot continue sans elles (journal : « Abonnement refusé »).

Comparaison écran / serveur : toutes les 15 s, le journal note les deux positions (pour trouver la conversion en H2).

## 19. v0.3.6 — découverte de la région du personnage (2026-11-18)

Avant de lire la région, si le jeu est fermé, le bot ouvre une liaison courte avec le module **global** (`bitcraft-live-global`) et demande : le compte a-t-il un personnage ? dans quelle région ? à quelle adresse ? Puis il ferme cette liaison et lit la bonne région. Si le jeu tourne, il saute cette étape (risque de déconnexion) et lit la région saisie. Si une région ferme la liaison avant le moindre message, le bot l'explique (personnage absent ou connexion du compte trop ancienne).

## 20. v0.3.7 — modules du monde 1.0 (2026-11-18)

Les régions du monde 1.0 s'appellent `idc-N` (R18 → `idc-18`) et le module global `idc-global`. Les versions 0.3.5 et 0.3.6 visaient `bitcraft-live-*`, un ancien monde : le serveur refusait le compte. Seuls ces deux noms changent.

## 21. v0.3.8 — journaux de liaison détaillés (2026-11-18)

Pour déboguer la connexion sans être devant le PC, le journal (catégorie « Serveur ») note maintenant :
- le jeton **sans le secret** : compte, date d'émission, âge, fin de validité ;
- chaque liaison (`[liaison idc-global]`, `[liaison idc-18]`) : adresse, durée de la poignée de main, code HTTP, sous-protocole accepté, en-têtes du serveur (sans ceux qui contiennent un jeton) ;
- en cas de refus : code HTTP, code d'erreur, cause ;
- les 5 premiers messages reçus : type, taille, extrait (jeton masqué) ; les suivants : type et taille (niveau « Détails ») ;
- chaque abonnement : numéro, raison, requêtes SQL ;
- la fin de liaison : durée, nombre de messages, octets reçus, statut de fermeture.

## 22. v0.3.9 — connexion refusée si le jeu est lancé (2026-11-18)

- Essais réels v0.3.7 et v0.3.8 : la lecture marche ; connecter le bot **avec le jeu ouvert éjecte le jeu** (« Logged in elsewhere »).
- Le bouton « Se connecter » refuse donc toute connexion si le processus BitCraft tourne, sans dérogation. La découverte par le module global se fait toujours (jeu fermé).
- « Se déconnecter » ne pose plus de question : le bot est toujours connecté avant le jeu, sa fermeture ne touche pas le jeu.
- Le journal compare toutes les 15 s la position écran et la position serveur ÷ 3, avec l'écart.

## 23. v0.3.10 — confort (2026-11-18)

- Fenêtre **toujours agrandie** au démarrage, sur l'écran où elle était.
- Onglet Journal : les 4 niveaux (erreurs, alertes, infos, détails) cochés par défaut ; la liste reste **collée à la dernière ligne** (à chaque ajout, après un filtre, à l'ouverture de l'onglet).
- Versions rangées par `tools/publier-version.ps1` dans `artifacts/release` (dernière) et `artifacts/old` (D-040).

## 24. v0.3.11 — H2 : carte des arbres (2026-11-18)

- **Coordonnées** : chaque position du serveur est traduite en coordonnées du jeu, E = arrondi(x ÷ 3), N = arrondi(z ÷ 3). Les distances sont données dans ces mêmes unités (« cases du jeu »).
- **Santé des arbres** : nouvelle demande de lecture `resource_health_state` (une ligne par ressource, créée à son apparition). Si le serveur la refuse, le bot réessaie sans la santé, puis sans les ressources.
- **Carte** (onglet Serveur, carte 4) : nord en haut, est à droite, vous au centre, cercles à 5 et 10 cases. Arbres mûrs en vert foncé, jeunes en vert clair, morts en brun, entamés entourés d'orange. Repères : meilleur arbre (jaune), cible à l'écran (rose), sélection (blanc). Un clic sur un point le sélectionne dans le tableau.
- **Tableau** : repère (★ meilleur, ◎ cible), nom, âge (jeune, mûr, mort, planté…), santé actuelle / max (%), distance, direction (N, NE… SO, O, NO), position N / E, palier. Mis à jour sur place : la sélection et le défilement restent.
- **Meilleur arbre** : l'arbre mûr le plus proche, jamais le Maker's Tree (D-036). C'est celui que H3 visera.
- **Cible à l'écran** : la cible lue sur le HUD (nom + santé) est retrouvée parmi les arbres du serveur ; même santé = sûr, même nom seulement = probable. Écrit dans le journal à chaque changement. Sert à vérifier H2 en jeu.
- **Zone lue** : centre, rayon (≈ 13 cases), heure de la dernière relecture ; le journal explique chaque relecture (distance parcourue).
- Tests : 200 OK (23 nouveaux : coordonnées réelles, direction, âge, santé, requêtes). Table de santé présente et publique dans le vrai schéma de `idc-18`.

## 25. v0.3.12 — onglet Carte, familles, cible stable (2026-11-18)

- Demandes de Julien après l'essai v0.3.11 : un onglet à part pour la carte, des cases à cocher par type de ressource.
- **Onglet « Serveur (H1) »** : compte, connexion, personnage, instantané. **Onglet « Carte (H2) »** : bandeau (votre position ±1, vie, endurance, satiété, région), cases par famille, carte agrandie (560 px), tableau avec la colonne « Famille ».
- **Familles** : les ~80 catégories du jeu regroupées en 12 familles (arbres, rochers et pierre, minerais, fleurs, plantes à fibres, baies et fruits, champignons, céréales et légumes, sable / argile / terre, pêche, obstacles, autres). Chaque case montre le nombre lu ; chaque famille a sa couleur, la même sur la carte. Boutons « Tout » et « Arbres seulement ».
- **Cible à l'écran** : ressources du même nom à moins de 8 cases seulement ; le serveur peut avoir jusqu'à 3 % d'avance sur l'écran ; « sûr » seulement si une seule ressource colle ; sinon on garde la précédente. Le journal note chaque changement de cible ou de certitude.
- Tests : 219 OK (19 nouveaux : familles, cas réels de la cible).

## 26. v0.3.13 — le jeu caché par une autre fenêtre (2026-11-19)

- Constat de l'essai long : la capture copie l'écran ; quand une autre fenêtre passe devant le jeu, c'est elle qui est lue et enregistrée.
- **Visibilité du jeu** : à 5 points clés (coordonnées, cible, barres, centre), le bot vérifie que la fenêtre du dessus est bien le jeu. Cliquer sur la fenêtre du bot (2ᵉ écran) ne compte pas : le jeu reste visible.
- Jeu caché ou introuvable → lecture de l'écran **en pause** : pas d'alerte, pas d'image de diagnostic. Le journal note la pause et la reprise.
- **Num 0** ignorée si le jeu est caché (le journal le dit). **Num 5** inchangée en attendant la réponse de Julien (Q-022).
- Refus du module global (jeu pas lancé depuis plus d'1 h) : simple information, la lecture directe de la région suffit.
- Pas vérifié en réel par Claude : le test avec un Bloc-notes par-dessus a été bloqué (risque de fermer celui de Julien). À vérifier pendant le prochain essai.

## 27. v0.3.14 — onglets, overlay, sac et banques, recettes (2026-11-19)

Demandes de Julien, triées dans cet ordre : sécurité (Num 5), rangement (Paramètres), overlay, banque, recettes.

- **Num 5** (D-041) : n'agit que si BitCraft est la fenêtre active. Ailleurs, rien (ligne « détail » au journal).
- **Onglets** : Supervision · Journal · **Personnage** · Carte (H2) · **Recettes** · Tests · **Paramètres**.
  - Paramètres : compte, connexion, affichage (overlay), système (CPU / RAM, seuils). Place prévue pour les futurs réglages.
  - Personnage : personnage en direct, instantané, **sac et banques**.
- **Overlay** : petite fenêtre à part, posée en haut à gauche de l'écran du jeu (pas une modification du jeu). Elle laisse passer les clics, ne prend jamais le clavier, et est exclue des captures d'écran (Windows « exclude from capture »). Lignes : connexion au serveur, lecture de l'écran (en cours / en pause / arrêtée), rappel Num 0 et Num 5. Visible seulement quand le jeu est actif ou non caché. Case à cocher dans Paramètres (gardée dans `config.local.json`). Vérifié : fenêtre affichée en (14, 14), 248 × 64 px, avec le jeu lancé.
- **Sac et banques** : `inventory_state` est **publique**. Deux lectures (sans jointure) : `owner_entity_id = personnage` (sac = index 0, ceinture = index 1) et `player_owner_entity_id = personnage` (un coffre par joueur dans chaque banque ; `owner_entity_id = 0` = banque régionale). Plus `bank_state` (position des banques de la région, case « axiale » → x + z / 2). Tableau : objet, quantité (piles regroupées), type, palier, où ; filtre par endroit et recherche. Si le serveur refuse, le bot réessaie sans le sac ni les banques. Au premier abonnement, le journal écrit un extrait brut des poches, pour vérifier le format réel (**à vérifier** au prochain essai).
- **Recettes** : nouvelles tables BitCraft_GameData `crafting_recipe_desc` (12 Mo) et `building_type_desc`, téléchargées avec les autres. Livre chargé en tâche de fond (≈ 0,5 s) : 9322 objets et cargaisons, 8707 recettes. Recherche par morceaux de nom ; fiche : description, palier, catégorie, rareté ; « Comment l'obtenir » (récolte : ressource, chance ; fabrication : atelier et palier, ingrédients cliquables, métier et niveau, outil et niveau, nombre d'actions, durée, endurance ; conditions : connaissance à découvrir, technologie de colonie) ; « Sert à ».
- Tests : 229 OK, dont le vrai schéma (`inventory_state`, `bank_state`) et les vraies données (Rough Plank, Rough Log).

## 28. v0.3.15 — repas automatique, images des objets (2026-11-19)

Demandes de Julien : repas automatique, images dans Recettes, nom de version dans le .exe, onglet Journal avant Paramètres, fichier des raccourcis.

- **Repas automatique** (D-042, remplace D-032) : le bot appuie sur **Maj + E**. Règle à part et testée (`MealPlanner`) : faim < 30 % du maximum → un aliment à la fois tant qu'il ne dépasse pas le manque ; plus de bonus « Food Regen » → un aliment. Confirmation par le serveur (faim en hausse, bonus revenu, ou pile du sac qui baisse) avant le suivant, puis pause de 8 à 14 s. Sécurités : case dans Paramètres (désactivée par défaut), BitCraft actif et visible, personnage connecté, pas de frappe de Julien depuis 3 s, 8 aliments max en 10 min, 2 échecs → pause 5 min, Num 5 décoche. Ligne « Repas auto » dans l'overlay.
- **Nouvelles lectures** (avec le sac, même repli si refus) : `active_buff_state` (bonus actifs : début en secondes Unix + durée ; restant = début + durée − maintenant, code serveur) et `character_stats_state` (faim maximale = case 19, `CharacterStatType::MaxSatiation`). Les deux sont publiques (vérifié sur le vrai schéma).
- **Données du jeu** : `buff_desc` et `buff_type_desc` ajoutées (téléchargées au démarrage). `FoodBook` : 413 aliments ; 11 niveaux « Food Regen » (15 min). Un aliment « repas » = donne le bonus ou rend de la faim sans autre bonus ; les potions, thés et élixirs bloquent le repas s'ils sont avant la nourriture dans le sac.
- **Images des objets** : `icon_asset_name` de BitCraft_GameData + CDN public `cdn.brico.app/sprites/<chemin>.webp` (trouvé dans le code de cereal). Téléchargées une fois, gardées dans `donnees-jeu/icones/`, décodées par Windows (décodeur WebP présent sur le PC de Julien). Onglet Recettes : petite image dans la liste et devant chaque ingrédient, grande image en titre et au survol.
- **.exe** : `artifacts/release/BitcraftBot-vX.Y.Z.exe` ; l'ancien part dans `artifacts/old/vX.Y.Z/`.
- Tests : 242 OK, dont le vrai schéma et les vraies données (poisson grillé : 25 de faim, bonus « Food Regen »). **Pas encore lancé à l'écran** (Julien jouait).

## 29. v0.3.16 — onglets regroupés, bonus actifs, serveur d'abord (2026-11-19)

Essai de la v0.3.15 par Julien (journal du 19/11 à 20:06) :
- **Repas automatique vérifié en réel** : « Plain Fish and Bulbs » mangé avec Maj + E, faim 64,6 → 100, bonus de nourriture revenu (30 min), confirmé par le serveur en 0,5 s.
- **Format des poches confirmé** : `[6000,[0,[1,50,[0,[]],[1,[]]]],false]` = option en somme `[0, pile]`, pile `[id, quantité, type [0]=objet / [1]=cargaison, durabilité]`. 4 inventaires, 52 piles. Banque bien affichée (Julien).
- **Format des bonus confirmé** : `[buff_id, [début], durée, [valeurs]]`. Début = secondes Unix quand le bonus tourne. Bonus « en pause » (seulement en jeu, personnage hors ligne) : le serveur garde le **temps déjà écoulé** (petit nombre) au lieu de l'heure de début (code serveur, `OnlineTimestamp::pause`). `0` = case vide. Durée `-1` = permanent.
- Faim maximale : `character_stats_state.values[19]` = 100.

Changements :
- **Onglets** (D-043) : Bot · Personnage · Carte · Recettes · Paramètres (journal à droite). Onglet Tests supprimé (D-035 terminée).
- **Bonus actifs** (Personnage) : nom, type, temps restant (ou « en pause »), effets traduits (`buff_desc.stats`, ex. « vitesse de récolte -40 % »).
- **Serveur d'abord** (D-044) : vie, endurance et faim de l'onglet Bot viennent du serveur ; l'alerte « faim sous 50 % » lue à l'écran est supprimée.
- Zones lues dessinées plus finement sur l'image (trait de 2 px au lieu de 5).
- Overlay : ligne « Repas auto » toujours affichée (activé ou désactivé).
- Tests : 246 OK. Vérifié à l'écran : les 5 onglets s'affichent, journal à droite des Paramètres, 29 images d'objets téléchargées.

## 30. v0.3.17 — tableau de bord, jauge de métier lue sur le serveur (2026-11-19)

Demandes de Julien : tableau de bord dans l'onglet Bot ; lire la jauge de métier sur le serveur ; D-044 précisée (tout prendre sur le serveur d'abord, puis l'écran pour ce qui manque) ; contrôleur d'actions commun plus tard.

- **Inventaire des données** : `donnees-serveur.md` (lu / disponible / seulement à l'écran).
- **Nouvelles lectures** (groupe du sac, même repli si refus ; toutes publiques, vérifié sur le vrai schéma) :
  - `experience_state` : expérience par métier. Niveau et % vers le niveau suivant avec la **formule du serveur** (`ExperienceStack` : 125 niveaux, 52 × (g^n − g) / (g² − g) × 10, g = 2^0,158, calcul en float 32 bits). C'est la jauge de métier, sans lire l'écran.
  - Métier « en cours » : celui dont l'expérience vient d'augmenter (moins de 20 s). Le serveur ne le dit pas directement.
  - `player_action_state` : action de la couche « Base » (récolte, fabrication, marche…), début et durée en millisecondes → progression du coup.
  - `extract_outcome_state` : **ressource frappée** et dégâts du dernier coup. Retrouvée dans les ressources lues : nom, santé, distance. Remplace la cible lue à l'écran dès le premier coup.
  - `player_state` (temps de jeu), `teleportation_energy_state`, `character_stats_state` cases 0 et 1 (vie et endurance maximales).
  - Sac : nombre total de cases (`PocketCount`) → « 18 cases pleines sur 24 », alerte « sac plein ».
- **Tableau de bord** (onglet Bot) : 4 cartes (Personnage : barres de vie, endurance, faim, bonus ; Activité : action, métier et sa barre, ressource frappée, niveaux ; Sac et alentours : sac, position, meilleur arbre ; Le bot : serveur, lecture de l'écran, repas auto, alertes). Image du jeu en dessous, dans la place restante.
- Num 0 : une seule capture par seconde (touche maintenue = deux fichiers identiques à l'essai v0.3.16).
- Le journal écrit un **extrait brut** de chaque nouvelle table au premier abonnement : format réel **à vérifier** au prochain essai.
- Tests : 254 OK. Tableau de bord vérifié à l'écran (sans connexion : le jeu était ouvert).

## 31. v0.3.18 — contrôleur d'actions commun, thème BitCraft, déconnexion garantie (2026-11-19)

Demandes de Julien : supprimer la case Num 5 ; Sécurité dans Paramètres ; repas auto actif de base, alerte sans nourriture ; beaucoup moins de texte ; nouveau thème aux couleurs de BitCraft, adaptatif ; raccourci bureau ; toujours se déconnecter avant de fermer ; contrôleur d'actions commun.

- **Contrôleur d'actions** (D-045) : `src/BitcraftBot.Input/Actions/ActionController.cs`. `PressAsync` (touche ou combinaison) et `ClickAsync` (trajet humain puis clic), `WhyNot()` (raison d'attendre), `Halt` / `Resume`. Le repas automatique l'utilise (`EatAsync`). `InputSender` implémente `IInputDevice` (un faux dans les tests).
- **Num 5** toujours actif ; il bloque le contrôleur tout de suite (depuis le fil du clavier, sans attendre l'interface). Reprise : « Démarrer » ou recocher le repas auto.
- **Repas auto** : remis à « actif » à chaque démarrage ; alerte « Aucune nourriture dans le sac » (une fois, puis info quand il y en a de nouveau).
- **Fermeture** : la fenêtre annule sa première fermeture, se déconnecte du serveur en **attendant la fin** (8 s au plus), puis se ferme. Arrêt de Windows : déconnexion directe (4 s au plus). Avant, l'attente était bloquante et limitée à 4 s (elle marchait déjà : « Liaison fermée (fermeture du bot) » dans le journal v0.3.17).
- **Interface** (D-046) : `Themes/BitCraft.xaml` ; fenêtre réécrite (`MainWindow.xaml`), textes raccourcis dans le code ; mise en page adaptative (`MainWindow.Layout.cs`). Retirés : lignes de lecture écran redondantes (zone, vie, endurance, faim : le tableau de bord les montre, l'écran en secours sans serveur), niveaux de tous les métiers, énergie de téléportation, temps de jeu, nombre de messages, chemin du journal.
- **Raccourci bureau** : `tools/creer-raccourci-bureau.ps1` crée « BitCraft Bot » → `tools/lancer-bot.ps1`, qui lance le `BitcraftBot-vX.Y.Z.exe` de numéro le plus haut dans `artifacts/release`.
- Vérifié : 264 tests ; rendu de la fenêtre **hors écran** à 1920, 1200 et 800 px de large (Julien jouait : aucune fenêtre ouverte). Deux défauts trouvés et corrigés au rendu : clés « Muted » et « Card » en double dans le thème (la fenêtre ne s'ouvrait pas), chevauchement en largeur étroite.
- **À vérifier en réel** (Julien) : déconnexion à la fermeture, repas auto actif au démarrage, alerte sans nourriture, aspect sur son écran.

## 32. v0.3.19 — Num 5 supprimée, journal des niveaux et des tâches, HUD allégé, moins de CPU (2026-11-19)

Demandes de Julien (passation session 14) : supprimer Num 5 ; plus d'informations dans le journal (niveaux, début et fin de tâche) ; ne plus lire vie, endurance, faim et niveaux à l'écran ; optimiser CPU, RAM et vitesse.

- **Num 5** (D-047) : touche, réglage et texte supprimés (`EmergencyKey`, arrêt « touche d'urgence », rappels dans l'overlay et la carte Sécurité). Restent : bouton « Arrêter », souris bougée, pause 3 s quand Julien tape, durée max, case du repas auto.
- **Journal des niveaux et des tâches** (`TaskJournal`, brique Server, testé sans le jeu). Appelé à chaque lecture du serveur (500 ms), il écrit **une ligne par événement** :
  - `[Métier] Forestry passe niveau 30 (était 29).` — niveau calculé avec la formule du serveur ; la première lecture ne compte pas.
  - `[Tâche] Début : récolte « Mature Oak Tree », Forestry niv. 29, à N 6707, E 8927.` — action de la couche « Base » ; ressource d'après la cible de l'action (ou le dernier coup) ; métier d'après les données du jeu.
  - `[Tâche] Fin : récolte « Mature Oak Tree », 42 s, 12 coups, Forestry +360 XP, objets : +6 Rough Bark.` — fin après 3 s sans action, ou dès que la cible ou la recette change ; coups = actions distinctes du serveur ; objets = différence du sac et de la ceinture (pas les banques). Fabrication : « étapes » et objets utilisés en « − ».
  - Pas des tâches : déplacements, gestes, escalade, téléportation, repas (déjà écrit par le repas auto).
  - Liaison coupée : la tâche en cours est écrite telle quelle.
  - Le passage de niveau masque la cible lue à l'écran 8 s (bandeau du jeu, Q-018) : avant, c'était la jauge lue à l'écran qui le faisait.
- **HUD allégé** : l'écran ne lit plus que la position (contrôle croisé), la zone, la région et la cible. Retirés : textes de vie et d'endurance (2 lectures OCR par passage), barres, ligne de faim, jauge de métier ; leurs zones ; l'alerte « endurance qui baisse sans jauge ». Sans serveur, le tableau de bord affiche « — ». Les classes de la jauge (`ProfessionGaugeReader`) restent, non appelées, pour H3 si besoin.
- **Optimisations** :
  - instantané du serveur et inventaires recalculés **seulement si un message est arrivé** depuis le passage précédent ; inventaires calculés une fois par passage (avant : 3 fois) ;
  - seul l'**onglet affiché** est redessiné (Bot : tableau de bord ; Personnage : fiche, sac, bonus ; Carte : carte et tableau). Le journal, le repas auto et la cible à l'écran tournent toujours ;
  - « Jeu lancé ? » (liste des processus) : toutes les 3 s et seulement dans Paramètres (avant : 2 fois par seconde) ;
  - capture à **2 images/s** quand l'image du jeu n'est pas visible (autre onglet, fenêtre réduite), au lieu de 5 ; l'image n'est plus envoyée à l'affichage ;
  - une seule recherche de la fenêtre du jeu (overlay, contrôleur, Num 0) ;
  - messages bruts du serveur : 1 sur 200 en « Détails » (avant : tous, chacun relu une 2e fois).
- Vérifié : 262 tests (nouveaux : `V0319Tests`, 9 cas) ; rendu **hors écran** des 5 onglets à 1920 et 800 px et de l'overlay (Julien jouait : aucune fenêtre ouverte).
- **Pas vérifié** : mesure CPU / RAM avant / après, journal des tâches en réel (il faut la connexion au serveur, donc le jeu fermé avant). .exe fabriqué en local (`artifacts/release/BitcraftBot-v0.3.19.exe`), sans Release GitHub.

## 33. v0.3.20 — stabilité : image du jeu et icônes réparées, écran = cible seulement (2026-11-19)

Essai v0.3.19 de Julien : journal des tâches OK (pêche, coupe : coups, XP, objets justes) ; **image du jeu absente**, **images des recettes absentes**, position écran décrochée.

- **Cause** (reproduite sur un banc hors écran qui fait tourner le vrai code) : les icônes des recettes étaient décodées **en parallèle**. Le décodeur WebP de Windows se bloque alors lui-même : 20 images en 3,9 s au lieu de 0,09 s une par une. Les fils partagés du programme restaient occupés (jusqu'à 30 fils, 50 tâches en attente) ; la capture et l'affichage du jeu attendaient leur tour. Le défaut existait depuis la v0.3.15, la v0.3.19 l'a rendu visible.
- **Correctifs** : une seule icône décodée à la fois ; la **capture du jeu a son propre fil** (elle ne dépend plus des fils partagés). Mesuré : image du jeu en 0,25 s après « Démarrer », même juste après l'onglet Recettes ; aucune tâche en attente.
- **Écran** (D-048) : position, zone et région ne sont plus lues ; plus de comparaison écran / serveur ni d'image de diagnostic. Reste la cible sélectionnée (aucune lecture de texte sans panneau de cible).
- **Journal des tâches** : ressource et métier seulement pour une récolte (avant : « DeployDeployable « Young Beech Tree » ») ; noms en français pour sortir et ranger un véhicule, etc.
- **Vérifié** : 263 tests ; banc hors écran : 2 sessions de suite, tous les onglets, recherche de recettes pendant la capture, fermeture propre ; aucune erreur. Pas d'essai réel des fonctions serveur (le jeu de Julien était lancé).

## 34. v0.4.0 — R1 : le bot apprend la caméra en regardant Julien (2026-11-19)

Étape R1 du plan de la récolte automatique (`echanges/2026-11-19-plan-recolte-auto.md`, D-049). **Aucune action nouvelle** : seule action sur le jeu, toujours Maj + E (repas auto).

- **Écoute de la souris** (`src/BitcraftBot.Input/Mouse/GlobalMouseHook.cs`) : lecture seule, comme celle du clavier. Elle tourne sur **son propre fil** avec sa boucle de messages : si l'interface du bot est occupée, la souris de Julien ne ralentit pas. Seuls les boutons enfoncés et la molette sont transmis (pas les déplacements). Les clics simulés (le bot lui-même, plus tard) sont marqués et ignorés pour l'apprentissage.
- **État de la caméra** (`src/BitcraftBot.Server/Aim/CameraLearner.cs`) : W (jeu au premier plan) = « recentrée » ; Q, D, molette, bouton du milieu = « tournée » jusqu'au prochain W ; rien vu depuis le démarrage = « inconnue » (les clics comptent, comme indices).
- **Paire apprise** : clic droit de Julien dans la zone utile du jeu → le bot attend que le serveur annonce une **nouvelle** récolte (action « Extract » ou coup sur une autre ressource) dans les 20 s. La paire = pixel cliqué + position de la ressource **par rapport au personnage au moment du clic**. Refusée si : caméra tournée, W il y a moins de 1 s, personnage en mouvement dans la seconde avant le clic, ressource inconnue ou à plus de 40 cases, serveur pas lu. Le coup suivant sur l'arbre déjà coupé ne confirme rien (sinon chaque coup confirmerait n'importe quel clic).
- **Formule de la caméra** (`src/BitcraftBot.Server/Aim/CameraModel.cs`) : position au sol (E, N) → point de l'écran, ramené au centre et divisé par la hauteur de l'image (indépendant de la taille de la fenêtre). Deux formules : « plate » (6 nombres, dès 3 clics) et « perspective » (8 nombres, essayée dès 7 clics, gardée si elle accepte plus de clics ou prédit nettement mieux). **Indices, pas vérité** : on essaie des petits groupes de clics, on garde la formule qui s'accorde avec le plus de clics (écart < 35 px), les autres sont écartés.
- **Précision annoncée = honnête** : pour chaque clic, formule calculée **sans lui**, puis écart avec le vrai clic ; médiane en pixels (dès 5 clics). Chaque nouveau clic appris donne aussi « Formule d'avant : écart N px » (la formule le prévoyait-elle ?).
- **Mémoire** : `camera-clics.json` à côté du .exe (300 clics au plus) ; l'apprentissage continue d'une session à l'autre. Bouton « Oublier les clics ».
- **Carte « Caméra »** (Paramètres) : état (recentrée / tournée : appuyer sur W), clics appris, précision (vert si < 25 px), formule, dernier clic. Journal, catégorie « Caméra » : clic appris en « Info », refus en « Détail ».
- **Vérifié** : 277 tests (`V040Tests` : caméra simulée en perspective, clics imprécis de ±8 px et hauteur variable sur le tronc → précision 10 à 12 px dès 8 à 15 clics ; 3 faux indices sur 17 écartés ; appariement clic / récolte : 8 cas). Banc hors écran (PowerShell) : fenêtre, carte, enregistrement puis relecture des clics, fermeture propre ; écoute de la souris installée et clic droit décodé. Sonde : l'écoute reçoit bien les vrais mouvements de souris de Windows.
- **Écarts notés** : (1) dans l'outil Bash de Claude, l'écoute de la souris ne reçoit rien (isolement du bac à sable) ; dans PowerShell, oui : tester les écoutes via PowerShell. (2) Windows ignore une molette « de 0 cran » : impossible de tester l'écoute sans toucher l'écran de Julien ; pas de vrai clic simulé (Julien utilisait son PC). (3) Pas d'essai réel en jeu : il faut Julien.
- **À vérifier** (Q-024) : le bouton du milieu tourne-t-il la caméra ? W ramène-t-il la caméra d'un coup ou en douceur ? La molette compte comme « caméra tournée ».

## 35. v0.4.1 — ce que voit le bot, 3 s après W, précision « pas fiable » (2026-11-22)

Retour de l'essai R1 (22/11) : voir `analyse-visee-2026-11-22.md` (la formule de la caméra ne marche pas : W ne fixe pas la direction du monde ; clics sur le feuillage). Demandes de Julien : reconnaître bureau / autre programme / jeu, et dans le jeu accueil / connexion / chargement / en jeu.

- **Ce que voit le bot** (`src/BitcraftBot.Perception/Screen/`, `MainWindow.Screen.cs`) : fil à part « Ce que voit le bot », **toujours actif** (même sans « Démarrer »), une fois par seconde.
  - Hors image : fenêtre au premier plan (classe et nom du programme, **jamais le titre** : il peut être privé). Bureau (`Progman`, `WorkerW`, barre des tâches), autre programme, jeu réduit, jeu visible.
  - Dans le jeu (image regardée seulement si le jeu est visible) : **en jeu** = coordonnées « N …, E … » sous la mini-carte (zone étroite d'abord : la zone large attrape la distance « 35m ») ; **chargement** = mot « LOADING » en bas à gauche ; sinon lecture de tout l'écran et mots-clés **supposés** pour connexion (« login », « sign in »…) et accueil (« press any », « click to »…) : À vérifier (Q-025). Écran pas reconnu : « écran inconnu » + texte lu dans le journal.
  - Une situation n'est écrite qu'après 2 lectures identiques (`[Écran] Jeu : écran de chargement (mot « loading » en bas à gauche).`). Affichée dans l'onglet Bot, carte Écran, ligne « Je vois ».
  - Captures gardées dans `images/analyse` : une par écran reconnu (accueil, connexion, chargement) par lancement du bot, et les écrans inconnus (une par minute au plus) : pour corriger la reconnaissance.
- **Sécurité** : le contrôleur d'actions refuse toute action si l'écran est accueil, connexion ou chargement (« le jeu n'est pas prêt »). Écran inconnu : pas de blocage (une fenêtre du jeu peut cacher les coordonnées). La lecture de la cible s'arrête hors du jeu.
- « Jeu introuvable » n'est plus une alerte : la ligne `[Écran]` dit où est Julien.
- **Caméra** (D-050) : 3 s d'attente après W (avant : 1 s). Précision annoncée **« pas fiable »** tant que moins de 8 clics sont gardés ou que plus d'un tiers est écarté (les 17 clics du 22/11 : « pas fiable », test automatique).
- **Vérifié** : 300 tests (`V041Tests` : capture en jeu, écran de chargement réel, image noire, mots-clés, situations, contrôleur). Banc hors écran (PowerShell) sur le vrai bureau : « Autre programme : le bot (jeu fermé) » ; CPU du bot 1,9 s sur 8 s, démarrage compris.
- **Pas vérifié** : accueil et connexion (aucune capture), lecture dans le vrai jeu en direct.

## 36. v0.4.2 — réglages du jeu vérifiés au démarrage (2026-11-22)

Demande de Julien : « lis les settings de mon client, dis-moi ce qu'il faut changer ».

- Réglages trouvés dans le registre (`HKCU\Software\ClockworkLabs\BitCraft`) : voir `technique/reglages-du-jeu.md`. Le même endroit contient le jeton du jeu et l'e-mail de Julien : le bot ne lit qu'une **liste blanche** de 10 réglages (`src/BitcraftBot.Perception/Screen/GameSettings.cs`), **n'écrit jamais**.
- Au démarrage, journal « [Réglages du jeu] » : alerte si un réglage indispensable est faux (langue, échelle, 1920 × 1080, mini-carte orientée nord) ; info pour les 4 réglages conseillés (Q-026).
- Vérifié : 303 tests (`V042GameSettingsTests`, dont une lecture du vrai registre sans aucun « @ ») ; banc hors écran : « 6 réglage(s) sur 10 comme conseillé », les 4 autres signalés.

## 37. v0.4.3 — direction de la caméra, fenêtres du jeu, écran de connexion (2026-11-23)

Retour de l'essai v0.4.2 (22/11, 23:44) : accueil, chargement et « en jeu » reconnus ; connexion pas reconnue ; des « écran inconnu » en jeu. Julien : « 1 s pour W » (D-053), « ok pour 0.4.3 ».

- **Corrections** :
  - Lecture de texte (OCR) : le moteur de Windows ne lit qu'une image à la fois. La lecture du HUD et « ce que voit le bot » se gênaient (« Another RecognizeAsync operation is already running! ») : elles passent maintenant chacune leur tour (`OcrReader`).
  - Écran de connexion : mots vus sur les captures de Julien (« ENTER BITCRAFT », « LOG OUT », « HAVING TROUBLE? », « CHANGE YOUR EMAIL »…). Le texte était coupé à 300 caractères **avant** la recherche des mots : seule la ligne du journal est coupée maintenant. Les mots doivent commencer un mot (« disconnecting » des annonces ne donne plus « connecting » = chargement). Si la petite zone des coordonnées est mal lue (bandeau « ENTERING … »), les coordonnées trouvées dans le texte de tout l'écran suffisent.
  - Après W : 1 s d'attente (D-053).
- **Direction de la caméra** (`Perception/Screen/MinimapCone.cs`) : le personnage (goutte bleue) est toujours au centre de la mini-carte, en (1752, 163) ; le fond est olive uni. Le cône a deux bords blancs à ~100° l'un de l'autre. Le bot garde les pixels clairs et gris autour du personnage (rayon 13 à 38 px) qui ne touchent aucun repère, mesure pour chaque rayon (1°) les pixels blancs alignés, puis cherche les deux bords (écart 86 à 110°) avec le voile entre eux. Cap = milieu des deux bords (0 = nord, 90 = est). Un bord caché par un repère : l'autre bord et le voile suffisent. Rien de net : « pas lue » (mieux vaut rien que faux). La goutte elle-même ne sert pas : sa pointe suit le personnage, pas la caméra.
- **Fenêtres du jeu** (`Perception/Screen/GameWindows.cs`) : grandes zones crème (corps des fenêtres, 233, 223, 196) hors des zones du HUD (panneau de la cible, panneau d'action, mini-carte, icônes du bas), au moins 150 × 150 px ; carte du monde = écran gris uni (45, 45, 45). Titre lu dans le bandeau bleu au-dessus (« INVENTORY »), seulement quand les fenêtres changent.
- **Dans le bot** (`MainWindow.View.cs`) : en jeu, une fois par seconde, carte ÉCRAN : lignes « Caméra » (« vers l'est (80°) ») et « Fenêtres » (« aucune », « « INVENTORY » »). Journal : changement de direction de plus de 20° vu 2 fois (`[Caméra] Caméra vers …`), fenêtres ouvertes ou fermées vues 2 fois (`[Écran] Fenêtre(s) du jeu ouverte(s) : …`).
- **Sécurité (D-052)** : une fenêtre du jeu ouverte bloque toute action du contrôleur (y compris Maj + E).
- **Apprentissage (R1)** : chaque clic appris garde maintenant la direction de la caméra (`camera-clics.json`, champ `Heading`) : base de la formule de la v0.4.4.
- **Vérifié** : 336 tests (`V043Tests` : 5 mini-cartes de référence à 12° près, noms des directions, fenêtres sur inventaire + chariot, banque, carte du monde, aucune fausse fenêtre sur 32 captures de jeu, titre « INVENTORY » lu ; `V041Tests` : accueil, connexion, jeu avec bandeau, lectures simultanées). Planche de 48 mini-cartes vérifiée à l'œil. Banc hors écran : démarrage sans erreur. **Pas vérifié** : en direct dans le jeu (le jeu était fermé).

## 38. v0.4.4 — barre d'action, sprint, sacs au sol, « en jeu » par la mini-carte, contrôle après W (2026-11-23)

Retour de la session de jeu du 23/11 (18:54 à 20:05 : chasse aux Sagi Birds, 19 coupes, banque). Demandes de Julien : sprint (état, recharge, touche 4), objets au sol (à qui, verrouillés ou non), barre d'action, cône pas mis à jour après W.

- **Barre d'action et capacités** (`Server/World/ActionBar.cs`, tables publiques `action_bar_state` et `ability_state`) : chaque case (barre, case 0 = touche 1) pointe vers une capacité : manger, attaque, attaque automatique, capacité spéciale (`ability_custom_desc` : Sprint, Hewing Strikes…), équiper, ceinture… Temps de recharge : début (ms) + durée (s), code serveur `AbilityState::is_under_cooldown`. Noms tirés des données du jeu (`ability_custom_desc`, `combat_action_desc`, téléchargées en plus).
- **Sprint** : capacité spéciale « Sprint » (594765058, recharge 120 s) qui donne le bonus « Sprint » (1845851314, 15 s, +40 % de vitesse). Le bot dit « prêt », « actif (10 s) » ou « en recharge (85 s) » et sur quelle touche il est. **Il ne le lance pas encore** : ce sera pendant les trajets (H3).
- **Sacs au sol** (`Server/World/GroundBags.cs`, table publique `dropped_inventory_state`) : objets qui ne rentraient pas dans le sac (butin d'un animal, tronc avec le sac plein…). Propriétaire : Julien, un autre joueur (verrouillé), ou personne (libre). Minuteries `dropped_inventory_ownership_timer` (quand le sac devient libre pour tous) et `dropped_inventory_despawn_timer` (quand il disparaît). Contenu : inventaire dont le propriétaire est le sac. Les animaux tués ne laissent pas de cadavre côté serveur : le butin va dans le sac, le surplus tombe au sol dans un sac à Julien (code serveur `enemy_spawn_loot`).
- **Abonnements** : ces requêtes sont abandonnées en premier si le serveur les refuse (« sans la barre d'action ni les sacs au sol »). Un extrait brut de chaque table est écrit une fois dans le journal (format à vérifier en réel).
- **Écran** : « en jeu » aussi quand le personnage (goutte bleue) est au centre de la mini-carte. Les 30 « écran inconnu » du 23/11 : 24 reconnus ; les autres ont le personnage caché (chariot, flèches de visée d'un combat) et passent par la lecture de tout l'écran.
- **Après W** : cap du cône relu 1,5 s après W, écrit dans le journal avec une capture du jeu (une toutes les 30 s au plus, `images/analyse/*_apres-w_*.png`), pour vérifier l'observation de Julien.
- **Interface** : onglet Bot, cartes « SPRINT ET BARRE D'ACTION » et « SACS AU SOL ».
- **Vérifié** : 352 tests (`V044Tests` : barre lue sous deux formes, recharge, états du sprint, touches, sacs à moi / libre / à un autre avec minuterie, requêtes en lecture seule, personnage au centre sur 6 captures). **Pas vérifié** : en direct (le bot v0.4.3 tournait encore pendant le travail).

## 39. v0.4.5 — réponses « @GPT » au clavier, chat ouvert reconnu (2026-11-23)

- Abonnement : `chat_message_state`, canal Région, 5 dernières minutes (dans le groupe « extras » avec la barre d'action et les sacs au sol).
- Lecture seule du serveur, comme toujours (D-039) : la réponse part **au clavier, dans le jeu**. Détail : `technique/chat-gpt.md` section 6.
- Écran : `ChatInput` (zone de saisie du chat) dans la vue du jeu, une fois par seconde. Chat ouvert : aucune action du bot ; W, Q, D ne comptent plus pour la caméra. Entre deux regards, Entrée et Échap tapées par Julien font foi.
- Le seul « W » vu en v0.4.4 (22:32:17) avait été tapé dans le chat : il ne dit rien sur la caméra. Vrai essai de W à faire.

## 40. v0.4.11 — onglet Voyageurs (session tâches, 2026-11-24)

- Données : `GameData/TravelerTaskBook` lit `traveler_task_desc` et `npc_desc` (ajoutées aux tables téléchargées).
- Serveur : abonnement à `traveler_task_state` du personnage ; refusé, le bot réessaie sans (premier essai de la chaîne de repli). `World/TravelerTasks` : lecture et comparaison au stock (`TravelerTaskPlanner`) : objets comparés par numéro et type (un autre palier n'est jamais confondu), stock réparti une seule fois entre les tâches (sac d'abord, dans l'ordre des tâches).
- Verdicts : faisable maintenant (tout dans le sac), à aller chercher (ceinture ou banque, avec l'endroit), partiel (manque exact), impossible, déjà rendue.
- Onglet VOYAGEURS (après PERSONNAGE), redessiné toutes les 2 s s'il est affiché et si quelque chose a changé.
- Correction : les onglets sont repérés par leur nom, plus par un numéro fixe (l'onglet CHAT de la v0.4.10 avait décalé PERSONNAGE et CARTE).

