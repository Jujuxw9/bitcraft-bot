# D-057 — W remet une caméra fixe par rapport au personnage ; passage automatique du chat sur Région

- **Statut** : Validée
- **Date** : 2026-11-23
- **Validée par** : Julien (« la caméra se remet dans une rotation spécifique, une hauteur spécifique et un zoom spécifique par rapport à la direction du personnage » ; « oui mais si possible, peut-il le faire quand le jeu se lance ? genre de manière auto ? »)
- **Liens** : D-052, D-054, D-055, D-056

## Contexte
- Journal v0.4.5 : après W, le cône de la mini-carte n'a pas bougé 5 fois sur 6. L'idée « après W, la caméra regarde dans la direction du personnage » ne collait pas.
- Le jeu ouvre le chat sur le dernier canal utilisé : une réponse « @GPT » a été abandonnée (canal Local).

## Décision
1. **W** (dit par Julien) : la caméra reprend une rotation, une hauteur et un zoom **fixes, par rapport à la direction du personnage**. L'écart entre la direction du personnage et le cône après W est donc un **décalage fixe**, à mesurer (goutte de la mini-carte contre cône) : **À vérifier**.
2. **Canal du chat** : le bot passe le chat sur **Région** lui-même, **automatiquement, une fois quand le jeu arrive en jeu** (et avant une réponse si le canal n'est pas Région) : Entrée, lecture du bouton du canal, clic sur ce bouton jusqu'à lire « REGION », puis chat refermé. Règles du contrôleur d'actions (D-045) : chat refermé et vérifié, arrêt si Julien tape.

## Conséquences
- À coder en v0.4.7. **À vérifier avant** : ce que fait un clic sur le bouton du canal (passe au canal suivant ou ouvre une liste ?). Une capture Num 0 de Julien, chat ouvert, après un clic sur ce bouton, le dira.
- `technique/hybride/analyse-visee-2026-11-22.md` (R1) : le cap après W = direction du personnage + décalage fixe.
