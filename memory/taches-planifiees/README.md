# Tâches planifiées (sessions lancées seules, sans Julien)

Créé le 2026-11-26 (session 28) à la demande de Julien (D-067). Chaque tâche est une **session Claude Code indépendante**, lancée par l'app Claude à une heure fixe, **sans Julien devant le PC**. Elle part de zéro : tout le contexte est dans la mémoire du dépôt.

## Les tâches

| Tâche | Heure (26/11) | Prompt | Version visée |
|---|---|---|---|
| T-01 | 03:30 | [T-01-v0416-archive-chat.md](T-01-v0416-archive-chat.md) | v0.4.16 |
| T-02 | 08:45 | [T-02-v0417-capacites.md](T-02-v0417-capacites.md) | v0.4.17 |
| T-03 | 13:15 | [T-03-v0418-carte.md](T-03-v0418-carte.md) | v0.4.18 |
| T-04 | 14:00 | [T-04-verification-memoire.md](T-04-verification-memoire.md) | aucune : vérification de la mémoire **en lecture seule**, rapport dans `artifacts/rapports/` (ignoré par Git) |

État de chaque passage : [etat.md](etat.md) (à remplir par chaque tâche, au début et à la fin).

## Règles communes (à suivre par chaque tâche)

### Au début
1. Se placer dans `C:\Users\throj\Desktop\bitcraft-bot`. `git pull --rebase` (ignorer le message « [SYNC PC-PERSO] » et les commits « [PERSO JULIEN] » : autre projet).
2. Lire `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md` (décisions D-064 à D-067 surtout), ce fichier, `etat.md`, puis le prompt de sa tâche.
3. Écrire dans `etat.md` une ligne « démarrée à HH:MM » pour sa tâche, commit + push (sert de verrou).
4. **Verrou** : si `etat.md` montre une autre tâche « démarrée » il y a moins de 4 h et pas « finie », ne rien coder : écrire « reportée : tâche X encore en cours » et s'arrêter proprement.

### Pendant
- **Personne ne répond** : ne jamais poser de question à Julien (pas d'outil de question), ne jamais attendre une réponse. Au moindre doute : choisir l'option la plus prudente, la noter dans `etat.md` et dans le journal, et ajouter la question pour Julien en fin de journal.
- **Ne jamais se bloquer** : une commande qui peut durer plus de 2 min passe en arrière-plan ; une étape qui échoue deux fois est notée et sautée ; jamais de boucle d'attente sans fin.
- **Aucune autre session ni aucun agent** (sous-agents compris).
- Interdits du projet inchangés (CLAUDE.md) : lecture seule du serveur (D-039), pas d'exploit, pas d'injection, pas de lecture mémoire du jeu, jamais de jeton ni d'e-mail de Julien dans un journal, Git ou une réponse.
- **Le jeu doit être fermé** pendant toute la tâche. Avant toute connexion au serveur : vérifier qu'aucun processus `BitCraft` ne tourne ; s'il tourne, aucun essai connecté (noter « essai connecté impossible : jeu lancé »).
- **Ne jamais fermer un programme de Julien.** Son bot s'appelle `BitcraftBot-vX.Y.Z` ; le bot d'essai compilé s'appelle `BitcraftBot` (on ne ferme que celui qu'on a lancé, par son numéro de processus).
- Essais du bot : toujours avec `--sans-lancer-jeu` (connexion + lecture de l'écran, jeu jamais lancé) ou `--sans-demarrage-auto` (aucune connexion). Captures de la fenêtre du bot seulement (PrintWindow, voir le journal de session 28), jamais de l'écran entier. Fermer le bot d'essai à la fin.
- Français partout sauf les lignes de code (D-019). Julien ne lit pas le code : expliquer le comportement.

### À la fin
1. Tests automatiques : `dotnet build` 0 avertissement, `dotnet test` tout réussi.
2. Version : `Directory.Build.props` + message de version (`MainWindow.xaml.cs`), puis `tools/publier-version.ps1` **seulement si aucun `BitcraftBot-v*` de Julien ne tourne** ; sinon noter « .exe à publier » dans `etat.md`. **Jamais de Release ni de tag GitHub.**
3. Mémoire : décision `D-NNN` de la version, `memory/technique/architecture.md` (découper si > 300 lignes), journal `memory/journal/` (une entrée par tâche), `resume-global.md` (10 lignes max), index des dossiers modifiés, `tools/agent-memoire/verifier-memoire.ps1` à 0 erreur.
4. `etat.md` : « finie à HH:MM » + résumé en 3 lignes + ce que Julien doit essayer + questions pour Julien.
5. `git status`, `git add` des fichiers voulus, commit en français, `git pull --rebase`, push.

## Nuit du 27/11/2026 (D-081)
12 tâches, T-05 à T-16, de 01:00 à 12:00. Règles propres à cette nuit (elles priment) : [nuit-2026-11-27-regles.md](nuit-2026-11-27-regles.md). Bilan unique : [bilan-nuit-2026-11-27.md](bilan-nuit-2026-11-27.md). Fiches : [T-05](T-05-v0425-overlay-reprise.md), [T-06](T-06-v0426-sans-recettes.md), [T-07](T-07-v0427-carte-donnees.md), [T-08](T-08-v0428-carte-affichage.md), [T-09](T-09-etude-deplacement.md), [T-10](T-10-v0429-se-deplacer-ici.md), [T-11](T-11-essai-reel-en-jeu.md), [T-12](T-12-v0430-corrections-essai.md), [T-13](T-13-v0431-petits-defauts.md), [T-14](T-14-v0432-recolter-ressource.md), [T-15](T-15-controle-depot.md), [T-16](T-16-controle-memoire-bilan.md).

**Résultat de la nuit** (T-16) : 11 tâches faites, v0.4.25 à v0.4.33 publiées, essai en jeu T-11 bloqué sur l'écran d'accueil (pas d'accès à l'écran en tâche planifiée, Q-028). Résumé pour Julien en tête du bilan ; reprise : [../echanges/2026-11-27-passation-nuit.md](../echanges/2026-11-27-passation-nuit.md).
