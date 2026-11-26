# D-075 — v0.4.21 : zone lue de 25 cases, boutons T1 à T5, Release GitHub de la v0.4.21

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (26/11 au soir)
- **Liens** : D-051, D-066, D-073, [architecture-suite.md](../technique/architecture-suite.md) section 25

## Décision
1. **Zone lue et carte : 25 cases** (20 avant) : ressources lues sur 75 petites cases, fond de carte sur 29 cases, cercle « 25 » ajouté sur la carte.
2. **Tiers** : boutons T1 à T5 seulement (Julien ne dépasse pas T4-T5 pour l'instant). Les tiers au-dessus n'ont pas de bouton et restent affichés, sauf avec « ce tier seul ».
3. **Release GitHub de la v0.4.21** (tag `v0.4.21`, avec le .exe) : **exception** à D-051, demandée par Julien pour télécharger le bot sur un autre PC. La règle reste : pas de Release ni de tag pour les autres versions, sauf nouvelle demande de Julien.

## Conséquences
- Tests 628/628. Essai sur le serveur pas fait (jeu ouvert, éjection) : la zone plus grande est « À vérifier » ; en cas de refus, le repli de D-071 retire d'abord le fond de carte.
- Données reçues plus lourdes (environ 1,5 fois celles de la zone de 20 cases).
