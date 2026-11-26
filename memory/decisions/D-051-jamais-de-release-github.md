# D-051 — Jamais de Release ni de tag GitHub : .exe local seulement

- **Statut** : Remplacée par D-076 (2026-11-26)
- **Date** : 2026-11-22
- **Validée par** : Julien (« JAMAIS », réponse du 2026-11-22 ; demande d'origine du 2026-11-19, inscrite dans CLAUDE.md)
- **Liens** : remplace en partie D-010 (GitHub Releases) ; D-040 (dossiers release / old)

## Décision
- Chaque version : `.exe` fabriqué en local par `tools/publier-version.ps1`, rangé dans `artifacts/release/BitcraftBot-vX.Y.Z.exe` (D-040). **Jamais** de Release GitHub, **jamais** de tag.
- Puis commit, push et synchronisation, comme d'habitude.

## Conséquences
- D-010 : le principe du .exe unique reste ; la publication en GitHub Releases est abandonnée.

## Exception
- v0.4.21 : Release GitHub faite à la demande de Julien (26/11), pour télécharger le bot sur un autre PC (D-075). La règle reste valable pour les autres versions.
