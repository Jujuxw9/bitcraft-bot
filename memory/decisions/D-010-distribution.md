# D-010 — Distribution : .exe unique en GitHub Releases

- **Statut** : Remplacée en partie par D-051 (le .exe unique reste ; plus de GitHub Releases)
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : D-009

## Contexte
Julien lance un .exe à côté du jeu.

## Décision
Un seul `.exe` autonome (self-contained, single-file), versions successives v0.1, v0.2… publiées en GitHub Releases.

## Alternatives écartées
- Installeur MSI : inutile à ce stade.

## Conséquences
- Workflow `.github/workflows/release.yml` sur tag `vX.Y.Z`.
