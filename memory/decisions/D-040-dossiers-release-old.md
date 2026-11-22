# D-040 — Versions rangées dans artifacts/release et artifacts/old
> Note (vérification de la mémoire, 2026-11-22) : depuis la v0.3.15, le .exe porte la version dans son nom : BitcraftBot-vX.Y.Z.exe (CLAUDE.md). Depuis le 2026-11-19, pas de Release ni de tag GitHub (demande de Julien, CLAUDE.md).

- **Statut** : Validée
- **Date** : 2026-11-18
- **Validée par** : Julien (demande directe : « un dossier old et release, toujours la dernière version dans release »)
- **Liens** : remplace D-020 ; résout Q-010 ; D-010, D-031, D-033

## Contexte
Le dossier `executable/` (D-020) n'avait plus été mis à jour depuis la v0.1.0. Chaque version avait aussi son dossier `artifacts/vX.Y.Z/`, plus un dossier de fabrication `artifacts/publish-vXYZ/` : difficile de savoir quoi lancer.

## Décision
- `artifacts/release/` : **toujours la dernière version**. Contenu : `BitcraftBot.exe`, `config.local.json` (réglages du PC), `donnees-jeu/`, `journaux/` (tous les journaux, rangés par version : `journaux/vX.Y.Z/AAAA-MM-JJ/`).
- `artifacts/old/vX.Y.Z/BitcraftBot.exe` : les versions précédentes.
- Le script `tools/publier-version.ps1` fait tout : il fabrique le .exe de la version écrite dans `Directory.Build.props`, range l'ancien dans `old/` et met le nouveau dans `release/`.
- `artifacts/` reste sur le PC (ignoré par Git). Aucun `.exe` dans le dépôt : le dossier `executable/` est supprimé.
- Les GitHub Releases (D-010, tag `vX.Y.Z`) restent possibles.

## Alternatives écartées
- Garder `executable/` dans Git : alourdit l'historique (~60 Mo par version, Q-010).

## Conséquences
- Julien lance toujours `artifacts\release\BitcraftBot.exe`.
- Les réglages et le jeton chiffré (dans `%LOCALAPPDATA%`) ne bougent plus d'une version à l'autre.
- Le v0.1.0 déjà présent dans l'historique Git y reste (65 Mo).
