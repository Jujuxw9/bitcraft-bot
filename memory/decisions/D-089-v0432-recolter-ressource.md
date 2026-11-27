# D-089 — v0.4.32 : « Aller à cette ressource » et « Récolter jusqu'à épuisement »

- **Statut** : Validée (tâche de nuit T-14, choix prudents ; à revoir avec Julien)
- **Date** : 2026-11-27
- **Validée par** : tâche T-14 d'après sa fiche (`taches-planifiees/T-14-v0432-recolter-ressource.md`) et les règles de la nuit (D-081)
- **Liens** : D-082 (reprise à la souris), D-086 et D-087 (« Se déplacer ici »), D-072 (toujours fuir), [architecture-suite.md](../technique/architecture-suite.md) section 36

## Contexte
Demande de Julien (26/11) : sur la carte du bot, clic sur une ressource → le bot y va et la récolte jusqu'à épuisement.

## Décision
1. **Menu** sur une ressource : clic droit sur la carte (ressource à 1,5 case au plus du clic, parmi celles affichées par les filtres), clic droit sur une ligne du tableau, bouton « Y aller… » à côté de « Plus proche ». Deux entrées : « Aller à cette ressource » et « Récolter jusqu'à épuisement ». Grisées avec la raison si refusé.
2. **Aller** : « Se déplacer ici » vers une **case voisine à 2 cases** de la ressource (jamais sur elle), la plus proche du personnage d'abord ; si elle est refusée (escalade, eau, relief inconnu, distance), les 7 autres sont essayées. Déjà à moins de 2,5 cases : pas de trajet.
3. **Récolter : le premier coup reste à Julien.** Après le trajet, la caméra a suivi le personnage : le point de la ressource à l'écran est inconnu. Aucune méthode (W puis recherche du contour vert, cible du serveur) n'a pu être essayée en jeu (T-11 non abouti) : trop risqué à l'aveugle. À l'arrivée, le bot écrit « cliquez une fois sur la ressource : le bot continuera jusqu'à épuisement ». La reprise des récoltes (D-082) prend le relais à chaque baisse d'endurance.
4. **Suivi** (journal et état, aucun geste) : premier coup vu sur le serveur ; « épuisée » quand la ressource n'est plus lue alors que le personnage est à moins de 10 cases ; abandon si Julien récolte une autre ressource, s'éloigne de plus de 6 cases, ou ne frappe pas en 3 min.
5. **Vie qui baisse** pendant le suivi : Sprint (4) puis fuite 10 cases à l'opposé de la ressource, puis arrêt (D-072). Pendant le trajet : sécurités de « Se déplacer ici ».
6. Reprise des récoltes coupée : le bot le dit à l'arrivée, il ne l'allume pas lui-même.

## Conséquences
- Tests 742/742 (12 nouveaux, `V0432Tests`).
- À vérifier en jeu : portée de récolte (2 cases suffisent-elles ?), trajet réel, fin « épuisée ».
- Plus tard (après un essai réel) : premier coup automatique (W, contour vert, confirmation `target_state`).
