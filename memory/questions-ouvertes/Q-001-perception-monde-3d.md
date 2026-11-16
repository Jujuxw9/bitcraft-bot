# Q-001 — Perception du monde 3D
- **Statut** : Ouverte
- **Priorité** : Haute
- **Créée le** : 2026-11-15
## Question
Comment détecter arbres, rochers, minerais, PNJ, bâtiments ?
## Pistes / options
- Détecteur YOLO entraîné sur captures annotées (inférence ONNX/DirectML sur RX 7800 XT).
- Segmentation couleur.
- Exploitation des info-bulles et surbrillances au survol de la souris.
- Indicateurs d'interface (prompt d'interaction).
- **Constat du 2026-11-16** ([analyse des captures](../technique/analyse-captures-2026-11-16.md)) : tout objet survolé reçoit un contour vert vif. Piste privilégiée : survol + détection du contour vert + lecture du nom, éventuellement guidé par un détecteur d'arbres. Difficulté : cycle jour/nuit.
- **Lot 2 du 2026-11-16** ([analyse des captures, lot 2](../technique/analyse-captures-2026-11-16.md)) : pendant la récolte, un panneau en haut affiche nom, catégorie et santé de la ressource (`Mature Oak Tree`, `Tier 1 - Tree`, `6746/12000`). L'OCR de ce panneau confirme la cible.
## Ce qui bloque
**Fondation de tout le projet** : à instruire avant tout code de récolte. Plus de blocage par les captures. Prochaine étape : mesurer le contour vert sur les pixels, puis prototype en v0.3.
