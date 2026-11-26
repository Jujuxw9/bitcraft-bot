# D-076 — Releases GitHub aux grandes étapes seulement (0.4, 0.5, 0.6…)

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (« on peut faire des releases, mais de temps en temps, quand il y a de grosses différences »)
- **Liens** : remplace D-051 ; D-040, D-075

## Décision
1. Chaque version : le `.exe` est toujours fabriqué en local (`tools/publier-version.ps1`, `artifacts/release`, D-040), puis commit et push.
2. **Release GitHub (avec tag et .exe) seulement aux grandes étapes** : une par palier de version. La 0.4 a la sienne (v0.4.21, D-075) ; la prochaine sera pour une version **0.5.x**.
3. En cas de doute (grosse différence au milieu d'un palier), demander à Julien avant.

## Conséquences
- D-051 passe à « Remplacée par D-076 ». CLAUDE.md et la mémoire automatique sont mis à jour.
- Le dépôt est public : le .exe d'une Release est téléchargeable par tous (il ne contient ni jeton ni e-mail).
