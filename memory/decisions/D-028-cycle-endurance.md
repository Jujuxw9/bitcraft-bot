# D-028 — Cycle d'endurance : attendre 100 % puis relancer

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-021

## Contexte (infos de Julien)
- À 0 d'endurance, la coupe **s'arrête toute seule**. Elle ne reprend pas quand l'endurance remonte : il faut la **relancer**.
- Pendant la remontée, le personnage peut bouger mais ne peut lancer aucune action (récolte ou autre).
- Régénération observée : +15 par minute (environ 7 min de 0 à 100).

## Décision
1. Quand l'endurance atteint 0 (ou que la coupe s'arrête), le bot **attend que la barre revienne à 100/100**.
2. Puis il refait un **clic droit** sur l'arbre pour relancer la coupe.
3. Pendant l'attente, le bot peut se déplacer (par exemple vers la Banque si l'inventaire est presque plein), mais ne lance aucune action.

## Alternatives écartées
- Relancer dès que l'endurance remonte un peu : risque de coupe très courte puis nouvel arrêt.

## Conséquences
- Le bot lit la barre d'endurance (texte `NN/100`) en continu.
- **À vérifier** : existe-t-il des actions sans endurance (ramasser des bâtons) ? Inconnu de Julien.
