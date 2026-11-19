# Session 12 — repas automatique, images, v0.3.15 (2026-11-19)

## Demandes de Julien
- Maj + E mange (info pour plus tard) ; ranger les raccourcis du jeu dans un fichier.
- Recettes : image de chaque objet (directe ou au survol).
- Repas automatique : faim < 30 % ou plus de bonus de nourriture ; un aliment à la fois, avec une pause, sans dépasser le manque.
- Nom de version dans le .exe ; onglet Journal avant Paramètres.
- La v0.3.14 n'a pas été essayée.

## Fait
- `technique/raccourcis-du-jeu.md`. Le registre du jeu ne contient aucune touche modifiée : Julien a les touches par défaut.
- D-042 (remplace D-032). Lecture des bonus actifs et de la faim maximale (tables publiques). Bonus « Food Regen » identifié (buff_type_desc).
- Images : CDN public de brico.app, gardées sur le PC.
- v0.3.15 publiée : `artifacts/release/BitcraftBot-v0.3.15.exe`. 242 tests OK.

## Pas fait / à vérifier
- Pas de lancement à l'écran : Julien jouait (le bot aurait pris le premier plan).
- En jeu : Maj + E mange-t-il bien le premier aliment du sac ? Format réel des bonus et des poches.

## Questions ouvertes à Julien
- Garder `technique/recettes-liste-2026-11-19.md` (autre session) ?
- Lancer H3 après l'essai ?

## Suite : v0.3.16 (même session)
- Essai v0.3.15 lu : repas automatique OK en réel, formats des poches et des bonus confirmés, banque OK.
- Demandes : zones plus fines, overlay « repas auto » activé ou non, bonus avec durée et effets, onglets regroupés, Journal dans Paramètres, onglet Tests supprimé, H3 en attente (« retravailler le bot de manière générale »).
- Fait : D-043 (onglets), D-044 (serveur d'abord), D-035 terminée. v0.3.16 publiée, 246 tests OK, onglets vérifiés à l'écran (bot de test lancé puis fermé).
- `technique/recettes-liste-2026-11-19.md` : gardé (notes de fabrication utiles à Julien en jeu, pas au bot). Une autre session le modifie encore : pas touché.
