# D-047 — Plus de touche d'arrêt d'urgence (Num 5 supprimée)

- **Statut** : Validée (remplaçants confirmés par Julien le 2026-11-19 : « OUI »)
- **Date** : 2026-11-19
- **Validée par** : Julien (« Num5 supprime la fonction associée à la touche »)
- **Liens** : remplace D-041 ; modifie D-022 et D-045 ; D-004

## Contexte
Depuis la v0.1, Num 5 était l'arrêt d'urgence (D-022), limité au jeu actif (D-041), puis toujours actif (D-045, v0.3.18).
Julien demande de supprimer la fonction de la touche.

**Écart signalé** : c'était la sécurité principale prévue par D-004 et D-022. La supprimer laisse le bot sans « coupe-circuit » au clavier.

## Décision
- La touche Num 5 n'a plus aucune fonction (v0.3.19). Num 0 (capture du jeu) reste.
- Ce qui reste pour arrêter ou bloquer le bot :
  - bouton **« Arrêter »** (session de lecture de l'écran) ;
  - case **« Manger (Maj + E) »** : la décocher coupe la seule action du bot ;
  - **souris bougée** par Julien pendant un geste du bot : arrêt et blocage des actions (case « Arrêt si la souris bouge », cochée) ;
  - **une touche tapée** par Julien : aucune action du bot pendant 3 s ;
  - BitCraft doit être **fenêtre active et visible** avant chaque appui ;
  - au plus 20 actions par minute ; **durée max** de session ;
  - **fermeture** du bot : tout est bloqué, touches relâchées.

## Alternatives écartées
- Garder Num 5 avec une case pour la couper : Julien avait déjà demandé la suppression de la case en v0.3.18.
- Une autre touche : pas demandé. À proposer si besoin avant H3 (le bot bougera la souris et la caméra).

## Conséquences
- Le contrôleur d'actions garde son blocage (`Halt`), utilisé par la souris bougée et la fermeture.
- Julien confirme (2026-11-19) que le bouton « Arrêter », la souris bougée et la pause quand il tape suffisent : pas d'autre touche d'arrêt, y compris pour H3.
