# Interface du jeu : fenêtres internes

Infos de Julien du 2026-11-16, à prendre en compte pour toute action du bot.

## Fenêtres internes
Inventaire, Compendium, carte, Banque, Vault, Character, Quest Log, chariot, etc. s'ouvrent comme des **petites fenêtres dans le jeu**.

- **Déplaçables** : on peut les faire glisser.
- **Position mémorisée** : une fenêtre se rouvre là où elle était la dernière fois (ex. l'inventaire).
- **Taille fixe** : non redimensionnables.
- **Fermables** : gros bouton carré bleu et blanc avec une croix, en haut à droite de chaque fenêtre.
- Plusieurs fenêtres peuvent être ouvertes en même temps et **se superposer**.

## Conséquences pour le bot
- Ne jamais supposer qu'une fenêtre est à une position fixe : la **trouver à l'écran** (titre, bouton croix) à chaque fois.
- Avant une action dans le monde (clic sur un arbre), vérifier qu'aucune fenêtre ne masque la zone visée, et fermer celles qui gênent (bouton croix).
- Une fenêtre ouverte peut cacher une partie du HUD lu en v0.2 (ex. cible, coordonnées). Une lecture qui échoue soudain peut venir de là.
- Rappel D-030 : aucune fenêtre de carte ouverte pendant le Transfer à la Banque.
- Détails et stratégie : Q-015.

## Barre d'action (infos de Julien, 2026-11-16)
- En bas au centre : cases 1 à 0, puis `-` et `=`.
- **Cadenas** à gauche des flèches : barre **verrouillée** (on ne peut pas déplacer ce qu'elle contient) ou déverrouillée.
- **Plusieurs barres** : les flèches haut / bas à côté du cadenas font défiler **4 barres différentes** (raccourcis Pg. Préc / Pg. Suiv dans les réglages de touches).

### Conséquences pour le bot
- Garder la barre **verrouillée** : un clic raté ne doit pas déplacer un objet (ex. l'aliment).
- Le bot doit savoir **quelle barre est affichée** avant de cliquer sur une case (repas en case 5, D-032).
  **Réponse (captures de Julien, 2026-11-16)** : un **petit point blanc** à droite des flèches indique la barre active. Il **descend d'un cran** par barre : barre 1 = à hauteur de la flèche haut, barre 4 = à hauteur de la flèche bas (`barre-action-1` à `-4`). Les barres 2 à 4 de Julien sont vides. Le bot lira la hauteur du point.
- Endurance vide : les compétences qui en demandent passent en **rouge avec un éclair** (cases 2 et 3, `barre-action-1-endurance-vide`). L'endurance peut descendre jusqu'à **0/100**.
- Possibilité utile plus tard : une barre dédiée au bot (aliment, outils).

## Notifications qui masquent le HUD (remarque de Julien, 2026-11-16)
- **Montée de niveau / succès** : triangle bleu qui descend du centre du haut de l'écran (« FORESTRY / LEVEL 22 »). Il cache la **santé de la cible** pendant quelques secondes.
- Autres : message d'inventaire plein (centre), « Carrying Cargo » (bas), « Entering / Leaving SNOWREST », gains d'objets (gauche).
- Le bot doit les **reconnaître** et considérer les valeurs cachées comme « masquées », pas « illisibles ». Détails : Q-018.

## Réglages du jeu choisis par Julien (2026-11-16)
- Heure affichée en **24 h** (en haut de la minicarte, ex. « 19:13 »).
- Ouverture de la carte **sans animation** : instantanée. À garder pour le bot (Q-002).

## Jauge de métier
Barre verticale à gauche de la minicarte, visible pendant une action : icône du métier, niveau, remplissage. Lue depuis la v0.3.1 (`technique/analyse-lot5-2026-11-16.md`, section 6).

## Inventaire plein (capture de Julien, 2026-11-16, `inventaire-plein-chariot_001`)
- Fenêtre « INVENTORY » : en haut, une case de **cargaison** (tronc porté, « Rough Wood Trunk »), puis une grille de **5 × 5 = 25 cases**, toutes occupées ici (piles de 1 à 85).
- En dessous : pièces (50) et un second compteur (0).
- Fenêtre du chariot à côté (« CHATGPT CART (I) ») : 144/250 en cargaison.
- Pour le bot : « inventaire plein » = les 25 cases occupées. Une case occupée n'est pas forcément une pile pleine. Message « Some items couldn't fit… » quand un objet ne rentre plus (Q-018).

## Capture du jeu par le bot (v0.3.2)
Julien a **2 écrans** : « Impr. écran » capture les deux. Le bot fournit donc la touche **Num 0** : capture exacte du jeu seul (1920×1080, sans perte), rangée dans `images/analyse/` sous le nom `AAAAMMJJ_analyse_capture-bot_HHMMSS.png`. **À vérifier** : Num 0 n'a pas d'effet dans le jeu.
