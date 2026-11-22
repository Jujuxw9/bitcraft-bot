# Règles de la mémoire (ce que l'agent mémoire contrôle)

## A. Contrôles mécaniques (faits par `verifier-memoire.ps1`)

| N° | Règle | Gravité |
|---|---|---|
| A1 | Tout lien Markdown vers un fichier mène à un fichier qui existe | Erreur |
| A2 | Chaque décision `D-NNN-sujet.md` et question `Q-NNN-sujet.md` : nom au bon format, numéro unique, listée dans l'index du dossier | Erreur |
| A3 | Le statut écrit dans le fichier correspond à celui de l'index | Attention |
| A4 | Un statut « Résolue → D-NNN » ou « Remplacée par D-NNN » renvoie vers une décision qui existe | Erreur |
| A5 | Pas de trou dans la suite des numéros | Attention |
| A6 | Chaque fichier de `memory/` est cité dans le `README.md` de son dossier ou dans `memory/README.md` | Erreur |
| A7 | `echanges/resume-global.md` : 10 lignes numérotées au plus | Erreur |
| A8 | Fichier de plus de ~300 lignes : à découper | Attention |
| A9 | La version de `Directory.Build.props` est citée dans le résumé global et son .exe existe dans `artifacts/release` | Attention |
| A10 | Aucun jeton ni adresse e-mail personnelle dans la mémoire (D-039) | Erreur |

## B. Contrôles de sens (faits par l'agent, à la lecture)

| N° | Règle |
|---|---|
| B1 | Deux décisions « Validée » ne se contredisent pas. Sinon, la plus ancienne doit être « Remplacée par D-NNN ». |
| B2 | Une question « Ouverte » n'a pas déjà sa réponse ailleurs (journal, essai, décision). |
| B3 | Le résumé global dit la vérité : version en cours, état, prochaine étape, questions en attente. |
| B4 | Ce qui n'est pas vérifié est marqué « À vérifier » (CLAUDE.md : ne pas inventer). |
| B5 | Le même sujet n'est pas décrit à deux endroits avec des contenus différents. |
| B6 | Français partout, sauf le code (D-019). |
| B7 | Les décisions remplacées ne sont jamais supprimées (memory/README.md, règle 2). |
| B8 | Chaque session a son fichier dans `journal/`, cité dans `journal/README.md`. |

## C. Organisation (rappel de `memory/README.md`)
- `decisions/` : une décision par fichier ; `questions-ouvertes/` : une question par fichier ; `journal/` : une entrée par session ; `echanges/` : conversations et essais ; `technique/` : architecture, analyses ; `tests/` : comptes rendus de tests.
- Identifiants séquentiels, jamais réutilisés. Dates au format `AAAA-MM-JJ`.
