# Q-002 — Cartographie et navigation
- **Statut** : Ouverte
- **Priorité** : Haute
- **Créée le** : 2026-11-15
## Question
La position absolue est lisible par OCR (D-018). Comment se déplacer et cartographier ?
## Pistes / options
- Relation coordonnées HUD ↔ grille hexagonale du serveur.
- Orientation de la caméra.
- Déplacement vers une cible (clic au sol, touches).
- Mémorisation des ressources et obstacles rencontrés (carte construite par le bot).
- Points de passage enregistrés par Julien en complément.
- Infos de Julien (2026-11-16) : la Banque peut être à 15 s comme à 2 min de marche. Le bot doit savoir **trouver** la Banque (carte ou vue), pas seulement y retourner.
- Touches et mode de déplacement : voir Q-011 et D-024.
- Constat du 2026-11-16 ([analyse des captures](../technique/analyse-captures-2026-11-16.md)) : option « Click to move on minimap » activée ; Banque à Snowrest vers `N 8937, E 6719`.
- **Lot 4 (2026-11-16)** : la grande carte affiche les coordonnées sous la souris (`Keldan (R18) | (8918, 6700)`) et propose « Set destination ». Piste principale pour aller à la Banque. Voir analyse, section 19.
## Ce qui bloque
Captures du HUD et de la carte nécessaires.
