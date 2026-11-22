# D-052 — Visée vérifiée et détection des fenêtres du jeu avant toute action

- **Statut** : Validée
- **Date** : 2026-11-22
- **Validée par** : Julien (réponses à Q-025 et « Détecter les fenêtres du jeu ouvertes avant toute action : OUI »)
- **Liens** : Q-025, Q-015, D-050, D-049, D-024 ; analyse `technique/hybride/analyse-visee-2026-11-22.md`

## Réponses de Julien (Q-025)
1. Le panneau du haut (nom, santé) ne change **pas** au survol : NON.
2. Pas de touche qui récolte la ressource la plus proche : NON.
3. Le cône clair de la mini-carte montre bien la direction de la caméra, et W met la caméra derrière le personnage : OUI. La mini-carte est **toujours** orientée nord (réglage du jeu, « LockMinimap »).
4. Captures Num 0 de l'accueil et de la connexion : OK (au prochain lancement).
5. Vue plus haute que celle de W : ne sait pas.

## Décision
- **Direction de la caméra** : lue sur la mini-carte (cône), à chaque visée. Sans elle, pas de visée.
- **Vérification avant le clic** : le contour vert du jeu autour de l'objet survolé (le panneau du haut ne sert pas). **Après le clic** : le serveur confirme l'arbre frappé (identité exacte).
- Pas de touche d'interaction : la visée se fait à la souris (clic droit, D-024).
- **Fenêtres du jeu** (inventaire, carte, banque…) : détectées à l'écran **avant toute action** ; si une fenêtre est ouverte, le bot n'agit pas (fermeture par Échap à étudier). Répond à Q-015 pour la récolte.

## Conséquences
- Prochaines versions (aucune action) : lecture du cône de la mini-carte, détection des fenêtres ouvertes, puis réapprentissage de la formule avec la direction.
