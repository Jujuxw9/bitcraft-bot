# Q-027 — Module « @GPT » : le bot répond dans le chat du jeu ?

- **Statut** : Résolue → D-054 et D-055 (2026-11-23)
- **Priorité** : Basse (la récolte automatique passe d'abord)
- **Créée le** : 2026-11-23
- **Liens** : D-024, D-039, D-045, D-052 ; étude : [technique/chat-gpt.md](../technique/chat-gpt.md)

## Question

Julien veut que le bot réponde (avec Claude Haiku 5.5) aux joueurs qui écrivent « @GPT » dans le chat. Lire le chat est possible en lecture seule. Écrire n'est possible que **dans le jeu, au clavier**, comme Julien (l'envoi direct au serveur est interdit, D-039). Avant de coder, il faut ses réponses :

1. **Touche Entrée** : D-024 interdit au bot la touche Entrée (le chat). Julien lève-t-il cette interdiction, pour le chat seulement ?
2. **Prendre le clavier** : le bot peut-il prendre le clavier quelques secondes pour écrire ? Et pendant la récolte (elle s'arrête le temps d'écrire) ?
3. **Qui** : répondre à tous les joueurs, ou à une liste de pseudos ?
4. **Où** : quels canaux ? Local, Région (« General »), messages privés, Commerce, Groupe ?
5. **Ton** : drôle, troll gentil, sérieux ? Un mélange ? Langue des réponses (celle du joueur, ou français) ?
6. **Fréquence** : combien de réponses au plus ? (proposition : 1 par joueur toutes les 5 minutes, 1 par minute en tout, 20 par heure)
7. **IA** : le message doit-il dire que c'est une IA (par exemple « [GPT] » au début) ?
8. **Clé API** : Julien a-t-il une clé API Anthropic ? Quel budget par mois ? (coût estimé : environ 0,20 $ pour 1 000 réponses)
9. **Mode essai** : d'accord pour commencer par un mode où le bot montre la réponse dans sa fenêtre **sans l'envoyer** ?

## Pistes / options

Voir l'étude : architecture, risques (règles du jeu, modération, contenu, coût), parades.

## Ce qui bloque

Les réponses 1, 3, 4, 6 et 8 sont données (D-054). Le mode essai (lecture, réponse affichée sans envoi) peut être codé. L'envoi réel attend les réponses 2, 7 et 9.

## Réponses de Julien (2026-11-23) → D-054

Ses mots : « je change de décision, c'est utile seulement pour repondre à des messages chat ok. Il faut que la personne utilise le @GPT (en majuscule). Reponse à tout le monde, seulement 1 réponse par minute max. Ensuite, répondre à la question la plus récente. Possible que la personne ne formule pas de question, mais juste une phrase. y repondre quand meme. canaux region seulement pour commencer. pas de clé API. besoin d'une clé ? pas possible de faire une phrase automatique simple universelle pour le debut ? »

- 1 : oui, Entrée permise pour le chat seulement. 3 : tout le monde, « @GPT » en majuscules. 4 : Région. 6 : 1 par minute, le plus récent. 8 : pas de clé → phrases toutes faites d'abord (Haiku plus tard).
- 5 (ton) : implicite : drôle et neutre, en phrases toutes faites.
- **Restent** : 2 (prendre le clavier **pendant la récolte** ?), 7 (marquer « [GPT] » / dire que c'est un bot ?), 9 (mode essai d'abord ? recommandé).

## Réponses de Julien, suite (2026-11-23, soir) → D-055
- Le bot **peut prendre le clavier pendant la récolte** ; il **ne doit pas oublier de fermer le chat** après l'envoi.
- **Ne pas dire que c'est un bot** (pas de « [GPT] » ni « IA »).
- **Haiku plus tard** : on commence tranquille, avec des phrases simples sans IA.

## Réponse au point 9 (2026-11-23, 22:45) → D-056
Pas de mode essai : la v0.4.5 envoie vraiment la réponse dans le chat Région.
