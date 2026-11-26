# T-14 (10:00) — v0.4.32 : « Aller à cette ressource » et « Récolter jusqu'à épuisement »

Règles communes : `nuit-2026-11-27-regles.md`. Lis le bilan (T-05 reprise, T-10 à T-12 déplacement) : tu assembles les deux.

## Pourquoi
Demande d'origine de Julien (26/11) : « quand je coupe du bois, il y a plein d'arbres autour, mais trouver le chemin est pénible ». Sur la carte du bot : clic (droit) sur une ressource → « Aller à cette ressource » ou « Récolter cette ressource » ; le bot y va et **la récolte jusqu'à ce qu'elle soit épuisée** (réponse de Julien).

## À faire
1. Menu sur une ressource de la carte (et sur une ligne du tableau) : « Aller à cette ressource », « Récolter jusqu'à épuisement ». Aussi pour la **ressource recherchée la plus proche** (bouton à côté de « Plus proche »).
2. Aller : « Se déplacer ici » (T-10/T-12) vers **une case voisine** de la ressource (pas sur elle), avec les mêmes refus (eau, marche, distance).
3. Récolter, une fois arrivé : il faut lancer le premier coup. La reprise (T-05) sait déjà relancer avec **souris sur la ressource + M**, mais il lui faut un point écran valable. Ici, le personnage vient de bouger et la caméra a suivi : **le point est inconnu**. Choisir la méthode la plus sûre d'après l'étude T-09 et les essais T-11 (par exemple : après l'arrivée, W remet la caméra derrière le personnage, puis recherche du contour vert autour du personnage en survolant quelques points proches, confirmation par la cible du serveur `target_state` avant M). **Si aucune méthode n'est sûre sans essai réel** : faire « Aller à cette ressource » complètement, et pour « Récolter » s'arrêter à l'arrivée avec le message « cliquez une fois sur la ressource : le bot continuera jusqu'à épuisement » (la reprise prend alors le relais, y compris quand l'endurance tombe). Le dire clairement dans le bilan.
4. Fin : ressource épuisée (serveur), sac et chariot pleins (M sans effet), ennemi (sécurités §3), Julien reprend la main.
5. Tests `V0432Tests.cs`.

## Fini quand
- v0.4.32 publiée, tests verts, essai sans le jeu OK, bilan : ce qui est automatique, ce qui demande un clic de Julien, quoi essayer.

## Interdit
- Lancer le jeu. Escalade. Chariot, bateau.
