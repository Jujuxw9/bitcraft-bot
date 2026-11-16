# Q-020 — Onglet « Bêta : exploration » (déplacements autonomes dans l'herbe)
- **Statut** : Résolue → D-038 (rayon 100 réglable, 5 min réglable, ennemi = arrêt + alerte)
- **Priorité** : Haute (prochaine version)
- **Créée le** : 2026-11-16
- **Liens** : D-035 (banc d'essai), D-037 (mouvements avec intention), D-024 (commandes), D-029 (zone sans ennemis), Q-014, Q-002

## Demande de Julien (ses mots, résumés fidèlement)
- Un **onglet bêta test** qui **combine** les mouvements automatiques de la caméra et les déplacements « aléatoires » du personnage.
- **Règle n° 1** : se déplacer **sans jamais cliquer sur un objet qui s'illumine en vert** (contour vert). Cliquer et se déplacer **uniquement dans l'herbe**, et **explorer** les zones.
- Se baser sur la **minicarte** et sur **ce qu'il voit à l'écran**.
- Ne pas hésiter à appuyer sur **W** pour remettre la caméra « au bon endroit ».
- Faire attention aux **buissons**, **fleurs**, **arbres**, **cailloux**, **veines** de minerai… et **surtout à l'eau** (« l'eau n'est pas encore bien apprise »).

## Ce qui existe déjà
- Banc d'essai v0.2.2 (onglet Tests) : gestes de souris « humains », clics gauche de déplacement près du personnage, caméra Z/Q/S/D/W, sécurités (Num 5, souris bougée à la main, jeu au premier plan). Mais les cibles sont **au hasard** (D-037).
- Lecture : position (98-100 %), jauge de métier, cible, vie, endurance, faim.
- Pas encore : reconnaissance de l'herbe, de l'eau, des contours verts, de la minicarte.

## Proposition de Claude (v0.4 bêta)
Onglet **« Bêta : exploration »**, **désactivé à chaque démarrage**, mêmes sécurités que le banc d'essai, plus une durée maximale et un **rayon maximal** autour du point de départ.

Boucle « voir → décider → agir » :
1. **Voir** : repérer dans l'image les zones d'**herbe** (couleur et texture), écarter l'**eau** (bleu / turquoise, reflets), l'interface, les zones sombres (ombres, objets).
2. **Choisir** une destination dans l'herbe, à distance moyenne, de préférence vers une direction **peu visitée** (le bot garde une carte des positions déjà parcourues grâce aux coordonnées).
3. **Vérifier par le survol** : la souris va sur le point (geste humain, D-037) et **attend 0,2 à 0,5 s** ; si un **contour vert** apparaît, le point est refusé et la souris repart ailleurs. C'est la garantie de la règle n° 1 : **aucun clic sur un objet récoltable**.
4. **Agir** : clic gauche → le personnage marche. Le bot suit les coordonnées : arrivé, bloqué (position figée) ou dévié.
5. **Caméra** : de temps en temps, tourner (Q/D) pour regarder autour, puis **W** pour recentrer, toujours avec une raison (chercher une zone dégagée, revenir face à la direction voulue).
6. **Minicarte** : ennemi (triangle rouge) proche → s'éloigner ou arrêter (Q-014). Plus tard : préférer les zones non découvertes.
7. **Garde-fous** : eau détectée devant → demi-tour ; position qui ne change plus 3 fois → autre direction ; vie qui baisse → arrêt immédiat.

## Questions pour Julien
1. **Rayon maximal** autour du point de départ ? (proposition : 100 unités de carte)
2. **Durée** d'une exploration ? (proposition : 5 min, réglable)
3. Ennemi proche sur la minicarte : **arrêt** du bot ou **fuite** ? (proposition : arrêt + alerte, plus sûr)
4. Pour **apprendre l'eau** : des captures Num 0 de rives, de rivière, de lac, de jour et de nuit (les 5 captures du 16/11 à 20:10 montrent déjà un lac).

## Ce qui bloque
Réponses aux questions ci-dessus. Puis : exemples d'eau et d'herbe (captures Num 0).
