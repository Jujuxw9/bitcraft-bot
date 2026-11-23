# D-054 — Module « @GPT » : Entrée permise pour répondre dans le chat Région, 1 réponse par minute

- **Statut** : Validée
- **Date** : 2026-11-23
- **Validée par** : Julien (« je change de décision, c'est utile seulement pour repondre à des messages chat ok. Il faut que la personne utilise le @GPT (en majuscule). Reponse à tout le monde, seulement 1 réponse par minute max. Ensuite, répondre à la question la plus récente. […] canaux region seulement pour commencer. pas de clé API. »)
- **Liens** : remplace en partie D-024 (touche Entrée) ; Q-027 ; D-039, D-045, D-052 ; étude : `technique/chat-gpt.md`

## Contexte
D-024 interdisait au bot la touche Entrée (le chat). Julien veut que le bot réponde aux joueurs qui écrivent « @GPT ». Lire le chat se fait en lecture seule ; écrire n'est possible qu'au clavier dans le jeu (l'envoi au serveur est interdit, D-039).

## Décision
1. **Entrée permise au bot, seulement pour répondre dans le chat.** Toute autre utilisation reste interdite.
2. **Déclencheur** : le message contient « @GPT » **en majuscules exactement** (« @gpt » ou « @Gpt » ne déclenchent rien).
3. **Qui** : tout le monde, sauf Julien lui-même (pas de réponse à ses propres messages).
4. **Où** : canal **Région** seulement pour commencer.
5. **Fréquence** : **1 réponse par minute au plus**. S'il y a plusieurs messages en attente, le bot répond **au plus récent seulement** ; les autres sont oubliés.
6. **Répondre même si ce n'est pas une question** (une simple phrase suffit).
7. **Pas de clé API pour le moment** : Claude Haiku demande une clé API Anthropic payante (à part de l'abonnement Claude). Pour commencer, le bot répond avec des **phrases toutes faites**, courtes, drôles et neutres, tirées au hasard (parfois avec le pseudo du joueur). Le branchement de Haiku est prévu pour plus tard, quand Julien aura une clé.
8. Écriture au clavier seulement, par le contrôleur d'actions (D-045) ; aucune action si une fenêtre du jeu est ouverte (D-052) ; aucun appel au serveur (D-039).

## Points encore ouverts (Q-027)
- Le bot peut-il prendre le clavier **pendant la récolte** ?
- Faut-il marquer la réponse (« [GPT] ») ou dire que c'est un bot ?
- Mode essai d'abord (réponse affichée dans la fenêtre du bot, sans l'envoyer) : recommandé.

## Alternatives écartées
- Appeler le serveur pour envoyer le message : interdit (D-039).
- Répondre à chaque message en attente : spam, écarté par la règle « le plus récent seulement ».

## Conséquences
- D-024 : la ligne « touches interdites : Entrée » ne vaut plus pour le chat.
- Le bot doit lire `chat_message_state` (canal 3, Région) en plus de ses abonnements actuels.
