# Mémoire du projet bitcraft-bot — INDEX

Ce dossier contient toute la mémoire du projet. Il doit être compréhensible par n'importe quel agent IA (Claude chat, Claude Code, autre) et par Julien.

## Règles de mise à jour

1. Tout agent (Claude chat, Claude Code, autre) lit `memory/README.md` puis `echanges/resume-global.md` avant toute action.
2. Toute décision validée par Julien devient un fichier `decisions/D-NNN-*.md`. Une décision remplacée passe au statut « Remplacée par D-NNN », elle n'est jamais supprimée.
3. Toute interrogation non tranchée devient `questions-ouvertes/Q-NNN-*.md`. Une fois tranchée : statut « Résolue → D-NNN ».
4. Chaque session de travail ajoute un fichier dans `journal/` et met à jour `echanges/resume-global.md`.
5. Identifiants séquentiels, jamais réutilisés. Dates au format ISO `AAAA-MM-JJ`.
6. Ne pas inventer : ce qui n'est pas vérifié est marqué « À vérifier ».

Règle de découpage : dès qu'un fichier dépasse ~300 lignes ou mélange plusieurs sujets, le découper et mettre l'index à jour. Le `README.md` de chaque dossier sert d'index.

## Rôle de chaque dossier

| Dossier | Rôle |
|---|---|
| `00-contexte/` | Contexte stable : projet, matériel, règles du jeu et risques |
| `decisions/` | Décisions validées (une par fichier, `D-NNN`) |
| `questions-ouvertes/` | Questions non tranchées (une par fichier, `Q-NNN`) |
| `journal/` | Une entrée par session de travail |
| `echanges/` | Détail des conversations + résumé global à jour |
| `technique/` | Architecture, analyses de code, sources |
| `tests/` | Comptes rendus de tests (`T-AAAAMMJJ-NN`) |

## Liste des fichiers

### 00-contexte/
- [projet.md](00-contexte/projet.md) — porteur, but, usage, jeu
- [materiel.md](00-contexte/materiel.md) — PC de Julien
- [regles-et-risques.md](00-contexte/regles-et-risques.md) — règles BitCraft, risque accepté, interdits du projet

### decisions/
- [README.md](decisions/README.md) — tableau D-001 à D-036

### questions-ouvertes/
- [README.md](questions-ouvertes/README.md) — tableau Q-001 à Q-019

### journal/
- [README.md](journal/README.md) — liste des sessions

### echanges/
- [resume-global.md](echanges/resume-global.md) — **à lire en premier**
- [2026-11-15-cadrage-initial.md](echanges/2026-11-15-cadrage-initial.md) — session de cadrage n°1
- [2026-11-15-prompt-init-repo.md](echanges/2026-11-15-prompt-init-repo.md) — prompt d'initialisation du repo (archive)
- [2026-11-16-feuille-de-route.md](echanges/2026-11-16-feuille-de-route.md) — prochaines étapes, captures et infos demandées à Julien
- [2026-11-16-demande-captures-lot5.md](echanges/2026-11-16-demande-captures-lot5.md) — lot 5 : captures et vidéos précises demandées
- [2026-11-16-demande-lot6.md](echanges/2026-11-16-demande-lot6.md) — lot 6 : reste du lot 5, captures avec Num 0

### technique/
- [README.md](technique/README.md) — index
- [architecture.md](technique/architecture.md) — architecture logicielle cible
- [analyse-bitcraftpublic.md](technique/analyse-bitcraftpublic.md) — analyse du code serveur
- [sources.md](technique/sources.md) — liens analysés
- [analyse-captures-2026-11-16.md](technique/analyse-captures-2026-11-16.md) — analyse des captures (lots 1 à 4)
- [interface-du-jeu.md](technique/interface-du-jeu.md) — fenêtres internes du jeu (déplaçables, superposées)
- [analyse-lot5-2026-11-16.md](technique/analyse-lot5-2026-11-16.md) — journaux v0.2.2, patch 1.0, lot 5 et vidéos

### tests/
- [README.md](tests/README.md) — tableau des tests (T-20261116-01 à 04)
