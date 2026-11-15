# Échange — 2026-11-15 — Cadrage initial

- **Agent** : Claude (chat)
- **Participants** : Julien, Claude

> Note (Claude Code, 2026-11-16) : ce fichier est reconstitué à partir du prompt d'initialisation. Le texte exact des 15 questions de cadrage n'y figurait pas. Seules les réponses retenues (décisions et questions ouvertes) sont reportées. **À compléter** par Claude chat si le détail est utile.

## 1. Création du repo

- Repo `Jujuxw9/bitcraft-bot` créé sur GitHub : public, licence MIT, README et `.gitignore` Python générés par GitHub.

## 2. Analyse des règles du jeu

- Les Community Guidelines interdisent toute automatisation, même supervisée. Détail : [regles-et-risques.md](../00-contexte/regles-et-risques.md).
- Julien accepte le risque sur son compte principal.

## 3. Choix de l'orientation

- Trois options : A (serveur officiel, vision + clavier/souris), B (serveur local), C (copilote en lecture seule).
- Choix : **A** → [D-001](../decisions/D-001-orientation-a.md).

## 4. Questions de cadrage et réponses

Réponses retenues, sous forme de décisions (toutes validées le 2026-11-15) :

| Sujet | Décision |
|---|---|
| MVP | [D-002](../decisions/D-002-mvp.md) |
| Niveaux d'autonomie | [D-003](../decisions/D-003-niveaux-autonomie.md) |
| Sécurité | [D-004](../decisions/D-004-securite.md) |
| Fenêtre de supervision | [D-005](../decisions/D-005-fenetre-supervision.md) |
| Affichage du jeu | [D-006](../decisions/D-006-affichage-jeu.md) |
| Vision de l'UI | [D-007](../decisions/D-007-vision-interface.md) |
| Données de jeu | [D-008](../decisions/D-008-donnees-jeu.md) |
| Langage | [D-009](../decisions/D-009-langage-csharp.md) |
| Distribution | [D-010](../decisions/D-010-distribution.md) |
| Mémoire | [D-011](../decisions/D-011-memoire.md) |
| Langue | [D-012](../decisions/D-012-langue.md) |
| Dossier images | [D-013](../decisions/D-013-dossier-images.md) |
| Repo public | [D-014](../decisions/D-014-repo-public.md) |
| Monde et langue du jeu | [D-015](../decisions/D-015-monde-langue-jeu.md) |
| Dépôt | [D-016](../decisions/D-016-depot-banque.md) |
| Récolte générique | [D-017](../decisions/D-017-recolte-generique.md) |
| Localisation | [D-018](../decisions/D-018-localisation-ocr-hud.md) |

Questions restées ouvertes : Q-001, Q-002, Q-003, Q-004, Q-006, Q-008. Questions tranchées en séance : Q-005 (→ D-012), Q-007 (→ D-015). Index : [questions-ouvertes/README.md](../questions-ouvertes/README.md).

## 5. Analyse des liens

Voir [sources.md](../technique/sources.md) et [analyse-bitcraftpublic.md](../technique/analyse-bitcraftpublic.md).

## 6. Sortie

- Rédaction du prompt d'initialisation du repo pour Claude Code : [2026-11-15-prompt-init-repo.md](2026-11-15-prompt-init-repo.md).
