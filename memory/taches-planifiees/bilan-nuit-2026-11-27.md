# Bilan de la nuit du 27/11/2026 (tâches T-05 à T-16)

Fichier unique, rempli par chaque tâche à la fin de son passage (règles : `nuit-2026-11-27-regles.md`). Julien le lit au réveil ; T-16 le résume en tête.

## Résumé pour Julien (écrit par T-16)
_À remplir par T-16._

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
