# D-018 — Localisation par OCR du HUD

- **Statut** : Validée
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : Q-002

## Contexte
Le bot doit savoir où il est.

## Décision
La position est lue sur l'interface, sous la minicarte ronde en haut à gauche : coordonnées (ex. `N 8969, E 6739`), nom de zone (ex. « Safe Meadows », **À vérifier** : orthographe), région (ex. `R18`). L'heure du jeu est affichée au-dessus de la minicarte.

## Alternatives écartées
- Estimation de position par la vision 3D seule : imprécise.

## Conséquences
- Zone OCR fixe, valable en 1920×1080 échelle par défaut (D-006).
