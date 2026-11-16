# Données de jeu

Base locale utilisée par `BitcraftBot.GameData` (D-008).

| Dossier | Contenu | Origine |
|---|---|---|
| `schemas/` | Schémas des tables `*Desc` (items, recettes, ressources…) | Extraits de [BitCraftPublic](https://github.com/clockworklabs/BitCraftPublic), `src/messages/static_data.rs` (Apache 2.0) |
| `gamedata/` | Inutilisé : le bot range les données dans `donnees-jeu/` à côté du .exe | [BitCraft_GameData](https://github.com/BitCraftToolBox/BitCraft_GameData) (D-033), téléchargé par le bot, jamais versionné |

## Format

JSON, un fichier par table, tel que publié par BitCraft_GameData (branche `cereal/cs`, dossier `static/`). Détail : D-033 et `memory/technique/architecture.md` (section 12).

Voir [memory/technique/analyse-bitcraftpublic.md](../memory/technique/analyse-bitcraftpublic.md).
