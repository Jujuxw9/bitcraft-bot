# D-078 — v0.4.22 : carte à 30 cases avec zoom et mémoire, vraie progression des fabrications, ceinture en vignettes, RAM

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (retours sur la v0.4.21, 26/11 au soir)
- **Liens** : D-071, D-073, D-075, [architecture-suite.md](../technique/architecture-suite.md) section 26

## Contexte
v0.4.21 : hexagones « nickel », chat en direct OK, fond de carte à 25 cases OK. Demandes : cercle à 30, un seul cercle, zoom, carte gardée pour ne pas tout redemander ; barre de fabrication qui monte à 12 % puis retombe ; ceinture en images plutôt qu'en tableau ; alertes RAM.

## Décision
1. **Carte** : zone lue de 30 cases (90 petites cases ; fond sur 34 cases), un seul cercle (la limite de la zone lue), boutons « − » / « + » (vue de 15 à 62 cases).
2. **Mémoire de la carte** : chaque morceau de terrain reçu est gardé sur le disque (dossier `carte` à côté du .exe, environ 12 Ko par morceau, ignoré par Git) et réaffiché tout de suite ; les ressources déjà vues restent sur la carte, en pâle, hors de la zone lue ; dans la zone lue, le serveur fait foi (un arbre coupé disparaît). Les demandes au serveur pour la zone autour du personnage continuent (mises à jour en direct).
3. **Fabrication** : progression lue dans `progressive_action_state` (gestes faits ÷ gestes par objet × nombre d'objets), plus la durée d'un seul geste.
4. **Ceinture d'outils** : vignettes (image, tier en bas à droite, nom dessous, quantité au survol), plus de tableau.
5. **RAM** : seuils 1000 Mo (alerte) / 1600 Mo (arrêt), au minimum ; une alerte seulement après 3 mesures de suite au-dessus ; mémoire rendue après chaque gros envoi du serveur (au plus une fois par minute).

## Conséquences
- Tests 632/632. Pas d'essai serveur (jeu ouvert) : zone de 30 cases et progression des fabrications « À vérifier » par Julien.
- Pas encore fait : demander au serveur seulement les morceaux nouveaux quand Julien bouge (abonnements par morceau). C'est la vraie économie d'échanges ; à essayer avec Julien (protocole à vérifier).
