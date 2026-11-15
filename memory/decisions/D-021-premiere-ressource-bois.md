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

## Alternatives écartées
- Autres ressources (minerai, fleurs…) : plus tard, l'architecture reste générique (D-017).

## Conséquences
- Le contour vert est un indice visuel fort pour Q-001 : piste « survol de la souris + détection du contour vert ».
- Le bot doit surveiller l'endurance et l'inventaire pour savoir quand l'action s'arrête.
