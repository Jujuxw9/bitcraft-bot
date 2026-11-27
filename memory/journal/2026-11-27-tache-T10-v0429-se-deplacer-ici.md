# Tâche T-10 (nuit du 27/11, 06:00) — v0.4.29 : « Se déplacer ici »

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Décision** : D-086 ; architecture : `technique/architecture-suite.md` section 33 ; étude suivie : `technique/deplacement-etude.md` (T-09)

## Fait
- Clic droit sur la carte du bot → menu « Se déplacer ici (N, E) », grisé avec la raison si refusé (30 cases au plus, relief inconnu, escalade ≥ 5, eau profonde ≥ 5, sur trois lignes parallèles).
- Geste complet : ouvrir la grande carte, lire « Set destination », mesurer l'échelle (3 lectures de la barre), viser (≤ 1 case, 3 corrections), un clic gauche, refermer et vérifier ; suivi sur le serveur (départ, arrivée, blocage, temps maximum, serveur muet).
- Sécurités : touches `-`, `=`, V, B interdites quoi qu'il arrive (et retirées des capacités) ; vie qui baisse = Sprint (4) + fuite à 10 cases puis arrêt ; vie à 0 = clic sur « Get Up » seulement si « Go Home » est lu ailleurs, sinon rien.
- Interrupteur « Déplacements » (OPTIONS), modes d'essai `--essai-deplacement` (pour T-11) et `--essai-menu-deplacement`.

## Testé
- Build 0 avertissement, tests 701/701 (34 nouveaux dans `V0429Tests`).
- Essai sans le jeu à 06:08 (`--sans-lancer-jeu --essai-menu-deplacement`) : menu à 5 E et 5 N « permis » puis « refusé : jeu fermé » ; à 40 E grisé « trop loin » ; aucun « Abonnement refusé », aucune erreur (la ligne « Erreur de liaison idc-global » existait déjà en v0.4.28).

## Pas fait
- Aucun essai en jeu (interdit : T-11).
- `threat_state` et ennemis de la zone pas lus (risque d'abonnement refusé) : danger vu seulement par la vie qui baisse.
