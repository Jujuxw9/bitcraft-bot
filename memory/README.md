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
- [README.md](decisions/README.md) — tableau D-001 à D-061

### questions-ouvertes/
- [README.md](questions-ouvertes/README.md) — tableau Q-001 à Q-027

### journal/
- [README.md](journal/README.md) — liste des sessions

### echanges/
- [resume-global.md](echanges/resume-global.md) — **à lire en premier**
- [2026-11-15-cadrage-initial.md](echanges/2026-11-15-cadrage-initial.md) — session de cadrage n°1
- [2026-11-15-prompt-init-repo.md](echanges/2026-11-15-prompt-init-repo.md) — prompt d'initialisation du repo (archive)
- [2026-11-16-feuille-de-route.md](echanges/2026-11-16-feuille-de-route.md) — prochaines étapes, captures et infos demandées à Julien
- [2026-11-16-demande-captures-lot5.md](echanges/2026-11-16-demande-captures-lot5.md) — lot 5 : captures et vidéos précises demandées
- [2026-11-16-demande-lot6.md](echanges/2026-11-16-demande-lot6.md) — lot 6 : reste du lot 5, captures avec Num 0
- [2026-11-18-essai-h1.md](echanges/2026-11-18-essai-h1.md) — essai H1 (fait : lecture OK, ordre bot → jeu)
- [2026-11-18-essai-h2.md](echanges/2026-11-18-essai-h2.md) — essais H2 (v0.3.11, v0.3.12 : faits)
- [2026-11-19-essai-v0314.md](echanges/2026-11-19-essai-v0314.md) — essai v0.3.14 (archive, version dépassée) (overlay, Num 5, sac et banques, recettes)
- [2026-11-19-essai-v0315.md](echanges/2026-11-19-essai-v0315.md) — essai v0.3.15 (archive, version dépassée) (repas automatique, images des recettes)
- [2026-11-19-essai-v0316.md](echanges/2026-11-19-essai-v0316.md) — essai v0.3.16 (onglets regroupés, bonus actifs)
- [2026-11-19-essai-v0317.md](echanges/2026-11-19-essai-v0317.md) — essai v0.3.17 (fait : formats et % du métier vérifiés)
- [2026-11-19-essai-v0318.md](echanges/2026-11-19-essai-v0318.md) — essai v0.3.18 (archive, version dépassée) (thème, repas auto actif, Num 5, fermeture)
- [2026-11-19-passation-session-14.md](echanges/2026-11-19-passation-session-14.md) — passation vers la session 14 (traitée)
- [2026-11-19-essai-v0319.md](echanges/2026-11-19-essai-v0319.md) — essai v0.3.19 (fait : journal des tâches OK, image du jeu et icônes en panne → v0.3.20)
- [2026-11-19-essai-v0320.md](echanges/2026-11-19-essai-v0320.md) — essai v0.3.20 (pas fait : Julien juge que ce n'est pas utile)
- [2026-11-19-plan-recolte-auto.md](echanges/2026-11-19-plan-recolte-auto.md) — **plan de bataille de la récolte automatique** (étapes R1 à R7, validé : D-049)
- [2026-11-19-passation-recolte-auto.md](echanges/2026-11-19-passation-recolte-auto.md) — **passation** : prompt pour le nouveau chat (R1 à R7)
- [2026-11-19-essai-v040.md](echanges/2026-11-19-essai-v040.md) — essai v0.4.0 (fait le 22/11 : formule de la caméra pas fiable, voir l'analyse de la visée)
- [2026-11-22-essai-v041.md](echanges/2026-11-22-essai-v041.md) — essai v0.4.2 (fait le 22/11 : connexion pas reconnue, lectures de texte en même temps → v0.4.3)
- [2026-11-23-essai-v043.md](echanges/2026-11-23-essai-v043.md) — essai v0.4.3 (fait le 23/11 : fenêtres OK, cône figé après W, 20 « écran inconnu » en chasse)
- [2026-11-23-essai-v044.md](echanges/2026-11-23-essai-v044.md) — **essai v0.4.4 à faire** (sprint, barre d'action, sacs au sol, contrôle après W)
- [2026-11-23-passation-v044.md](echanges/2026-11-23-passation-v044.md) — **passation** : prompt pour le prochain chat (après la v0.4.4)
- [2026-11-23-passation-v045.md](echanges/2026-11-23-passation-v045.md) — **passation** après la v0.4.5 (module « @GPT » branché, envoi réel ; chat ouvert reconnu ; prochaines étapes).
- [2026-11-23-passation-v046.md](echanges/2026-11-23-passation-v046.md) — **passation** après la v0.4.6 (recettes en logigramme ; chat qui marche ; W à revoir ; prochaines étapes).
- [2026-11-23-passation-v047.md](echanges/2026-11-23-passation-v047.md) — **passation** après la v0.4.7 (chat dans 4 canaux par les commandes du jeu ; prochaines étapes).
- [2026-11-23-prompt-chatbot.md](echanges/2026-11-23-prompt-chatbot.md) — **prompt** de la session chatbot (module « @GPT », en parallèle de la session principale : zone de fichiers, versions, règles).
- [2026-11-25-prompt-reprise-opus.md](echanges/2026-11-25-prompt-reprise-opus.md) — **prompt** de reprise pour une session Opus 5.5 : analyser les journaux de la v0.4.10, faire le bilan, puis demander les nouvelles fonctionnalités.
- [2026-11-24-prompt-session-taches.md](echanges/2026-11-24-prompt-session-taches.md) — **prompt** de la session « tâches » (Sonnet, quota réduit : onglet Voyageurs, bugs Recettes, tâches au fil de l'eau).
- [2026-11-24-reprise-apres-compact.md](echanges/2026-11-24-reprise-apres-compact.md) — phrase de reprise après le /compact de la session « tâches » et état de la v0.4.13.

### technique/
- [README.md](technique/README.md) — index
- [architecture.md](technique/architecture.md) — architecture logicielle cible
- [analyse-bitcraftpublic.md](technique/analyse-bitcraftpublic.md) — analyse du code serveur
- [sources.md](technique/sources.md) — liens analysés
- [liens-utilises.md](technique/liens-utilises.md) — tous les liens utilisés, classés
- [hybride/architecture-serveur.md](technique/hybride/architecture-serveur.md) — brique Server et versions v0.3.5 à v0.4.4
- [hybride/](technique/hybride/README.md) — voie hybride : plan, livrables, constats (D-039)
- [hybride/analyse-visee-2026-11-22.md](technique/hybride/analyse-visee-2026-11-22.md) — **analyse de la visée après l'essai R1** : méthode fiable, prochaines étapes, améliorations
- [reglages-du-jeu.md](technique/reglages-du-jeu.md) — réglages du jeu lus dans le registre, 4 à changer ? (Q-026)
- [chat-gpt.md](technique/chat-gpt.md) — **étude du module « @GPT »** (répondre dans le chat avec Claude Haiku) : lecture du chat, envoi, risques (Q-027)
- [analyse-captures-2026-11-16.md](technique/analyse-captures-2026-11-16.md) — analyse des captures (lots 1 à 4)
- [interface-du-jeu.md](technique/interface-du-jeu.md) — fenêtres internes du jeu (déplaçables, superposées)
- [analyse-lot5-2026-11-16.md](technique/analyse-lot5-2026-11-16.md) — journaux v0.2.2, patch 1.0, lot 5 et vidéos

### tests/
- [README.md](tests/README.md) — tableau des tests (T-20261116-01 à T-20261123-02)
