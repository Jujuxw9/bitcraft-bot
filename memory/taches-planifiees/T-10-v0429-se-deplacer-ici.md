# T-10 (06:00) — v0.4.29 : « Se déplacer ici » depuis la carte du bot

Règles communes : `nuit-2026-11-27-regles.md`. **Lis l'étude de T-09** (`memory/technique/deplacement-etude.md`) et la section T-09 du bilan : tu suis son plan.

## Pourquoi
Demande « très importante » de Julien : clic droit sur la carte du bot → « Se déplacer ici » ; le personnage y va par « Set destination » sur la grande carte du jeu (D-074), petites distances d'abord, sans escalade.

## À faire (suivre le plan de T-09 ; ce qui suit est le minimum)
1. **Menu** : clic droit sur la carte du bot → « Se déplacer ici (N …, E …) ». `ResourceMap.ToCell` donne la case. Distance limitée au départ (ex. **≤ 40 cases** ; au-delà, message « trop loin pour l'instant »).
2. **Trajet refusé** si le relief gardé montre de l'eau ou une marche trop haute sur la ligne droite (seuil d'après l'étude), avec un message clair.
3. **Geste** (contrôleur d'actions, mêmes sécurités que le repas et les capacités) : « . » ouvre la grande carte → mesure de l'échelle par la barre du bas (OCR) → souris sur la cible, relecture jusqu'à l'écart voulu → **un clic gauche** → « . » ferme la carte → vérification sur le serveur que la destination vaut la cible → suivi de la marche jusqu'à l'arrivée ou le temps maximum. Toute anomalie : fermer la carte, arrêter, journal.
4. **Sécurités de Julien** (`nuit-2026-11-27-regles.md` §3) : jamais `-`, `=`, Maj + V, Maj + B ; si la vie baisse pendant le trajet : Sprint (4) et destination à l'opposé (ou ce que recommande l'étude) ; fenêtre « Get Up / Go Home » : **Get Up seulement** (si la reconnaissance n'est pas sûre : ne rien cliquer, alerte dans le journal).
5. **Mode d'essai pour T-11** : option en ligne de commande décrite par l'étude (ex. `--essai-deplacement`) : attend l'écran « en jeu », fait la suite courte (ouvrir/fermer la carte, lire l'échelle, aller à ~5 cases est, revenir), garde des captures de l'écran du jeu à chaque étape (dans `images/analyse/`, préfixe `20261127_essai-deplacement_`), dont **la cible en haut de l'écran** (pour T-13), et s'arrête au bout de **15 min** au plus. Elle ne coupe jamais d'arbre et n'utilise jamais le chariot.
6. **Interrupteur** « Déplacements » dans OPTIONS (allumé), journal catégorie « Déplacement ».
7. Tests `V0429Tests.cs` : calcul d'échelle, conversion case ↔ point de la grande carte, refus eau/marche, conditions d'arrêt, touches interdites jamais envoyées, choix « Get Up ».

## Fini quand
- v0.4.29 publiée, tests verts, essai sans le jeu OK (le menu existe, refus « jeu fermé » propre). **Pas d'essai en jeu** : c'est T-11. Bilan : comment T-11 doit lancer le mode d'essai (ligne de commande exacte) et quoi regarder.

## Interdit
- Lancer le jeu. Escalade. Trajets longs. Marche « visée » à la souris dans le monde (D-074 : Set destination seulement pour l'instant).

## Si T-09 a raté
- Lire `2026-11-26-demande-deplacement-carte.md` et D-074, faire la version minimale ci-dessus avec les choix les plus prudents, et noter les choix dans une décision.
