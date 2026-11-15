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
## Ce qui bloque
**Fondation de tout le projet** : à instruire avant tout code de récolte. Besoin de captures du jeu 1.0.
