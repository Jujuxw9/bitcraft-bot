# D-009 — Langage : C# / .NET 10, WPF

- **Statut** : Validée
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : D-010, D-012

## Contexte
Besoin de capture et d'entrées Windows natives, d'une bonne UI et d'un .exe unique.

## Décision
C# / .NET 10, interface WPF. Raisons : capture et entrées Windows natives, bonne interface graphique, OpenCvSharp et ONNX Runtime disponibles, .exe unique de taille raisonnable.

## Alternatives écartées
- Python : exe lourd, UI faible.
- Rust : UI et vision immatures, développement lent.

## Conséquences
- Solution `BitcraftBot.sln`, cible `net10.0-windows`.
