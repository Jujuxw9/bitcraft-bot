# D-058 — v0.4.7 : « @gpt » et « @chatgpt », réponse dans le canal du message par les commandes du jeu, frappe humaine

- **Statut** : Validée
- **Date** : 2026-11-23
- **Validée par** : Julien (« il doit répondre à @chatgpt ou @gpt (minuscules ou majuscules) » ; « il y a des commandes pour écrire en local, region ou autre, utilise ça pour répondre en fonction de ce qui est dit dans le chat en te taguant » ; 30 phrases de plus ; smiley 30 % ; délai aléatoire entre deux frappes ; temps de lecture ; minuteur affiché)
- **Liens** : D-054 et D-055 (règles changées ici), D-057 (point 2 remplacé), D-045, D-039

## Contexte
Capture du 23/11 (23:39, `images/reference-ui/chat/menu-canaux_001.png`) : un clic sur le bouton du canal ouvre une **liste** avec les commandes : Local `/s`, Region `/y`, Looking For Group `/lfg`, Trade `/t`.

## Décision
1. **Déclencheur** : « @gpt » ou « @chatgpt », majuscules ou minuscules, pas collé à un autre mot (remplace « @GPT en majuscules exactement » de D-054).
2. **Canaux** : Local, Région, Looking For Group, Trade (remplace « Région seulement »). Réponse **dans le canal du message**.
3. **Choix du canal par les commandes** : le chat ouvert, si le bouton ne montre pas le bon canal, le bot tape la commande (« /y ») puis une espace ; si le bouton change, il écrit la suite ; s'il ne change pas, la commande reste au début de la ligne (le jeu la lit à l'envoi). **Plus de clic sur le bouton, plus de passage au lancement du jeu** (remplace D-057 point 2 : chaque réponse choisit son canal).
4. **Phrases** : 40 par langue (30 de plus), jamais d'allusion à un bot (D-055) ; **smiley** simple (« :) », « ^^ »…) ajouté 30 % du temps.
5. **Comme une vraie main** : temps de lecture avant de répondre (1,5 s + 60 ms par lettre du message, 6 s au plus, + 0,5 à 2,5 s) ; frappe irrégulière (55 à 165 ms par lettre, plus après une espace et la ponctuation, hésitation de 0,3 à 0,9 s 4 % du temps).
6. **Minuteur** dans la carte CHAT : « Prochaine réponse possible dans N s ».
7. Inchangé : 1 réponse par minute au plus, au message le plus récent ; jamais aux messages de Julien ; une seule tentative ; chat refermé et vérifié.

## Conséquences
- Abonnement : une requête simple par canal (4), messages des 5 dernières minutes.
- **À vérifier** au premier essai : le bouton change-t-il dès « /y » + espace, ou la commande est-elle lue à l'envoi ? (ligne « canal changé par… » ou « bouton du canal pas changé… » dans le journal).
- Local : la table ne dit pas où est l'auteur ; le bot répond même à un joueur loin (sa réponse Local ne lui arrivera peut-être pas).
- Limite vue sur la capture : quand la liste des canaux est ouverte, le cadre blanc de la zone de saisie disparaît, et le chat est vu « fermé ».
