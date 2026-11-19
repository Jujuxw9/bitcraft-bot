# Session 15 — 2026-11-19 — v0.3.20 : stabilité, écran = cible, plan de la récolte automatique

**Agent** : Claude Code

## Réalisé
- Journal de l'essai v0.3.19 analysé : journal des tâches juste (pêche, coupe d'arbres : coups, XP, objets ; passage Fishing 20).
- **Image du jeu et images des recettes réparées** : cause trouvée sur un banc hors écran (décodage des icônes en parallèle qui bloquait les fils du programme). Une icône à la fois ; capture du jeu sur son propre fil.
- **Écran = cible seulement** (D-048) : position, zone et région viennent du serveur.
- Journal des tâches : ressource et métier seulement pour une récolte ; noms français ajoutés.
- **Plan de bataille de la récolte automatique** : `echanges/2026-11-19-plan-recolte-auto.md` (R1 à R7). Questions : Q-023.
- v0.3.20 : 263 tests, banc hors écran (sessions, onglets, recettes, fermeture). .exe fabriqué en local, sans Release GitHub.

## Écarts notés
- Le défaut des icônes existait depuis la v0.3.15 ; la v0.3.19 (capture sur les fils partagés + plus de lectures) l'a rendu visible sur l'image du jeu.
- Pas d'essai réel : Julien était en jeu (connexion au serveur impossible sans éjecter le jeu, fenêtre du bot non ouverte).
- Quelques objets n'ont pas d'image sur le CDN (ex. Wooden Armguards) : case vide, normal.

## Décisions prises
- D-048 (écran = cible seulement), D-049 (choix de la récolte automatique).

## Questions ouvertes créées
- Q-023 (récolte automatique), résolue dans la même session → D-049.

## Fin de session
- Réponses de Julien à Q-023 → D-049 : arbres d'abord ; vieux arbres (« le plus simple ») ; W remet caméra et zoom, Q / D sensibles ; écoute de ses clics d'accord, mais « source d'idée, pas vérité absolue ».
- Julien ne teste pas la v0.3.20.

## Prochaines étapes
- Nouveau chat avec `echanges/2026-11-19-passation-recolte-auto.md` : R1 (apprendre la caméra), puis R2.
