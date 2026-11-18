# Voie hybride — constats (2026-11-17)

## Règles officielles (FAQ Clockwork Labs, « developer program »)
- Lire les données avec **son propre jeton** : permis. Construire un outil qui le fait : permis.
- Lecture limitée à la **région où se trouve le personnage**.
- Partager son jeton : interdit (accès complet au compte).
- Bots, triche, diffusion d'entrées multi-clients : interdits, ban définitif possible.
- Les développeurs prévoient de restreindre l'inventaire et les ressources **loin du joueur**. Peu gênant pour nous (on farme autour du personnage), mais à surveiller.
- Programme « Verified developer » (formulaire Google) : jeton multi-régions. Pas pour nous (bot).
- Limites de débit : non indiquées.

## Connexion (source : dépôt communautaire `raffiandev/bitcraft-stdb`, juillet 2025, dérivé de Bitjita)
- Jeton : `POST https://api.bitcraftonline.com/authentication/request-access-code?email=…` → code reçu par e-mail → `POST …/authenticate?email=…&accessCode=…` → jeton (format JWT, utilisé en `Authorization: Bearer …`).
- Hôte : `bitcraft-early-access.spacetimedb.com` (le nom résout toujours le 2026-11-17). **À vérifier pour la 1.0.**
- Modules : un module global, puis un module par région. La table `region_connection_info` (id, host, module) donne l'hôte et le nom du module de région. Noms vus en 2025 : `bitcraft-global`, `bitcraft-3`. **À vérifier pour la 1.0.**
- Protocole : WebSocket `wss://{hôte}/v1/database/{module}/subscribe`, sous-protocole JSON `v1.json.spacetimedb`, abonnement par requêtes SQL (`SELECT * FROM table WHERE …`). Puis réception des changements en direct. **À vérifier avec SpacetimeDB 2.0.**
- Schéma : `GET https://{hôte}/v1/database/{module}/schema?version=9`.
- Le dépôt prévient : « si vous êtes connecté au jeu, ces commandes peuvent vous déconnecter ».

## Point critique : « la connexion la plus récente gagne » (code serveur 1.0, `game/src/lib.rs`)
- À chaque connexion, le serveur note la **dernière connexion** du compte comme connexion active.
- À la déconnexion : si c'est la connexion active → le joueur est **déconnecté du jeu** (sign_out). Si c'est une ancienne connexion → rien.
- Conséquence : si le bot se connecte **après** le jeu, puis se ferme, le personnage risque d'être déconnecté.
- Parade à tester : **lancer la lecture serveur avant le jeu**. Le jeu devient la connexion active ; la fermeture du bot ne fait rien. **À vérifier en réel.**
- Chaque connexion est enregistrée côté serveur : la lecture est visible (elle est permise par la FAQ).

## Région
- Le monde est carré, découpé en régions (nombre carré : 4, 9, 16…). La taille exacte en 1.0 : **à vérifier** (sera lue à la connexion).
- Une région est un grand morceau de carte : largement assez pour farmer autour de Snowrest.

## Tables utiles (code serveur 1.0, toutes publiques)
| Besoin | Table |
|---|---|
| Position, destination, en marche | `mobile_entity_state` (location_x, location_z, destination_x/z, is_walking) |
| Vie / endurance / faim | `health_state`, `stamina_state`, `satiation_state` |
| Arbres et ressources | `resource_state` (resource_id → BitCraft_GameData : type, âge), `resource_health_state` |
| Ennemis | `enemy_state` |
| Métiers | `experience_state` |
| Équipement | `equipment_state` |
| Joueur | `player_state`, `signed_in_player_state` |
Inventaire, position des ressources : noms exacts **à vérifier** dans le schéma réel.

## Gain attendu par rapport à la vision
- Position exacte sans OCR. Arbres connus avec leur type et leur âge **sans cliquer** (règle Q-019 / D-036 simplifiée). Ennemis connus même hors écran. Inventaire plein connu sans lire l'écran.

## Vérifié le 2026-11-18 (session 10)
- Modules **1.0** sur l'hôte `bitcraft-early-access.spacetimedb.com` : global = `bitcraft-live-global`, régions = `bitcraft-live-N`. **R18 → `bitcraft-live-18`** (le schéma répond). Les anciens `bitcraft-global`, `bitcraft-1`… existent encore (accès anticipé, non utilisés).
- Schéma de `bitcraft-live-18` lisible sans jeton : 542 tables. Toutes les tables utilisées en H1 sont publiques, avec clé primaire.
- Liaison WebSocket avec un faux jeton : **refus 401**. Adresse, protocole et lecture du jeton par le serveur : OK.
- Global : `user_region_state` (identité → région) existe. Non utilisé : le bot ne se connecte qu'à la région (une seule liaison, moins de risque).
- Positions (code serveur) : `mobile_entity_state.location_x/z` = petites cases × 1000 ; `location_state.x/z` = petites cases. Une grande case (terrain) = 3 petites cases. Lien avec les coordonnées N / E de l'écran : **à vérifier** (le bot note les deux toutes les 15 s).
- SQL des abonnements (doc SpacetimeDB) : jointure de 2 tables permise si les deux colonnes sont indexées (`entity_id` l'est) ; identité en `0x…`.
