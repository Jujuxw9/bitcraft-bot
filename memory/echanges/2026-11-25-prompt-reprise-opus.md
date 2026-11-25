# Prompt de reprise pour une nouvelle session Opus 5.5 (2026-11-25)

Julien a joué avec la v0.4.10 (ou v0.4.9) : il y a des **journaux à analyser**. Une nouvelle session (Claude Opus 5.5) reprend le travail. Julien colle la phrase ci-dessous dans la nouvelle session.

## Phrase à donner

> Reprends le projet bitcraft-bot : lis CLAUDE.md, puis memory/echanges/2026-11-25-prompt-reprise-opus.md et suis-le. Ne lance aucune autre session ni aucun agent sans mon accord.

## Ta mission, dans cet ordre
1. **Analyser les journaux de Julien** (il a joué) : `artifacts/release/journaux/`, dossiers de version les plus récents (v0.4.10, sinon v0.4.9), dernier jour d'abord. Lire en entier, sans deviner.
2. **Faire le bilan à Julien, simplement** : ce qui a marché, ce qui a échoué, ce qui est encore « À vérifier ». Citer les heures et les lignes.
3. **Lui demander les nouvelles fonctionnalités** (une question à la fois, courte), avant de coder quoi que ce soit. Attendre son go.

Ne rien coder, ne rien fabriquer, avant le bilan et le go de Julien.

## Ce qu'il faut lire d'abord
1. `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md`.
2. Chatbot : `memory/technique/chat-gpt.md` (sections 8, 9, 10), décisions D-054, D-055, D-056, D-058, D-059, D-060, D-061, journaux de session 23 et 24 (`memory/journal/`).
3. Reste du projet (récolte, caméra, Banque) : `memory/echanges/2026-11-23-passation-v047.md`. Julien dit « reprendre tout le travail » : après le bilan, **demande-lui sur quoi il veut travailler** (chatbot, récolte, autre) plutôt que de supposer.

## État au 25/11 (v0.4.10, chatbot)
- Réponses « @gpt » / « @chatgpt » dans Local, Région, LFG et Trade, par les commandes du jeu (`/s`, `/y`, `/lfg`, `/t`), au clavier uniquement (jamais d'envoi par le serveur, D-039).
- Sécurités : chat vérifié à l'écran avant chaque lettre et avant Entrée ; clic ou touche de Julien = arrêt ; 2 s de calme après un retour dans le jeu ou un clic.
- Phrases par thème : `src/BitcraftBot.Server/World/Phrases/fr.txt` et `en.txt` (28 thèmes), modifiables sans nouvelle version dans le dossier `phrases-chat` à côté du .exe.
- Langue du message, style relâché, fautes volontaires (5 %) corrigées une fois sur deux (« *mot »), autres bots ignorés, jamais deux fois la même phrase de suite.
- **Nouveau en v0.4.10 (D-061)** : plafonds (20/h, 100/jour, 3 par joueur et par heure), pause de 5 min quand Julien écrit lui-même, amis et liste noire (`amis.txt`, `liste-noire.txt` ; « The Krazy Turkey » ami d'office), ton selon le canal, bonjour/bonsoir selon l'heure, infos du jeu `{region}` `{niveau}`, onglet CHAT (historique, statistiques, « Tester une phrase »).
- **Pas fait** : messages privés (autre module du serveur, commande de réponse non vérifiée) ; idée 8 (IA Claude Haiku) plus tard ; idée 1 (répondre à « ChatGPT » sans « @ ») refusée.
- Julien a prévu : la **v0.4.11 = correction** (de ce que les journaux montrent), puis il se concentre sur autre chose que le chat.

## Ce que les journaux doivent confirmer (À vérifier)
- Réponses dans **Local, LFG, Trade** : jamais vues jusqu'ici.
- **Pause quand Julien écrit** : le bot ne doit pas prendre ses propres messages pour ceux de Julien (reconnaissance par le texte, ou envoi du bot à ± 6 s).
- **Plafonds**, lignes « sans réponse : … » dans le journal.
- Ligne de démarrage « Amis : … ; liste noire : … ; Plafonds … » ; base de phrases « 28 thèmes ».
- **Frappe vérifiée lettre par lettre** : temps d'une capture d'écran par lettre sur le PC de Julien (la frappe peut ralentir) ; texte resté dans la zone de saisie après Échap ; liste des canaux ouverte = chat vu fermé.
- `{region}` bien lue sur le serveur, jamais d'accolade `{ }` dans une réponse.
- Le clic inconnu du 24/11 à 00:03:48 (journal v0.4.7) : Julien ne sait plus.
- L'**essai de démarrage du .exe v0.4.10** n'a pas été fait (le jeu était lancé) : le bot doit démarrer **avant** le jeu.
- Onglet CHAT : affichage, historique, bouton de test.

## Règles à respecter (rappel)
- Français partout, sauf les lignes de code (D-019). Julien n'est pas technique : phrases courtes, concrètes ; **une question au moindre doute** ; **rappeler en fin de message les questions sans réponse**.
- **Jamais de session ni d'agent lancé sans l'accord de Julien** (quota 5 h).
- Nouvelle version : `Directory.Build.props` + message de version (`MainWindow.xaml.cs`), puis `tools/publier-version.ps1` (bot fermé). **.exe local seulement, jamais de Release ni de tag GitHub** (D-051). Prochaine version libre : 0.4.11.
- Avant de lancer le bot pour un essai : le jeu doit être fermé (le bot se connecte avant le jeu). Ne jamais fermer les programmes de Julien.
- `git pull --rebase` avant de travailler et avant de pousser. Ignorer les commits « [PERSO JULIEN] » / « [perso] » et le message [SYNC PC-PERSO] : c'est un autre projet.
- Chaque livrable : tests automatiques + essai réel, puis journal de session, `resume-global.md`, index, `tools/agent-memoire/verifier-memoire.ps1` à 0 erreur, commit et push.
- Interdits non négociables : pas d'exploit, pas de modification du client, pas de lecture mémoire du processus, pas d'injection, aucune action envoyée au serveur officiel par un client tiers (D-039). Jamais de jeton ni d'e-mail de Julien dans un journal, Git ou une réponse.
