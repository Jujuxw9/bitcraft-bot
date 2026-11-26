# Nuit du 27/11/2026 : règles communes des tâches T-05 à T-16

Tâches voulues par Julien le 27/11 vers 00:45 (D-081). **12 sessions Claude Code indépendantes**, une par heure de 01:00 à 12:00, chacune lancée seule par l'app Claude, **sans Julien** (il dort, PC allumé, jeu et bot fermés). Chaque session part de zéro : tout le contexte est dans le dépôt. Ces règles complètent `README.md` (même dossier) ; en cas de conflit, **ce fichier gagne**.

## 0. Où tu en es
- Tu es une des 12 tâches. Ta fiche : `memory/taches-planifiees/T-NN-*.md`. La liste et l'ordre sont au §5.
- **Avant tout**, lis le **bilan de la nuit** : `memory/taches-planifiees/bilan-nuit-2026-11-27.md`. Chaque tâche avant toi y a écrit ce qu'elle a fait, ce qui a raté, ce qu'elle laisse à la suite. C'est ta source principale sur « ce qui a été fait avant ». Lis aussi les journaux `memory/journal/2026-11-27-tache-T*.md` des tâches qui te concernent.
- Contexte du projet : `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md` (D-078 à D-081 surtout), `memory/echanges/2026-11-26-demande-deplacement-carte.md` (déplacement, grande carte du jeu, touche M), `memory/technique/architecture-suite.md` (sections 26 à 28).

## 1. Au début (dans cet ordre)
1. `cd C:\Users\throj\Desktop\bitcraft-bot`, `git pull --rebase` (ignorer « [SYNC PC-PERSO] » et les commits « [PERSO JULIEN] » : autre projet).
2. **Quota** : appeler l'outil `mcp__ccd_session_mgmt__get_usage` (le charger avec ToolSearch « select:mcp__ccd_session_mgmt__get_usage » s'il est différé). Lire « 5-hour limit » et « Weekly · all models ».
   - **5 h ≥ 90 %** (il reste moins de 10 %) **ou semaine ≥ 80 %** → ne rien faire d'autre : écrire dans le bilan « T-NN non lancée : quota 5 h X %, semaine Y % » (+ commit/push) et s'arrêter.
   - Sinon noter les deux pourcentages dans le bilan (ligne de ta tâche). Si l'outil n'existe pas ou répond « unavailable » : le noter et continuer.
3. **Verrou** : dans `etat.md`, si une tâche T-05 à T-16 est « démarrée » sans « finie » ni « arrêtée » : attendre (vérifier toutes les 3 min avec `git pull`, 15 min au plus ; utiliser l'outil Monitor ou des commandes courtes, jamais un `sleep` long au premier plan). Toujours bloquée au bout de 15 min → écrire « T-NN reportée : T-MM encore en cours » dans le bilan et `etat.md`, et s'arrêter.
4. Écrire dans `etat.md` « T-NN démarrée à HH:MM (quota 5 h X %, semaine Y %) », commit + push (c'est le verrou).
5. Lire ta fiche jusqu'au bout **avant** de coder. Si une tâche dont tu dépends a raté (voir le bilan), suis la consigne « si la tâche d'avant a raté » de ta fiche.

## 2. Pendant
- **Personne ne répond** : aucune question à Julien, aucune attente de réponse. Au moindre doute, l'option la plus prudente ; la noter dans le bilan avec la question pour Julien.
- **Jamais bloquée** : commande de plus de 2 min en arrière-plan ; étape ratée deux fois = notée et sautée ; **arrêt dur à 50 min** après ton début (même incomplet : on publie seulement ce qui est testé, le reste est noté pour la suite).
- **Aucune autre session, aucun agent, aucun sous-agent.** Ne crée ni ne modifie aucune tâche planifiée.
- Interdits du projet (CLAUDE.md) : serveur en lecture seule (D-039), pas d'exploit, pas d'injection, pas de lecture mémoire du jeu, jamais le jeton ni l'e-mail de Julien dans un journal, Git ou une réponse. Le dépôt est **public** : aucune adresse IP, aucun mot de passe.
- **Le jeu** : avant toute connexion au serveur, vérifier qu'aucun processus `BitCraft` ne tourne (`Get-Process BitCraft`). S'il tourne (Julien s'est levé, ou T-11), **aucun essai connecté** : noter « essai connecté sauté : jeu ouvert ». **Seule T-11 lance le jeu.**
- **Programmes de Julien** : ne jamais en fermer un. Son bot = `BitcraftBot-vX.Y.Z.exe` ; s'il tourne, pas de `publier-version.ps1` (noter « .exe à publier »). Tu ne fermes que les processus que **tu** as lancés (par leur numéro).
- Essais du bot sans le jeu : `artifacts\release\BitcraftBot-vX.Y.Z.exe --sans-lancer-jeu` lancé depuis `artifacts\release` (il lit `config.local.json` là), 45 à 60 s, puis fermeture propre (`CloseMainWindow` sur **son** numéro de processus). Lire le journal `artifacts/release/journaux/vX.Y.Z/AAAA-MM-JJ/` : **aucun « Abonnement refusé »**, aucune ERREUR nouvelle, les données attendues reçues. Une capture de la fenêtre du bot seulement (jamais l'écran entier : Julien peut avoir autre chose d'ouvert).
- Français partout sauf les lignes de code (D-019). Julien ne lit pas le code.

## 3. Sécurité en jeu (Julien, 27/11 ; vaut pour tout code de déplacement et pour T-11)
- **Ne jamais déployer le chariot (kart) ni le bateau à voile** : jamais les touches `-` et `=` de la barre, jamais Maj + V (Quick Deploy/Store), jamais Maj + B (Quick Board). Julien les range avant la nuit.
- **Ennemis** : si un ennemi approche ou attaque (vie qui baisse, sur le serveur), d'abord **Sprint (touche 4)** et s'éloigner (D-072 : toujours fuir).
- **À terre** : une fenêtre propose « **Get Up** » ou « **Go Home** ». Toujours **Get Up**, **jamais Go Home**. Puis repartir. Mourir est interdit : Julien porte beaucoup de ressources.
- **Collines** : le personnage ne monte pas les marches trop hautes, il doit grimper (escalade). Pour l'instant : **aucune escalade** ; un trajet qui en demande est refusé (relief lu sur le serveur, `terrain_chunk_state`).

## 4. À la fin (même si la tâche est incomplète)
1. `dotnet build` 0 avertissement, `dotnet test` tout réussi. Sinon : corriger, ou annuler ses changements qui cassent (ne jamais laisser `main` cassé).
2. Version (si du code a changé) : `Directory.Build.props` + message de version (`MainWindow.xaml.cs`, ligne « Version X : … »), puis `tools/publier-version.ps1`. **Essai sans le jeu obligatoire après la publication** (§2). S'il échoue (refus du serveur, plantage) : corriger ; sinon revenir au code d'avant et republier la version d'avant sous un nouveau numéro, en le disant. **Jamais de Release ni de tag GitHub** (D-076 : prochaine Release en 0.5.x, décidée par Julien).
3. Mémoire : décision `D-NNN` si un choix est fait (vérifier le dernier numéro pris : `ls memory/decisions`), `memory/technique/architecture-suite.md` (nouvelle section), journal `memory/journal/2026-11-27-tache-TNN-*.md` + ligne dans `memory/journal/README.md`, `resume-global.md` (10 lignes max), index des dossiers touchés, `tools/agent-memoire/verifier-memoire.ps1` à **0 erreur**.
4. **Bilan de la nuit** : remplir ta section (modèle dans le fichier) : fait, testé (comment), pas fait, problèmes, ce que Julien doit essayer, questions pour Julien, consignes pour les tâches suivantes, quota en fin de tâche.
5. `etat.md` : « finie à HH:MM » (ou « arrêtée à HH:MM : raison »).
6. `git status` ; `git add` **de tes fichiers seulement** (jamais `.claude/`, `website-recipes/`, `memory/site-recettes/` : session du site) ; commit en français avec la ligne `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` ; `git pull --rebase` ; `git push`.

## 5. Les 12 tâches
| Tâche | Heure | Fiche | Version | Sujet |
|---|---|---|---|---|
| T-05 | 01:00 | `T-05-v0425-overlay-reprise.md` | v0.4.25 | Overlay plus étroit ; reprise des récoltes : souris sur l'arbre avant M |
| T-06 | 02:00 | `T-06-v0426-sans-recettes.md` | v0.4.26 | Onglet Recettes retiré (on garde ce qui sert ailleurs) |
| T-07 | 03:00 | `T-07-v0427-carte-donnees.md` | v0.4.27 | Carte : données (200 cases, zones noires, mémoire 72 h, échanges allégés) |
| T-08 | 04:00 | `T-08-v0428-carte-affichage.md` | v0.4.28 | Carte : affichage (plus grande, tableau plus étroit, rapide à 200) |
| T-09 | 05:00 | `T-09-etude-deplacement.md` | — | Déplacement : étude écrite (pas de code) |
| T-10 | 06:00 | `T-10-v0429-se-deplacer-ici.md` | v0.4.29 | « Se déplacer ici » + sécurités (ennemis, Get Up) + mode d'essai |
| T-11 | 07:00 | `T-11-essai-reel-en-jeu.md` | — | Essai réel en jeu (le bot lance le jeu), 20 min au plus |
| T-12 | 08:00 | `T-12-v0430-corrections-essai.md` | v0.4.30 | Corrections après l'essai réel |
| T-13 | 09:00 | `T-13-v0431-petits-defauts.md` | v0.4.31 | Cible lue à l'écran, petites fabrications, pêche/minage sans capacités |
| T-14 | 10:00 | `T-14-v0432-recolter-ressource.md` | v0.4.32 | « Aller à cette ressource » / « Récolter jusqu'à épuisement » |
| T-15 | 11:00 | `T-15-controle-depot.md` | (v0.4.33 si code changé) | Contrôle complet du dépôt |
| T-16 | 12:00 | `T-16-controle-memoire-bilan.md` | — | Contrôle complet de la mémoire, bilan final, phrase de reprise |

Si une tâche a sauté (quota, verrou, échec), la suivante prend **le numéro de version libre suivant** (lire `Directory.Build.props`) : les numéros du tableau sont indicatifs.
