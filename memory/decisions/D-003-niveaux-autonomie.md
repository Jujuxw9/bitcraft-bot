# D-003 — Niveaux d'autonomie N1 → N2 → N3

- **Statut** : Validée
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : Q-003

## Contexte
Le but est d'apprendre à concevoir un agent autonome, par étapes.

## Décision
- N1 : boucles de tâches pilotées par la vision (machine à états).
- N2 : planificateur d'objectifs (ex. « 200 planches » → récolte, transport, craft).
- N3 : consignes de haut niveau.

Développement dans cet ordre.

## Alternatives écartées
- Démarrer directement par un planificateur ou un LLM : pas de base perception/action fiable.

## Conséquences
- `BitcraftBot.Core` contient d'abord la machine à états.
