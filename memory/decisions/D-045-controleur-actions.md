# D-045 — Contrôleur d'actions commun

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien (« Un contrôleur d'actions commun => super mais en 2nd temps », puis « Attaque le contrôleur d'actions commun ensuite »)
- **Liens** : D-022, D-041, D-042, D-035 ; avant H3

## Contexte
Jusqu'à la v0.3.17, chaque fonction qui agissait sur le jeu avait ses propres sécurités (le repas automatique, le banc d'essai D-035). Avant H3 (aller au meilleur arbre), il faut un seul point de passage.

## Décision
Toutes les touches et tous les clics du bot passent par **un seul contrôleur** (`ActionController`, brique Input), avec les **mêmes sécurités** :
- une seule action à la fois ;
- **liste de touches autorisées** (Maj, E, et Z Q S D W pour la caméra) : toute autre touche est refusée ;
- BitCraft **fenêtre active et visible**, vérifié avant l'action **et avant chaque appui ou pas de souris** ;
- rien si Julien a tapé au clavier depuis moins de 3 s ;
- au plus **20 actions par minute** ;
- ~~Num 5~~ (supprimée en v0.3.19, D-047). Le blocage (`Halt`) reste : souris bougée, fermeture du bot. Reprise : « Démarrer » ou réactivation du repas auto ;
- **souris bougée par Julien** pendant un geste du bot (écart > 4 px) : arrêt d'urgence (case « Arrêt si la souris bouge », cochée par défaut) ;
- clics seulement dans une zone autorisée ;
- touches et bouton **toujours relâchés**, même en cas d'erreur ou d'arrêt ;
- chaque action, refus ou interruption est écrit dans le journal (catégorie « Actions »).

v0.3.18 : Num 5 toujours actif. v0.3.19 : Num 5 supprimée (D-047).

## Alternatives écartées
- Garder des sécurités propres à chaque fonction : risque d'oubli quand H3 et H4 ajouteront des actions.

## Conséquences
- Le repas automatique (Maj + E) est la première action branchée sur le contrôleur.
- H3 utilisera `ClickAsync` (trajet de souris humain, `HumanMouse`) et `PressAsync`.
- Tests : `V0318Tests` (10 cas : ordre des touches, refus, limite par minute, Num 5 pendant l'appui, jeu quitté, clic, zone, souris bougée).
