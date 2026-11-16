# Q-017 — Mode test : mouvements aléatoires « humains » de la souris et de la caméra
- **Statut** : Ouverte (idée de Julien, 2026-11-16)
- **Priorité** : Moyenne
- **Créée le** : 2026-11-16

## Idée de Julien
Une fonction **de test uniquement** :
1. déplacement **aléatoire de la souris** ;
2. déplacement **aléatoire de la caméra** ;
toujours avec une allure **humaine** (« human being like »). But : voir comment Claude s'en sort, que Julien puisse filmer et donner son ressenti, et préparer la suite.

## Proposition de Claude
Un **banc d'essai des mouvements**, dans un nouvel onglet « Tests », en **v0.2.2** (avant la v0.3) :
- C'est la première fois que le bot **agit** sur le jeu, mais sans risque : **aucun clic**, aucune autre touche que la caméra.
- **Souris** : trajets en courbe (pas en ligne droite), vitesse qui accélère puis ralentit, petit dépassement de la cible puis correction, légers tremblements, pauses de durée variable. Uniquement **dans la fenêtre du jeu**, en évitant le HUD.
- **Caméra** : appuis de durée variable sur **Z / Q / S / D** (rotation), pauses, et **W** de temps en temps pour recentrer. Aucune autre touche.
- **Durée** courte et réglable (30 s par défaut), bouton « Lancer le test souris » et « Lancer le test caméra ».
- **Sécurité** : Num 5 arrête tout immédiatement ; durée max ; toutes les touches relâchées à l'arrêt.
- **Journal** : chaque mouvement noté (départ, arrivée, durée), pour comparer avec la vidéo de Julien.
- Ce code servira tel quel en v0.4 pour viser les arbres.

## Points à confirmer avec Julien
- Placement : v0.2.2 (tout de suite) ou plus tard ?
- La souris ne fait que bouger (pas de clic) : d'accord ?
- Pendant le test, le personnage doit-il être dans un endroit calme (pas d'ennemis, pas de joueurs) ?

## Ce qui bloque
Accord de Julien sur le placement.
