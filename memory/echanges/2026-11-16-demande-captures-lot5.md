# Demande de captures et vidéos — lot 5 (2026-11-16)

Pour : lecture fiable des coordonnées (Q-016), v0.3 (repérer les arbres, D-034), la nuit (D-025), la barre d'action et les fenêtres (Q-015).

## Règles générales
- **Écran complet** par défaut (touche Impr. écran, ou capture de tout l'écran). Je découpe moi-même. Une capture écran complet vaut mieux qu'un zoom, car le bot a besoin de la **position exacte** des éléments.
- **Zoom** seulement quand c'est écrit, et **toujours en plus** de l'écran complet.
- Jeu en 1920×1080, fenêtré sans bordure, comme d'habitude.
- Tout dans `images/analyse/`, noms libres. Dis-moi juste « lot 5 déposé ».

## Captures

| # | Quoi | Format | Pourquoi |
|---|---|---|---|
| C1 | **Coordonnées contenant un 5** : marche jusqu'à ce que `N` ou `E` contienne un 5 (ex. `E 6750`), 2 ou 3 positions différentes | Écran complet | Il me manque le modèle du chiffre 5 (Q-016) |
| C2 | **De nuit** : écran au repos (HUD visible), puis un **vieux arbre survolé** (contour vert) et le panneau de cible pendant la coupe | Écran complet | Vérifier que tout reste lisible la nuit (D-025) |
| C3 | **Les 4 barres d'action** : une capture par barre (flèches haut / bas), cadenas fermé | Écran complet + zoom sur la barre | Savoir comment le bot reconnaît la barre affichée |
| C4 | **Bonus de nourriture** : une capture **avec** l'icône verte (juste après avoir mangé), une **sans** | Écran complet | Détecter le bonus (D-032) |
| C5 | **Inventaire plein**, fenêtre inventaire ouverte | Écran complet | Condition de retour à la Banque |
| C6 | **Fenêtres superposées** : inventaire + Banque (ou Compendium) ouverts et qui se chevauchent | Écran complet | Q-015 |
| C7 | **Mature Beech Tree** survolé (contour vert) et pendant la coupe | Écran complet | 2e espèce de vieil arbre (D-034) |
| C8 | Si ça arrive **sans prendre de risque** : ennemi proche sur la minicarte, attaque, écran de mort. **Ne te mets pas en danger exprès.** | Écran complet | Q-014 |

## Vidéos
Format : **MP4, 1920×1080, 30 images/s**, un sujet par vidéo, durée indiquée. Nom libre, avec un mot sur le contenu.

| # | Quoi faire, exactement | Durée | Pourquoi |
|---|---|---|---|
| V1 | **Survol à la souris, sans cliquer** : personnage immobile, caméra recentrée (W). Passe **lentement** la souris sur plusieurs arbres (proches et loin), un tronc au sol, un rocher, de l'herbe vide. Reste 1 à 2 s sur chaque objet. | 1 min | Voir quand le contour vert et l'étiquette apparaissent : base de la v0.3 |
| V2 | **Caméra** : personnage immobile. Tourne lentement à 360° avec **Q** puis **D**, puis vers le haut et le bas avec **Z** / **S**, puis **W** pour recentrer. Ne touche pas la souris. | 30 s | Comprendre ce que voit le bot quand il tourne la caméra |
| V3 | **Coupe d'un vieil arbre jusqu'à l'endurance à 0** : commence la coupe avec l'endurance vers 30/100 pour que ce soit court. Laisse tourner jusqu'à l'arrêt, puis 20 s de plus. Ne touche à rien pendant la coupe. | 2 à 3 min | Voir exactement ce qui se passe à 0 (D-028) |
| V4 | **Set destination** : ouvre la grande carte, zoome, clique une destination dans une zone découverte, ferme la carte, laisse le personnage marcher jusqu'au bout. | 1 min | Navigation (Q-002) |
| V5 | **Comme V1, mais de nuit** | 30 s | D-025 |

## Ordre conseillé
C1 et V1 en premier : ce sont eux qui débloquent la suite (lecture fiable des coordonnées et v0.3). Le reste quand tu peux.
