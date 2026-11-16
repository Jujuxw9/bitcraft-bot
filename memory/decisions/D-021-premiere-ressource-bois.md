# D-021 — Première ressource : le bois (arbres)

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : Q-006, D-002, D-017

## Contexte
Le MVP doit commencer par un seul type de ressource (Q-006).

## Décision
- Ressource : **arbres → bois**.
- Outil : **hache**. Julien indique qu'elle est choisie automatiquement par le jeu (**À vérifier** sur capture).
- Zone : **au choix de Claude**, une fois les captures analysées.

## Mécanique de récolte décrite par Julien
1. Placer la souris sur l'arbre : son contour s'entoure d'un **trait vert** (sélection).
2. **Un seul clic droit** : le personnage lance l'action.
3. L'action continue tant qu'il reste de l'endurance (stamina) et de la place dans l'inventaire.

## Constats du 2026-11-16 (lot 2 de captures)
- Arbre observé : `Mature Oak Tree`, `Tier 1 - Tree`, 12 000 points de santé, conditions `Axe Tier 1` et `Lv. 1 Forestry` remplies.
- Inventaire plein : message « Some items couldn't fit in your inventory and were dropped on the ground ». La récolte continue et le bois tombe au sol : le bot doit s'arrêter avant.

## Constats du lot 3 (2026-11-16)
- Un arbre donne de l'**écorce** (« Rough Tree Bark », inventaire) pendant la coupe, puis un **tronc** (« Rough Wood Trunk », cargaison lourde) dans un sac au sol quand il tombe. Choix à faire : Q-013.
- Jeunes arbres : 3 000 points (quelques minutes). Vieux arbres : 12 000 points.
- L'endurance tombe à 0 après environ 9 min de récolte et remonte de +15/min.

## Alternatives écartées
- Autres ressources (minerai, fleurs…) : plus tard, l'architecture reste générique (D-017).

## Conséquences
- Le contour vert est un indice visuel fort pour Q-001 : piste « survol de la souris + détection du contour vert ».
- Le bot doit surveiller l'endurance et l'inventaire pour savoir quand l'action s'arrête.
