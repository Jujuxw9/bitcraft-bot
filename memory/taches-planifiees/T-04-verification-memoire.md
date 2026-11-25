# T-04 — Vérification complète de la mémoire, en lecture seule (tâche planifiée du 26/11, 14:00)

Demande de Julien (26/11) : après les trois versions de la journée, un contrôle **global** de la mémoire. Cette tâche **ne modifie rien** : elle lit tout, comprend, et écrit un **rapport de recommandations**. Julien donnera le rapport à la session principale, qui jugera quoi corriger.

## Lecture seule, strictement
- Aucune modification dans le dépôt : ni `memory/`, ni `CLAUDE.md`, ni le code, ni `etat.md`. **Pas de `git pull`, pas de commit, pas de push** (la tâche T-03 peut encore travailler dans le dossier : ne pas la gêner).
- Commandes permises : lire des fichiers, chercher, `git log` / `git show` / `git status` (lecture), et le script de contrôle `tools/agent-memoire/verifier-memoire.ps1` (il ne fait que lire).
- **Seul fichier écrit** : le rapport, `artifacts\rapports\rapport-memoire-2026-11-26.md` (dossier ignoré par Git ; le créer s'il manque).
- Jamais de question à Julien, jamais d'attente, aucune autre session ni aucun agent, ne jamais se bloquer (une étape qui échoue deux fois est notée dans le rapport et sautée).
- Si `memory/taches-planifiees/etat.md` montre T-03 pas finie : le noter en tête du rapport (la mémoire peut être en cours de modification) et continuer.

## Ce qu'il faut lire
**Toute la mémoire** : `CLAUDE.md`, `memory/` entier (tous les dossiers, tous les fichiers, y compris `journal/` et `echanges/`), `tools/agent-memoire/REGLES.md` et `PROMPT.md` (méthode de l'agent mémoire), et la mémoire automatique de Claude : `C:\Users\throj\.claude\projects\C--Users-throj-Desktop-bitcraft-bot\memory\` (MEMORY.md et ses fichiers). Pour vérifier un fait technique, regarder le code (`src/`) ou les journaux (`artifacts/release/journaux/`) en lecture.

## Ce qu'il faut chercher (« lint » de la mémoire)
1. **Contrôles automatiques** : lancer `powershell -ExecutionPolicy Bypass -File tools/agent-memoire/verifier-memoire.ps1`, reporter chaque ERREUR et ATTENTION.
2. **Doublons** : un même sujet expliqué à plusieurs endroits, surtout avec des contenus différents ; fichiers qui pourraient être fusionnés.
3. **Faux ou dépassé** : fait contredit par le code, les journaux ou une décision plus récente ; version, prochaine étape ou état faux dans `resume-global.md` ; décisions qui se contredisent ; décision remplacée sans le dire ; question « Ouverte » déjà répondue ; « À vérifier » déjà vérifié (ou l'inverse : fait affirmé sans preuve).
4. **Structure** : liens cassés, index (`README.md` de chaque dossier) incomplets ou en trop, numéros D-NNN / Q-NNN en double ou sautés, fichiers de plus de ~300 lignes à découper, fichiers orphelins (liés nulle part).
5. **Règles du projet** : langue (français partout sauf code), jeton ou e-mail de Julien écrit quelque part (le signaler **sans le recopier**), règles de CLAUDE.md respectées par la mémoire elle-même.
6. **Lisibilité pour une nouvelle session** : ce qu'une session qui reprend à blanc ne trouverait pas ou comprendrait mal.

## Le rapport (`artifacts\rapports\rapport-memoire-2026-11-26.md`)
En français, phrases courtes, classé par importance :
- En-tête : date, heure, état de T-01 à T-03 vu dans `etat.md`, nombre de fichiers lus.
- **Résultat du script** (erreurs, points d'attention).
- **Recommandations**, numérotées, chacune avec : fichier(s) et ligne(s), citation courte, problème, correction proposée, importance (haute, moyenne, basse), et « sûr » (correction mécanique) ou « à trancher par Julien » (touche au sens d'une décision).
- **Résumé en 5 lignes** pour Julien.
Terminer par le chemin complet du rapport dans la réponse finale de la session.
