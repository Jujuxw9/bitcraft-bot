# Outils

Scripts utilitaires : capture d'écran, annotation d'images, extraction des schémas de BitCraftPublic.

| Outil | Rôle | Usage |
|---|---|---|
| `valider-exe.ps1` | Publie le .exe d'une version validée, archive l'ancien (D-020) | `powershell -ExecutionPolicy Bypass -File tools/valider-exe.ps1 -Version 0.1.0` |
| `generer-icone.ps1` | Dessine le logo (arbre-robot dans un hexagone) et produit `src/BitcraftBot.App/Assets/bitcraftbot.ico` | `powershell -ExecutionPolicy Bypass -File tools/generer-icone.ps1` |
