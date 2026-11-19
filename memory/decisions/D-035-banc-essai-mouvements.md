# D-035 — v0.2.2 : banc d'essai des mouvements humains

- **Statut** : Terminée (2026-11-19) : banc retiré de l'interface en v0.3.16, les mouvements seront testés directement en H3 (D-043). Le code des mouvements humains reste dans la brique Input.
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : Q-017, D-004, D-022, D-024

## Contexte
Idée de Julien (Q-017) : tester des mouvements aléatoires « humains » de la souris et de la caméra, pour donner son ressenti (vidéo) avant que le bot agisse vraiment.

## Décision
- **v0.2.2, avant la v0.3**.
- Fonction de **test**, **désactivée par défaut** : Julien l'active lui-même, une fois dans un endroit calme (sans ennemis). Elle se désactive à chaque redémarrage du bot.
- Par étapes :
  1. **Souris qui bouge**, sans clic.
  2. **Clics gauches** autorisés par une case à part : petits déplacements du personnage (« Move To Cursor »), pas loin. Jamais de clic droit.
  3. **Caméra** : Z / Q / S / D (rotation) et W (recentrer). Aucune autre touche.
- Allure **humaine** : trajets en courbe, accélération puis ralentissement, léger dépassement, petits tremblements, pauses variables.

## Sécurités
- Num 5 arrête tout immédiatement ; toutes les touches relâchées à l'arrêt.
- Le bot n'agit que si **la fenêtre du jeu est au premier plan** ; sinon arrêt (jamais d'action dans une autre application).
- Si Julien **bouge la souris lui-même** pendant un test : arrêt (mécanisme 2 de D-004).
- Durée courte (30 s par défaut, 2 min max).
- La souris reste dans une zone sûre au centre de l'écran (hors HUD, minicarte, chat, barre d'action).

## Conséquences
- Ce code (brique Input) servira en v0.4 pour viser les arbres.
