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
