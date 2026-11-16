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
