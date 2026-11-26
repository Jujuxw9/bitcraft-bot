# D-071 — v0.4.19 : ressources demandées par morceau de carte, repli qui ne retire que le surplus

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (« commence par tout reprendre et tout corriger », 26/11 au soir, après l'essai de la v0.4.18)
- **Liens** : D-039, D-066, D-070, [architecture-suite.md](../technique/architecture-suite.md) section 23

## Contexte
Essai de la v0.4.18 par Julien (26/11, 18:53) : carte, voyageurs, personnage, chat et activité vides. Journal : le serveur refuse l'abonnement aux ressources autour du personnage (« Estimated cardinality … exceeds limit (20000000 rows) »). Les mêmes requêtes passaient à 13:27, au même endroit : le serveur a changé sa façon d'estimer entre-temps (cause exacte « À vérifier »). Le bot se repliait alors en retirant d'abord le fond de carte, puis les voyageurs, la barre d'action et le chat, puis le sac et les banques, et seulement à la fin les ressources : tous les onglets se sont vidés.

## Décision
1. Les requêtes de zone (ressources, santé des ressources, sacs au sol) ajoutent le **numéro du morceau de carte** (colonne indexée du serveur) au carré de coordonnées : une requête par morceau touché (1 à 4 en général). Mêmes lignes reçues qu'avant. Vérifié sur le serveur avec une sonde de lecture : carré seul refusé, morceau + carré accepté ; abonnement complet accepté.
2. **Repli dans le bon ordre**, une partie à la fois : fond de carte → sacs au sol → santé des ressources → ressources → tâches de voyageur → barre d'action, colonies et chat → sac et banques. Les sacs au sol se retirent à part.

## Alternatives écartées
- Morceaux entiers sans le carré : accepté aussi, mais 2 fois plus de données (1,9 Mo au lieu de 1,1 Mo).
- Plusieurs morceaux dans une seule requête (OR) : refusé par le serveur.

## Conséquences
- Essai réel (jeu fermé, 26/11 19:27) : tout accepté en une fois (1 848 ressources, 24 tâches de voyageur, sac et banques, chat, 6 morceaux de fond de carte). Tests 626/626.
- Vérifié par Julien (26/11, 19:33 → 19:52) : la v0.4.19 en jeu, tous les onglets se remplissent, aucune erreur au journal.
