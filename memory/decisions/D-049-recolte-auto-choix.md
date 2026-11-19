# D-049 — Récolte automatique : arbres d'abord, vieux arbres, caméra W, clics de Julien comme indices

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien (réponses à Q-023)
- **Liens** : résout Q-023 ; D-021, D-024, D-034, D-036 ; plan `echanges/2026-11-19-plan-recolte-auto.md`

## Réponses de Julien
1. « Commence par arbre. »
2. Vieux ou jeunes arbres : « comme tu veux, le plus simple ».
3. Caméra : « en appuyant sur W tu fais un reset caméra pour avoir le bon zoom. Ensuite tu as Q et D pour aller à droite et à gauche en vision. À manipuler avec précaution car sensible. »
4. Écoute des clics de souris : « OK pas de problème, mais ce n'est pas une vérité absolue, c'est une source d'idée. »

## Décision
- **Arbres d'abord** (R1 à R5). Les roches ensuite (R6), avec la même chaîne.
- **Vieux arbres seulement** (« Mature », règle D-034 déjà codée : `ResourceNames.IsHarvestTarget`, Maker's Tree exclu). Le plus simple : la règle existe, la cible est plus grande à l'écran, le bot reste longtemps sur le même arbre. Les jeunes arbres pourront venir après, si aucun vieil arbre n'est proche.
- **Caméra** : **W** avant chaque visée (orientation et zoom de référence). **Q / D** (rotation gauche / droite) : seulement si un arbre est caché, par **appuis très courts**, puis W pour revenir à la référence. Aucune autre touche de caméra.
- **Clics de Julien** (R1) : écoutés en lecture seule, gardés comme **indices** pour trouver la formule de la caméra, **jamais comme vérité**. Une paire n'est retenue que si le serveur confirme la ressource frappée juste après, et les paires qui s'écartent trop des autres sont écartées. Seuls les clics du bot vérifiés par le serveur (R4) valident la formule.

## Conséquences
- Contrôleur d'actions : ajouter W, et Q / D en appuis courts limités (liste des touches autorisées, D-045 : Z Q S D W y sont déjà).
- Si Q / D s'avèrent trop sensibles, ne pas tourner la caméra : passer à l'arbre suivant.
