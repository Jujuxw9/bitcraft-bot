# Prompt pour la session « chatbot » (2026-11-23)

Une **autre session Claude Code** développe le module « @GPT » (le chatbot), en parallèle de la session principale (récolte, caméra, Banque). Julien colle le texte ci-dessous dans cette nouvelle session.

## Phrase à donner

> Reprends le projet bitcraft-bot, **partie chatbot seulement** : lis CLAUDE.md, puis memory/echanges/2026-11-23-prompt-chatbot.md et suis-le. Ne lance aucune autre session ni aucun agent sans mon accord.

## Ta mission
Développer le module « @GPT » : quand un joueur écrit « @gpt » ou « @chatgpt » dans le chat du jeu, le bot répond au clavier, comme Julien. Rendre les réponses plus vivantes et plus crédibles, sans jamais casser les sécurités.

## À lire d'abord
1. `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`.
2. Décisions du chat : D-054, D-055, D-056, D-058 (D-057 point 2 est remplacé par D-058). `memory/decisions/`.
3. `memory/technique/chat-gpt.md` : tout le module, sections 1 à 7 (étude, envoi, essais réels, v0.4.7).
4. Journaux de Julien : `artifacts/release/journaux/vX.Y.Z/` (lignes « [Chat] » et « [Actions] Réponse « @GPT » »).

## État au 23/11 (v0.4.7)
- Lecture du chat par le serveur (lecture seule) : Local, Région, Looking For Group, Trade, 5 dernières minutes.
- Déclencheur « @gpt » / « @chatgpt » (majuscules ou minuscules) ; jamais les messages de Julien (son pseudo en jeu est « ChatGPT ») ; 1 réponse par minute au plus, au message le plus récent ; message de plus de 2 min oublié.
- 40 phrases toutes faites par langue (français si le joueur a « fr/ », anglais sinon), smiley 30 %.
- Réponse dans le canal du message par les commandes du jeu : Local `/s`, Région `/y`, Looking For Group `/lfg`, Trade `/t`.
- Temps de lecture avant de répondre, frappe irrégulière, minuteur dans la carte CHAT de l'onglet Bot.
- Envoi réel vérifié sur Région (journaux v0.4.5 et v0.4.6). Les autres canaux et les commandes : **pas encore vus dans un journal**.

## Fichiers du chatbot (ta zone)
- `src/BitcraftBot.Server/World/GptChat.cs` : canaux, requêtes, choix du message, phrases.
- `src/BitcraftBot.App/MainWindow.Chat.cs` : boucle toutes les 500 ms, envoi, carte CHAT.
- `src/BitcraftBot.Input/Actions/ActionController.cs` : **seulement** `ChatAsync`, `TypeHumanAsync`, `KeyDelay`, `CloseChatAsync`, `WaitChatAsync`, `ChatProbe`.
- `src/BitcraftBot.Perception/Screen/ChatInput.cs` : chat ouvert à l'écran, bouton du canal.
- Carte « CHAT @GPT » dans `src/BitcraftBot.App/MainWindow.xaml`.
- Tests : `GptChatTests.cs`, `V045Tests.cs` (partie chat), `V047Tests.cs`, et tes nouveaux fichiers de tests.
- Images : `images/reference-ui/chat/`.
**Ne touche pas** au reste (récolte, caméra, recettes, repas, sprint) : c'est la session principale. Si tu en as besoin, demande à Julien.

## Pistes (à proposer à Julien avant de coder, une par une)
1. Lire le prochain journal : la commande change-t-elle le bouton tout de suite, ou est-elle lue à l'envoi ? Les réponses Local, LFG, Trade partent-elles ?
2. Réponses selon le message : mots-clés simples (salut / hello, merci / thanks, question avec « ? », « où » / « where », métiers, arbres…) → phrases adaptées ; sinon phrase au hasard.
3. Ne pas redire la même phrase à un même joueur (mémoire des dernières phrases).
4. Local : ne répondre qu'aux joueurs proches (position de l'auteur par `mobile_entity_state`, lecture seule) ; distance à fixer avec Julien.
5. Langue : reconnaître aussi un message écrit en français par un joueur sans « fr/ ».
6. Plus tard, avec une clé API de Julien : Claude Haiku (D-054). **Pas de clé pour l'instant** : ne rien brancher sans son accord.
7. Points « À vérifier » de `chat-gpt.md` (texte restant après Échap ; liste des canaux ouverte = chat vu « fermé »).

## Règles du chatbot (non négociables)
- **Jamais** dire ou laisser entendre que c'est un bot (D-055) : un test vérifie les mots interdits, garde-le.
- Tout passe par le **clavier**, via le contrôleur d'actions (D-045). **Rien n'est envoyé au serveur** (D-039), ni par une autre voie.
- Le chat est toujours refermé et vérifié à l'écran ; si Julien tape une touche, la frappe s'arrête.
- Aucune action si le jeu n'est pas « en jeu », si une fenêtre du jeu est ouverte ou si Julien a le chat ouvert.
- Pas de spam : 1 réponse par minute au plus (changer ce chiffre = demander à Julien).

## Travail à deux sessions en même temps
- `git pull --rebase` **avant de commencer** et **avant chaque push** ; `git status` avant `git add` ; n'ajouter **que tes fichiers**.
- **Version et .exe** : avant de publier, `git pull`, puis prends le numéro suivant libre dans `Directory.Build.props` (ex. 0.4.8 si 0.4.7 est la dernière), change aussi le message de version dans `MainWindow.xaml.cs` (ligne « Version X : … »), puis `tools/publier-version.ps1` **bot fermé**. Pas de Release ni de tag GitHub.
- Si un fichier commun change chez l'autre session (`MainWindow.xaml`, `MainWindow.xaml.cs`, `Directory.Build.props`), garde les deux modifications en cas de conflit.

## Méthode (Julien)
- Français partout sauf le code ; phrases courtes ; Julien ne lit pas le code (expliquer le comportement).
- Tester avant de livrer : tests automatiques (`dotnet test`), compilation sans avertissement (`-warnaserror`), essai de démarrage du .exe (journal d'essai effacé ensuite).
- Poser une question au moindre doute ; rappeler les questions en suspens à la fin de chaque message.
- Fin de session : journal `memory/journal/` (numéro suivant, préciser « session chatbot »), `chat-gpt.md`, décision D-NNN si Julien valide une règle, `resume-global.md`, index, `tools/agent-memoire/verifier-memoire.ps1` à 0 erreur, commit, push.
