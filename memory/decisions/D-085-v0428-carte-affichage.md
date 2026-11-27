# D-085 — v0.4.28 : carte, affichage (carte plus grande, tableau plus étroit, rapide à 200 cases)

- **Statut** : Validée (tâche de nuit T-08, d'après les demandes de Julien du 27/11)
- **Date** : 2026-11-27
- **Validée par** : Julien (agrandir la carte, réduire le tableau, carte claire et rapide dézoomée à 200 cases ; fiche `taches-planifiees/T-08-v0428-carte-affichage.md`, D-081)
- **Liens** : D-073 (hexagones), D-079 (proportions), D-084 (données, zoom 200), [architecture-suite.md](../technique/architecture-suite.md) section 32

## Contexte
Jusqu'en v0.4.27, la carte faisait 560 × 560 points fixes et le tableau avait 9 colonnes. Le fond était dessiné case par case (une forme par hexagone) sur le fil de l'interface : à 200 cases de vue, environ 260 000 formes, la fenêtre gelait.

## Décision
1. **Carte carrée, toute la hauteur de l'onglet**, jusqu'à **69 % de la largeur** ; le tableau prend le reste. Sur une fenêtre large et basse (1936 × 1048 : la fenêtre de Julien), la carte fait 706 × 706 ; sur 1280 × 800, 458 × 458 (la hauteur limite). Fenêtre étroite (< 1050) : carte au-dessus du tableau, comme avant.
2. **Tableau** : 6 colonnes (repère, nom, tier, distance, direction, santé). Famille, âge et position passent **au survol de la ligne**. Bloc du haut plus compact (zone lue en une ligne courte, détail au survol).
3. **Fond calculé point par point, en arrière-plan** : chaque point de l'image prend la couleur de la case dont le centre est le plus proche (c'est exactement le pavage en hexagones). L'image est calculée **à la taille où elle sera vue** (points par case à l'écran, 2 à 12) ; bords sombres entre cases seulement quand les cases font au moins 8 points à l'écran (vues proches). Un calcul dépassé (zoom changé entre-temps) est jeté.
4. **Points des ressources plus petits** quand on dézoome (×1 jusqu'à 45 cases, ×0,45 à 200), sans contour au-delà de 65 cases. Le personnage et les anneaux (recherchée, cible, sélection) gardent leur taille.
5. **Clic droit** sur la carte : la case visée (E, N) est notée au journal (détail). Le menu « Se déplacer ici » viendra avec T-10.
6. **Mode d'essai** `--sans-lancer-jeu --essai-carte` : ouvre l'onglet Carte, passe aux vues 30, 100, 200 puis 100 en 1280 × 800, note les durées et enregistre une image de la fenêtre à chaque fois (dossier `captures-essai/`), puis remet la taille de la fenêtre.

## Mesures (essai sans le jeu, 27/11 04:11, près de Keldan)
| Vue | Morceaux | Image | Calcul (hors interface) | Dessin à l'écran | Interface bloquée au plus |
|---|---|---|---|---|---|
| 30 cases | 11 | 1414 × 915 | 29 ms (7 ms ensuite) | 0,8 ms | 20 ms |
| 100 cases | 47 | 1154 × 998 | 19 ms | 0,8 ms | 20 ms |
| 200 cases | 74 | 769 × 721 | 27 ms | 0,7 ms | 20 ms |
| 100 cases, fenêtre 1280 × 800 | 47 | 866 × 749 | 7 ms | 0,7 ms | 21 ms |

Aucun gel (limite fixée : 300 ms).

## Conséquences
- Tests 667/667 (5 nouveaux dans `V0428Tests`).
- À vérifier avec Julien : les carrés plus sombres alignés sur les morceaux (visibles à 100 cases) : zones non découvertes assombries ou différence de biome ? Pas changé ici (partie données).
