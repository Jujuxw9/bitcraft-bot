# Tous les liens utilisés (2026-11-15 → 2026-11-18)

Liens consultés par Claude pour récupérer des données, comprendre le jeu ou le serveur, ou donner du contexte. Évaluation détaillée des sites communautaires : [sources.md](sources.md).

## Règles et politique du jeu
| Lien | Pourquoi |
|---|---|
| https://bitcraftonline.com/community-guidelines | Règles : bots et macros interdits, outils en lecture seule permis |
| https://bitcraftonline.com/eula | Conditions d'utilisation |
| https://clockworklabs.helpshift.com/hc/en/4-bitcraft/faq/132-monthly-update-tomorrow---housing-group-crafting-developer-program | FAQ officielle des outils tiers : lecture avec son propre jeton permise, limitée à sa région ; bots interdits ; programme développeurs |
| https://bitcraftonline.com/news/bitcraft-online-verified-developer-program | Programme « Verified developer » (page en 404 le 2026-11-17) |

## Code du serveur et SpacetimeDB
| Lien | Pourquoi |
|---|---|
| https://github.com/clockworklabs/BitCraftPublic | Code serveur 1.0 : tables, connexion (« la connexion la plus récente gagne »), authentification (24 h région, 1 h global), coordonnées |
| https://github.com/clockworklabs (liste des dépôts) | Vérifier que le client du jeu n'est **pas** public |
| https://spacetimedb.com/docs/ | Fonctionnement de SpacetimeDB (abonnements, reducers) |
| https://spacetimedb.com/docs/core-concepts/authentication/ | Jetons et identités SpacetimeDB |
| https://github.com/clockworklabs/SpacetimeDB/tree/master/crates/client-api-messages/src/websocket | Format exact des messages WebSocket (v1 JSON) |
| https://github.com/clockworklabs/SpacetimeDB/blob/master/docs/docs/00300-resources/00400-sql-reference.md | Syntaxe SQL des abonnements (jointures, identité en 0x…) |
| https://bitcraftonline.com/news/spacetimedb-and-bitcraft | Article technique sur le serveur |

## Serveur officiel (adresses lues directement)
| Lien | Pourquoi |
|---|---|
| https://api.bitcraftonline.com/authentication/request-access-code | Demande du code par e-mail |
| https://api.bitcraftonline.com/authentication/authenticate | Échange du code contre le jeton |
| https://bitcraft-early-access.spacetimedb.com/v1/database/idc-18/schema?version=9 | Schéma de la région 18 du monde 1.0 |
| https://bitcraft-early-access.spacetimedb.com/v1/database/idc-global/schema?version=9 | Schéma du module global du monde 1.0 |
| wss://bitcraft-early-access.spacetimedb.com/v1/database/idc-18/subscribe | Liaison de lecture de la région 18 |
| …/bitcraft-live-18, …/bitcraft-live-global | **Ancien monde** (erreur des v0.3.5 et v0.3.6) |

## Outils communautaires
| Lien | Pourquoi |
|---|---|
| https://github.com/raffiandev/bitcraft-stdb | Lecteur communautaire (dérivé de Bitjita) : méthode du code e-mail, protocole JSON |
| https://relay.bitjita.com/ | Relais Bitjita en lecture seule, sans jeton : a révélé les vrais noms de modules (`idc-N`, `idc-global`) |
| https://bitjita.com/ (envoyé par Julien le 2026-11-18) | Site communautaire : joueurs, régions, marché, état des serveurs |
| https://bitjita.com/docs/api (envoyé par Julien le 2026-11-18) | API publique : recherche de joueur, équipement, inventaire, régions (≈ 82 adresses). Pas de position en direct documentée |
| https://github.com/BitCraftToolBox/BitCraft_GameData | Données du jeu (ressources, objets, recettes), source de D-033 |
| https://gist.github.com/Kattoor/3a259b3881d3cc9f15c757cc767b140b | Ancien exemple de connexion (2025), périmé |

## Mises à jour du jeu
| Lien | Pourquoi |
|---|---|
| https://store.steampowered.com/news/app/3454650/view/655997268258717736 (envoyé par Julien les 16 et 18/11) | Patch note 1.0 « A Fresh World » |
| https://bitcraftonline.com/news/bitcraft-online-1-0-patch-notes | Notes de version 1.0 |
| https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=3454650 | Texte complet des patch notes |
| https://api.steamcmd.net/v1/info/3454650 | Numéro et date du dernier build |
| https://steamcommunity.com/app/3454650/discussions/0/685240979008140619/ | Discussion Steam sur le code d'accès |
