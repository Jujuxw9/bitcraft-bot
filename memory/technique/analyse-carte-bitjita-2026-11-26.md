# Analyse de la carte de bitjita.com (2026-11-26, session 28)

Demande de Julien : analyser (sans rétro-ingénierie) comment marche la carte de https://bitjita.com/map, pour savoir si le bot peut faire aussi bien (v0.4.18). Méthode : page ouverte dans le navigateur intégré, liste des requêtes réseau, lecture des fichiers de données publics.

## Ce que charge la page
- **Code** : application SvelteKit (`/_app/immutable/…`, ~126 fichiers JS).
- **Données de la carte** (`mapdata.bitjita.com`, fichiers statiques gardés 4 h en cache) :
  - `v1/manifest.json` : description du monde (12 800 × 12 800 cases), « supertuiles » de 256 cases (+1 de bord), 6 niveaux de zoom (900 tuiles au plus fin, 4 au plus large), et aussi `roads` (routes), `wind`, `passability` (où l'on peut passer), date de génération.
  - `v1/l<niveau>/<x>_<z>.<empreinte>.bin.gz` : tuile de terrain brute, 258 × 258 cases × 8 octets (≈ 530 Ko compressés) ; probablement hauteur, eau, biome par case. **Le dessin (ombres, couleurs) est fait dans le navigateur** à partir de ces nombres, pas des images toutes faites.
  - `resources.json` + `resources.bin.gz` (3,5 Mo) : **densité des ressources par palier** sur une grille 400 × 400 (11 paliers, nombre maximal par case) : ce n'est pas la position de chaque ressource, c'est une carte de chaleur par palier.
  - `entities.json` (1,5 Mo) : régions (nom, position en morceaux de carte), colonies, donjons, temples, villes en ruine, campements de voyageurs, etc.
- **Icônes** : `bitjita.com/map-icons/…` (paliers de colonies T1–T10, minerais T1–T10, donjon, temple, joueur…).
- **Données en direct** : `relay.bitjita.com/v1/database/idc-7/schema` (relais SpacetimeDB en lecture seule, déjà connu, voir `sources.md`).
- Mesure d'audience (Cloudflare, PostHog) : sans intérêt pour nous.

## Conclusions pour le bot
1. **Même source que nous** : bitjita lit les tables publiques du jeu (`terrain_chunk_state` : hauteur, eau, biomes ; positions des ressources et bâtiments), puis les prépare sur son serveur en tuiles. Le bot lit déjà le serveur : il peut lire `terrain_chunk_state` autour de Julien (pas toute la région d'un coup : 80 × 80 morceaux de 32 × 32 cases, trop lourd) et dessiner un **vrai fond de carte** (relief ombré, eau, biomes), comme bitjita.
2. **Zones découvertes** : `exploration_chunks_state` (une case par morceau de carte) permet d'assombrir ce que Julien n'a pas encore vu ; utile aussi pour l'exploration automatique.
3. **Filtres par palier** : bitjita montre une densité par palier sur tout le monde ; le bot, lui, a la position exacte des ressources mais seulement autour de Julien (20 cases en v0.4.15). Les deux se complètent.
4. **Passabilité et routes** : bitjita calcule où l'on peut passer ; utile pour l'exploration. Le bot peut le déduire du relief et de l'eau (À vérifier).
5. Utiliser directement les fichiers de bitjita est possible techniquement, mais c'est le service de quelqu'un d'autre (conditions d'usage à lire : `bitjita.com/docs/api`) : **à décider avec Julien**. Proposition : dessiner nous-mêmes à partir du serveur du jeu.

## Faisabilité (estimation)
- Fond de carte autour de Julien (relief, eau, biomes) + zones découvertes + filtres par palier simples : **faisable** en une version (v0.4.18), à condition de vérifier le format réel de `terrain_chunk_state` (journal, exemple brut).
- Carte de toute la région : possible en lisant les morceaux petit à petit et en les gardant sur le PC (À vérifier : poids et temps).
