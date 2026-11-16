# D-025 — Le bot fonctionne de jour comme de nuit

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : Q-001, D-022

## Contexte
Le jeu a un cycle jour/nuit. La nuit, l'image devient très sombre (bleu-vert). En 15 minutes de session, on peut passer du jour à la nuit.

## Décision
Le bot doit savoir détecter les arbres, lire le HUD et naviguer **de jour comme de nuit**. Pas de pause automatique la nuit.

## Alternatives écartées
- Jouer seulement de jour : impossible à garantir sur une session de 15 min.

## Conséquences
- La détection ne repose pas sur des couleurs fixes de l'environnement. Les éléments d'interface (panneaux, textes, contour vert) restent lisibles la nuit : ils sont prioritaires.
- **À vérifier** : le contour vert reste-t-il aussi visible la nuit ? Il faut des captures de nuit avec un arbre survolé.
- Chaque test de perception se fait de jour **et** de nuit.
