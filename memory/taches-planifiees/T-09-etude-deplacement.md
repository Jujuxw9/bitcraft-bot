# T-09 (05:00) — Déplacement automatique : l'étude (aucun code)

Règles communes : `nuit-2026-11-27-regles.md`. Lis le bilan (T-05 à T-08).

## Pourquoi
Julien veut que le bot déplace le personnage : d'abord « Se déplacer ici » depuis la carte du bot, puis aller à une ressource et la récolter, plus tard l'exploration et la Banque. Il dit lui-même : « il y a pas mal de questions à se poser, de choix à faire, de comment gérer l'environnement ». **Ta tâche : réfléchir et écrire, pas coder.** T-10 (06:00) code la première version à partir de ton étude.

## Ce que l'on sait déjà (à lire)
- `memory/echanges/2026-11-26-demande-deplacement-carte.md` (demande, réponses de Julien : grande carte « . » centrée sur le personnage avec le dernier zoom, **un clic gauche = le personnage y va**, fermeture par « . » (sûr) ou Échap (pas sûr), marche qui continue carte fermée ; M = clic sur l'élément survolé).
- D-074 (« Set destination » par défaut), D-072 (toujours fuir), règles de sécurité de Julien (`nuit-2026-11-27-regles.md` §3 : jamais de chariot ni de bateau, Sprint 4 et s'éloigner, Get Up jamais Go Home, pas d'escalade).
- `memory/technique/analyse-captures-2026-11-16.md` §19 (grande carte : barre du bas « Keldan (R18) | (8918, 6700) » = coordonnées sous la souris ; zoom molette, déplacement en glissant) ; captures `images/analyse/*carte*`.
- Serveur : `mobile_entity_state` donne position **et destination** du personnage (déjà lues : `PlayerSnapshot.Destination`, `Walking`) ; `terrain_chunk_state` donne hauteur du sol et de l'eau par grande case ; `health_state` ; lecture de l'écran : `BitcraftBot.Perception` (OCR, écrans reconnus, fenêtres du jeu ouvertes dont « carte du monde »).
- Code serveur officiel : `references/BitCraftPublic/` (règles de marche, d'escalade, hauteur franchissable : chercher `climb`, `elevation`, `pathfinding`, `player_move`).

## Questions à traiter (une section chacune, avec recommandation)
1. **Placer le clic sur la grande carte** : échelle inconnue (dernier zoom gardé). Méthode proposée : ouvrir, souris au centre, lire la barre du bas (OCR), déplacer de N points, relire → échelle ; viser ; relire jusqu'à moins d'une case d'écart ; cliquer. Précision attendue, nombre de lectures, que faire si l'OCR échoue.
2. **Vérifier** : la destination lue sur le serveur doit valoir la cible (à combien près ?) ; puis suivre la marche ; arrivée = distance < X ; temps maximum selon la distance.
3. **Terrain** : à partir du relief gardé (TerrainStore), comment savoir si le trajet en ligne droite (ou le chemin du jeu) passe par de l'eau ou une marche trop haute (escalade) ? Le jeu calcule-t-il lui-même un chemin (Set destination contourne-t-il les obstacles) ? Ce que dit le code serveur. Recommandation pour la v1 : refuser les trajets douteux plutôt que risquer.
4. **Ennemis** : ce que le serveur publie sur les ennemis proches (tables d'entités ennemies, menace `threat_state`, combat), comment les détecter dans la zone lue ; réaction : Sprint (4) puis destination à l'opposé ; quand reprendre.
5. **À terre** : reconnaître la fenêtre « Get Up / Go Home » (OCR, position), cliquer **seulement** Get Up ; vérifier sur le serveur (vie, état). Ce que le serveur montre quand le personnage est à terre (chercher dans le code serveur).
6. **Sécurité** : toutes les conditions d'arrêt (Julien bouge la souris ou tape, fenêtre du jeu ouverte, chat, écran inconnu…), le contrôleur d'actions existant (`BitcraftBot.Input`), touches interdites.
7. **Mode d'essai pour T-11** : une option en ligne de commande (ex. `--essai-deplacement`) qui fait toute seule une suite courte et sûre (ouvrir et fermer la carte, lire l'échelle, aller à 5 cases puis revenir), écrit tout dans le journal, garde des captures (dont la cible en haut de l'écran pour T-13), et rend la main. Décrire précisément ce qu'elle doit faire et ne pas faire.
8. **Plus tard** (juste lister) : aller à une ressource et la récolter (T-14), chemins longs, Banque (palier 0.5), exploration.

## Livrables
- `memory/technique/deplacement-etude.md` (≤ 300 lignes) : les 8 sections, chacune « ce qu'on sait / ce qui est À vérifier / recommandation ». Fin du fichier : **plan précis pour T-10** (classes, étapes, tests) et **questions pour Julien**.
- Une ligne dans `memory/technique/README.md` (index), journal de tâche, bilan.

## Fini quand
- L'étude existe, T-10 peut coder sans réfléchir de nouveau aux choix de base. Mémoire à 0 erreur.

## Interdit
- Aucun code, aucune version. Aucun essai en jeu.
