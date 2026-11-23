# Q-025 — Visée fiable : survol, touche d'interaction, cône de la mini-carte, écrans d'accueil et de connexion

- **Statut** : Résolue → D-052, D-056 (2026-11-23) ; à essayer : la touche Interact (M)
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

## Suite (2026-11-23)
- **Point 4 fait** : Julien a fait Num 0 sur l'accueil (« PRESS ANY BUTTON TO START ») et la connexion (« WELCOME », « ENTER BITCRAFT », « LOG OUT », annonces). Les mots-clés sont corrigés en v0.4.3 (captures de référence dans `images/reference-ui/ecrans/`, e-mail masqué). L'écran de connexion **sans compte connu** (champ e-mail) n'a pas été vu : ses mots restent supposés.
- **Point 5** : la capture du 22/11 à 23:46:00 montre une vue **bien plus haute** (personnage petit, prairie entière), prise juste après un coup de molette (journal 23:45:57). La molette éloigne donc la caméra ; mais W remet le zoom (D-050). Reste à savoir s'il existe un réglage pour une vue haute par défaut (À vérifier).

## Captures du menu (2026-11-23, 22:35) → D-056
- **Point 2** : aucune touche « Interact » dans les raccourcis du jeu (case vide). Pas de récolte sans viser au clavier. A = « Target », Tab = cible suivante (pistes, À vérifier).
- **Point 5** : aucun réglage de hauteur ni de distance de la caméra dans le menu. Julien est d'accord pour la molette : quand le bot fera W lui-même, il donnera ensuite toujours le même nombre de crans (D-056, nombre À vérifier).
- Reste : point 1 (le panneau du haut change-t-il au survol ?).

## Réponses de Julien (2026-11-23, 23:00) : question résolue
- **Point 1** : le panneau du haut (nom, santé) change **seulement après le clic**, pas au survol. Avant de cliquer, seul le contour vert montre la cible.
- **Point 2, suite** : Julien a donné la touche **M** à « Interact » (case vide avant). À essayer : que fait Interact (ressource la plus proche ? cible actuelle ?) — **À vérifier**. M n'est pas encore dans les touches autorisées du bot (D-045).
