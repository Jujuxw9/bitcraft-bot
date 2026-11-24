# Données du serveur : ce qui est lu, ce qui est disponible, ce qui reste à l'écran

Mis à jour le 2026-11-19 (v0.3.17). Méthode voulue par Julien (D-044) : **tout prendre sur le serveur d'abord**, puis lire à l'écran seulement ce qui manque.
Toutes ces tables sont **publiques** (vérifié sur le vrai schéma de `idc-18`) et lues en **lecture seule**, avec le jeton de Julien.

## 1. Déjà lu par le bot

| Information | Table du serveur | Depuis | Remarque |
|---|---|---|---|
| Personnage du compte | `user_state` | v0.3.5 | identité → numéro d'entité |
| Position, destination, en marche | `mobile_entity_state` | v0.3.5 | E = x ÷ 3, N = z ÷ 3, ±1 case |
| Vie, endurance, faim | `health_state`, `stamina_state`, `satiation_state` | v0.3.5 | |
| Vie, endurance, faim maximales | `character_stats_state` (cases 0, 1, 19) | v0.3.15 / v0.3.17 | |
| En jeu ou non | `signed_in_player_state` | v0.3.5 | |
| Région | `world_region_state`, `world_region_name_state` | v0.3.5 | |
| Ressources autour (arbres, rochers…) | `location_state` + `resource_state` | v0.3.9 | carré de 40 petites cases |
| Santé des ressources | `resource_health_state` | v0.3.11 | |
| Sac, ceinture, coffres de banque | `inventory_state` | v0.3.14 | format vérifié en réel |
| Position des banques | `bank_state` | v0.3.14 | |
| Bonus actifs (nourriture, élixirs…) | `active_buff_state` | v0.3.15 | format vérifié en réel ; « en pause » hors ligne |
| **Expérience par métier** (= jauge de métier) | `experience_state` | v0.3.17 | niveau et % calculés avec la formule du serveur |
| **Action en cours** (récolte, fabrication, marche…) | `player_action_state` | v0.3.17 | début et durée en ms → progression du coup |
| **Ressource frappée** et dégâts du dernier coup | `extract_outcome_state` | v0.3.17 | remplace en partie la cible lue à l'écran |
| Temps de jeu | `player_state` | v0.3.17 | |
| Énergie de téléportation | `teleportation_energy_state` | v0.3.17 | |

Format réel des nouvelles tables v0.3.17 : **vérifié** à l'essai du 2026-11-19 (extraits bruts du journal) : `experience_state.experience_stacks` = liste de `[id du métier, expérience]` ; `player_action_state` (start_time et duration en ms, `action_type` et `layer` en énumérations `[index, {}]`) ; `extract_outcome_state` (`last_timestamp` en microsecondes Unix, `damage`, `is_crit`) ; `player_state.time_played` en secondes ; `teleportation_energy_state.energy` (100.0).

**% du métier comparé à la jauge du jeu** : Forestry 101 712 XP → niv. 29, 87,5 % (jeu : 29, 87 %) ; Mining 41 600 XP → niv. 22, 24,9 % (jeu : 22, 26 % après quelques coups). **Concordant.**

## 2. Disponible, pas encore lu (à étudier selon les besoins)

| Information | Table | Utile pour |
|---|---|---|
| Équipement porté (vêtements, armure) | `equipment_state` | sécurité, état |
| Fabrication en cours sur un atelier | `progressive_action_state` | plus tard (artisanat) |
| Ennemis proches, combat | `enemy_state`, `combat_state`, `threat_state`, `attack_outcome_state` | Q-014 (danger) |
| Bâtiments autour (dont la Banque) | `building_state`, `location_state` | H4 (aller à la Banque) |
| Véhicules / charrette | `deployable_state` | plus tard |
| Barre d'action (ce qui est rangé dans chaque case) | `action_bar_state` | plus tard |
| Quêtes | `quest_chain_state` | plus tard |
| Revendications (claims) | `claim_state`, `claim_member_state` | plus tard |

## 3. Ce que seul l'écran donne

| Information | Pourquoi l'écran | Lu depuis |
|---|---|---|
| **Cible sélectionnée** (avant le premier coup) | le serveur ne connaît la ressource qu'au premier coup (`extract_outcome_state`) | v0.2 |
| Menus, fenêtres et info-bulles ouverts | interface du jeu, pas sur le serveur | v0.3.13 (jeu caché) |
| Ce que voit la caméra (arbre visible, obstacle) | rendu 3D | H3 |
| ~~Contrôle croisé de la position~~ | retiré en v0.3.20 (D-048) : le serveur est juste, l'écran décrochait | v0.3.7 |

Vie, endurance, faim et jauge de métier ne sont **plus lues à l'écran** depuis la v0.3.19 ; position, zone et région non plus depuis la v0.3.20 (D-048) (demande de Julien) : le serveur les donne exactement. Passages de niveau et tâches (début, fin) : écrits dans le journal d'après le serveur (`experience_state`, `player_action_state`, `extract_outcome_state`, inventaires).

## Tâches de voyageur (v0.4.11, session tâches, 2026-11-24)

- **`traveler_task_state`** (publique, lue par `SELECT * FROM traveler_task_state WHERE player_entity_id = <personnage>`) : `entity_id`, `player_entity_id`, `traveler_id` (= `npc_desc.npc_type`), `task_id` (= `traveler_task_desc.id`), `completed`. Format tiré du code serveur (`messages/components.rs`) ; **format réel vérifié le 2026-11-24** (24 lignes reçues, `{entity_id, player_entity_id, traveler_id, task_id, completed}`).
- **`traveler_task_desc`** et **`npc_desc`** : données fixes, prises dans BitCraft_GameData (D-033) et **vérifiées** le 2026-11-24 (321 tâches, 7 voyageurs). `required_items` : liste `{item_id, quantity, item_type "Item"/"Cargo", durability}`.
- Rendu d'une tâche (code serveur `player_complete_task`) : les objets sont pris **dans le sac seulement** (inventaire n° 0 du personnage), pas dans la ceinture ni les banques ; il faut être à 8 cases du voyageur. Une tâche rendue passe à `completed = true`, puis le serveur la remplace par une nouvelle s'il en trouve une.

## Colonies (v0.4.13)

- `claim_state` (nom, propriétaire, neutre) et `claim_local_state` (nombre de cases, réserves, `location` en option) : lues sans filtre de zone, **format réel À vérifier** au prochain journal (« Exemple brut de claim_state / claim_local_state »).
