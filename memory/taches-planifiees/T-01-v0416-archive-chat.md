# T-01 — v0.4.16 : archive du chat en local (tâche planifiée du 26/11, 03:30)

Lire d'abord [README.md](README.md) (règles communes) et les suivre du début à la fin.

## Le besoin (Julien, 25/11, D-064 point 4)
Enregistrer **tout le chat texte** vu par le bot, à chaque connexion : Local, Région, LFG, Trade, et les **messages privés** (reçus et envoyés) quand le module global les donne. Pour chaque message : qui, quand, quel canal, quel texte. Dans une **petite base de données locale** (SQLite), dans un dossier **ignoré par Git**. But : garder l'historique et, plus tard, nourrir le chatbot (le faire « réfléchir »).

## Ce qui existe déjà
- Lecture du chat : `src/BitcraftBot.Server/World/GptChat.cs`, requêtes dans `Server/World/Queries.cs` (`chat_message_state`, canaux Local = 2, Région = 3, LFG = 7, Trade = 8 ; voir le journal pour le format brut). Le serveur ne garde que les messages récents : on archive ce qui passe pendant que le bot est connecté.
- Messages privés : table `direct_message_state` du module **global** (`idc-global`). Le module global **refuse parfois la liaison** (si le jeu n'a pas été lancé depuis plus d'une heure) : dans ce cas, archive sans messages privés, et une ligne dans le journal. Vérifier le format réel avant de coder (référence : `references/BitCraftPublic`, et la doc `memory/technique/chat-gpt.md`). Si les messages privés ne sont pas lisibles proprement, ne pas forcer : les noter « À vérifier » et livrer le reste.
- Onglet CHAT : existe mais **masqué** (`Visibility="Collapsed"` dans `MainWindow.xaml`, v0.4.13) ; code dans `MainWindow.Chat.cs`.
- `.gitignore` ignore déjà `[Rr]elease/` (donc `artifacts/release/`).

## À faire
1. Base SQLite (paquet `Microsoft.Data.Sqlite`) dans un dossier `archive-chat` **à côté du .exe** (ignoré par Git ; ajouter la règle au `.gitignore` si le dossier peut se trouver ailleurs). Une table des messages : identifiant du serveur (pour ne jamais enregistrer deux fois le même message), date et heure, canal, joueur (nom + identifiant d'entité si disponible), texte, région, et si c'est un message du bot lui-même (réponse @gpt). Écriture hors du fil de la fenêtre, sans ralentir le bot.
2. Remettre l'onglet **CHAT** visible, avec un **tableau** des messages archivés (heure, canal, joueur, texte), une **recherche par joueur** et un **filtre par canal**, le plus récent en haut, et le nombre total. Garder ce qui existe (historique @gpt, « Tester une phrase ») en dessous ou dans un coin, sans surcharger : Julien veut des écrans simples et peu de texte.
3. Ligne de journal au démarrage : « Archive du chat : N messages, fichier … ».
4. Tests automatiques : enregistrement, doublons ignorés, recherche, filtre (base en mémoire ou fichier temporaire).
5. Essai réel (jeu fermé, `--sans-lancer-jeu`) : le bot se connecte, archive quelques messages Région en quelques minutes, l'onglet CHAT les montre (capture de la fenêtre du bot). Fermer le bot d'essai.

## Version
v0.4.16 (si la version actuelle n'est pas 0.4.15, prendre la suivante libre et le noter). Décision suivante libre (D-068 a priori).
