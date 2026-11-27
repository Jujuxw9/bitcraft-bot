# Bilan de la nuit du 27/11/2026 (tâches T-05 à T-16)

Fichier unique, rempli par chaque tâche à la fin de son passage (règles : `nuit-2026-11-27-regles.md`). Julien le lit au réveil ; T-16 le résume en tête.

## Résumé pour Julien (écrit par T-16)

**En bref** : 11 tâches sur 12 ont fait leur travail. 9 versions sorties (v0.4.25 à v0.4.33). Tout passe les tests (756/756) et chaque version a démarré sans erreur, **jeu fermé**. Mais **rien n'a pu être essayé dans le jeu** : l'essai de 07:00 (T-11) est resté bloqué sur l'écran d'accueil du jeu.

### Quelle version lancer
`artifacts\release\BitcraftBot-v0.4.33.exe` (la dernière, elle contient tout).

### Ce qui a été fait cette nuit
- **v0.4.25** : reprise des récoltes à la souris. Le bot pose la souris sur ton dernier clic, puis M. Plus de Tab. Overlay plus étroit.
- **v0.4.26** : onglet RECETTES retiré (il vit sur le site). Le reste est gardé.
- **v0.4.27** : carte jusqu'à 200 cases. Les zones noires venaient de morceaux jamais demandés : corrigé. Ressources vues gardées 72 h.
- **v0.4.28** : carte grande, tableau plus étroit, pas de gel à 200 cases.
- **v0.4.29 et v0.4.30** : clic droit sur la carte du bot → « **Se déplacer ici** » (30 cases au plus, sans escalade ni nage). Chariot et bateau interdits. Vie qui baisse = Sprint et fuite. À terre = Get Up seulement.
- **v0.4.31** : la cible en haut de l'écran est de nouveau lue. Barre de fabrication jusqu'à 100 %. Conseil si tu mines ou pêches sans capacités.
- **v0.4.32** : clic droit sur une ressource → « **Aller à cette ressource** » ou « **Récolter jusqu'à épuisement** ». Tu donnes le premier coup, le bot continue.
- **v0.4.33** : contrôle du code, aucun changement de comportement.
- T-09 : étude écrite du déplacement. T-16 : contrôle de la mémoire et ce résumé.

### Ce qui n'a pas marché
- **T-11 (essai en jeu)** : le bot a lancé le jeu, mais le jeu attend « Press any button » puis « Enter BitCraft ». Une tâche planifiée n'a pas le droit de cliquer à l'écran, et le bot ne fait pas ces clics (règle D-052). Jeu ouvert 40 s, refermé. Ton personnage n'a pas bougé.
- Donc **le déplacement, la cible, la reprise à la souris n'ont jamais été vus dans le vrai jeu.** Les premiers essais seront avec toi.

### À essayer, dans l'ordre (avec la v0.4.33)
1. Entrer en jeu, terrain plat, **sans ennemi proche**. Onglet CARTE : clic droit sur une case à 5-10 cases → « Se déplacer ici ». Puis **Alt + Tab** vers le jeu et ne plus toucher souris ni clavier. Le personnage doit y aller. Si la grande carte du jeu reste ouverte : la refermer avec la même touche.
2. Clic droit sur un arbre à 5-15 cases → « Récolter jusqu'à épuisement », Alt + Tab. À l'arrivée, **un clic sur l'arbre**. Laisser l'endurance tomber : le bot doit relancer jusqu'à ce que l'arbre soit épuisé.
3. Couper un gros arbre toi-même, **souris immobile après ton clic**, endurance à 0 deux fois : la reprise doit relancer seule.
4. Regarder la cible en haut de l'écran (onglet CARTE, « Cible écran »).
5. Une petite fabrication : la barre doit finir à 100 % (« terminée »).
6. Dézoomer la carte jusqu'à 200 près de Keldan : plus de noir là où tu es passé, pas de gel.
7. Miner 1 min sans Heavy Swings : une ligne « [Conseil] » à la fin.
8. Regarder l'overlay (plus étroit) et les onglets PERSONNAGE et VOYAGEURS (images, tiers).
Après chaque essai, Claude lit le journal (`artifacts/release/journaux/v0.4.33/`).

### Toutes tes questions (regroupées)
**Déplacement** (fiche Q-029) :
1. Touche de la grande carte : « . » du pavé numérique, ou Maj + « ; » ?
2. Après une fuite : reprendre le trajet seul, ou s'arrêter ? (aujourd'hui : s'arrêter)
3. Eau peu profonde (1 à 4) permise ?
4. 30 cases au plus pour commencer : d'accord ?
5. Après un Get Up : aller ramasser le cargo tombé, ou s'arrêter ? (aujourd'hui : s'arrêter)
6. Interrupteur « Déplacements » allumé d'office : d'accord ?
7. Départ : attendre ton Alt + Tab (20 s), ou le bot met lui-même le jeu devant ?
8. Arrêt à 2 cases de la ressource : assez près pour les gros arbres ? Premier coup automatique, à essayer ensemble ? Allumer la reprise des récoltes d'office pour « Récolter jusqu'à épuisement » ?

**Démarrage du jeu** (fiche Q-028) :
9. Le bot peut-il faire seul les 2 clics de démarrage (accueil, « Enter BitCraft ») quand c'est lui qui a lancé le jeu ? Sinon : permettre l'écran du jeu aux tâches planifiées ?

**Reprise des récoltes** :
10. La souris qui se pose seule sur l'arbre te gêne-t-elle ?
11. Si le contour vert est bien mesuré : en faire une condition (pas de M sans contour) ?

**Carte** :
12. Le gris-bleu des zones inconnues te va ?
13. À 100 cases, des carrés plus sombres alignés : tu les vois aussi ?
14. Carte limitée par la hauteur : ça te va, ou réduire la bande des filtres ?
15. Essayer ensemble de ne lire les colonies et banques qu'une fois (≈ 360 Ko de moins à chaque relecture) ?

**Conseil en fin de récolte** :
16. Conseil aussi pour le bois (Hewing Strikes) ? Durée minimale de 20 s : ça te va ?

### Quota de la nuit (abonnement Pro)
- Premier relevé (00:50) : 5 h 7 %, semaine 29 %. Dernier relevé (T-16, 12:01) : 5 h 7 %, semaine 41 %.
- La nuit a pris **environ 12 points de la semaine**. La limite de 5 h n'a jamais dépassé 50 % (plus haut : 50 % à 05:04). Aucune tâche arrêtée pour le quota.

## Point de départ (session principale, 27/11 vers 00:50)
- Dernière version : **v0.4.24** (D-080), publiée, tests 640/640.
- Essai de Julien (journal `artifacts/release/journaux/v0.4.24/2026-11-27/`) : la reprise des récoltes marche 1 fois sur 4. L'arbre **reste sélectionné** quand l'endurance tombe (Tab inutile) ; **M = clic sur l'élément survolé** (contour vert) : sans la souris sur l'arbre, M ne fait rien. Défaut : compteur d'échecs pas remis à zéro (abandon après 1 essai à 00:29).
- Carte v0.4.23 : validée par Julien ; 51 morceaux de terrain gardés dans `artifacts/release/carte/`. Le bot ne lit plus la cible en haut de l'écran (« cible aucune »).
- Quota au départ : 5 h 7 %, semaine 29 % (abonnement Pro).
- **Situation du personnage (Julien, 27/11 vers 00:55)** : à **N 9041, E 6808**, dans la colonie d'une autre guilde (il y a une banque ; sa guilde à lui est à Riverwood). **Inventaire vide.** Chariot et bateau rangés. Bonus « **Nearby Flame** » : un feu proche tient les ennemis à distance ; **il y a des ennemis aux alentours**. Jeu et bot fermés, app Claude ouverte, modèle Opus 5.5, tâches en mode Auto. Julien rentre vers 15:00. **Deux écrans** : jeu sur l'écran principal, bot sur l'écran secondaire. Démarrage du jeu : attendre ≥ 15 s, clic dans la fenêtre BitCraft (« continuer »), clic sur le bouton de connexion, chargement, en jeu (détail : fiche T-11).

## Modèle d'une section (à recopier)
```
## T-NN — sujet (vX.Y.Z)
- Début / fin : HH:MM / HH:MM · quota début : 5 h X %, semaine Y % · fin : 5 h X %, semaine Y %
- Fait :
- Testé (comment) :
- Pas fait / reporté :
- Problèmes rencontrés :
- À essayer par Julien :
- Questions pour Julien :
- Consignes pour les tâches suivantes :
```

## Sections des tâches

## T-05 — overlay plus étroit, reprise des récoltes à la souris (v0.4.25)
- Début / fin : 01:00 / 01:10 · quota début : 5 h 15 %, semaine 30 % · fin : 5 h 21 %, semaine 31 %
- Fait : Tab retiré de la reprise ; le bot retient ton dernier clic (gauche ou droit) dans le jeu, avec ta position et l'état de la caméra ; pour reprendre il pose la souris sur ce point (sans cliquer), attend 150 ms, mesure le contour vert (journal seulement), puis M ; le serveur confirme. Point refusé si tu as bougé (plus d'une demi-case) ou tourné la caméra (molette, bouton du milieu, Q, D, W) : rien n'est appuyé, état « reprise impossible : cliquez une fois sur l'arbre ». Chaque relance faite par toi met le point à jour. Compteur d'échecs remis à zéro (défaut de 00:29). Overlay : police et marges réduites, largeur ≤ 300, ligne « Bot 1,5 % · 585 Mo │ Jeu 22 % · 4,3 Go ». D-082, architecture-suite §29.
- Testé (comment) : build 0 avertissement, tests 649/649 (10 nouveaux dans `V0425Tests` : point valable / caméra tournée / personnage déplacé / clic trop ancien, pas de Tab, compteur remis à zéro au cas de 00:29, geste souris sans clic, longueur de la ligne système). v0.4.25 publiée ; essai sans le jeu 50 s (01:07) : aucun « Abonnement refusé », aucune erreur, « Reprise des récoltes activée » au journal.
- Pas fait / reporté : mesure du contour vert pas vérifiée sur une vraie image (jeu fermé) : « À vérifier ». Overlay pas vu à l'écran (il ne s'affiche qu'avec le jeu).
- Problèmes rencontrés : aucun.
- À essayer par Julien : couper un gros arbre **sans toucher la souris après ton clic**, laisser l'endurance tomber deux fois ; dans le journal, catégorie « Reprise » : « Point de la souris retenu », « Contour vert autour du point … », puis « Récolte … reprise (confirmé par le serveur) ». Regarder la largeur de l'overlay.
- Questions pour Julien : 1) la souris qui se pose seule sur l'arbre te gêne-t-elle quand tu es là ? 2) si le contour vert est bien mesuré, faut-il en faire une condition (pas de M sans contour) ?
- Consignes pour les tâches suivantes : nouveau geste `ActionController.MoveAsync` (souris seule, mêmes sécurités) utilisable pour « Se déplacer ici » (T-10). `_cameraMoves` (MainWindow.Resume.cs) compte les mouvements de caméra. T-11 peut essayer la reprise si un arbre est à portée (cliquer l'arbre, laisser tomber l'endurance).

## T-06 — onglet Recettes retiré (v0.4.26)
- Début / fin : 02:00 / 02:06 · quota début : 5 h 23 %, semaine 31 % · fin : 5 h 27 %, semaine 31 %
- Fait : carte des dépendances (journal T-06) ; page RECETTES et dessin de la fiche (`RecipeView`) retirés ; gardés car utilisés ailleurs : livre des recettes (noms, tiers, barre de fabrication, voyageurs), planificateur et matières premières (Voyageurs), images des objets et leur téléchargement, toutes les données du jeu téléchargées. `MainWindow.Recipes.cs` → `MainWindow.ItemData.cs`. D-083, architecture-suite §30, `architecture.md` (6 onglets).
- Testé (comment) : build 0 avertissement, tests 653/653 (4 nouveaux dans `V0426Tests` : onglet absent, autres onglets présents, livre et images toujours chargés, tables toujours téléchargées). v0.4.26 publiée ; essai sans le jeu 55 s (02:04) : aucun « Abonnement refusé », aucune erreur, « Recettes chargées : 9322 objets », 5 inventaires reçus.
- Pas fait / reporté : onglets Personnage et Voyageurs pas regardés à l'écran (pas de clic dans le bot la nuit) ; leur code n'a pas changé. Petites fonctions du livre qui ne servent plus qu'aux tests (recherche par nom, résumé) gardées.
- Problèmes rencontrés : aucun.
- À essayer par Julien : lancer la v0.4.26 : plus d'onglet RECETTES ; regarder PERSONNAGE (images et tiers du sac, de la ceinture, des banques), VOYAGEURS (images, matières premières) et la barre de fabrication du tableau de bord pendant une fabrication.
- Questions pour Julien : aucune.
- Consignes pour les tâches suivantes : le livre des recettes est dans `MainWindow.ItemData.cs` (`_recipes`, `IconImage`) ; ne pas retirer `RecipeBook`, `RecipePlanner`, `RawMaterials` ni les tables `crafting_recipe_desc` / `building_type_desc` / `item_conversion_recipe_desc`. Prochaine version libre : v0.4.27.

## T-07 — carte, données (v0.4.27)
- Début / fin : 03:00 / 03:12 · quota début : 5 h 28 %, semaine 31 % · fin : 5 h 35 %, semaine 32 %
- Fait : zoom jusqu'à 140 et 200 cases ; morceaux découverts cherchés à 230 cases (au lieu de 130) ; zones inconnues en gris-bleu discret au lieu du bleu nuit presque noir ; mémoire des ressources sur le disque (`carte/ressources/ressources.bin`, toutes les familles) avec la date « vue le », nettoyage 72 h au démarrage puis chaque heure, 200 000 au plus, écriture toutes les 2 min si changé et à la fermeture ; survol d'une ressource pâle = « Nom (Tn) — vue il y a N h » ; rejets comptés (position hors monde, identifiant nul, doublons, morceaux illisibles) ; terrain gardé depuis moins de 24 h pas redemandé (sauf 3 × 3 autour) ; mesures au journal (Ko par envoi, abonnements par minute, total). D-084, architecture-suite §31.
- **Cause des zones noires** : dans ta région, 74 morceaux découverts, 51 gardés ; les 23 manquants étaient à plus de 130 cases, donc jamais demandés. Le reste du noir = zones jamais découvertes (le jeu ne les montre pas non plus), dessinées presque en noir. Le calcul des zones découvertes a été vérifié dans le code du serveur : il est juste.
- Testé (comment) : build 0 avertissement, tests 662/662 (9 nouveaux, `V0427Tests` : nettoyage 72 h avec date simulée, relecture du fichier, fichier abîmé, ressource coupée retirée dans la zone lue, rejets, toutes familles, crans de zoom, rayon 230, terrain frais pas redemandé). Essai sans le jeu n° 1 (03:07, 75 s) : aucun refus, aucune erreur ; 10 morceaux découverts récupérés → **77 gardés sur 77 découverts** ; 3 419 ressources écrites (198 Ko). Essai n° 2 (03:08, 45 s) : « Mémoire des ressources : 3419 lues », « 3419 gardées, 0 retirées (plus de 72 h) », aucun refus.
- Mesures : avant (v0.4.24) envoi complet 1 358 Ko, dont ≈ 364 Ko de parties fixes (colonies 1 280 lignes, banques, inventaires), ≈ 167 Ko de terrain (9 morceaux), le reste = ressources (3 tables par ressource). Après (même endroit) : 1 472 Ko (3 419 ressources lues, plus qu'avant), terrain toujours 9 morceaux : ici le 3 × 3 couvre déjà toute la zone, le gain n'existe que quand la zone touche 4 × 4 morceaux. Abonnements : 3 au démarrage, puis 1 de plus pour les morceaux découverts ; en marche, 1 tous les 15 cases (inchangé).
- Pas fait / reporté : lire une seule fois les parties fixes (colonies, banques) : il faudrait plusieurs abonnements séparés ou un désabonnement = changement de protocole, interdit sans essai réel (« À vérifier » avec toi). Affichage rapide à 200 cases : T-08. Capture de la fenêtre pas faite (onglet Carte pas ouvert la nuit).
- Problèmes rencontrés : aucun.
- À essayer par Julien : lancer la v0.4.27, onglet CARTE, **dézoomer jusqu'à 200 près de Keldan** : les zones où tu es passé doivent avoir un fond (plus de noir), le reste en gris-bleu. Survoler un point pâle : « vue il y a N h ». Fermer puis rouvrir le bot : les points pâles reviennent. Dézoomer à 200 peut être lent tant que T-08 n'est pas passée.
- Questions pour Julien : 1) le gris-bleu des zones inconnues te va (ou préfères-tu un quadrillage, ou plus sombre) ? 2) essayer ensemble les abonnements séparés pour ne lire les colonies et banques qu'une fois (environ 360 Ko de moins par réabonnement) ?
- Consignes pour les tâches suivantes : **T-08** : à 200 cases, `store.Around` donne ≈ 240 morceaux, soit ≈ 260 000 hexagones dessinés un par un dans `ResourceMap.HexImage` : trop lent, à revoir (image par pixels, ou carrés à petite échelle). La couleur du fond est `ResourceMap.Background` = `TerrainColors.Unknown` (#18222E). La mémoire des ressources est `_resourceMemory` (MainWindow.Map.cs), `SeenText` pour le survol. Prochaine version libre : v0.4.28.

## T-08 — carte, affichage (v0.4.28)
- Début / fin : 04:00 / 04:16 · quota début : 5 h 37 %, semaine 33 % · fin : 5 h 44 %, semaine 34 %
- Fait : carte carrée sur toute la hauteur de l'onglet (jusqu'à 69 % de la largeur ; 706 × 706 dans ta fenêtre 1936 × 1048, 458 × 458 en 1280 × 800) ; tableau à 6 colonnes (repère, nom, tier, distance, direction, santé), famille / âge / position **au survol d'une ligne** ; bloc du haut compact (zone lue : détail au survol) ; fond de carte **calculé point par point en arrière-plan**, à la taille vue à l'écran (plus de 260 000 formes dessinées sur l'interface) ; points des ressources plus petits quand on dézoome ; légende « pâle = vue avant » ; clic droit = case visée notée au journal (pas de menu). Mode d'essai `--essai-carte`. D-085, architecture-suite §32.
- Testé (comment) : build 0 avertissement, tests 667/667 (5 nouveaux, `V0428Tests`). Trois essais sans le jeu (jeu fermé), aucun refus, aucune erreur ; images de la fenêtre regardées (dossier `artifacts/release/captures-essai/`). Mesures : calcul du fond 19 à 29 ms hors interface (30, 100, 200 cases), dessin < 1 ms, interface bloquée au plus 20 ms. Aucun gel.
- Pas fait / reporté : menu « Se déplacer ici » (T-10).
- Problèmes rencontrés : 1er essai : bandes vides autour de la carte et essai lancé trop tôt ; 2e : santé sur deux lignes et fond granuleux à 100 cases. Tout corrigé avant la publication finale.
- À essayer par Julien : lancer la v0.4.28, onglet CARTE : la carte doit être grande ; dézoomer jusqu'à 200 avec « − » : pas de gel ; survoler une ligne du tableau (famille, âge, position) ; clic droit sur la carte : ligne « Clic droit sur la carte : case E…, N… » au journal (en détail).
- Questions pour Julien : 1) à 100 cases, des carrés plus sombres alignés sur les morceaux de terrain : zones non découvertes assombries, ou autre ? (pas touché : partie données) 2) la carte limitée par la hauteur te va, ou préfères-tu réduire la bande des filtres en haut pour la grandir encore ?
- Consignes pour les tâches suivantes : **T-10** : `ResourceMap.CellRightClicked(east, north)` existe (abonné dans `InitMapMemory`, `MainWindow.Map.cs`, qui écrit au journal) : y brancher le menu. `ResourceMap.ToCell(Point)` inchangé. L'image du fond arrive par `TerrainReady` (arrière-plan). Pour un essai de la carte : `--sans-lancer-jeu --essai-carte` (images dans `captures-essai/`). Prochaine version libre : v0.4.29.

## T-09 — étude du déplacement automatique (pas de version)
- Début / fin : 05:00 / 05:04 · quota début : 5 h 46 %, semaine 34 % · fin : 5 h 50 %, semaine 34 %
- Fait : étude `memory/technique/deplacement-etude.md` (8 sections : clic sur la grande carte, vérification, relief, ennemis, Get Up, sécurité, mode d'essai, plus tard), plan précis pour T-10, questions pour Julien. Fondée sur le code serveur officiel (marche, escalade, mort, réapparition, tables ennemis) et le code du bot.
- Trouvé : 1) le **chemin est calculé par le jeu sur le PC**, le serveur vérifie seulement chaque pas ; 2) la destination lue sur le serveur est **la fin du pas en cours**, peut-être pas le point cliqué ; 3) **escalade dès 5 de hauteur** entre deux cases voisines, **nage dès 5 de profondeur** ; 4) Get Up = relevé sur place avec 10 de vie, Go Home = téléporté ; la mort fait tomber le cargo ; 5) le jeu lit les touches **par position physique** (Interact = `/Keyboard/semicolon`, réglage de Julien lu dans le registre, sans jamais lire le jeton) ; 6) **les touches `-` et `=` (chariot, bateau) sont permises aujourd'hui** par le contrôleur (cases 11 et 12 de la barre).
- Testé (comment) : rien à tester (aucun code). Vérificateur de la mémoire lancé.
- Pas fait / reporté : aucun essai en jeu (interdit). Ordre des nombres de la barre de la carte, touche exacte de la carte, comportement de « Set destination » devant une falaise ou un lac : à voir en T-11.
- Problèmes rencontrés : aucun.
- À essayer par Julien : rien (étude). Lire les questions ci-dessous.
- Questions pour Julien : 1) la touche « . » de la carte : celle du pavé numérique, ou Maj + « ; » ? 2) après une fuite, reprendre le trajet tout seul après 20 s de calme, ou s'arrêter (choix de la v1 : s'arrêter) ? 3) eau peu profonde (1 à 4) permise ? eau profonde toujours refusée ? 4) distance maximale de 30 cases pour commencer ? 5) après un Get Up, aller ramasser le cargo tombé, ou s'arrêter ?
- Consignes pour les tâches suivantes : **T-10** : suivre « Plan précis pour T-10 » à la fin de l'étude. Priorités : a) `GameKeys.Forbidden` (`-` 0x0C, `=` 0x0D, Maj + V, Maj + B) vérifié avant `Allowed`, avec tests ; b) `PathCheck` (ligne droite ± 1 case, refus si écart de hauteur ≥ 5, eau ≥ 5, relief inconnu) ; c) `MapScale` (calibration par 3 lectures de la barre, ordre E/N détecté, pas de molette) ; d) `TravelGuard` ; e) menu sur `ResourceMap.CellRightClicked` ; f) `threat_state` vers le joueur dans l'abonnement (retirer si « Abonnement refusé ») ; g) `--essai-deplacement` (§7). Touche de la carte : rendre les deux candidates (0x34, 0x53) essayables, une seule gardée après T-11. **T-11** : §7 de l'étude ; noter l'ordre des nombres de la barre, la touche qui ouvre la carte, le nombre de destinations différentes vues sur le serveur pendant 5 cases, la vitesse.

## T-10 — « Se déplacer ici » + sécurités + mode d'essai (v0.4.29)
- Début / fin : 06:00 / 06:12 · quota début : 5 h 1 %, semaine 34 % · fin : 5 h 10 %, semaine 36 %
- Fait : plan de T-09 suivi (D-086, `technique/architecture-suite.md` §33). Clic droit sur la carte du bot → « Se déplacer ici (N, E) », grisé avec la raison si refusé (30 cases au plus ; relief inconnu ; escalade = écart de sol ≥ 5 ; eau ≥ 5 = nage ; vérifié sur la ligne droite et à ± 1 case). Geste : touche de la carte → « Set destination » lu → échelle mesurée (3 lectures de la barre, ordre N/E trouvé seul, jamais de molette) → visée ≤ 1 case (3 corrections) → un clic gauche → carte refermée et vérifiée → suivi serveur (départ 3 s avec un nouvel essai, arrivée ≤ 1,5 case, bloqué, temps max 10 s + 2 s/case, serveur muet). Vie qui baisse : Sprint (4) + fuite 10 cases à l'opposé puis arrêt. Vie 0 : « Get Up » cliqué seulement si « Go Home » est lu ailleurs ; position qui saute après = tout bloqué. **Touches `-`, `=`, V, B interdites** quoi qu'il arrive (retirées de la liste permise, capacités auto aussi). Interrupteur « Déplacements » (OPTIONS, allumé).
- Testé (comment) : build 0 avertissement ; tests 701/701 (34 nouveaux, `V0429Tests` : barre lue / abîmée, échelle (N,E) et (E,N), zoom trop fort / faible, visée corrigée, plat / marche 4 et 5 / eau 4 et 5 / relief inconnu / obstacle à côté / trop loin, arrivée, blocage, temps, vie, fuite, touches interdites jamais envoyées, touches de la carte et Sprint permises, choix Get Up, rectangle OCR). Essai sans le jeu 06:08 (`--sans-lancer-jeu --essai-menu-deplacement`) : menu « permis » à 5 E et 5 N puis « refusé : jeu fermé » ; 40 E grisé « trop loin » ; aucun « Abonnement refusé », aucune ERREUR.
- Pas fait / reporté : aucun essai en jeu (T-11). `threat_state` et ennemis de la zone **pas** ajoutés à l'abonnement (risque de refus sans essai) : danger vu seulement par la vie qui baisse. Pas de capture de la fenêtre du bot (rien de visuel nouveau hors du menu).
- Problèmes rencontrés : aucun. La grande carte est reconnue comme « fenêtre du jeu » (D-052) et bloquait toute action : exception ajoutée, seulement pour la carte ouverte par le bot lui-même (30 s au plus, 4 s après fermeture).
- À essayer par Julien : petit trajet (5 à 10 cases, terrain plat) par clic droit sur la carte du bot, jeu au premier plan, souris lâchée ; vérifier que la carte du jeu se referme.
- Questions pour Julien : celles de T-09 (touche de la carte, reprise après fuite, eau peu profonde, 30 cases, cargo après Get Up) ; en plus : interrupteur « Déplacements » allumé par défaut, d'accord ?
- Consignes pour les tâches suivantes : **T-11** : depuis `artifacts\release`, lancer **`.\BitcraftBot-v0.4.29.exe --essai-deplacement`** (sans `--sans-lancer-jeu` : le bot lance le jeu). Le mode attend l'écran « en jeu », la position, BitCraft au premier plan et 3 s sans souris ni clavier (15 min au plus), puis : capture « 01-depart-cible-en-haut » ; essaie la touche 0x34 puis 0x53 (captures « 02-carte-… ») ; mesure l'échelle (journal « Mesure de la carte : … ordre (N, E) ou (E, N), X case/pixel » ou le refus) ; referme (« 03-carte-fermee ») ; va à 5 cases (N, E, S, O puis diagonales, première qui passe le relief) puis revient (« 04-aller », « 05-retour »). Captures dans `images/analyse/20261127_essai-deplacement_*.png`. Regarder dans le journal (catégorie « Déplacement ») : touche gardée, ordre des nombres, échelle, « Visée n », « Départ confirmé : destination du serveur … (écart …) », « Arrivé : … en X s (Y case/s), N destination(s) différente(s) ». Si l'échelle est refusée (« zoom à régler à la main » ou « centre … loin de la position du serveur ») : noter les nombres lus, ne rien forcer. **Ne jamais toucher à la souris pendant l'essai** (arrêt d'urgence). Si la carte reste ouverte : le bot bloque tout (alerte) ; la refermer avec la même touche. **T-12** : zones de la barre (`MapBarZone`, x 30-69 %, y 91-97,5 %) et de la souris (`MapClickArea`) à corriger d'après les captures si besoin ; l'écart destination du serveur / cible dira si la destination vaut le point cliqué. Prochaine version libre : v0.4.30.

## T-11 — essai réel en jeu (pas de version)
- Début / fin : 07:00 / 07:06 · quota début : 5 h 12 %, semaine 36 % · fin : 5 h 15 %, semaine 36 %
- Fait : jeu et bot de Julien vérifiés fermés ; bot lancé à 07:01:33 (`.\BitcraftBot-v0.4.29.exe --essai-deplacement` depuis `artifacts\release`) ; serveur connecté, position N 9041, E 6808, jeu lancé par Steam (07:01:39), fenêtre trouvée, **écran d'accueil « Press any button to start » reconnu** (07:01:47). **Essai bloqué là** : l'accès à l'écran du jeu (contrôle de l'ordinateur) est **refusé d'office pendant une tâche planifiée**, donc les deux clics de démarrage autorisés par Julien étaient impossibles, et le bot ne les fait pas lui-même. Bot fermé proprement (07:02:19, déconnexion d'abord), jeu fermé (07:02:20), plus rien d'ouvert. Jeu ouvert 40 s en tout. Détail : `journal/2026-11-27-tache-T11-essai-reel.md`.
- Testé (comment) : journal du bot lu (aucune ERREUR, aucun « Abonnement refusé ») ; capture d'accueil du bot regardée (`images/analyse/20261127_essai-reel_01-ecran-accueil.png`, locale) ; processus vérifiés avant et après.
- Pas fait / reporté : **tout le déplacement** (touche de la carte, ordre des nombres de la barre, échelle, visée, destination du serveur, aller-retour 5 cases), capture de la cible en haut de l'écran (T-13). Pas de deuxième lancement : le même blocage se serait reproduit. Le refus n'a pas été contourné (pas de clic par script).
- Problèmes rencontrés : 1) outil d'écran non permis en tâche planifiée ; 2) le bot reconnaît l'accueil et la connexion mais n'agit pas dessus (D-052 : aucune action hors « en jeu »).
- À essayer par Julien : l'essai du déplacement avec toi présent (5 min) : lancer `BitcraftBot-v0.4.29.exe --essai-deplacement`, faire toi-même les deux clics de démarrage, puis lâcher souris et clavier.
- Questions pour Julien : 1) le bot peut-il faire seul les deux clics de démarrage (accueil, « Enter BitCraft ») quand c'est lui qui a lancé le jeu (exception à D-052, comme la grande carte) ? 2) ajouter « BitCraft Online » aux applications permises de la tâche planifiée, pour un futur essai de nuit ? 3) refaire l'essai ensemble ?
- Consignes pour les tâches suivantes : **T-12** : aucun résultat d'essai sur la carte, la barre ou la visée : **ne pas changer** `MapBarZone`, `MapClickArea` ni l'échelle à l'aveugle. Correction utile possible : passage seul de l'accueil (clic au centre de la fenêtre) et de la connexion (clic sur « Enter BitCraft » lu à l'écran), **seulement si le bot a lancé le jeu, jamais « Quitter »**, **coupé par défaut** et allumé seulement par `--essai-deplacement` tant que Julien n'a pas répondu à la question 1 (choix prudent, à noter en D-NNN). Sinon, T-12 peut se limiter à des corrections sans essai. Position du personnage inchangée (jamais entré en jeu). Prochaine version libre : v0.4.30.

## T-12 — corrections de « Se déplacer ici » après relecture (v0.4.30)
- Début / fin : 08:00 / 08:09 · quota début : 5 h 17 %, semaine 37 % · fin : 5 h 21 %, semaine 37 %
- Fait : T-11 n'ayant pas pu essayer, relecture du code de T-10 étape par étape (fiche, point 3). **Défaut bloquant trouvé et corrigé** : le trajet part d'un clic dans la fenêtre du bot, donc le jeu n'est pas au premier plan et la touche de la carte était refusée tout de suite (le trajet aurait raté à chaque fois). Le bot attend maintenant **20 s au plus** que tu repasses sur le jeu (**Alt + Tab**, sans cliquer dans le jeu) et lâches souris et clavier. Gênants corrigés : carte lente à s'ouvrir (3 lectures de la barre), touche de la carte **jamais rappuyée si rien ne s'est ouvert** (avant : pouvait ouvrir la carte pour rien et tout bloquer), carte lente à se fermer (3 regards avant l'alerte). Zone de la barre vérifiée sur une vraie capture de la grande carte. D-087, `technique/architecture-suite.md` §34. Détail : `journal/2026-11-27-tache-T12-v0430-corrections-essai.md`.
- Testé (comment) : build 0 avertissement ; tests 708/708 (7 nouveaux, `V0430Tests` : barre « (9196, 7258) » et « Set destination » lus par OCR sur `images/reference-ui/fenetres/carte-du-monde_001.png` ; refus « BitCraft n'est pas la fenêtre active » reproduit avec un faux clavier puis attente réussie après le retour sur le jeu ; attente limitée à 20 s ; arrêt immédiat si actions bloquées ; rappui seulement si la carte est vue ; fermeture lente). Essai sans le jeu 08:05 (`--sans-lancer-jeu --essai-menu-deplacement`) : menu permis à 5 E / 5 N puis « refusé : jeu fermé », 40 E « trop loin » ; aucun « Abonnement refusé », aucune ERREUR.
- Pas fait / reporté : **aucun essai en jeu** (interdit à T-12). Clics de démarrage du jeu (accueil, « Enter BitCraft ») **pas codés** : contraire à D-052 → **Q-028**. Échelle, ordre des nombres, zone de la souris : inchangés (pas de mesure en jeu). Pas de capture de la fenêtre du bot (rien de visuel nouveau).
- Problèmes rencontrés : aucun.
- À essayer par Julien (en premier) : lancer `BitcraftBot-v0.4.30.exe`, entrer en jeu, sur terrain plat. Dans l'onglet CARTE du bot : clic droit sur une case à **5-10 cases**, « Se déplacer ici », puis **Alt + Tab** vers le jeu et **ne plus toucher** souris ni clavier. Regarder le journal, catégorie « Déplacement » : « Mesure de la carte : … ordre (N, E) », « Visée n », « Départ confirmé … (écart avec la cible …) », « Arrivé … ». Si la carte reste ouverte : la refermer avec la même touche.
- Questions pour Julien : 1) **Q-028** : le bot peut-il faire seul les deux clics de démarrage quand il a lancé le jeu ? 2) l'attente « Alt + Tab, 20 s » te va, ou préfères-tu que le bot mette lui-même le jeu au premier plan ? 3) celles de T-09 à T-11.
- Consignes pour les tâches suivantes : `WorldMapZones` (Perception) porte la zone de la barre ; `ActionReadiness.WaitAsync` sert à tout geste lancé depuis la fenêtre du bot (ex. T-14 « Aller à cette ressource » : l'utiliser aussi). Sur la capture de la grande carte, la boussole en haut à droite donne la position (« N 8899, E 6723 ») : la barre donne donc le **nord d'abord**. Prochaine version libre : **v0.4.31**.

## T-13 — petits défauts (v0.4.31)
- Début / fin : 09:00 / 09:10 · quota début : 5 h 23 %, semaine 37 % · fin : 5 h 33 %, semaine 39 %
- Fait : **1) Cible lue à l'écran : corrigé.** Cause : une mise à jour du jeu (23/11 entre 20:04 et 23:01) a rendu le panneau de la cible **sombre, texte clair** ; le bot cherchait le fond crème. Il reconnaît les deux apparences (sans confondre avec les menus) et lit le texte clair. **2) Barre de fabrication : corrigé (cause probable).** Le serveur garde la fabrication à 100 % jusqu'au ramassage ; la barre s'arrêtait avant la fin s'il y avait d'autres fabrications dans d'autres ateliers ou si les objets étaient ramassés tout de suite. Le bot retient la recette et la dernière part ; « Fabrication · terminée ». **3) Conseil pêche / minage : fait.** En fin de minage ou de pêche d'au moins 20 s, si aucune capacité du métier présente sur la barre n'est partie (début de recharge lu sur le serveur) : « [Conseil] Minage de 3 min 05 s sans Heavy Swings : pensez à l'allumer (Capacités auto). » D-088, `technique/architecture-suite.md` §35. Détail : `journal/2026-11-27-tache-T13-v0431-petits-defauts.md`.
- Testé (comment) : build 0 avertissement ; tests 730/730 (22 nouveaux, `V0431Tests`) dont lecture OCR sur 3 vraies captures du panneau sombre (Lavender 40/550, Rough Hieroglyphs 1700/1700, Simple Fishing Station 120/120 ; bandes du haut recadrées dans `images/reference-ui/cible-sombre/`, sans chat ni nom de joueur) et un menu plein écran (pas de cible). Essai sans le jeu 09:09 (45 s) : capacités et barre reçues, aucun « Abonnement refusé », aucune ERREUR.
- Pas fait / reporté : rien d'essayé en jeu (cible, barre, conseil). Les journaux n'écrivent pas la barre de fabrication : le cas exact vu par Julien n'est pas prouvé.
- Problèmes rencontrés : incohérence : la fiche range Break the Roots avec le minage, les données du jeu avec le bois (Forestry). Le conseil suit les données du jeu.
- À essayer par Julien : couper un arbre : le journal doit dire « Perception : … cible Mature Oak Tree … » (et l'onglet CARTE « Cible écran ») ; une petite fabrication : la barre finit à 100 % / « terminée » ; miner 1 min sans Heavy Swings : ligne « [Conseil] » après « Fin : récolte ».
- Questions pour Julien : 1) conseil aussi pour le bois (Hewing Strikes) ou seulement pêche et minage ? 2) durée minimale de 20 s pour le conseil : ça te va ?
- Consignes pour les tâches suivantes : cible de l'écran de nouveau disponible (`HudReader`, `BarReader.GetTargetPanelStyle`) ; journal des tâches : `TaskInput.Abilities`, événement « Conseil ». Prochaine version libre : **v0.4.32**.

## T-14 — « Aller à cette ressource » / « Récolter jusqu'à épuisement » (v0.4.32)
- Début / fin : 10:00 / 10:36 · quota début : 5 h 35 %, semaine 39 % · fin : 5 h 40 %, semaine 40 %
- Fait : **menu sur une ressource** : clic droit sur une ressource de la carte (1,5 case autour du clic, ressources affichées seulement), clic droit sur une ligne du tableau, bouton **« Y aller… »** à côté de « Plus proche ». Deux entrées : « Aller à cette ressource : Nom (Tn) » et « Récolter jusqu'à épuisement », grisées avec la raison si refusé. **Aller (automatique)** : « Se déplacer ici » vers une **case voisine à 2 cases** de la ressource (jamais sur elle), la plus proche de toi d'abord, les 7 autres essayées si elle est refusée (escalade, eau, relief, distance). **Récolter : le premier coup reste à toi.** Après le trajet, le bot ne sait pas où est la ressource à l'écran (caméra déplacée) et aucune méthode n'a pu être essayée en jeu : il s'arrête à l'arrivée avec « cliquez une fois sur « Nom » ; le bot continuera jusqu'à épuisement ». **Ensuite (automatique)** : la reprise des récoltes relance à chaque baisse d'endurance ; le bot écrit « premier coup vu », puis « épuisée : récolte finie ». Abandon si tu récoltes autre chose, t'éloignes de plus de 6 cases ou ne frappes pas en 3 min. **Vie qui baisse** : Sprint (4) et fuite 10 cases, puis arrêt. D-089, architecture-suite §36, journal `journal/2026-11-27-tache-T14-v0432-recolter-ressource.md`.
- Testé (comment) : build 0 avertissement ; tests 742/742 (12 nouveaux, `V0432Tests` : ressource sous le clic, case voisine du bon côté, case refusée → suivante, toutes refusées, déjà à côté, trop loin, premier coup puis épuisée, ancien coup sur un autre arbre ignoré, autre ressource, 3 min sans coup, vie qui baisse, éloigné / hors zone). Essai sans le jeu 10:05 (55 s, `--sans-lancer-jeu --essai-menu-deplacement`) : menu « Aller à cette ressource : Daisy (T1) / Récolter jusqu'à épuisement » à 3,7 cases, case voisine N 9040, E 6807 choisie, trajet refusé (jeu fermé) ; aucun « Abonnement refusé », aucune ERREUR.
- Pas fait / reporté : **premier coup automatique** (W, recherche du contour vert, confirmation par la cible du serveur) : à faire après un essai réel. Pas de capture de la fenêtre du bot (pas de clic dans le bot la nuit). Le reprise des récoltes n'est pas allumée par le bot si tu l'as coupée (il le dit).
- Problèmes rencontrés : aucun.
- À essayer par Julien : v0.4.32, onglet CARTE, clic droit sur un arbre à 5-15 cases → « Récolter jusqu'à épuisement », Alt + Tab vers le jeu, ne plus toucher ; à l'arrivée, **un clic sur l'arbre** ; laisser l'endurance tomber. Journal, catégorie « Récolte guidée » : « case voisine … », « Arrivé près de … », « Premier coup … », « … est épuisée ». Essayer aussi le bouton « Y aller… » avec une ressource recherchée.
- Questions pour Julien : 1) 2 cases de la ressource : assez près pour la récolter sans bouger (gros arbres) ? 2) le premier coup automatique, à essayer ensemble ? 3) faut-il allumer la reprise des récoltes d'office quand tu choisis « Récolter jusqu'à épuisement » ?
- Consignes pour les tâches suivantes : `ResourceApproach` / `HarvestWatch` (Server/World/ResourceApproach.cs), `MainWindow.Harvest.cs`. Le premier coup automatique devra passer par `_resume` (point écran) ou un nouveau geste avec confirmation `s.Target`. Prochaine version libre : **v0.4.33**.

## T-15 — contrôle complet du dépôt (v0.4.33)
- Début / fin : 11:00 / 11:12 · quota début : 5 h 1 %, semaine 40 % · fin : 5 h 6 %, semaine 40 %
- Fait : build Debug et Release 0 avertissement ; **139 remarques d'espaces corrigées** (13 fichiers, mise en forme seule), `dotnet format --verify-no-changes` à 0 ; aucun reste de l'onglet Recettes, aucun TODO, aucun fichier vide ; bulle d'aide « Reprise des récoltes » corrigée (parlait encore de Tab) ; dépôt public vérifié (aucune adresse, aucun e-mail, aucun jeton vrai, `.gitignore` correct) ; **14 tests ajoutés** (`V0433Tests` : mémoire à 72 h pile, « vue il y a », positions refusées, barre de la grande carte, touches du chariot / bateau jamais permises) ; `README.md` racine réécrit (il disait v0.3.18), `tests/README.md` créé, en-tête de `architecture.md`, `architecture-suite.md` §37. Rapport : `technique/controle-depot-2026-11-27.md`.
- Testé (comment) : tests 756/756 ; v0.4.33 publiée ; essai sans le jeu 11:07:50 → 11:08:45 (fermé proprement) : connexion en lecture seule, aucun « Abonnement refusé », aucune ERREUR, 3 412 ressources relues, 77 morceaux de terrain.
- Pas fait / reporté : grosses classes signalées, pas découpées (`ActionController.cs` 742 lignes, `MainWindow.Travel.cs` 738, `MainWindow.xaml.cs` 914, `ServerReader.cs` 677) ; Tab laissé dans les touches permises (inutile, sans effet ; le retirer = changer la liste des sécurités).
- Problèmes rencontrés : aucun.
- À essayer par Julien : rien de nouveau (v0.4.33 = v0.4.32 + textes et tests) ; la bulle d'aide de « Reprise des récoltes » dans OPTIONS.
- Questions pour Julien : aucune nouvelle.
- Consignes pour les tâches suivantes : **T-16** : tests 756 ; README racine et `tests/README.md` à jour ; aucune décision nouvelle (pas de D-NNN). Prochaine version libre : **v0.4.34**.

## T-16 — contrôle de la mémoire, bilan final, phrase de reprise (pas de version)
- Début / fin : 12:00 / 12:05 · quota début : 5 h 7 %, semaine 41 % · fin : 5 h 11 %, semaine 41 %
- Fait : vérificateur de la mémoire lancé (0 erreur ; 1 attention : `echanges/2026-11-15-prompt-init-repo.md` 304 lignes, archive, laissé tel quel). Corrigé : **lignes d'index rangées hors des tableaux** (26 décisions D-064 à D-089, 17 lignes du journal, Q-028 étaient écrites après le gabarit, donc hors tableau) ; `memory/README.md` (D-001 à D-089, Q-001 à Q-029, fichiers de la nuit, étude du déplacement, contrôle du dépôt, nouvelle passation ; essai v0.4.4 et passation 29 marqués dépassés) ; Q-015 et Q-025 mises à jour (détection des fenêtres codée en v0.4.5 ; Interact = M utilisée) ; demande de déplacement marquée « faite, pas essayée en jeu ». **Q-029 ouverte** (réglages du déplacement, 8 points de T-09 à T-14 regroupés). `resume-global.md` réécrit (10 lignes). Résumé pour Julien en tête de ce bilan. Passation `echanges/2026-11-27-passation-nuit.md`. Mémoire automatique de Claude vérifiée (une leçon ajoutée : pas d'accès à l'écran en tâche planifiée).
- Testé (comment) : vérificateur à 0 erreur après les corrections ; liens relus.
- Pas fait / reporté : aucun code, aucune version (fiche). Pas de découpage du fichier de 304 lignes (archive, Julien accepte un peu plus).
- Problèmes rencontrés : aucun.
- À essayer par Julien : voir le résumé en tête.
- Questions pour Julien : toutes regroupées dans le résumé en tête.
- Consignes pour la suite : reprendre par la phrase de `echanges/2026-11-27-passation-nuit.md`. Prochaine version libre : **v0.4.34**. Prochaine décision : D-090 ; prochaine question : Q-030.
