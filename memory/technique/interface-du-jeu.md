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
- Le bot doit savoir **quelle barre est affichée** avant de cliquer sur une case (repas en case 5, D-032). **À vérifier** : le numéro de la barre active est-il affiché ? Sinon, reconnaître l'icône de l'aliment dans la case.
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
