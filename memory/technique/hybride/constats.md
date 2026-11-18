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
- Hôte : `bitcraft-early-access.spacetimedb.com`. **Vérifié en 1.0** ; le module global indique aussi `bitcraft-release.spacetimedb.com` pour les régions (les deux marchent).
- Modules : un module global, puis un module par région. La table `region_connection_info` (id, host, module) donne l'hôte et le nom du module de région. Noms 2025 : `bitcraft-global`, `bitcraft-3`. **En 1.0 : `idc-global` et `idc-N`** (voir plus bas).
- Protocole : WebSocket `wss://{hôte}/v1/database/{module}/subscribe`, sous-protocole JSON `v1.json.spacetimedb`, abonnement par requêtes SQL (`SELECT * FROM table WHERE …`). Puis réception des changements en direct. **Vérifié en réel le 2026-11-18.**
- Schéma : `GET https://{hôte}/v1/database/{module}/schema?version=9`.
- Le dépôt prévient : « si vous êtes connecté au jeu, ces commandes peuvent vous déconnecter ».

## Point critique : « la connexion la plus récente gagne » (code serveur 1.0, `game/src/lib.rs`)
- À chaque connexion, le serveur note la **dernière connexion** du compte comme connexion active.
- À la déconnexion : si c'est la connexion active → le joueur est **déconnecté du jeu** (sign_out). Si c'est une ancienne connexion → rien.
- Conséquence : si le bot se connecte **après** le jeu, puis se ferme, le personnage risque d'être déconnecté.
- Parade : **lancer la lecture serveur avant le jeu**. Le jeu devient la connexion active ; la fermeture du bot ne fait rien. **Vérifié en réel** (essais v0.3.7, v0.3.8, v0.3.9). Bot connecté après le jeu = jeu éjecté (voir plus bas).
- Chaque connexion est enregistrée côté serveur : la lecture est visible (elle est permise par la FAQ).

## Région
- Le monde est carré, découpé en régions. En 1.0 (`world_region_state`, lu le 2026-11-18) : **25 régions (5 × 5)**, chaque région = 80 × 80 chunks ; R18 commence au chunk x 160, z 240. Seules 9 régions (7-9, 12-14, 17-19) ont un module visible.
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
Position des ressources : `location_state` (jointure sur `entity_id`), **vérifié** en v0.3.7. Inventaire : nom de table **à vérifier** dans le schéma réel.

## Gain attendu par rapport à la vision
- Position exacte sans OCR. Arbres connus avec leur type et leur âge **sans cliquer** (règle Q-019 / D-036 simplifiée). Ennemis connus même hors écran. Inventaire plein connu sans lire l'écran.

## Vérifié le 2026-11-18 (session 10)
- (Corrigé plus bas : les modules 1.0 sont `idc-N` et `idc-global`.) Modules `bitcraft-live-*` trouvés en premier sur l'hôte `bitcraft-early-access.spacetimedb.com`.
- Schéma de `bitcraft-live-18` lisible sans jeton : 542 tables. Toutes les tables utilisées en H1 sont publiques, avec clé primaire.
- Liaison WebSocket avec un faux jeton : **refus 401**. Adresse, protocole et lecture du jeton par le serveur : OK.
- Global : `user_region_state` (identité → région) existe. Non utilisé : le bot ne se connecte qu'à la région (une seule liaison, moins de risque).
- Positions (code serveur) : `mobile_entity_state.location_x/z` = petites cases × 1000 ; `location_state.x/z` = petites cases. Une grande case (terrain) = 3 petites cases. Lien avec les coordonnées N / E de l'écran : trouvé, voir « Essais réels » plus bas.
- SQL des abonnements (doc SpacetimeDB) : jointure de 2 tables permise si les deux colonnes sont indexées (`entity_id` l'est) ; identité en `0x…`.

## Premier essai réel de Julien (2026-11-18, v0.3.5)
- Code e-mail et jeton : **OK** (compte c200…cc03).
- Liaison avec `bitcraft-live-18` : acceptée (pas de 401), puis **fermée aussitôt par le serveur**, aucun message reçu.
- Code serveur : à l'ouverture, une région refuse le compte si (1) il n'est pas « authentifié » depuis moins de **24 h**, ou (2) le compte n'a pas de `user_state` dans **cette** région. Le module global, lui, exige une authentification de moins d'**1 h**, mais pas de personnage. L'« authentification » est posée par le service de connexion de BitCraft (reducer `authenticate`, appelé par un compte administrateur).
- L'API SQL par HTTP est bloquée (403) : seule la liaison WebSocket est possible.
- Réponse en v0.3.6 : découverte par le module global (compte connu ? région du personnage ? adresse du module ?) quand le jeu est fermé, puis lecture de la bonne région ; message clair si la région refuse.

## Cause trouvée (2026-11-18, essai v0.3.6) : mauvais noms de modules
- Essai v0.3.6 : le module `bitcraft-live-global` **et** la région `bitcraft-live-18` ferment la liaison, même 10 s après un nouveau code, jeu lancé ou non.
- Le monde **1.0** utilise les modules **`idc-N`** (régions) et **`idc-global`**, sur le même hôte. Schéma `idc-18` : 809 Ko, propriétaire différent de `bitcraft-live-*`. Régions vues : 7, 8, 9, 12, 13, 14, 17, 18, 19 (un carré de 3 × 3). **R18 → `idc-18`**.
- `bitcraft-live-*` = ancien monde, où le personnage de Julien n'existe pas : d'où les refus.
- Toutes les tables et colonnes utilisées existent dans `idc-18` et `idc-global` (test sur le vrai schéma : OK).
- Corrigé en **v0.3.7**.

## Essais réels v0.3.7 et v0.3.8 (2026-11-18, Julien) : la lecture marche
- Découverte par `idc-global` : OK (compte connu, région 18 « Keldan », `region_connection_info` donne l'hôte **`bitcraft-release.spacetimedb.com`**). `bitcraft-early-access` accepte aussi `idc-18`.
- Lecture de `idc-18` : OK. Personnage (entité 648518346637266951), vie, endurance, faim, `signed_in_player_state` (passe à 1 quand le jeu se connecte). Environ **700 ressources** dans un carré de 80 petites cases (jointure acceptée).
- Changements reçus en direct : ≈ 1 message par seconde (faim qui baisse), plus les déplacements.
- **Ordre bot → jeu, puis déconnexion du bot : le jeu reste connecté** (vérifié 2 fois).
- **Bot connecté alors que le jeu est ouvert : le jeu est éjecté** aussitôt (« Logged in elsewhere », capture de Julien). Le client du jeu se ferme lui-même quand une connexion plus récente du compte apparaît. Pas besoin d'attendre la déconnexion du bot.
- Correctif v0.3.9 : connexion **refusée** si le jeu est lancé (aucune dérogation) ; plus de question à la déconnexion.
- Coordonnées : **E = arrondi(x ÷ 3), N = arrondi(z ÷ 3)** (x, z = position serveur en petites cases). Vérifié à l'arrêt en v0.3.9 : écarts de 0,3 à 0,4, toujours vers l'arrondi. En marche, écart jusqu'à 1 (décalage de temps entre l'écran et le serveur).
- Arbres : les instantanés v0.3.9 listent bien des « Young Oak Tree », « Young Beech Tree » et « Mature Oak Tree » (8,6 et 10,2 cases). L'instantané ne garde que les 50 ressources les plus proches, fleurs comprises : un arbre peut donc manquer dans le fichier mais pas dans le bot. **À vérifier** : l'arbre que coupe Julien correspond-il à une ligne de la liste (position, nom) ?

## Relais Bitjita (piste, non utilisée)
- `relay.bitjita.com` : relais SpacetimeDB en **lecture seule**, protocole v1, modules `idc-N` et `idc-global`, identités propres au relais (**aucun jeton BitCraft**). Exploitant, règles d'usage et limites : non indiqués.
- Avantage : aucune liaison avec le compte de Julien, donc **aucun risque de déconnexion** du personnage. Inconvénients : service tiers, disponibilité non garantie.
