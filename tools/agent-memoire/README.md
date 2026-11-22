# Agent mémoire

But : garder la mémoire du projet (`memory/`) juste et cohérente, sans y passer de temps. Demande de Julien (2026-11-22).

## Comment s'en servir (Julien)
1. De temps en temps (par exemple une fois par semaine, ou après une grosse session), ouvrir Claude Code dans `C:\Users\throj\Desktop\bitcraft-bot` avec le modèle **Sonnet**.
2. Coller le contenu de [PROMPT.md](PROMPT.md) (le bloc sous la ligne).
3. Lire son rapport (`memory/journal/…-verification-memoire.md`) : la partie « À trancher par Julien » demande ta réponse.

## Contrôle rapide sans agent
```powershell
powershell -ExecutionPolicy Bypass -File tools/agent-memoire/verifier-memoire.ps1
```
Liens cassés, numéros des décisions et questions, index, résumé global, version, jeton ou e-mail. Bilan en une ligne.

## Fichiers
- [PROMPT.md](PROMPT.md) — le prompt de l'agent.
- [REGLES.md](REGLES.md) — les règles contrôlées (A : automatiques, B : de sens).
- `verifier-memoire.ps1` — les contrôles automatiques.
