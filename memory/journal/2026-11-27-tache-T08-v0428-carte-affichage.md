# Tâche T-08 (nuit du 27/11, 04:00) — v0.4.28 : carte, affichage

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Décision** : D-085 ; architecture : `technique/architecture-suite.md` section 32

## Fait
- Carte carrée sur toute la hauteur de l'onglet (jusqu'à 69 % de la largeur), tableau à 6 colonnes (famille, âge, position au survol), bloc du haut compact.
- Fond de carte calculé point par point en arrière-plan (`HexRaster`), à la taille vue à l'écran ; plus de gel à 200 cases.
- Points des ressources plus petits quand on dézoome ; légende « pâle = vue avant (survol : il y a…) ».
- Clic droit sur la carte : case visée notée au journal (préparation de T-10, sans menu).
- Mode d'essai `--essai-carte` (vues 30, 100, 200, puis 1280 × 800 ; mesures et images).

## Testé
- Build 0 avertissement, tests 667/667 (5 nouveaux, `V0428Tests` : taille de l'image, couleur au centre de chaque case, aucun trou, bords seulement pour les grandes cases, vue 200 calculée vite, disposition).
- Trois essais sans le jeu (04:06, 04:09, 04:11), jeu fermé : aucun « Abonnement refusé », aucune erreur. Images de la fenêtre regardées : carte grande, tableau étroit, hexagones nets à 30 cases, fond lisse à 100 et 200.
- Mesures : calcul du fond 7 à 39 ms hors interface, dessin à l'écran < 1 ms, interface bloquée au plus 20 à 34 ms (détail dans D-085).

## Corrigé pendant l'essai
- 1er essai : la carte laissait des bandes vides sur une fenêtre large et basse → colonne ajustée à la carte. L'essai démarrait avant l'arrivée des ressources → 25 s d'attente.
- 2e essai : santé sur deux lignes → colonne élargie ; fond granuleux à 100 cases (bords d'hexagones écrasés) → image calculée à la taille vue à l'écran.

## Pas fait
- Menu « Se déplacer ici » (T-10).
- Carrés plus sombres alignés sur les morceaux à 100 cases : pas expliqués (données, pas l'affichage) ; à vérifier avec Julien.
