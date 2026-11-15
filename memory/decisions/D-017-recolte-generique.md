# D-017 — Récolte générique

- **Statut** : Validée
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : D-002, Q-001, Q-006

## Contexte
Le jeu propose de nombreux types de ressources.

## Décision
Le bot doit savoir identifier puis récolter n'importe quel type de ressource (arbres, bushes, ferralith veins, limestone, lavande, fleurs, champignons, branches/sticks au sol, silex…). Architecture générique dès le départ ; le premier type validé en test sert de référence (choix en Q-006).

## Alternatives écartées
- Code spécifique à un seul type : à réécrire ensuite.

## Conséquences
- Les ressources sont décrites par des données (`ResourceDesc`, `ExtractionRecipeDesc`), pas codées en dur.
