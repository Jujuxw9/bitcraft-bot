# Plan de bataille : récolte automatique (arbre ou roche)

Rédigé le 2026-11-19 (session 15), à la demande de Julien. À valider par Julien (questions en fin de page, Q-023).

## 1. Où on en est

| Brique | État |
|---|---|
| Lecture du serveur | Fait : position exacte, vie, endurance, faim, sac et banques, bonus, métiers, action en cours, ressources autour (nom, âge, santé, position) |
| Journal des tâches | Fait (v0.3.19) : début, fin, coups, XP, objets. Vérifié en réel : pêche et coupe |
| Écran | Cible sélectionnée seulement (D-048), image du jeu en direct |
| Contrôleur d'actions | Fait (D-045) : toutes les touches et clics passent par les mêmes sécurités. Déjà utilisé par le repas auto |
| Gestes humains | Faits (D-035, D-037) : trajet de souris en courbe, vitesse variable, imperfections |
| **Manque** | **Savoir où cliquer à l'écran pour toucher un arbre donné** |

## 2. Le vrai défi

Le serveur dit où est l'arbre **dans le monde** (x, z). Pour le couper, il faut un **clic droit sur l'arbre à l'écran** (D-024). Il faut donc convertir « position dans le monde » en « pixel à l'écran ». Ça dépend de la caméra : son angle, son zoom, sa rotation.

Bonne nouvelle : le personnage est toujours au centre de l'écran, et la touche **W** remet la caméra dans la même orientation. Avec un zoom fixe, la conversion est une simple formule (une projection), à mesurer une fois.

Deuxième bonne nouvelle : le serveur dit **quelle ressource le personnage frappe** (`player_action_state`, `extract_outcome_state`). Le bot peut donc **vérifier chaque clic** sans lire l'écran : « j'ai visé l'arbre n° 123, le personnage frappe-t-il l'arbre n° 123 ? ».

La boucle complète :

```
serveur : meilleur arbre ──► formule : pixel à l'écran ──► souris : clic droit (contrôleur d'actions)
     ▲                                                                   │
     └──── serveur : le personnage frappe-t-il CET arbre ? ◄─────────────┘
           oui → laisser couper ; non → annuler, corriger la formule, réessayer
```

## 3. Arbre ou roche ?

Recommandation : **arbre d'abord**. Ensuite la roche réutilise tout.
- Le métier est le plus haut (Forestry 30) et la zone de Keldan est pleine d'arbres (497 ressources lues autour).
- Un arbre est une grande cible, facile à toucher au clic. Une roche est basse et petite.
- Les règles existent déjà : choix de l'arbre (D-034, D-036), Maker's Tree interdit, cycle d'endurance (D-028).
- Roche : même chaîne, seuls changent le filtre (famille « Roche ») et l'outil (pioche). Coût estimé : une petite étape (R6).

## 4. Les étapes (un livrable testable à chaque fois)

Chaque étape s'arrête sur un essai de Julien. On ne passe à la suivante que si l'essai est bon.

### R1 — Apprendre la caméra en regardant Julien jouer (v0.4.0, aucune action)
- Le bot note **où Julien fait un clic droit** (écoute de la souris, comme celle du clavier : lecture seule) et **ce que le serveur dit qu'il frappe** juste après.
- Chaque paire « pixel cliqué / position de la ressource dans le monde » sert à calculer la formule de la caméra.
- Livrable : carte « Caméra » dans Paramètres : nombre de clics appris, précision (en pixels).
- **Test de Julien** : couper 10 à 15 arbres à la main, à des distances variées, caméra recentrée (W), zoom habituel.
- Réussi si : précision meilleure que ~25 pixels (la moitié de la largeur d'un tronc).

### R2 — Dessiner les arbres du serveur sur l'image du jeu (v0.4.1, aucune action)
- Sur l'image du jeu dans l'onglet Bot : un rond sur chaque arbre connu du serveur, une étoile sur le meilleur.
- C'est la preuve visuelle que la formule marche, avant tout clic.
- **Test de Julien** : se promener, tourner un peu, vérifier que les ronds suivent les arbres. Capture Num 0 si décalage.
- Réussi si : les ronds sont sur les troncs, caméra recentrée.

### R3 — Viser sans cliquer (v0.4.2, la souris bouge, aucun clic)
- Bouton « Viser le meilleur arbre » : le bot recentre la caméra (W), puis amène la souris sur l'arbre avec un geste humain (contrôleur d'actions). Pas de clic.
- Le jeu entoure l'objet survolé : le bot vérifie à l'écran que le contour est sur un arbre (à étudier, sinon on passe à R4).
- **Test de Julien** : 10 visées, regarder où s'arrête la souris.
- Réussi si : 9 sur 10 sur le bon arbre.

### R4 — Un clic, une coupe (v0.4.3, première vraie action de récolte)
- Bouton « Couper le meilleur arbre » : visée (R3), **clic droit**, puis le bot attend la preuve du serveur : le personnage frappe-t-il **cet** arbre dans les 10 s ?
- Non : clic gauche à côté du personnage pour annuler (D-036), journal « raté », la paire sert à corriger la formule.
- Une seule coupe par appui sur le bouton : Julien garde la main.
- **Test de Julien** : 10 appuis.
- Réussi si : 8 sur 10 du premier coup, aucun clic sur autre chose qu'une ressource.

### R5 — Boucle de récolte sur place (v0.4.4)
- Le bot enchaîne seul : choisir → viser → couper → attendre la fin (arbre abattu ou épuisé, d'après le serveur) → arbre suivant.
- **Endurance** : à 0, attendre 100 % puis reprendre (D-028). **Faim** : repas auto (déjà fait).
- **Arrêts** : sac plein (le bot s'arrête et le dit), vie qui baisse (attaque), souris bougée par Julien, bouton « Arrêter », durée max, jeu plus au premier plan.
- Le journal des tâches sert de bilan : XP et objets par heure.
- **Test de Julien** : 15 min sans toucher à rien, puis 1 h.
- Réussi si : aucun blocage, aucune action hors du jeu, arrêt propre à chaque cas d'arrêt provoqué.

### R6 — Les roches (v0.4.5)
- Même boucle, famille « Roche » et métier Mining. Vérifier l'outil (pioche) dans la ceinture via le serveur avant de partir.
- **Test de Julien** : 15 min de minage.

### R7 — Sécurité face aux ennemis (v0.4.6)
- Le serveur a des tables publiques d'ennemis (Q-014, « À vérifier » : nom exact et contenu). Ne pas choisir un arbre près d'un ennemi ; s'éloigner si un ennemi approche ; s'arrêter si la vie baisse.

### Après : H4 — la Banque (v0.5)
- Trajet long vers la Banque de Snowrest quand le sac est plein, dépôt, retour (D-016, D-030). Gros morceau à part, planifié une fois R5 stable.

## 5. Risques et parades

| Risque | Parade |
|---|---|
| La caméra tourne ou zoome (Julien, ou le jeu) | W avant chaque visée ; zoom fixe demandé (Q-023) ; la vérification serveur détecte tout raté |
| Un objet devant l'arbre (autre arbre, bâtiment, joueur) | Le serveur dit ce qui est frappé ; raté = annuler, essayer l'arbre suivant |
| Fenêtre du jeu ouverte (inventaire…) qui cache la vue | Écran : vérifier qu'aucune fenêtre n'est ouverte (Q-015) ; sinon Échap |
| Le personnage marche loin (clic sur un arbre lointain) | Ne viser que des arbres à moins de ~10 cases |
| Clic sur un autre joueur ou un PNJ | Ne cliquer que là où la formule place une ressource ; vérification serveur |
| Détection par l'éditeur du jeu | Risque accepté (D-001). Gestes humains, pauses, sessions bornées |
| Mort du personnage | R7 ; arrêt dès que la vie baisse |

## 6. Ce que Julien devra faire

- R1 : jouer normalement 15 min en coupant à la main (le bot apprend).
- À chaque étape : un essai court et le journal.
- Garder le zoom de la caméra fixe pendant les essais.
