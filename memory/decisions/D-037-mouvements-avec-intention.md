# D-037 — Mouvements de souris et de caméra guidés par une intention

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien (retour sur le banc d'essai v0.2.2)
- **Liens** : D-035, D-036, D-024

## Contexte
Banc d'essai v0.2.2 (D-035) : la souris allait vers des points **au hasard** dans le centre de l'écran.
Retour de Julien : « Le déplacement en tant que tel est plutôt bien fait, ça ressemble à une main humaine. Mais on ne comprenait pas pourquoi la souris allait à ces endroits. Ce n'est pas en raccord avec le jeu. »

## Décision
1. **Garder** la forme des gestes (courbes, vitesse en cloche, dépassements, tremblements).
2. **Chaque mouvement a un but** lié au jeu : regarder ce que montre la caméra, choisir quoi faire, aller vers l'endroit où cliquer. Plus de déplacements gratuits au hasard.
3. **Imperfections volontaires** autour de ce but : petits décalages par rapport au centre de la cible, hésitations, bruit, parfois un survol d'un objet voisin avant le bon.
4. La caméra bouge pour une raison (chercher un arbre hors de l'écran, recentrer avec W), pas au hasard.

## Conséquences
- En v0.4 (premières actions), les mouvements suivent la boucle « voir → décider → agir » : la cible vient de la perception (contour vert, D-036).
- Le banc d'essai au hasard reste un simple outil de réglage, pas un modèle de comportement.
