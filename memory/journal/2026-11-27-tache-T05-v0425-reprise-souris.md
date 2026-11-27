# Tâche T-05 — v0.4.25 : reprise des récoltes à la souris, overlay étroit

- **Date** : 2026-11-27, 01:00 à 01:10
- **Agent** : Claude Code (Opus 5.5, tâche planifiée de nuit)

## Fait
- Reprise des récoltes : Tab retiré ; souris posée sur le point du dernier clic de Julien, puis M (D-082). Point refusé si le personnage a bougé ou si la caméra a tourné : rien n'est appuyé, message « cliquez une fois sur l'arbre ».
- Compteur d'échecs remis à zéro (défaut de 00:29).
- Contrôleur d'actions : nouveau geste « souris seule » (`MoveAsync`).
- Overlay plus étroit (ligne système courte, police et marges réduites).
- Tests `V0425Tests` (10) ; `V0424Tests` et `V0423Tests` adaptés. 649/649.
- v0.4.25 publiée, essai sans le jeu propre.

## Pas fait
- Mesure du contour vert : seulement notée dans le journal, pas vérifiée sur une vraie image (jeu fermé).
- Aucun essai en jeu (interdit pour T-05).
