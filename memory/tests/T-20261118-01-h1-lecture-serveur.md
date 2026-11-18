# T-20261118-01 — H1 : lecture du serveur (essais de Claude, sans jeton)

- **Type** : unitaire + intégration
- **Version testée** : v0.3.5 (H1)
- **Durée** : 1 session

## Objectif
Vérifier la brique Server et l'onglet « Serveur (H1) » avant l'essai réel de Julien.

## Conditions
PC de Julien, jeu fermé, aucun jeton (Claude n'a pas accès au compte).

## Procédure et résultats
| Essai | Résultat |
|---|---|
| 167 tests automatiques (dont 21 pour H1 : schéma, lignes, messages, ressources proches, jeton chiffré, requêtes en lecture seule) | OK |
| Garde-fou : aucune fonction d'appel de « reducer » dans la brique | OK |
| Lecture du **vrai** schéma de la région 18 (542 tables) : toutes les tables et colonnes utilisées existent et sont publiques | OK |
| Liaison réelle avec `bitcraft-live-18` et un **faux** jeton : refus 401 du serveur | OK (adresse et liaison correctes) |
| Exe v0.3.5 lancé : onglet visible, « Se connecter » sans jeton → message clair, e-mail vide → message clair, fermeture propre | OK |

## Non testé (besoin de Julien)
- Code par e-mail et jeton réel.
- Lecture du personnage, des arbres, de la vie, etc.
- Ordre de lancement : bot puis jeu ; fermeture du bot sans déconnecter le personnage.
- Correspondance des coordonnées serveur / écran.
