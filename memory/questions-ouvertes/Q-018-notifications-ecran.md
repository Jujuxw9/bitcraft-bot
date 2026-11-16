# Q-018 — Notifications à l'écran qui masquent le HUD
- **Statut** : Ouverte (remarque de Julien, 2026-11-16)
- **Priorité** : Moyenne
- **Créée le** : 2026-11-16

## Constat
Quand un métier monte de niveau (ou pour un succès), un **triangle bleu** descend du centre du haut de l'écran, avec des effets d'apparition : icône du métier, texte **« FORESTRY / LEVEL 22 »**, losange avec une flèche en bas.
- Il masque le **milieu du panneau de la cible** : la **santé de la ressource** devient illisible pendant quelques secondes. Le nom (à gauche) et la catégorie (à droite) restent visibles.
- Captures : `recolte-jeune-arbre-niveau_001` (Forestry 22), `recolte-silex_002` (Mining 13).
- Julien : certains échecs de lecture venaient sans doute de là. Ce n'est **pas** la cause du problème de la position (le 2 lu « Q ») : les coordonnées sont en haut à droite, hors du triangle.

## Autres notifications du même genre (déjà vues)
- Message au centre : « Some items couldn't fit in your inventory… » (inventaire plein).
- Bandeau en bas : « Carrying Cargo » (tronc porté).
- Bandeau « Entering / Leaving SNOWREST » (entrée et sortie de ville).
- Gains d'objets à gauche, « +13 EXP » au-dessus du personnage.

## Proposition de Claude
Une brique « **événements à l'écran** » qui :
1. **reconnaît** chaque notification (couleur bleue du triangle, texte « LEVEL » lu par OCR, ou message connu) ;
2. **la note dans le journal** (« Forestry passe niveau 22 ») ;
3. marque les valeurs masquées comme **« cachées par une notification »**, et non comme « illisibles » : pas d'alerte inutile, on garde la dernière valeur connue.

**Placement** : en **v0.3**, avec la fiabilisation de la lecture (Q-016). C'est de la lecture d'écran, comme la v0.2, et c'est utile avant que le bot agisse (v0.4).

## Ce qui bloque
Rien. Utile si possible : une **vidéo de 10 s** d'une montée de niveau (pour voir l'animation et sa durée).
