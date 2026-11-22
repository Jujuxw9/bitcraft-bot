# D-051 — Jamais de Release ni de tag GitHub : .exe local seulement

- **Statut** : Validée
- **Date** : 2026-11-22
- **Validée par** : Julien (« JAMAIS », réponse du 2026-11-22 ; demande d'origine du 2026-11-19, inscrite dans CLAUDE.md)
- **Liens** : remplace en partie D-010 (GitHub Releases) ; D-040 (dossiers release / old)

## Décision
- Chaque version : `.exe` fabriqué en local par `tools/publier-version.ps1`, rangé dans `artifacts/release/BitcraftBot-vX.Y.Z.exe` (D-040). **Jamais** de Release GitHub, **jamais** de tag.
- Puis commit, push et synchronisation, comme d'habitude.

## Conséquences
- D-010 : le principe du .exe unique reste ; la publication en GitHub Releases est abandonnée.
