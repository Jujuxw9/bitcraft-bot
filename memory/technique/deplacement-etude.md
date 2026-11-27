# Déplacement automatique : étude (T-09, 27/11/2026)

Étude écrite la nuit, sans Julien et sans essai en jeu. Base de T-10 (v0.4.29 « Se déplacer ici »), T-11 (essai réel), T-14 (aller à une ressource). Sources : demande de Julien (`echanges/2026-11-26-demande-deplacement-carte.md`), D-072, D-074, règles de la nuit §3, code serveur officiel (`references/BitCraftPublic/`), code du bot. Chaque section : **Ce qu'on sait / À vérifier / Recommandation**.

## 0. Trois faits trouvés cette nuit (ils changent le plan)
1. **Le chemin est calculé par le jeu sur le PC, pas par le serveur.** Le serveur ne fait que vérifier chaque petit pas envoyé par le jeu (`player_move` : point de départ, point d'arrivée, durée). Le seul « chercheur de chemin » du serveur sert à créer le monde. Donc : ce que fait « Set destination » (contourner ou non) ne se lit pas dans le code serveur → **À vérifier en jeu (T-11)**.
2. **La « destination » lue sur le serveur (`mobile_entity_state.destination_x/z`) est la fin du pas en cours**, pas forcément le point cliqué sur la carte. Si le jeu découpe le trajet, elle change à chaque pas. **À vérifier** : en ligne droite, le jeu envoie-t-il un seul pas jusqu'au bout ?
3. **Le jeu lit les touches par leur position physique** (registre du jeu, réglages de Julien : Interact = `/Keyboard/semicolon` = la touche M en AZERTY). La touche « . » de la carte n'est pas redéfinie par Julien : sa position physique exacte est **À vérifier** (voir §1).

Autre point : **les touches `-` et `=` sont aujourd'hui permises** par le contrôleur (cases 11 et 12 de la barre d'action, v0.4.17). Or ce sont le chariot et le bateau (interdits, règles §3). T-10 doit les interdire (§6).

## 1. Placer le clic sur la grande carte

**Ce qu'on sait**
- La carte s'ouvre **centrée sur le personnage**, au **dernier zoom** utilisé (Julien, 26/11). Un **clic gauche** = le personnage y va. Fermeture sûre : « . » (Échap : pas sûr, à ne pas utiliser).
- Barre du bas : `Keldan (R18) | (8918, 6700)` = coordonnées **sous la souris** (capture `images/analyse/20261116_analyse_carte-barre-coordonnees_001.png`). Texte bleu foncé sur fond clair, bien lisible.
- Le bot sait reconnaître la carte ouverte (`GameWindows.IsWorldMap` : fond gris uni) et lire du texte (`OcrReader`, Windows OCR).
- Le bot a déjà les gestes souris sûrs : `ActionController.MoveAsync` (souris seule, T-05) et `ClickAsync`, avec `HumanMouse`.

**À vérifier**
- Ordre des deux nombres de la barre : (N, E) ou (E, N) ? Julien était à N 9041, E 6808 près de Keldan : `(8918, 6700)` fait penser à (N, E), sans certitude. La calibration le tranche toute seule (voir méthode).
- Mêmes unités que la position du serveur (`PlayerSnapshot`, en grandes cases) ? Très probable (Julien lit les mêmes nombres en jeu). La calibration le vérifie : centre de la carte ≈ position du serveur.
- Touche d'ouverture : candidate 1 = **scancode 0x34** (position du « . » QWERTY, la touche « : » en AZERTY) ; candidate 2 = **0x53** (« . » du pavé numérique). Chaque essai est suivi d'une capture : carte ouverte ou non.
- `IsWorldMap` a été réglé sur des zones non explorées (gris) : peut rater si la carte est zoomée sur une zone toute verte. Plus sûr : **lire « Set destination » dans la barre du bas**.
- Le clic dans la barre du bas ou le panneau « Map Options » ne doit jamais arriver : zones à exclure.

**Méthode recommandée (calibration puis visée)**
1. Ouvrir la carte, attendre 600 ms, capture : la barre du bas doit contenir « Set destination ». Sinon : refermer avec la même touche si une fenêtre s'est ouverte, abandon « carte non ouverte ».
2. Souris au **centre** de la zone de carte (écran principal) avec `MoveAsync`, attendre 150 ms, lire la barre → `(a0, b0)`. Doit être à moins de 3 cases de la position du serveur, sinon arrêt (unités ou ordre faux).
3. Souris **200 px à droite**, lire `(a1, b1)` ; puis **200 px vers le bas**, lire `(a2, b2)`. Le nombre qui change vers la droite = E (doit croître) ; vers le bas = N (doit décroître). Échelle = cases par pixel, dans les deux sens (doivent être égales à 15 % près, sinon arrêt).
4. Si l'échelle est **> 0,5 case par pixel** (trop dézoomé : un pixel de travers = une demi-case) ou **< 0,01** (trop zoomé : la cible sort de l'écran), arrêt « zoom de la carte à régler à la main ». **Pas de molette par le bot en v1** (le jeu garde le zoom : on ne veut pas le dérégler pour Julien).
5. Calculer le point visé, poser la souris, lire la barre. Corriger avec l'écart mesuré (**3 lectures au plus**). Accepter si l'écart est **≤ 1 case** dans les deux sens.
6. Cliquer (clic gauche, sans bouger), attendre 300 ms, **fermer avec la même touche**, capture : carte fermée.
- Précision attendue : 1 case (la barre donne des nombres entiers). Lectures : 3 pour l'échelle + 1 à 3 pour viser = **4 à 6 lectures**, ≈ 2 à 3 s carte ouverte.
- Cible hors de l'écran de la carte (trop loin pour ce zoom) : refus « trop loin pour le zoom actuel » (pas de glisser-déplacer en v1).
- **OCR qui échoue** : 2 nouvelles lectures (seuil différent) ; toujours rien → fermer la carte, ne rien cliquer, journal « barre de la carte illisible » avec capture de la barre.
- L'OCR peut lire `8918` comme `89l8` : n'accepter que des nombres, deux nombres entre 0 et 30 000 (garde-fou), et un écart cohérent avec la lecture précédente (< 2 × la distance attendue).

## 2. Vérifier que le personnage y va

**Ce qu'on sait**
- Serveur : `mobile_entity_state` donne la position, la « destination » (fin du pas en cours, §0) et `is_walking` ; déjà lus (`PlayerSnapshot.Destination`, `Walking`). Le serveur refuse un pas qui saute plus de 6 de hauteur à la frontière d'une grande case.
- Vitesse de marche : paramètre `parameters_player_move_desc.default_speed` (non lu par le bot ; **À vérifier** sa valeur), plus lente dans l'eau.

**À vérifier (T-11)** : la destination lue vaut-elle le point cliqué ? Combien de pas le jeu envoie-t-il pour 5 cases ? Vitesse réelle en cases par seconde.

**Recommandation**
- **Départ confirmé** si, dans les **3 s** après le clic, la destination change ET elle est **plus proche de la cible** que la position (ou `is_walking` = vrai et la position bouge vers la cible). Écart destination / cible noté au journal à chaque fois (pour T-12), **sans en faire une condition** tant que §0-2 n'est pas vérifié.
- Rien en 3 s : un seul nouvel essai (rouvrir la carte, re-viser). Deuxième échec : arrêt « le jeu n'a pas accepté la destination ».
- **Suivi** : toutes les 500 ms, distance restante (serveur). **Arrivée** = distance **≤ 1,5 case** et personnage immobile 1 s.
- **Temps maximum** = 10 s + 2 s par case (marge large tant que la vitesse n'est pas mesurée). Dépassé : arrêt « trajet trop long ».
- **Blocage** : distance qui ne baisse pas de 0,5 case en 5 s → arrêt « bloqué » (obstacle, falaise). Pas de nouvel essai automatique en v1.
- **Distance maximale en v1 : 30 cases** (petits trajets, demande de Julien). Au-delà : refus.

## 3. Terrain : eau et marches trop hautes

**Ce qu'on sait (code serveur)**
- Hauteur d'une grande case pour le joueur = sol, ou **niveau de l'eau** si l'eau fait **≥ 5** de profondeur (`PLAYER_MIN_SWIM_DEPTH` = 5 : le personnage nage).
- Escalade (`player_climb.rs`) : un écart de hauteur entre deux cases voisines de **5 à 7** = Hoist1, **8 à 10** = Hoist2, **≥ 11** = grimper ; vers le bas pareil (-5 à -10 = descente, ≤ -11 = descente grimpée). **Donc |écart| ≤ 4 = marche normale ; ≥ 5 = escalade.** L'escalade coûte de l'endurance et demande un niveau.
- Le bot garde déjà le relief : `TerrainStore` (morceaux de 32 × 32 grandes cases, `Elevations` et `WaterLevels`, gardés sur le disque, 77 morceaux près de Keldan).
- Le serveur ne calcule aucun chemin pour le joueur (§0-1).

**À vérifier** : « Set destination » contourne-t-il une falaise ou un lac, ou va-t-il tout droit et s'arrête ? Le jeu fait-il grimper ou nager tout seul ? Les bâtiments et arbres (obstacles) ne sont pas dans le relief.

**Recommandation pour la v1 : refuser les trajets douteux.**
- Nouvelle classe pure (sans écran) `PathCheck` : parcourt la **ligne droite** du personnage à la cible, case par case (grandes cases, pas de 0,5 case), et refuse si :
  - une case du trajet n'est pas dans `TerrainStore` (relief inconnu) → « relief inconnu » ;
  - deux cases voisines du trajet ont un écart de hauteur (règle du joueur ci-dessus) **≥ 5** → « escalade nécessaire » (avec la case) ;
  - une case a de l'eau **≥ 5** de profondeur → « eau profonde (nage) » ; eau de 1 à 4 : permis, noté au journal ;
  - la cible elle-même est dans l'eau profonde ou sur une case plus haute de 5 que ses voisines.
- Marge : vérifier aussi les deux lignes parallèles à **± 1 case** (le personnage dévie un peu). Si une seule des trois passe : refuser quand même en v1.
- Plus tard (pas en v1) : petit chercheur de chemin (A*) sur les grandes cases, puis plusieurs destinations courtes en ligne droite.

## 4. Ennemis

**Ce qu'on sait (code serveur, tables publiques)**
- `enemy_state` (type d'ennemi, troupeau) ; leur position est dans `mobile_entity_state` (comme le joueur).
- `threat_state` (propriétaire, cible, menace) : une ligne quand un ennemi est **en combat** avec quelqu'un. Lignes **ciblant le personnage** = il est attaqué ou visé.
- `health_state` du joueur (déjà lu) : `health`, `last_health_decrease_timestamp` (vie qui baisse).
- `attack_outcome_state` (dégâts reçus), `target_state` des ennemis.
- Le bot ne lit **aucune** table d'ennemis aujourd'hui.
- Julien : bonus « Nearby Flame » près d'un feu (tient les ennemis à distance) ; ennemis aux alentours de sa position actuelle.

**À vérifier** : requête permise par le serveur (abonnement « enemy_state JOIN mobile_entity_state » limité à la zone lue, comme les ressources) ; taille des données ; distance d'attaque des ennemis courants.

**Recommandation**
- T-10 ajoute à l'abonnement : `threat_state WHERE target_entity_id = <joueur>` (petit, sûr). Les positions des ennemis de la zone : seulement si l'abonnement est accepté au premier essai sans le jeu (sinon noter « Abonnement refusé » et s'en passer) ; **aucun « Abonnement refusé » ne doit rester** (règles §2).
- **Danger** = une ligne `threat_state` vers le joueur, **ou** vie qui baisse (`last_health_decrease_timestamp` récent), **ou** ennemi à **< 8 cases** (si positions lues).
- **Réaction** (D-072, règles §3) : arrêter le trajet ; **Sprint (touche 4)** ; nouvelle destination **à l'opposé de l'ennemi** (ou du dernier point de départ si l'ennemi est inconnu), 10 cases, en passant par `PathCheck` (si refusée : opposé ± 45°). Journal « Fuite : … ».
- **Reprise** : plus de menace depuis **20 s** et vie qui ne baisse plus → le bot **ne reprend pas tout seul en v1** : il s'arrête et l'écrit (Julien relance). Plus tard : reprise auto.
- Jamais d'attaque, jamais de capacité de combat.

## 5. À terre : Get Up, jamais Go Home

**Ce qu'on sait (code serveur)**
- « À terre » = **vie à 0** (`health_state.health <= 0`, `died_timestamp` rempli). Le serveur lance l'action « Death » (`player_action_state`, type Death) ; le joueur **perd son cargo** (case cargo de l'inventaire posée au sol) ; ennemis et menaces effacés.
- `player_respawn(teleport_home)` : **Get Up** = `false` → relevé **sur place avec 10 de vie**, endurance à 1, bonus « Inner Light » (les ennemis l'ignorent un moment). **Go Home** = `true` → téléporté chez lui, vie pleine. Possible seulement après `respawn_seconds` (valeur **À vérifier**).
- Le bot n'envoie jamais `player_respawn` (lecture seule, D-039) : il **clique** le bouton à l'écran.

**À vérifier** : position et texte exacts des deux boutons (aucune capture). Ordre gauche / droite.

**Recommandation**
- Détection : vie lue sur le serveur **= 0** → arrêt de tout trajet, mode « à terre ».
- Toutes les secondes : capture, **OCR** de la zone centrale ; trouver le mot « Get Up » (et « Go Home » ailleurs). Cliquer **seulement** dans le rectangle du texte « Get Up », et **seulement** si « Go Home » est lu **ailleurs** (à plus de 100 px) : on sait alors qu'on ne vise pas le mauvais. Sinon, ne rien cliquer.
- Après le clic : vie > 0 sur le serveur en 5 s = réussi ; position du serveur **inchangée** (< 3 cases) = bien Get Up. Si la position a sauté : alerte « Go Home cliqué par erreur ? » + arrêt.
- Puis s'éloigner (§4) avec 10 de vie. Le cargo reste au sol : journal + arrêt pour Julien.
- Deux échecs : arrêt, captures gardées.

## 6. Sécurité : quand tout arrêter

**Déjà dans le bot** (`ActionController`) : arrêt si Julien bouge la souris (> 4 px) ou tape au clavier (3 s de calme exigées), jeu pas au premier plan, écran pas prêt (chargement, connexion), seules les touches de la liste `GameKeys.Allowed` partent ; `Halt` / `Resume`.

**Recommandation pour T-10**
- **Touches interdites, quoi qu'il arrive** : nouvelle liste `GameKeys.Forbidden` = `-` (0x0C), `=` (0x0D), et toute combinaison Maj + V / Maj + B ; vérifiée **avant** `Allowed` dans le contrôleur (même si une capacité est mise sur ces cases). `Échap` jamais envoyé. Test automatique pour chacune.
- Nouvelles touches permises : la touche de la carte (une seule, celle trouvée en T-11) et le Sprint (4, déjà dans la barre).
- Arrêt du trajet si : souris ou clavier de Julien ; fenêtre du jeu ouverte autre que la carte (inventaire, chat, banque…) ; chat ouvert ; écran inconnu ou de chargement ; vie qui baisse ou menace (§4) ; vie à 0 (§5) ; perte de la liaison serveur (plus de mise à jour depuis 5 s) ; personnage monté (`mounting_state`, chariot) ; temps maximum ou blocage (§2) ; bouton « Arrêter » du bot.
- Le clic sur la carte se fait **seulement** dans la zone de carte (hors barre du bas, hors panneau « Map Options » à droite, hors 40 px des bords).
- Toujours **une seule** action de déplacement à la fois ; aucune reprise automatique en v1.
- Tout est écrit au journal (catégorie « Déplacement »), avec captures **de la fenêtre du jeu** seulement aux étapes clés.

## 7. Mode d'essai pour T-11 : `--essai-deplacement`

Option en ligne de commande (comme `--essai-carte`), qui n'agit **que** si le jeu est en jeu (écran « en jeu » reconnu) et que Julien n'a pas touché la souris depuis 3 s.

**Ce qu'elle fait, dans l'ordre** (tout au journal, captures dans `captures-essai/deplacement/`)
1. Attendre la liaison serveur et la position du joueur. Capture du haut de l'écran (**cible**, pour T-13).
2. **Touche de la carte** : essayer 0x34 ; capture ; carte ouverte ? sinon 0x53 ; capture. Noter laquelle marche. Aucune → arrêt.
3. **Lire l'échelle** (§1, étapes 2-4) ; noter ordre des nombres, unités, échelle. Fermer la carte avec la même touche ; capture : fermée ?
4. Choisir une cible à **5 cases** dans une des 8 directions qui passe `PathCheck` (essayer N, E, S, O, puis diagonales). Aucune → noter et sauter à 7.
5. Aller à la cible (§1-2) ; à chaque pas, noter position, destination, `is_walking`, temps. Noter le nombre de destinations différentes vues (réponse à §0-2), la vitesse mesurée.
6. **Revenir au point de départ** de la même façon.
7. Capture finale, journal « Essai déplacement fini : … », **rendre la main** (le bot reste ouvert, aucune autre action).

**Ce qu'elle ne fait jamais** : molette, glisser sur la carte, Échap, `-`, `=`, Maj + V, Maj + B, escalade, nage, trajet de plus de 5 cases, plus de **2 aller-retour**, plus de **3 min** en tout ; rien si danger (§4) sauf la fuite ; capture de l'écran entier (seulement la fenêtre du jeu).

## 8. Plus tard (liste)
- **T-14** : « Aller à cette ressource » : viser une case **à côté** de la ressource (pas dessus), côté personnage, puis souris sur la ressource (contour vert) + M ; « Récolter jusqu'à épuisement » = reprise des récoltes (v0.4.25) jusqu'à la disparition de la ressource sur le serveur.
- Chemins longs : chercheur de chemin sur le relief gardé, plusieurs étapes courtes ; glisser la carte si la cible sort de l'écran.
- Banque (palier 0.5) : aller à la banque de la colonie, fermer la carte avant le Transfer (D-030).
- Exploration automatique (D-072 : sessions de 30 min à 4 h, onglet Exploration).
- Reprise automatique après une fuite.

## Plan précis pour T-10 (v0.4.29)
**Classes nouvelles** (projet entre parenthèses)
- `MapScale` (Perception ou Server) : lit `(a, b)` dans le texte de la barre, calcule ordre E/N et échelle à partir de 3 lectures, point écran → case et case → point écran. Pur, testé.
- `PathCheck` (Server/World) : ligne droite + 2 parallèles sur `TerrainStore`, règles du §3, rend « permis » ou la raison du refus et la case. Pur, testé.
- `TravelGuard` (Server/World) : danger (menace, vie qui baisse, vie 0), arrivée, blocage, temps maximum à partir des instantanés du joueur. Pur, testé avec des instantanés faits à la main.
- `MapTravel` (App, `MainWindow.Travel.cs`) : enchaîne ouvrir carte → calibrer → viser → cliquer → fermer → suivre, via `ActionController` (`MoveAsync`, `ClickAsync`, `PressAsync`). Une seule à la fois.
- `GameKeys.Forbidden` + contrôle dans `ActionController` (avant `Allowed`).
- Menu sur la carte du bot : brancher « Se déplacer ici » sur `ResourceMap.CellRightClicked(east, north)` (déjà là, T-08). Avant de lancer : `PathCheck` + distance ≤ 30 ; refus affiché et écrit.
- Abonnement : `threat_state` vers le joueur (et, si accepté, ennemis de la zone).
- Option `--essai-deplacement` (§7).

**Tests à écrire** : lecture de la barre (« Keldan (R18) | (8918, 6700) », texte abîmé, nombres hors bornes) ; échelle (droite / bas, ordre inversé, zoom trop fort ou trop faible) ; visée corrigée ; `PathCheck` (plat permis, marche de 5 refusée, marche de 4 permise, eau 5 refusée, eau 4 permise, relief inconnu refusé, cible dans l'eau) ; `TravelGuard` (arrivée, blocage, temps dépassé, menace, vie 0) ; touches interdites (`-`, `=`, Maj + V, Maj + B refusés même si permis) ; menu absent si la distance > 30.

**Essai sans le jeu (T-10)** : seulement le menu et les refus (le jeu est fermé : aucune touche ne part ; le contrôleur doit répondre « jeu absent »), et aucun « Abonnement refusé ».

## Questions pour Julien
1. La touche « . » de la carte : celle du **pavé numérique**, ou Maj + « ; » du clavier principal ? (Le jeu lit la position des touches : ça change le code envoyé.)
2. Quand le bot fuit un ennemi, doit-il **reprendre** le trajet tout seul après 20 s de calme, ou s'arrêter et t'attendre (choix de la v1) ?
3. Nager dans l'eau peu profonde (1 à 4) : permis ? Et l'eau profonde (nage) : toujours refusée ?
4. Distance maximale de « Se déplacer ici » pour commencer : 30 cases te va ?
5. Si tu meurs malgré tout : après Get Up, le bot doit-il aller **ramasser le cargo** tombé au sol, ou s'arrêter ?
