# D-086 — v0.4.29 : « Se déplacer ici » depuis la carte du bot, avec sécurités

- **Statut** : Validée (tâche de nuit T-10, d'après la demande de Julien du 26/11 et ses règles du 27/11)
- **Date** : 2026-11-27
- **Validée par** : Julien (demande « très importante » : `echanges/2026-11-26-demande-deplacement-carte.md` ; règles de sécurité : `taches-planifiees/nuit-2026-11-27-regles.md` §3 ; fiche `taches-planifiees/T-10-v0429-se-deplacer-ici.md`, D-081)
- **Liens** : D-074 (Set destination seulement), D-072 (toujours fuir), D-052 (sécurités des actions), étude [deplacement-etude.md](../technique/deplacement-etude.md), [architecture-suite.md](../technique/architecture-suite.md) section 33

## Décision
1. **Menu** : clic droit sur la carte du bot → « Se déplacer ici (N …, E …) ». Grisé avec la raison si refusé : trop loin (**30 cases au plus**), relief inconnu, **escalade** (écart de hauteur ≥ 5 entre deux cases voisines), **eau profonde** (≥ 5, nage), sur la ligne droite **et** sur les deux lignes parallèles à ± 1 case. Eau peu profonde (1 à 4) permise, notée.
2. **Geste** (tout par le contrôleur d'actions) : touche de la carte → « Set destination » lu dans la barre du bas, sinon arrêt → **mesure de l'échelle** (souris au centre, 200 px à droite, 200 px en bas ; trois lectures de la barre ; ordre (N, E) ou (E, N) trouvé tout seul ; centre à moins de 3 cases de la position du serveur ; échelle entre 0,01 et 0,5 case par pixel, sinon « zoom à régler à la main », **jamais de molette**) → souris sur la case, 3 corrections au plus, écart ≤ 1 case → **un clic gauche** → même touche pour fermer, carte vérifiée fermée (sinon tout est bloqué, alerte).
3. **Suivi** (serveur, toutes les 500 ms) : départ dans les 3 s (sinon un seul nouvel essai) ; arrivée = ≤ 1,5 case et immobile 1 s ; bloqué = moins de 0,5 case gagnée en 5 s ; temps maximum 10 s + 2 s par case ; serveur muet 5 s = arrêt. Nombre de destinations différentes vues et vitesse écrits au journal (pour T-11/T-12).
4. **Danger** (vie qui baisse) : **Sprint (touche 4)** puis fuite à 10 cases à l'opposé de la cible (même geste, même contrôle du relief), puis **arrêt** (pas de reprise automatique en v1).
5. **À terre** (vie 0) : OCR du centre de l'écran ; clic **seulement** sur « Get Up », et seulement si « Go Home » est lu ailleurs (> 100 px) ; sinon rien, alerte. Après le clic : position qui saute de 3 cases ou plus = alerte « Go Home par erreur ? » et tout bloqué.
6. **Touches interdites quoi qu'il arrive** (`GameKeys.Forbidden`, vérifiées avant la liste permise) : `-` (chariot), `=` (bateau), V et B (donc Maj + V, Maj + B). Retirées de la liste permise ; les capacités automatiques sautent ces cases.
7. **Touche de la carte** : 0x34 (« . » QWERTY, « : » en AZERTY) par défaut, 0x53 (« . » du pavé numérique) possible ; réglage `MapKeyScanCode`, fixé par le mode d'essai.
8. **Interrupteur « Déplacements »** dans OPTIONS, allumé par défaut (chaque trajet est demandé par un clic de Julien). Journal : catégorie « Déplacement ».
9. **Modes d'essai** : `--essai-deplacement` (T-11, avec le jeu : touche de la carte, échelle, aller à 5 cases et retour, captures dans `images/analyse/20261127_essai-deplacement_*`, 15 min au plus) ; `--essai-menu-deplacement` (sans le jeu : menu à 5 E, 5 N et 40 E, refus attendus).

## Pas fait (reporté)
- Abonnement `threat_state` et positions des ennemis (étude §4) : pas ajoutés, pour ne pas risquer un « Abonnement refusé » sans essai ; le danger se voit seulement à la vie qui baisse.
- Arrêt si une fenêtre du jeu autre que la carte s'ouvre pendant la marche : seulement via le contrôleur (au prochain geste).

## Conséquences
- Tests 701/701 (34 nouveaux, `V0429Tests`). Essai sans le jeu propre (06:08).
- **Rien n'a été essayé en jeu** : T-11.
