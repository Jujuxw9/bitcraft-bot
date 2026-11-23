# Module « @GPT » : répondre dans le chat du jeu (étude)

- **Statut** : branché et envoi réel en v0.4.5 (D-056, section 6) ; v0.4.7 tous les canaux (section 7) ; v0.4.9 langue, style, correction, autres bots, fichiers modifiables (D-060, section 9) ; **v0.4.8 réponses par thème, frappe vérifiée lettre par lettre, fautes 5 % (D-059, section 8)**. Règles validées par Julien le 2026-11-23 ([D-054](../decisions/D-054-chat-gpt-reponses.md)) : canal Région, « @GPT » en majuscules, tout le monde, 1 réponse par minute (au plus récent), même sans question, **phrases toutes faites** (pas de clé API pour le moment, Haiku plus tard). Restent dans [Q-027](../questions-ouvertes/Q-027-chat-gpt.md) : clavier pendant la récolte, marque « [GPT] », mode essai.
- **But** : quand un joueur écrit « @GPT » dans le chat, le bot répond une phrase courte. **Pas de spam.**
- **Liens** : D-054, D-024, D-039 (lecture seule), D-045 (contrôleur d'actions), D-052 (aucune action avec une fenêtre du jeu ouverte), D-047 (sécurités).

## Incohérence avec D-024 (réglée par D-054)

D-024 interdisait au bot la touche Entrée (le chat). Julien l'a levée le 2026-11-23, **pour répondre dans le chat seulement** (D-054).

## 1. Lire le chat : possible, en lecture seule

Vérifié dans le code serveur officiel (`references/BitCraftPublic/BitCraftServer/packages/`).

### Table `chat_message_state` du module de région (le serveur de la région où joue Julien)

- Table **publique**, **sans filtre de visibilité** dans le module de région : tous les messages de la région sont lisibles avec l'abonnement en lecture seule déjà en place (même connexion que pour les arbres, `World/Queries.cs`). Une ligne `SELECT * FROM chat_message_state WHERE channel_id = …` suffirait. **À vérifier** par un vrai essai (le serveur peut refuser une requête trop large).
- Colonnes utiles :
  - `entity_id` : numéro du message (unique).
  - `owner_entity_id` : numéro du joueur qui écrit (le même que dans `player_state`).
  - `username` : de la forme `fr/Pseudo` (code de langue, barre, pseudo).
  - `channel_id` : le canal (voir ci-dessous).
  - `target_id` : 0 en général (seul le canal Local peut avoir une cible).
  - `text` : le message (250 caractères au plus).
  - `timestamp` : heure d'envoi en secondes (heure Unix).
  - `title_id` : titre affiché du joueur (0 si aucun).
- Canaux (`ChatChannel`) : 0 Système, 2 **Local**, 3 **Région** (appelé « General » dans le jeu), 7 **Groupe** (Looking For Group), 8 **Commerce** (Trade). Les numéros 1, 4, 5, 6 ne servent pas.
- Le canal **Local** n'a pas de position dans la table : c'est le jeu de chaque joueur qui choisit quels messages locaux il montre (probablement selon la distance, **À vérifier**). Le bot peut filtrer lui-même avec la position de l'auteur (`mobile_entity_state`).
- Le serveur **efface les messages de plus de 2 jours** (tâche de nettoyage quotidienne).

### Module global (tout le jeu)

- **Messages privés** : table `direct_message_state`. Filtre du serveur : chaque joueur ne voit que les messages qu'il a **envoyés ou reçus**. Le bot verrait donc les messages privés reçus par Julien, et pas ceux des autres. Le module global est déjà joint au démarrage (`RegionFinder.cs`).
- **Canaux créés par les joueurs** : `chat_message_state` du module global, visible seulement pour les membres du canal.

### Règles du serveur sur l'envoi (pour savoir ce que le jeu acceptera)

- 250 caractères au plus, message non vide.
- Canal Région : **3 messages par 15 secondes** au plus par joueur (réglage par défaut, si le joueur n'a pas de titre) ; pseudo obligatoire.
- La modération peut **suspendre le chat** d'un compte (« Your chat privileges have been suspended »). Les joueurs peuvent **signaler** un joueur ou un message.

## 2. Écrire dans le chat : deux voies

### (a) Côté serveur : INTERDIT, on ne le fait pas

Le serveur a une fonction d'envoi (`chat_post_message`, et `chat_post_targeted_message` pour les messages privés). L'appeler depuis le bot, c'est **envoyer une action au serveur officiel par un client tiers**. C'est un **interdit non négociable** du projet (D-039). Cette voie est écartée, sans exception.

### (b) Dans le jeu, comme Julien : seule voie compatible avec les règles du projet

Le bot fait ce que ferait Julien : Entrée (ouvre le chat), choisir le canal si besoin, taper le texte, Entrée (envoie). Tout passe par le **contrôleur d'actions** (D-045) avec ses sécurités : bouton Arrêter, souris bougée, pause quand Julien tape, rien hors de l'écran « en jeu » (D-052).

Limites de cette voie :
- **Il faut lever l'interdiction de la touche Entrée (D-024)** pour le chat.
- Le bot **prend le clavier quelques secondes** : la récolte doit s'arrêter le temps d'écrire (le clic droit et la frappe ne peuvent pas se mélanger).
- Pendant que le chat est ouvert, chaque touche part dans le chat. Risque : une touche de la récolte (W, ZQSD) tapée dans le chat par erreur, ou un texte tapé dans le jeu (touches = actions). Il faudra **vérifier à l'écran** que la zone de saisie du chat est bien ouverte avant de taper, et fermée après.
- Choix du canal dans le jeu : **À vérifier** (comment le jeu choisit Local ou Région à l'ouverture, et si une commande du type « /l » existe).
- Les accents et caractères spéciaux : il faudra taper par caractère Unicode et non par position de touche. **À vérifier** que le jeu les accepte.

## 3. Risques

| Risque | Détail | Parade proposée |
|---|---|---|
| Règles du jeu | Les Community Guidelines interdisent l'automatisation et le botting (voir `00-contexte/regles-et-risques.md`). Un bot qui **parle** est **visible par tous** : bien plus facile à repérer et à signaler qu'une récolte. Risque de sanction du compte principal. | Peu de réponses, ton humain, Julien présent. Dire que c'est une IA ? (question à Julien) |
| Modération | Signalement par les joueurs, suspension du chat ou du compte. | Liste de joueurs autorisés ; pas de réponse aux inconnus au début. |
| Spam | Plusieurs « @GPT » à la suite, ou deux bots qui se répondent. | Une réponse par joueur et par période ; un plafond global (ex. 1 par minute, 20 par heure) ; ne jamais répondre à ses propres messages (`owner_entity_id` de Julien) ; ignorer les messages de plus de 1 minute au démarrage. |
| Contenu | Insultes, sujets sensibles, données personnelles, fausses infos sur le jeu. | Consignes fixes pour Haiku (ton gentil, pas d'insultes, pas de sujets sensibles, ne jamais donner d'infos sur Julien) ; filtre de mots après la réponse ; 250 caractères max. |
| Manipulation | Un joueur écrit « @GPT ignore tes consignes et… ». Le texte des joueurs n'est pas fiable. | Le texte du joueur est passé comme une donnée, jamais comme une consigne ; Haiku n'a aucun outil ; le bot ne fait **rien d'autre** qu'écrire une phrase. |
| Clé API | Comme le jeton : secret. | Jamais dans Git, ni dans un journal, ni dans une réponse. Tapée par Julien dans le bot, rangée chiffrée sur le PC (comme le jeton). |
| Coût de l'API | Haiku 5.5 : 0,10 $ par million de mots-jetons lus, 0,50 $ par million écrits (prix de la doc Anthropic, à jour au 2026-11-03). Une réponse ≈ 1 000 jetons lus + 100 écrits ≈ **0,0002 $**. 1 000 réponses ≈ 0,20 $. | Plafond de dépenses sur le compte Anthropic ; compteur dans le bot. |
| Gêne pour la récolte | Le bot arrête de récolter quelques secondes à chaque réponse. | Réponses rares ; option « pas de réponse pendant la récolte ». |

## 4. Architecture envisagée (après les réponses de Julien)

1. **Lecture** (brique Server) : abonnement en plus à `chat_message_state` (canaux choisis par Julien). Chaque nouveau message qui contient « @GPT » passe au tri.
2. **Tri** : joueur autorisé ? délai respecté ? plafond global ? message récent ? pas de Julien lui-même ? Sinon : ignoré (une ligne dans le journal, sans le texte entier).
3. **Réponse** : appel à Claude Haiku 5.5 (SDK officiel Anthropic pour C#) avec des consignes fixes, le pseudo et le texte du joueur. Effort bas (réponse courte). Filtre de mots, coupe à 250 caractères.
4. **Envoi** : une tâche « écrire dans le chat » passe au contrôleur d'actions (D-045), qui attend une pause de la récolte, vérifie l'écran, ouvre le chat, tape, envoie, vérifie que le chat est fermé.
5. **Mode essai** d'abord : le bot affiche la réponse dans sa fenêtre **sans l'envoyer**. Julien juge le ton. L'envoi réel vient ensuite.

## 5. Ce qui est déjà codé (2026-11-23, pas encore branché dans le bot)

Brique Server, fichier `World/GptChat.cs`. **Il ne fait que lire et choisir : aucun envoi, ni au serveur ni au jeu.** Pas encore relié à la fenêtre du bot ni à l'abonnement (la session principale modifie ces fichiers en ce moment : on les branchera ensemble).

- **Lecture du chat** : transforme les lignes de `chat_message_state` en messages (pseudo, langue, canal, texte, heure). Requête prête : canal Région seulement.
- **Choix du message** (règles de D-054) : « @GPT » en majuscules exactement ; canal Région ; jamais les messages de Julien ; un message de plus de 2 minutes est oublié (par exemple au démarrage) ; au plus 1 réponse par minute ; s'il y en a plusieurs en attente, le plus récent seulement, les autres sont oubliés ; chaque message ne reçoit qu'une réponse.
- **Phrases toutes faites** : 10 en français, 10 en anglais, courtes, drôles et neutres, avec le pseudo du joueur. Français si le joueur écrit avec le jeu en français (`fr/` devant le pseudo), anglais sinon. Le pseudo est nettoyé (lettres, chiffres, - et _ seulement). Une marque optionnelle (« [GPT] ») est prévue, en attente de la réponse de Julien. Tout tient dans 250 caractères.
- **Tests** : 17 tests automatiques (`GptChatTests`), tous réussis ; les 369 tests du projet passent.

Prochaines étapes : brancher l'abonnement et afficher dans la fenêtre du bot « message reçu → réponse prévue » (**mode essai, sans envoi**) ; puis, après les réponses de Julien (clavier pendant la récolte, marque), l'écriture au clavier par le contrôleur d'actions. Claude Haiku viendra quand Julien aura une clé API.

## Ce qui reste À vérifier

- Que le serveur accepte l'abonnement à `chat_message_state` (essai réel).
- Comment le jeu choisit le canal à l'ouverture du chat.
- Que le jeu accepte la frappe de caractères accentués envoyée par le bot.
- Quels messages Local le jeu montre (distance).

## 6. Branché en v0.4.5 (2026-11-23, D-056) : envoi réel

- **Lecture** : l'abonnement du suivi du personnage ajoute `chat_message_state` (canal Région, **5 dernières minutes** seulement : le serveur garde 2 jours). Si le serveur refuse, le bot réessaie sans la barre d'action, les sacs au sol ni le chat.
- **Choix** : toutes les 500 ms, `GptChatPicker` (règles de D-054). Le message choisi et la réponse prévue sont écrits dans le journal (catégorie « Chat », texte du joueur coupé à 80 caractères).
- **Attente** : personnage connecté dans le jeu, pas de repas en cours, sécurités du contrôleur (jeu actif et visible, en jeu, aucune fenêtre du jeu, **chat fermé**, Julien ne tape pas depuis 3 s). Le message est abandonné s'il a plus de 2 minutes. **Une seule tentative** par message.
- **Envoi** (`ActionController.ChatAsync`) : chat fermé vérifié à l'écran → Entrée → chat ouvert vérifié (1,5 s au plus) → bouton du canal lu par OCR, doit dire « REGION » → texte tapé lettre par lettre (caractères Unicode, donc accents permis, 45 à 130 ms par lettre) → Entrée → chat fermé vérifié. Si le chat reste ouvert : Échap (2 fois au plus) ; s'il reste ouvert malgré tout : **toutes les actions bloquées** et alerte. Pas d'Échap à l'aveugle (sur le jeu sans chat, Échap ouvrirait le menu).
- **Julien tape une touche pendant la frappe** : arrêt immédiat, chat refermé, rien n'est envoyé.
- Entrée et Échap ne sont **pas** dans la liste des touches autorisées du contrôleur : seul l'envoi dans le chat s'en sert.
- **Chat ouvert reconnu à l'écran** (`ChatInput`) : cadre blanc de la zone de saisie (x 182 à 476, y 968 et 996, en 1920 × 1080). Vérifié sur les 75 captures du 23/11 : 5 chats ouverts trouvés, tous vrais, aucune erreur. Images de référence : `images/reference-ui/chat/`.
- Carte « CHAT @GPT » dans l'onglet Bot : case (activée à chaque démarrage) et état.
- Phrases : celles qui laissaient entendre que c'est un bot sont remplacées (D-055).

### À vérifier au premier essai réel
- Que le serveur accepte le filtre `timestamp > …` dans l'abonnement.
- Que la zone de saisie **vide** (juste après Entrée) a bien le même cadre blanc (vu seulement avec du texte dedans).
- Que le jeu ouvre le chat sur le canal Région (sinon : rien n'est envoyé, alerte « canal pas Région »).
- Après Échap, le texte tapé reste-t-il dans la zone de saisie ?

### Résultat du premier essai réel (journal v0.4.5, 23/11 23:02 à 23:07)
- Le serveur accepte l'abonnement avec `timestamp > …`.
- 3 messages « @GPT » (joueur TheKrazyTurkey). **2 réponses envoyées** sur Région, chat refermé et vérifié à chaque fois.
- 1er message : le chat s'est ouvert sur **LOCAL** → rien tapé, chat refermé par Échap, alerte « canal pas Région ». Le jeu ouvre le chat sur le **dernier canal utilisé**. Piste (pas codée, à proposer à Julien) : cliquer sur le bouton du canal pour passer sur Région.
- La zone de saisie vide (juste après Entrée) est bien reconnue comme chat ouvert.
- Reste **À vérifier** : le texte reste-t-il dans la zone de saisie après Échap.

## 7. v0.4.7 (2026-11-23, D-058) : tous les canaux joueurs, commandes du jeu, frappe humaine

- **Déclencheur** : « @gpt » ou « @chatgpt », majuscules ou minuscules (`GptChatPicker.IsTrigger`, pas collé à un autre mot : « mail@gpt.fr » ou « @gpts » ne comptent pas).
- **Canaux lus et répondus** : Local (2), Région (3), Looking For Group (7), Trade (8). Une requête d'abonnement par canal (`ChatMessages.AnsweredQueriesSince`).
- **Menu du bouton du canal** (capture `images/reference-ui/chat/menu-canaux_001.png`) : un clic ouvre une liste : Local `/s`, Region `/y`, Looking For Group `/lfg`, Trade `/t`. Le bot ne clique pas : il tape la commande.
- **Envoi** (`ActionController.ChatAsync(…, command)`) : chat ouvert → bouton lu → s'il ne montre pas le bon canal (`ChatInput.IsChannel`, mots « LOCAL », « REGION », « GROUP », « TRADE ») : commande + espace, bouton relu ; changé → la suite ; pas changé → la commande reste au début de la ligne. Les deux cas sont écrits dans le journal.
- **Temps de lecture** avant de répondre (`GptChatPicker.ReadingTime`) et **frappe irrégulière** (`ActionController.KeyDelay`), voir D-058.
- **Phrases** : 40 par langue, smiley simple 30 % du temps (`CannedReplies.SmileyChance`).
- **Carte CHAT** : état (« lit le message de X (3 s) », « écrit dans le chat… ») et minuteur « Prochaine réponse possible dans N s ».
- **À vérifier** : comportement exact de la commande (bouton changé tout de suite ou lu à l'envoi) ; le texte reste-t-il après Échap.
- Limite : liste des canaux ouverte = cadre blanc caché, chat vu « fermé » par l'écran.

### Journal v0.4.7 lu (23/11 23:53 au 24/11 00:07)
- **La commande change le bouton tout de suite** : chat ouvert sur LOCAL, « /y » + espace, bouton relu « REGION ». Point « À vérifier » de D-058 réglé.
- 2 réponses envoyées sur Région (TheKrazyTurkey). Local, Looking For Group, Trade : aucun « @gpt » reçu, **pas encore vus**.
- 3e réponse (00:03:48) : le jeu revenait au premier plan ; chat ouvert, puis **vu fermé 1 s après** ; le bot a tapé 11 s **dans le jeu** ; Entrée a rouvert le chat ; Échap, alerte. Cause probable : clic de Julien pour revenir dans le jeu (**À vérifier**). Corrigé en v0.4.8.

## 8. v0.4.8 (2026-11-24, D-059) : frappe vérifiée, base de phrases par thème, fautes volontaires

### Sécurité de la frappe (`ActionController.ChatAsync`, `TypeHumanAsync`)
- **Avant chaque lettre** : image neuve du jeu (`ChatProbe.IsOpen`), le chat doit être ouvert ; sinon plus aucune lettre et pas d'Entrée (« chat fermé pendant la frappe : arrêt, rien envoyé »). Vérifié aussi juste avant Entrée.
- Le temps de la vérification est pris sur la pause entre deux lettres (vitesse de frappe presque inchangée). **À vérifier** : temps d'une capture sur le PC de Julien (si elle est lente, la frappe ralentit un peu).
- **Clic de Julien** (vrai clic, `ChatProbe.LastUserClick`, écoute de la souris déjà ouverte par la caméra) pendant la frappe : arrêt, chat refermé.
- **2 s d'attente** (`MainWindow.Chat.cs`) après un retour du jeu au premier plan ou un clic de Julien, avant d'ouvrir le chat.

### Base de phrases (`World/ChatPhrases.cs`, fichiers `World/Phrases/fr.txt` et `en.txt`)
- Fichiers texte intégrés au .exe (ressources). Format expliqué en tête de `fr.txt` : `[theme]`, `mots: …` (« b* » = début de mot, « c d » = mots à la suite, accents et majuscules ignorés, « ? » = point d'interrogation), `- phrase` (`{nom}` = pseudo).
- **28 thèmes**, dans cet ordre (l'ordre départage les égalités) : toi, perso, insulte, arbres, minage, peche, chasse, cueillette, cuisine, artisanat, construction, commerce, groupe, niveau, ou, aide, jeu, meteo, fatigue, blague, compliment, excuse, merci, cava, aurevoir, salutation, question, autre. Environ 10 phrases par thème et par langue ; « autre » = les 40 phrases de la v0.4.7.
- **Choix du thème** : le « @gpt » est retiré, puis on compte les mots de chaque thème trouvés dans le message (mots de toutes les langues). Le plus de mots gagne ; égalité : le plus haut dans le fichier ; aucun : « autre ».
- **Choix de la phrase** : au hasard dans le thème, dans la langue du joueur (sinon anglais, sinon « autre ») ; une phrase dite parmi les 12 dernières n'est pas reprise.
- Thèmes sensibles : « toi » (on demande si c'est un bot) répond en restant vague, sans jamais parler de bot ; « perso » (âge, pays, réseaux) ne donne **aucune info** sur Julien ; « insulte » répond calmement.
- Le journal écrit le thème choisi : « Thème : arbres. »
- Ajouter une langue : un fichier `xx.txt` (même thèmes). Ajouter un thème : un bloc dans `fr.txt` et `en.txt`.

### Fautes volontaires (`World/ChatTypos.cs`)
- 5 % des réponses (`ChatReplies.TypoChance`), une seule faute, un seul mot, jamais le pseudo ni le smiley.
- 40 % du temps, si le message s'y prête : faute de grammaire courante (fr : a/à, et/est, ces/ses, on/ont, c'est/s'est, terminaisons é/er/ez, ais/ait… ; en : your/you're, its/it's, there/their, then/than, to/too…).
- Sinon faute de frappe : deux lettres inversées, une lettre oubliée, une lettre doublée, la touche d'à côté (AZERTY en français, QWERTY en anglais), accent oublié. Jamais sur la première lettre.
- Le journal écrit la faute : « Faute volontaire : « cope » au lieu de « coupe » (lettre oubliée). »

### Tests
- `V048Tests` (13 tests, plus 25 messages types) : chat vu avant chaque lettre, chat fermé pendant la frappe ou juste avant Entrée, clic de Julien ; fichiers sans erreur, mêmes thèmes, 8 phrases au moins ; aucun mot interdit (D-055) sur toute la base ; 25 messages → thème attendu ; pas de répétition ; fautes (pseudo jamais touché, une seule, ~5 %).

## 9. v0.4.9 (2026-11-24, D-060) : langue, style, correction, autres bots, fichiers modifiables

- **Langue** (`World/ChatStyle.cs`, `LanguageOf`) : « fr/ », ou message écrit en français (mots courants français contre anglais, accents comptés).
- **Style relâché** (`ChatStyle.Casual`, 25 %) : pas de majuscule au début (sauf pseudo), pas de point final, une abréviation une fois sur deux.
- **Faute corrigée** (`ChatReply.Correction`, 50 % des fautes) : 2e message « *mot » 1,5 à 4 s après, par `SendGptCorrectionAsync` (mêmes sécurités ; pas prêt = abandon).
- **Autres bots ignorés** (`GptChatPicker.LooksAutomated`) : pseudo avec « bot », marques de bot dans le texte, écho d'une des 10 dernières réponses du bot. Journal : « ignoré : ressemble à un autre bot ».
- **Fichiers à côté du .exe** (`ChatPhraseBook.Load`) : dossier `phrases-chat` ; `modele-fr.txt` / `modele-en.txt` réécrits à chaque démarrage ; `fr.txt` / `en.txt` posés là remplacent ceux du .exe ; phrase avec un mot interdit refusée (D-055) ; fichier sans [autre] : phrases du .exe. Journal : « Base de phrases : 28 thèmes, 629 phrases (phrases intégrées au .exe) ».
- **Essai réel** : démarrage du .exe v0.4.9 vérifié (jeu fermé), modèles écrits, fermeture propre. Envoi dans le jeu : pas encore vu.
- Tests : `V049Tests` (9).

### Pistes pour la suite (proposées le 2026-11-24, pas validées)
Voir la liste de 10 idées du message de fin de session 23 (aussi recopiée ici) :
1. Réponse au nom : répondre aussi quand on écrit « ChatGPT » sans « @ ».
2. Messages privés reçus (table `direct_message_state`, lecture seule) : réponse par la commande du jeu (**À vérifier** : commande /w ?).
3. Plafond par heure (ex. 20) et par joueur (ex. 1 toutes les 3 min) en plus de 1 par minute.
4. Pause du chatbot quand Julien écrit lui-même dans le chat depuis peu.
5. Onglet « Chat » dans le bot : historique des messages reçus et des réponses, bouton « tester une phrase » (mode essai sans envoi).
6. Réponses selon l'heure (bonjour / bonsoir), le jour, ou le canal (Trade : ton commerce ; LFG : ton groupe).
7. Infos vraies du jeu lues sur le serveur : niveau de Julien, région, nom de la région (« je suis à Snowrest »), sans info privée.
8. Claude Haiku avec une clé API de Julien (D-054), phrases toutes faites en secours.
9. Liste de joueurs « amis » (réponses plus familières) et liste noire (jamais de réponse).
10. Statistiques : thèmes les plus demandés, messages tombés dans [autre] (pour ajouter des thèmes).
