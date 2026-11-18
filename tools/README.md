# Outils

Scripts utilitaires.

| Outil | Rôle | Usage |
|---|---|---|
| `publier-version.ps1` | Fabrique le .exe de la version de `Directory.Build.props`, range l'ancien dans `artifacts/old/`, met le nouveau dans `artifacts/release/` (D-040) | `powershell -ExecutionPolicy Bypass -File tools/publier-version.ps1` |
| `generer-icone.ps1` | Dessine le logo (arbre-robot dans un hexagone) et produit `src/BitcraftBot.App/Assets/bitcraftbot.ico` | `powershell -ExecutionPolicy Bypass -File tools/generer-icone.ps1` |
| `ModelesChiffres/` | Fabrique les modèles des chiffres des coordonnées à partir de `images/reference-ui/coordonnees/` (nom du fichier = valeur), fait un contrôle croisé et réécrit `GlyphTemplates.cs` | `dotnet run --project tools/ModelesChiffres` |
