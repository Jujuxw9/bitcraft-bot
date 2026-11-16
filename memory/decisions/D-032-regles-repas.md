# D-032 — Règles de repas

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-030, D-028

## Décision
Le bot mange (un clic sur l'aliment de la barre d'action) dans deux cas :
1. **Faim sous 50 %**.
2. Il doit **récupérer de l'endurance** (à 0, ou avant toute action qui en demande) **et** le bonus de nourriture (« Food Regen ») n'est **plus actif**.

## Emplacement de l'aliment
Choix laissé à Claude : **case 5** de la barre d'action, celle où Julien a déjà sa nourriture sur les captures. Le bot clique dessus à la souris (D-024). Il vérifie qu'il en reste (nombre affiché sur la case).

## Informations utiles
- La faim baisse de **1 par minute** (info-bulle « -1/1m »).
- Le bonus de nourriture donne endurance +5/min et vie +3/min pendant environ 14 min (icône verte au-dessus des barres, avec la durée).

## Conséquences
- Le bot doit savoir lire : la faim (ligne blanche à gauche du losange), la présence du bonus de nourriture (icône verte) et le nombre d'aliments en case 5.
