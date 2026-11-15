# Analyse de BitCraftPublic

Résultat de l'analyse du clone du 2026-11-15 (dernier commit : 2026-11-13, « release 1.0 »). Dépôt : https://github.com/clockworklabs/BitCraftPublic

## Contenu

- `BitCraftServer/packages/game` et `global_module`. 723 fichiers Rust. Module SpacetimeDB : tables + reducers.
- Licence Apache 2.0, limitée au code. Art, musique et IP exclus. Interdits : exploiter un serveur, publier des exploits.

## Données statiques : schémas présents, valeurs absentes

- `src/messages/static_data.rs` définit environ 115 tables `*Desc` (dont `ItemDesc`, `ResourceDesc`, `CraftingRecipeDesc`, `ExtractionRecipeDesc`, `BuildingDesc`, `ToolDesc`, `SkillDesc`, `BiomeDesc`).
- Les valeurs sont chargées au déploiement par des reducers `import_*` (`src/import_reducers.rs`). Elles ne sont pas dans le repo. → [Q-004](../questions-ouvertes/Q-004-source-valeurs-donnees.md).

## Champs utiles au bot

- `ExtractionRecipeDesc` (récolte) : `resource_id`, `time_requirement`, `stamina_requirement`, `tool_requirements`, `level_requirements`, `extracted_item_stacks`, `range`.
- `ResourceDesc` : `max_health`, `tier`, `on_destroy_yield`, `scheduled_respawn_time`, `footprint`.
- `CraftingRecipeDesc` : `consumed_item_stacks`, `building_requirement`, `tool_requirements`, `time_requirement`.
- `ItemDesc` : `volume` (capacité inventaire), `tier`, `tag`.

## Monde

- Monde en **grille hexagonale** (`src/game/coordinates/` : `hex_coordinates`, `small_hex_tile`, `large_hex_tile`, `chunk_coordinates`, `region_coordinates`). Important pour la cartographie ([Q-002](../questions-ouvertes/Q-002-cartographie-navigation.md)).
- Le serveur valide les déplacements (`MoveValidationParamsDesc`) et a un agent de déconnexion automatique (`src/agents/auto_logout_agent.rs`). → impact sur les sessions longues à étudier ([Q-008](../questions-ouvertes/Q-008-deconnexion-cadence.md)).

## Open source : suite

- Phase 2 de l'open source annoncée environ 6 mois après janvier 2026 (Relay, Mob Manager, Map server). Client non publié.
