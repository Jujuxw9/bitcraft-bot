# D-012 — Langue : doc en français, code en anglais

- **Statut** : Validée
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : Q-005, Q-009

## Contexte
Julien est francophone ; les outils et bibliothèques sont en anglais.

## Décision
Documentation et mémoire en français. Code (identifiants, noms de fichiers, messages de commit techniques) en **anglais**, commentaires du code en **français**.

## Alternatives écartées
- Tout en français : identifiants peu lisibles avec les API .NET.

## Conséquences
- **Incohérence relevée le 2026-11-16** : le prompt d'init exige des messages de commit en français. Voir Q-009.
