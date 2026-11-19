# D-043 — Onglets regroupés par usage

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien (« repenser les onglets pour que ce soit plus simple… regrouper par fonction et utilité » ; « supprime onglet test » ; « supprime onglet journal, mets-le dans les paramètres sur la moitié droite »)
- **Liens** : D-005, D-035 (banc d'essai terminé), D-044

## Décision
Cinq onglets (v0.3.16), au lieu de sept :

| Onglet | Usage | Contenu |
|---|---|---|
| **Bot** | Piloter | Démarrer / arrêter, image du jeu et zones lues, repas automatique, sécurité, dernier événement |
| **Personnage** | État du personnage (serveur) | En direct, bonus actifs, sac et banques |
| **Carte** | Autour du personnage (serveur) | Carte et tableau des ressources (H2) |
| **Recettes** | Consulter | Recherche d'objets, images, comment les obtenir |
| **Paramètres** | Régler et surveiller | À gauche : compte, connexion, overlay, système (CPU / RAM, seuils). À droite : **journal** |

- Onglet « Tests » supprimé : les mouvements seront essayés directement en H3.
- Règle pour la suite : **une nouvelle fonction va dans un onglet existant** selon son usage. Un nouvel onglet seulement si aucun ne convient, après accord de Julien.
