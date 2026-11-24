# Prompt pour la session « tâches » (2026-11-24)

Session Claude Code lancée **par Julien**, sur **Sonnet 5.5, effort moyen**. Julien n'a presque plus de quota : travailler au plus court, sans lecture inutile. Les tâches arrivent une par une dans le chat ; ce fichier est le cadre commun.

## Phrase à donner

> Reprends le projet bitcraft-bot : lis CLAUDE.md, puis memory/echanges/2026-11-24-prompt-session-taches.md et suis-le. Ne touche pas au chatbot. Ne lance aucune autre session ni aucun agent sans mon accord.

## Contexte de cette demande (échange du 2026-11-24)

- Julien a demandé un prompt pour une session Sonnet 5.5 (effort moyen) qui exécute ses tâches successives, car il a peu de quota.
- Le **chatbot « @GPT » a déjà sa propre session** (`2026-11-23-prompt-chatbot.md`). Cette session peut lire ses fichiers pour comprendre, **jamais les modifier**.
- Priorité 1 : **onglet « Tâches de voyageur »** : dire si Julien peut remplir une tâche de voyageur avec ce qu'il a en stock (sac, ceinture, coffres de banque).
- Priorité 2 : **corriger des bugs dans l'onglet Recettes** (v0.4.6). Julien donnera la liste des bugs dans le chat : **la lui demander avant de coder**.
- Ensuite : d'autres tâches, données au fil de l'eau. Chaque tâche = un livrable testé, puis on attend la suivante.
- Rappel : cette discussion est notée ici ; la mettre à jour dans ce fichier si le cadre change.
- **État (2026-11-24, session 27)** : tâche 1 livrée (v0.4.11 à 0.4.13), retours de Julien appliqués (D-062, D-063). Suite : retour d'essai de la v0.4.13. Reprise : `reprise-apres-compact.md`.

## Économie de quota (priorité)

- Lire seulement `CLAUDE.md`, `memory/echanges/resume-global.md` et les fichiers de la tâche en cours. Pas de lecture de tout `memory/`.
- Pas de sous-agent, pas d'agent mémoire (`tools/agent-memoire/PROMPT.md`) sans l'accord de Julien. Utiliser `verifier-memoire.ps1` (script, sans modèle).
- Pas de longues explications : résultat, ce qui a été testé, questions en suspens.
- Regrouper plusieurs petites corrections dans une seule version.

## Tâche 1 : onglet « Tâches de voyageur »

**But** : pour chaque tâche de voyageur active du compte de Julien, afficher les objets demandés, ce que Julien a en stock, et un verdict : **faisable maintenant / partiel / impossible**, avec le manque exact.

À lire d'abord :
- `memory/technique/hybride/donnees-serveur.md` : la table `traveler_task_state` y est notée « plus tard » (ligne « Tâches de voyageur, quêtes »). Format **non vérifié** en réel.
- `references/BitCraftPublic/BitCraftServer/packages/game/src/agents/traveler_task_agent.rs` et les types `traveler_task*` de `game/autogen/` : structure des tâches et des demandes.
- `src/BitcraftBot.Server/World/Inventories.cs` et `src/BitcraftBot.App/MainWindow.Inventory.cs` : le stock (sac, ceinture, banques) est **déjà lu**.
- `src/BitcraftBot.GameData/` (`GameDataStore.cs`, `KnownNames.cs`) et `artifacts/release/donnees-jeu/` : noms et icônes des objets (traduction id → nom, icône).
- `src/BitcraftBot.App/MainWindow.Tasks.cs`, `MainWindow.Layout.cs`, `MainWindow.xaml` : comment ajouter un onglet (D-043 onglets par usage, D-046 interface BitCraft).

Méthode :
1. Vérifier le **vrai schéma** de `traveler_task_state` et de la table de description des tâches sur le serveur (lecture seule, comme pour les autres tables). Écrire le format vérifié dans `donnees-serveur.md` ; ce qui n'est pas vu en réel reste « À vérifier ».
2. Lire les tâches **du personnage de Julien seulement** (par son numéro d'entité).
3. Comparer demande et stock : quantités, objets identiques mais de niveaux différents (ne pas les confondre), objets dans un coffre de banque (dire où ils sont).
4. Cas à traiter explicitement : plusieurs tâches demandant le même objet (le stock ne se compte qu'une fois), tâche déjà remplie, aucune tâche active, stock non lu (hors ligne).
5. Si un point est ambigu (ex. compter ou non les coffres de banque dans « faisable »), **poser la question à Julien**.

Livrable : onglet dans l'interface, tests automatiques (cas ci-dessus), essai réel sur le PC de Julien (bot lancé avant le jeu).

## Tâche 2 : bugs de l'onglet Recettes

Fichiers : `src/BitcraftBot.App/MainWindow.Recipes.cs`, `RecipeView.cs`, `src/BitcraftBot.GameData/RecipeBook.cs`. Tests : `V047Tests.cs` n'est **pas** à toi (chat) ; chercher les tests de recettes à part.
**Demander la liste des bugs à Julien** avant de coder ; pour chaque bug : reproduire (test ou capture), corriger, ajouter un test. Ne pas ajouter de fonction non demandée.
Bugs déjà connus (journal session 21, **À vérifier**) : aucun bug de recette précis noté ; la pêche était notée « fabrication » dans le journal des tâches (`TaskJournal.cs`), et les fabrications disent « aucun objet gagné ».

## Tâches suivantes

Julien les donne dans le chat. Candidates déjà notées dans `2026-11-23-passation-v047.md` : décalage de la caméra après W (D-057), touche Interact = M (D-045), puis la Banque (v0.5). **Ne rien commencer sans demande de Julien.**

## Règles non négociables (rappel de CLAUDE.md)

- Français partout, sauf les lignes de code (D-019). Julien ne lit pas le code : expliquer le comportement.
- Lecture seule du serveur (D-039). Le jeton et l'e-mail ne sont jamais écrits (journal, Git, réponse). Ne jamais lire le registre.
- Bot connecté **avant** le jeu. Aucune action hors de l'écran « en jeu » (D-052).
- Tout passe par le contrôleur d'actions (D-045). Interdits : exploit, modification du client, lecture mémoire, injection.
- **Jamais de session ou d'agent lancé sans l'accord de Julien.**
- Poser une question au moindre doute ; signaler toute incohérence ; rappeler les questions en suspens en fin de message.

## Travail avec une autre session (chatbot)

- **Pas à toi** : `Server/World/GptChat.cs`, `Chat*.cs`, `App/MainWindow.Chat.cs`, `Perception/Screen/ChatInput.cs`, la carte CHAT du XAML, les fonctions du chat de `ActionController.cs`, tests `GptChatTests`, `V045Tests` (partie chat), `V047Tests`.
- `git pull --rebase` avant de commencer et avant chaque push ; `git status` avant `git add` ; n'ajouter que tes fichiers.
- Version : prendre le numéro libre suivant (`Directory.Build.props` + message de version de `MainWindow.xaml.cs`), puis `tools/publier-version.ps1` **bot fermé**. Pas de Release ni de tag GitHub (D-051). Fichiers communs en conflit : garder les deux modifications.

## Livrer une tâche

1. `dotnet test` et compilation sans avertissement (`-warnaserror`) ; essai de démarrage du .exe (journal d'essai effacé ensuite).
2. Nouvelle version + .exe local, commit, push.
3. En fin de tâche : un fichier dans `memory/journal/` (« session tâches »), `resume-global.md` (10 lignes max), décision D-NNN si Julien valide une règle, index à jour, `tools/agent-memoire/verifier-memoire.ps1` à 0 erreur.
4. Message final court : ce qui marche, ce qui a été testé, questions en suspens.
