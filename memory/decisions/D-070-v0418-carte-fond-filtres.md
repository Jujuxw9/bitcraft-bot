# D-070 — v0.4.18 : onglet CARTE refait (fond de carte du serveur, filtres en pastilles)

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (besoin du 25/11, D-066 point 7 ; « dessiner à partir du serveur du jeu », bitjita en secours plus tard ; réalisée par la tâche planifiée T-03, D-067)
- **Liens** : D-039, D-065, D-066, D-067, [analyse-carte-bitjita-2026-11-26.md](../technique/analyse-carte-bitjita-2026-11-26.md), [architecture-suite.md](../technique/architecture-suite.md) section 22

## Contexte
Filtres de la carte en cases à cocher (familles, puis paliers en v0.4.14) jugés peu pratiques ; fond uni bleu nuit. Julien veut trouver d'un coup d'œil les ressources d'un tier donné autour de lui, sur une vraie carte comme bitjita.com/map.

## Décision
1. **Fond de carte lu sur le serveur du jeu** (tables publiques, lecture seule) : `terrain_chunk_state`, seulement les morceaux de 32 × 32 cases autour du personnage (4 à 9 morceaux, une requête par morceau sur sa clé ; jamais toute la région), et `exploration_chunks_state_v2` du personnage (zones découvertes).
2. Dessin : une case du jeu = un point ; couleur du biome (mélange des biomes de la case selon leur part), eau en bleu (plus foncé quand c'est profond), relief ombré (lumière du nord-ouest), zones non découvertes assombries ; léger voile pour garder les points lisibles. Image refaite seulement quand les morceaux reçus changent.
3. **Filtres en pastilles** : 7 catégories (Bois ; Pierre et minerai ; Plantes ; Nourriture ; Sable et argile ; Pêche ; Autres), tiers **T1 à T10 et « ? »**, chacun avec sa couleur et le nombre de ressources de la zone lue. Clic = allumer / éteindre ; **clic droit sur un tier = ce tier seul** (encore une fois = tous). Boutons « Tout », « Bois seul », « Tous » (tiers). Filtres **retenus** dans `config.local.json`.
4. Refus du serveur : le fond de carte est abandonné **en premier** dans la chaîne de repli (la carte garde son fond uni).
5. Zone lue des ressources **gardée à 20 cases** (pas de passage à 25 dans cette version, voir ci-dessous).
6. Pas de données de bitjita.com.

## Mesures (essai réel du 26/11, 13:24 à 13:29, jeu fermé)
- 6 morceaux de terrain reçus, ≈ 119 Ko en texte ; abonnement complet 1 003 Ko (pas de mesure sans terrain pour comparer). Zones découvertes lisibles : 68 morceaux découverts (monde de 400 morceaux de large).
- Bot d'essai, onglet CARTE affiché : 17 % d'un cœur, 512 Mo de mémoire (1 894 ressources dans la zone).

## Choix prudents de la tâche (sans Julien)
- Toutes les catégories et tous les tiers affichés au premier démarrage (avant : arbres seuls).
- Rayon des ressources pas augmenté : pas de mesure de référence sans le terrain pour comparer la charge ; à faire avec Julien.

## Alternatives écartées
- Fichiers de bitjita.com (service d'un tiers, conditions à lire) : secours plus tard.
- Toute la région d'un coup (80 × 80 morceaux) : trop lourd.
