# D-065 — v0.4.14 : barres animées, PERSONNAGE, curseur, filtre par palier, PARAMÈTRES refait, overlay en bas

- **Statut** : Validée
- **Date** : 2026-11-25
- **Validée par** : Julien (demandes du 25/11, go du 25/11 soir)
- **Liens** : D-063, D-064

## Décision
1. **Barres animées** : vie, endurance, faim, action, métier et sac glissent en 500 ms (durée du rafraîchissement) vers leur nouvelle valeur.
2. **PERSONNAGE** : texte « Clique sur le titre… » retiré ; colonne « Où » des banques retirée : un objet présent dans plusieurs banques = une ligne (total), les lieux et quantités s'affichent **au survol** de la ligne ; colonnes Quantité, Type, Palier élargies pour la flèche de tri ; la colonne Objet prend la largeur libre ; les trois listes ont la même largeur.
3. **Curseur main** : il venait des onglets (le style des onglets le donnait à tout leur contenu). Il est maintenant seulement sur l'en-tête des onglets, les boutons, interrupteurs, cases, titres de colonne triables, la carte et les liens des recettes.
4. **CARTE** : filtre par **palier** (cases T1, T2… et « ? », avec le nombre de ressources des familles cochées, bouton « Tous ») en plus du filtre par famille. Limite : seulement la zone lue autour du personnage.
5. **PARAMÈTRES** : par ordre d'importance : connexion en grand (gros boutons, état du jeton en petit), caméra, puis trois petites cartes (Sécurité, Affichage, Système) à interrupteurs rouge/vert, puis Compte replié (bouton « Afficher » ; ouvert d'office s'il n'y a pas de jeton). Cases du journal gardées.
6. **Overlay** : collé au bord bas du jeu, au centre (coins du bas carrés), par-dessus les chiffres de la barre d'action. Pas de touche (D-064).
7. **Touche tenue** : seul le premier appui compte jusqu'au relâchement (W tenu écrivait ~50 lignes). Relâchement perdu : la touche compte de nouveau après 1,2 s.
8. **Capture à 2 images/s** : pas une panne. Voulu depuis la v0.3.19 quand l'image du jeu n'est pas affichée dans l'onglet BOT.

## Limites connues
- Curseur main : vérifié par raisonnement et compilation ; le test automatique du curseur n'a pas marché (écran sans doute verrouillé). **À vérifier** par Julien.
- Position de l'overlay sur la barre d'action : **À vérifier** en jeu (il couvre les chiffres 1, 2, 3… sous les icônes).
