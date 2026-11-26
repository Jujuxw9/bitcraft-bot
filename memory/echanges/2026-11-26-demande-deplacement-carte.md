# Demande de Julien : « Se déplacer ici » depuis la carte du bot (2026-11-26, plus tard)

Demande notée pendant la session 29. **Pas encore planifiée** : à faire plus tard, petit à petit, avec les essais de Julien.

## Ce que veut Julien
- **Clic droit sur la carte du bot** → petit menu → **« Se déplacer ici »**. Le bot dit au jeu d'aller à cet endroit.
- Le bot doit trouver **le bon chemin, sans escalader** pour l'instant.
- Mieux encore : clic sur une **ressource** → « Aller à cette ressource » ou « Récolter cette ressource » ; le bot y va (et la récolte).
- Pourquoi : quand Julien coupe du bois, il y a plein d'arbres autour, mais trouver le chemin est pénible.
- Méthode : commencer par les **petites distances** ; Julien note les bugs ; on avance pas à pas.

## Liens
- D-074 : « Set destination » sur la grande carte par défaut, marche visée pour les petits trajets.
- D-072 : le bot fuit toujours les ennemis.
- Relief et eau déjà lus sur le serveur (fond de carte, `terrain_chunk_state`) : base possible pour éviter l'eau et les pentes.
- Exploration automatique (`echanges/2026-11-25-demandes-julien-session-28.md`, section 5, point 8).

## Questions à poser avant de coder
1. Petites distances : marche visée (souris, caméra) ou « Set destination » sur la grande carte du jeu ?
2. « Récolter cette ressource » : récolter jusqu'à ce qu'elle soit épuisée, ou un seul coup ?
