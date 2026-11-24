# D-061 — v0.4.10 : plafonds, pause, amis et liste noire, ton selon canal et heure, infos du jeu, onglet CHAT

- **Statut** : Validée
- **Date** : 2026-11-24
- **Validée par** : Julien (« 2 oui, 3 oui, 4 oui, 5 oui, 6 oui, 7 oui, 9 oui, 10 oui ; la 1 non ; la 8 plus tard ; The Krazy Turkey en ami ; plafonds très bien ; pause 5 min très bien ; tout d'un coup dans la 4.10 »)
- **Liens** : D-054, D-055, D-058, D-059, D-060 ; détail : `technique/chat-gpt.md` section 10

## Décision
1. **Plafonds** : 20 réponses par heure, 100 par jour (24 h glissantes), 3 par joueur et par heure. Au plafond, le message reste sans réponse (écrit dans le journal). Les compteurs repartent de zéro à chaque démarrage du bot.
2. **Pause** : si Julien écrit lui-même dans le chat (un message à lui que le bot n'a pas écrit), le bot ne répond plus pendant 5 minutes. Reconnaissance d'un message du bot : son texte est connu, ou il a été écrit moins de 6 s autour d'un envoi du bot. Une réponse en attente est abandonnée.
3. **Amis et liste noire** : fichiers `amis.txt` et `liste-noire.txt` dans `phrases-chat` (à côté du .exe), un pseudo par ligne, « # » = commentaire, pseudos comparés sans majuscules ni signes. Ami : ton familier (style relâché 70 %, smiley 50 %). Liste noire : jamais de réponse (plus forte que « ami »). Ami d'office : **The Krazy Turkey** (avec un K, demande de Julien).
4. **Ton selon le canal** : Trade sérieux (smiley 10 %, relâché 5 %), Local détendu (40 % / 35 %), LFG 30 % / 25 %, Région inchangé (30 % / 25 %). **Selon l'heure** : « Bonjour » le matin et l'après-midi, « Bonsoir » de 18 h à 5 h (idem « bonne journée / soirée », « Good morning / afternoon / evening »).
5. **Vraies infos du jeu** : `{region}` (région du personnage) et `{niveau}` (plus haut niveau de métier), lues sur le serveur. Une phrase qui les utilise n'est choisie que si l'info est connue. **Jamais** d'info privée (e-mail, jeton, position précise, inventaire).
6. **Onglet CHAT** : historique des 200 dernières lignes, état (plafonds, pause), statistiques, et bouton « Tester une phrase » (thème et réponse, rien n'est envoyé ni compté).
7. **Statistiques** : comptes par thème dans `stats.txt` ; les messages sans thème sont notés dans `messages-sans-theme.txt` (pour savoir quels thèmes ajouter).

## Pas fait : messages privés
Les messages privés sont dans un autre module du serveur (module global, table `direct_message_state`, envoi par `chat_post_targeted_message`). Il faudrait une 2e liaison au serveur (le module global refuse après 1 h sans connexion au jeu, voir RegionFinder) et la commande du jeu pour répondre en privé n'est pas vérifiée. Reporté, **À vérifier** avant de coder.

## Inchangé
1 réponse par minute au plus, jamais d'allusion à un bot (D-055), « pas deux fois la même phrase » (D-060), idée 1 (répondre à « ChatGPT » sans « @ ») refusée, idée 8 (IA Claude Haiku) plus tard.
