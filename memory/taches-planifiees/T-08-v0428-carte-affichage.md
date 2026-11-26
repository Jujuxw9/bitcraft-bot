# T-08 (04:00) — v0.4.28 : carte, partie 2 : l'affichage

Règles communes : `nuit-2026-11-27-regles.md`. **Lis la section T-07 du bilan** : tu continues son travail.

## Pourquoi
Julien (27/11) : agrandir la carte dans l'onglet CARTE ; réduire le tableau sur le côté (liste des ressources et des cibles) ; la carte doit rester claire et rapide même dézoomée à 200 cases.

## État actuel
- `MainWindow.xaml`, onglet CARTE : carte dans un `Viewbox` de 560 × 560 (`MapCard`, `SrvMap`), tableau à droite (`MapList` : ressource recherchée, plus proche, cible écran, zone lue, liste `SrvResourceList` à 9 colonnes). Disposition selon la largeur dans `MainWindow.Layout.cs`.
- Dessin : `ResourceMap.cs` (fond en hexagones par `RenderTargetBitmap`, plafonné à ~2 400 points ; points des ressources ; anneaux recherchée / entamé / cible / sélection).

## À faire
1. **Carte plus grande** : elle prend la plus grande part de l'onglet (viser ~65-70 % de la largeur, et toute la hauteur disponible, carrée). Elle s'adapte à la taille de la fenêtre (Julien a une fenêtre de bot assez grande ; tester aussi une fenêtre de 1280 × 800).
2. **Tableau plus étroit** : garder les colonnes utiles (marque, nom, tier, distance, direction, santé), retirer ou fusionner le reste (famille, âge, position → au survol). Le bloc du haut (recherchée, plus proche, cible écran, zone lue) plus compact.
3. **Rapide à 200 cases** : à 200 cases, ~200 morceaux de 1 024 hexagones : ne pas dessiner 200 000 géométries sur le fil de l'interface. Pistes : image du terrain calculée **hors du fil de l'interface** (remplir directement des points dans un `WriteableBitmap`/tableau, hexagones tracés par calcul, pas par `DrawGeometry`), cache par morceau, détail réduit quand on dézoome (au-delà de ~65 cases, un point par case suffit), redessin seulement quand le morceau du personnage, le zoom ou la mémoire change. Mesurer et noter dans le journal le temps de dessin à 30, 100 et 200 cases (essai : `--sans-lancer-jeu` + onglet CARTE ; le bot sait enregistrer une image de sa fenêtre en mode essai, voir `SaveTestCapture` dans `MainWindow.Map.cs`).
4. **Lisibilité dézoomée** : à 140-200 cases, les points de ressources se chevauchent : les réduire, et ne garder que les catégories allumées ; la ressource recherchée et le personnage restent bien visibles. Garder la légende à jour (recherchée, entamé, cible ; « vue il y a… » pour la mémoire de T-07).
5. Garder tout ce qui marche : boutons + et −, clic sur une ressource (sélection dans le tableau), ressource recherchée, filtres.
6. Préparer T-10 (« Se déplacer ici ») sans le faire : `ResourceMap.ToCell(Point)` existe déjà ; prévoir un **clic droit** propre (pas encore de menu).

## Fini quand
- v0.4.28 publiée, tests verts, essai sans le jeu : image de la fenêtre enregistrée et regardée par toi (carte grande, tableau étroit), temps de dessin notés, aucun gel visible (> 300 ms) au changement de zoom.

## Interdit
- Changer la récupération des données (T-07) sauf bug bloquant (le noter). Ajouter le menu « Se déplacer ici » (T-10).

## Si T-07 a raté
- Faire quand même l'affichage, avec les crans de zoom existants ; ajouter 140 et 200 seulement si T-07 ne l'a pas fait ; noter ce qui manque.
