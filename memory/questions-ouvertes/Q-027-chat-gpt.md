# Q-027 — Module « @GPT » : le bot répond dans le chat du jeu ?

- **Statut** : Ouverte
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

Rien n'est codé. Il faut les réponses de Julien, au moins 1, 2, 3, 4 et 8.
