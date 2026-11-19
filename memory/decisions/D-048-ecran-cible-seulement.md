# D-048 — L'écran ne lit plus que la cible sélectionnée

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien (« est-ce que y'a encore besoin d'analyser ces infos à l'écran ? on les récupère déjà avec le serveur non ? »), réponse de Claude : oui, appliqué en v0.3.20
- **Liens** : D-044 (serveur d'abord), D-018 (localisation par OCR du HUD, devenue inutile), Q-016

## Contexte
En v0.3.19, l'écran lisait encore la position (N, E), le nom de la zone et la région. Le serveur donne la position exacte (à 0,3 case près, vérifié des dizaines de fois) et le nom de la région.
Essai v0.3.19 : la position lue à l'écran a décroché (lue 50 % du temps, écart de 3 cases), alors que le serveur était juste.

## Décision
- L'écran ne lit plus que la **cible sélectionnée** (nom, catégorie, santé) : le serveur ne la connaît qu'au premier coup.
- Position, zone, région : **serveur seulement**. Plus de comparaison écran / serveur, plus d'image de diagnostic « position illisible ».
- Sans panneau de cible à l'écran, aucune lecture de texte n'a lieu (le coût tombe presque à zéro).
- L'image du jeu reste capturée : pour voir ce que fait le bot, pour la cible, et pour la récolte automatique (viser à la souris).

## Alternatives écartées
- Garder la position à l'écran comme contrôle : elle a servi à valider le serveur (H2) ; c'est fait, et elle crée de fausses alertes.

## Conséquences
- Les lecteurs de position (modèles de chiffres, filtre) restent dans le code, testés mais non appelés.
- D-018 n'est plus appliquée.
