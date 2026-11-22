# Q-025 — Visée fiable : survol, touche d'interaction, cône de la mini-carte, écrans d'accueil et de connexion

- **Statut** : En partie résolue → D-052 (2026-11-22) ; restent : captures de l'accueil et de la connexion (point 4), vue plus haute (point 5)
- **Priorité** : Haute
- **Créée le** : 2026-11-22
- **Liens** : D-050, D-049, D-024, analyse `technique/hybride/analyse-visee-2026-11-22.md`

## Questions pour Julien
1. **Survol** : quand la souris passe sur un arbre **sans cliquer**, le jeu l'entoure en vert (vu sur la capture du 22/11 à 21:02:54). Le panneau du haut (nom, santé) change-t-il aussi au survol, ou seulement après le clic ?
2. **Touche d'interaction** : existe-t-il une touche qui récolte la ressource la plus proche, sans viser à la souris ?
3. **Cône de la mini-carte** : le cône clair autour de ton personnage montre-t-il bien où regarde la caméra ? Après W, la caméra se met-elle derrière le personnage ?
4. **Écrans d'accueil et de connexion** : faire Num 0 sur chacun au prochain lancement (la v0.4.1 garde aussi une capture d'elle-même). Les mots-clés actuels sont supposés.
5. **Zoom** : W met une vue très rapprochée (l'arbre remplit l'écran). Existe-t-il un réglage pour une vue plus haute par défaut ?

## Pourquoi
Une visée fiable a besoin de connaître la direction de la caméra (Q3), et de vérifier la cible avant de cliquer (Q1, Q2).
