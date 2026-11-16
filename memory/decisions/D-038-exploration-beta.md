# D-038 — Onglet « Bêta : exploration » : règles et réglages

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien (réponses à Q-020)
- **Liens** : Q-020, D-035, D-037, D-024, D-029, Q-014

## Décision
Onglet **« Bêta : exploration »**, **désactivé à chaque démarrage**. Le bot combine déplacements du personnage et mouvements de caméra pour explorer, avec les règles de Q-020 :
1. **Jamais de clic sur un contour vert** : survol du point visé avant chaque clic ; contour vert → point refusé.
2. **Herbe seulement**. Éviter buissons, fleurs, arbres, rochers, veines et **surtout l'eau**.
3. Repères : **minicarte** et **image actuelle**. **W** pour recentrer la caméra quand il le faut.
4. Mouvements avec un but et du bruit (D-037). Mémoire des positions déjà visitées pour explorer.

## Réglages (réponses de Julien)
| Réglage | Valeur par défaut | Remarque |
|---|---|---|
| **Rayon maximal** autour du point de départ | **100** (unités des coordonnées du jeu, sans unité affichée) | Réglable dans l'onglet |
| **Durée** d'une exploration | **5 min** | Réglable |
| **Ennemi proche** (triangle rouge sur la minicarte) | **Arrêt du bot + alerte** | Le plus sûr |

Sécurités habituelles en plus : Num 5, souris bougée à la main, jeu au premier plan, vie qui baisse → arrêt.

## Conséquences
- Il faut apprendre au bot l'**herbe** et l'**eau** : captures ou vidéo demandées à Julien (lot 6).
- Prochaine version de code après la compaction de la session 08.
