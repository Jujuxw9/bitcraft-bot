# D-019 — Français partout, sauf le code

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : Q-009, remplace D-012

## Contexte
D-012 prévoyait des messages de commit techniques en anglais. Le prompt d'init demandait des commits en français. Incohérence relevée en session 02 (Q-009).

## Décision
- **En anglais** : uniquement les lignes de code (identifiants, mots-clés, noms de fichiers source `.cs`, `.xaml`, `.csproj`).
- **En français** : tout le reste. Commentaires du code, messages de commit, documentation, `memory/`, README, noms d'étapes du workflow, messages affichés par les scripts, textes de l'interface.

Julien doit pouvoir tout comprendre sans traduire.

## Alternatives écartées
- Commits techniques en anglais (D-012 d'origine).
- Mixte `docs:` en français / `feat:` en anglais.

## Conséquences
- D-012 passe au statut « Remplacée par D-019 ».
- Les préfixes de commit restent `docs:`, `chore:`, `feat:`, `fix:` (convention), la description est en français.
