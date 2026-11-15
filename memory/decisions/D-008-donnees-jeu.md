# D-008 — Données de jeu : base locale depuis BitCraftPublic

- **Statut** : Validée
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : Q-004

## Contexte
Le bot a besoin des items, recettes et ressources.

## Décision
Base locale (items, recettes, ressources) construite à partir des schémas de BitCraftPublic.

## Alternatives écartées
- Lecture en direct du serveur : interdit (D-001).

## Conséquences
- Les schémas existent, pas les valeurs : source des valeurs à trouver (Q-004).
- Code dans `BitcraftBot.GameData`, données dans `data/`.
