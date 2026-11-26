# T-16 (12:00) — Contrôle complet de la mémoire, bilan de la nuit, phrase de reprise

Règles communes : `nuit-2026-11-27-regles.md`. Tu es la **dernière** tâche. Lis tout le bilan.

## À faire
1. **Contrôle de la mémoire** : `tools/agent-memoire/verifier-memoire.ps1` à 0 erreur ; puis vérification de sens selon `tools/agent-memoire/PROMPT.md` (sans lancer d'agent : tu la fais toi-même) : index de chaque dossier à jour, décisions de la nuit numérotées sans doublon et indexées, questions ouvertes à jour (fermer celles réglées, ouvrir celles de la nuit), `resume-global.md` exact et ≤ 10 lignes, liens cassés, fichiers de plus de ~300 lignes (Julien accepte un peu plus si l'espace reste réduit), doublons entre fichiers, informations périmées (versions, états « À vérifier » réglés).
2. Corriger ce qui est sûr. Ce qui demande Julien : dans la liste des questions.
3. **Mettre à jour l'état du projet** : `etat.md` des tâches, `memory/taches-planifiees/README.md` (ajouter la nuit du 27/11), `memory/journal/README.md`.
4. **Résumé pour Julien** en tête du bilan (`bilan-nuit-2026-11-27.md`, section « Résumé pour Julien ») : en français simple, phrases courtes :
   - versions sorties (laquelle lancer : la dernière de `artifacts\release`) ;
   - ce qui marche, ce qui a été essayé en vrai (T-11), ce qui n'a pas été fait ;
   - **ce que Julien doit essayer, dans l'ordre** (une ligne chacun) ;
   - toutes les questions pour Julien (regroupées, sans doublon) ;
   - quota consommé dans la nuit (premier et dernier relevé).
5. **Passation** : `memory/echanges/2026-11-27-passation-nuit.md` (même forme que `2026-11-26-passation-session-29.md`) avec la **phrase de reprise** pour la prochaine session :
   « Reprends le projet bitcraft-bot : lis CLAUDE.md, puis memory/echanges/2026-11-27-passation-nuit.md et suis-le. Ne lance aucune autre session ni aucun agent sans mon accord. »
   Mettre ce fichier dans `memory/README.md` et `resume-global.md` (ligne « Prochaine étape »).
6. Mémoire automatique de Claude (`C:\Users\throj\.claude\projects\C--Users-throj-Desktop-bitcraft-bot\memory\`) : vérifier que les fiches existantes sont toujours vraies (versions, règles) ; corriger si besoin (sans y mettre de secret).

## Fini quand
- Vérificateur à 0 erreur, bilan avec le résumé, passation écrite, tout poussé. Aucune version.
