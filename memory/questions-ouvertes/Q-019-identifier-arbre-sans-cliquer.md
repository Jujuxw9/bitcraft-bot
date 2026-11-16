# Q-019 — Comment savoir si un arbre est vieux ou jeune avant de le couper ?
- **Statut** : Ouverte (recommandation de Claude ci-dessous)
- **Priorité** : Haute (bloque la v0.3, D-034)
- **Créée le** : 2026-11-16

## Constat (vidéos V1 et V2 du lot 5)
- Au survol, le jeu dessine un **contour vert** et le curseur devient une **hache**. Mais le **panneau du haut ne change pas** : il garde la dernière cible.
- Donc, sans cliquer, le bot sait « arbre ici », pas « Mature » ou « Young ».

## Pistes
| Piste | Principe | Pour | Contre |
|---|---|---|---|
| **A. Cliquer puis vérifier** | Clic droit (aller + couper), lire le nom dans le panneau dès qu'il apparaît ; si ce n'est pas un vieil arbre, s'écarter et chercher ailleurs | Sûr : le nom exact est lu | Quelques pas et coups perdus sur un jeune arbre |
| **B. Taille du contour** | Un vieil arbre (12 000 points) est bien plus grand qu'un jeune : contour plus haut et plus large | Sans clic | Dépend de la distance et de l'angle de caméra ; à calibrer |
| **C. A + B** | B pour choisir le candidat le plus probable, A pour confirmer | Rapide et sûr | Un peu plus de code |

## Recommandation de Claude
**C** : viser d'abord le plus grand contour proche (B), puis confirmer par le nom lu (A). Le nom lu est aussi vérifié dans les données du jeu (étiquette `Tree`, jamais `Wonder Resource` comme le Maker's Tree).

## Ce qui bloque
L'accord de Julien sur la piste. Pour calibrer B : une capture **écran complet** avec un vieil arbre et un jeune arbre côte à côte, chacun survolé.
