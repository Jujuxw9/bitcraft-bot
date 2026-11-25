# T-03 — v0.4.18 : onglet CARTE refait (tâche planifiée du 26/11, 13:15)

Lire d'abord [README.md](README.md) (règles communes) et les suivre du début à la fin. Lire aussi `memory/technique/analyse-carte-bitjita-2026-11-26.md`.

## Le besoin (Julien, 26/11, demandes section 5 point 2, D-066 point 7)
1. **Filtres beaucoup plus simples et « stylés »** : plus de cases à cocher partout. Des **boutons de rang (tier)** T1 à T10 qu'on allume ou éteint d'un clic (pastilles colorées, avec le nombre), et des **catégories refaites** (familles de ressources regroupées clairement, avec une icône ou une couleur, un clic par catégorie). Très « user-friendly ». But de Julien : trouver les ressources d'un tier donné autour de lui.
2. **Meilleur visuel de la carte** : un vrai fond de carte (relief ombré, eau, biomes), des zones, comme sur bitjita.com/map.
3. **Voir plus loin** : zone lue passée à 20 cases en v0.4.15 ; on augmente petit à petit en surveillant la charge du PC (pas plus de 25 cases dans cette version, et seulement si le journal montre une charge correcte).

## Décision de Julien
Dessiner **à partir du serveur du jeu** (tables publiques, lecture seule). bitjita.com seulement en **secours**, plus tard : ne pas l'utiliser dans cette version.

## Ce qui existe déjà
- Carte et filtres : `src/BitcraftBot.App/MainWindow.Map.cs`, `ResourceMap.cs` (dessin), `FamilyColors.cs`, partie CARTE de `MainWindow.xaml`. Filtre par palier en cases (v0.4.14) à remplacer.
- Lecture des ressources autour : `Server/ServerReader.cs` (`ResourceRadius = 60` petites cases ≈ 20 cases), `Server/World/Queries.cs`.
- Tables utiles (copie du code officiel dans `references/BitCraftPublic`) : `terrain_chunk_state` (morceaux de 32 × 32 cases : `biomes`, `elevations`, `water_levels`, `water_body_types`… ; `chunk_index = (dimension-1)*1000000 + chunk_z*1000 + chunk_x + 1`), `exploration_chunks_state` (zones découvertes, bitmap ; dans quel module : à vérifier).

## À faire
1. **Vérifier d'abord le format réel** : s'abonner aux morceaux de terrain autour du personnage (quelques morceaux seulement, jamais toute la région), écrire un exemple brut dans le journal, mesurer le poids. Si la table est refusée ou trop lourde : garder le fond actuel, faire quand même les nouveaux filtres, et l'expliquer dans `etat.md`.
2. Fond de carte dessiné une fois par morceau (image gardée en mémoire) : couleur par biome, ombre selon la hauteur, eau en bleu. Zones non découvertes assombries si `exploration_chunks_state` est lisible.
3. Nouveaux filtres : pastilles de tier cliquables, catégories en boutons, bouton « Tout » ; filtres retenus dans les réglages.
4. Garder ce qui marche : position du joueur, meilleur arbre, cible à l'écran, tableau des ressources, clic sur la carte.
5. Tests automatiques : conversion terrain → couleurs, calcul des morceaux autour du joueur, filtres.
6. Essai réel (jeu fermé, `--sans-lancer-jeu`) : capture de l'onglet CARTE (fenêtre du bot), charge du PC notée (CPU, RAM, octets reçus).

## Version
v0.4.18 (ou la suivante libre). Décision suivante libre.
