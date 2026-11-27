# Tests automatiques

Un seul projet xUnit : `BitcraftBot.Tests` (756 tests à la v0.4.33, tous verts). Lancer depuis la racine :

```powershell
dotnet test
```

## Organisation

- **Un fichier par version** : `VXYZTests.cs` = ce qui a été ajouté ou corrigé dans la version X.Y.Z (ex. `V0429Tests` = « Se déplacer ici », `V0433Tests` = cas limites ajoutés au contrôle du dépôt du 27/11/2026).
- **Fichiers par sujet** (plus anciens) : `H1ServerTests` (lecture du serveur), `H2MapTests` / `H2TargetTests` (carte, cible), `GameDataTests`, `GptChatTests`, `RecipePlannerTests`, `RawMaterialsTests`, `TravelerTaskTests`.
- Quelques tests lisent de vraies captures du jeu dans `images/reference-ui/` (versionnées, sans chat ni nom de joueur).

## Règles

- Les tests ne touchent jamais au jeu ni au serveur : clavier, souris, écran et horloge sont simulés.
- Jetons dans les tests : toujours faux (ex. `eyJhbGciOi.secret.sig`).
- Chaque nouvelle version ajoute ses tests ; build sans avertissement et tests verts avant toute publication.
