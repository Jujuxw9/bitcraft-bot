# Sources analysées

| Source | Contenu | Utilité projet |
|---|---|---|
| github.com/clockworklabs/BitCraftPublic | Code serveur Rust, schémas de données | **Haute** : modèle de données, règles de récolte et de craft, coordonnées hexagonales |
| bitcraftonline.com/community-guidelines | Règles anti-bot | **Haute** : risques ([regles-et-risques.md](../00-contexte/regles-et-risques.md)) |
| bitcraftonline.com/news/bitcraft-open-sourcing-update (16 janv. 2026) | Phase 1 open source, permis et interdits | Moyenne : cadre légal du code |
| bitcraftonline.com/news/open-sourcing-bitcraft-online (28 avr. 2025) | Annonce de l'intention | Faible : historique |
| bitcraftonline.com/news/bitcraft-1-0-your-questions-answered (11 nov. 2026) | 1.0 : nouveau monde, reset de progression, refonte des professions, changements Banque et Marché | **Haute** : l'UI et les mécaniques viennent de changer ; captures de référence à refaire en 1.0 |
| bitcraftonline.com/news | Patch notes (cadence régulière) | **Haute** : chaque patch peut casser la vision ; à surveiller |
| store.steampowered.com/app/3454650 | Config requise, 12 professions, UI en français disponible | Moyenne |
| bitcraftonline.com/discover | Présentation marketing | Faible |
| bitcraftonline.com | Liens support, EULA, Discord | Faible |
| github.com/Snipey/Hexel | Bot Discord (TypeScript, juil. 2025). Contient des **exports JSON des tables de jeu** : `item_desc`, `resource_desc`, `extraction_recipe_desc`, `crafting_recipe_desc`… (`src/data/region/`) | **Haute** pour Q-004 : valeurs réelles, mais **d'avant la 1.0** (À vérifier : écarts avec la 1.0) |
| bitjita.com (API, ex. `/api/crafts`) | Site communautaire qui expose les données du jeu en lecture seule | **Haute** pour Q-004 : source possiblement à jour en 1.0 (À vérifier : quelles routes, conditions d'usage) |
| github.com/EisPaprika/bitcraft-bot | Bot Discord Python (avr. 2026) : infos de craft d'une colonie via l'API Bitjita | Faible : exemple d'appel à Bitjita |
| github.com/KascadeKyle/bitcraft-job-bot | Bot Discord JavaScript (mars 2026) : suivi des crafts via l'API Bitjita | Faible : exemple d'appel à Bitjita |
| github.com/Lebenet/KoF_bot | Bot Discord TypeScript du « Royaume de France » (août 2026) : commandes, crafts, colonies, Google Sheets | Faible : aucun lien avec la vision ou le pilotage du jeu |
| bitjita.com/items, bitjita.com/xp-calculator | Base d'objets et calculateur d'expérience (même site que l'API Bitjita) | **Haute** pour Q-004 : à évaluer en premier |
| bitcraftdb.com | Base de données du jeu (objets, recettes) | Haute pour Q-004 : à évaluer (version 1.0 ?) |
| bccodex.com | Encyclopédie du jeu | Moyenne : à évaluer |
| bitcraft.wiki.gg (accueil, page BitCraft, Guides) | Wiki communautaire actif | Moyenne : mécaniques (endurance, faim, banque, chariot) |
| bitcraft.fandom.com | Ancien wiki communautaire | Faible : sans doute moins à jour que wiki.gg |
| bitcraftplanner.com, bitcraftcalculator.web.app, bitcraftulator.com | Planificateurs et calculateurs de craft | Faible pour le MVP ; utile pour le niveau N2 (D-003) |
| reddit.com/r/BitCraftOnline (sujet calculateur) | Annonce d'un calculateur communautaire | Faible |
| **github.com/BitCraftToolBox/BitCraft_GameData** (branche `cereal/cs`) | **Tables de données du jeu en JSON, mises à jour chaque jour** (2026-11-15 = après la 1.0) : ressources, objets, recettes | **Très haute** : source proposée pour Q-004 (D-033). Pas de licence : ne pas recopier dans notre dépôt |
| bitcraftonline.com/news/bitcraft-online-1-0-patch-notes | Notes de version 1.0 | Haute : changements de mécaniques et d'interface |
| steamdb.info/app/3454650/patchnotes, store.steampowered.com/news/app/3454650 | Historique des mises à jour | Haute : surveiller les patchs qui peuvent casser la lecture de l'écran |
| bitjita.com/status, status.technobezz.com/fr/bitcraft | État des serveurs | Moyenne : savoir si un problème vient du jeu |
| bitjita.com/stats, steamdb.info/app/3454650/charts | Statistiques de joueurs | Faible |
| bitcrafted.info, bitcraft-helper.com, bitcraftdex (Reddit) | Bases de données et aides communautaires | Faible : doublons de BitCraft_GameData |
| bitcraftonline.com/news/spacetimedb-and-bitcraft | Article technique sur le serveur | Moyenne : comprendre le serveur lu en voie hybride (D-039) |
| jeuxonline.info, actugaming.net | Articles de presse sur la sortie 1.0 | Faible |
| spacetimedb.com/docs | Docs SpacetimeDB 2.0, SDK TypeScript/Rust/C#/C++ | **Haute** depuis D-039 : protocole de la lecture serveur (H1) |
| api.steampowered.com/ISteamNews/GetNewsForApp (appid 3454650) | **API publique des actualités Steam** : texte complet des patch notes (la page web ne s'affiche pas sans navigateur) | **Haute** : lire chaque patch (utilisée le 2026-11-16) |
| store.steampowered.com/news/app/3454650/view/655997268258717736 (lien de Julien, 2026-11-16) | Patch note « BitCraft Online 1.0 – A Fresh World » (13/11) | Haute : analysé dans `analyse-lot5-2026-11-16.md` ; rien de bloquant, Maker's Tree à exclure |
| api.steamcmd.net/v1/info/3454650 | Numéro et date du dernier build du jeu | **Haute** : détecter une mise à jour sans note (ex. build 26707176 du 16/11 à 17:51) |
| github.com/clockworklabs/BitCraftPublic (renvoyé par Julien, 2026-11-17) | Code serveur 1.0. Le README dit que le client **n'est pas publié** | Déjà analysé (`analyse-bitcraftpublic.md`) ; base de Q-021 |
| spacetimedb.com/docs (renvoyé par Julien, 2026-11-17) | Fonctionnement : WebSocket chiffré, abonnement aux tables, appels de reducers | Moyenne : comprendre le jeu ; Q-021 |
| clockworklabs.helpshift.com (FAQ « developer program ») | Règles officielles des outils tiers : lecture avec son propre jeton permise, outils de bot interdits | **Haute** pour Q-021 |
| bitcraftonline.com/news/bitcraft-online-verified-developer-program | Programme « Verified developer » (rouvert le 2026-03-31). Page en erreur 404 le 2026-11-17 ; le texte est dans la FAQ | Faible |
| github.com/raffiandev/bitcraft-stdb (licence MIT, juillet 2025) | Lecteur WebSocket dérivé de Bitjita : obtention du jeton par code e-mail, hôte, modules, abonnement JSON | **Très haute** pour H1 (`technique/hybride/constats.md`). Ancien : à revérifier en 1.0 |
| relay.bitjita.com | Relais SpacetimeDB en lecture seule, sans jeton ; modules `idc-N` et `idc-global` | Haute : a donné les vrais noms de modules ; solution de secours sans risque de déconnexion |
| bitjita.com et bitjita.com/docs/api (envoyés par Julien, 2026-11-18) | Site communautaire et API publique (joueurs, inventaires, régions, ressources) | Moyenne : recherche de joueur, contrôles ; pas de position en direct documentée |
| github.com/BitCraftToolBox/cereal (AGPL-3.0) et cdn.brico.app/sprites | Navigateur des données du jeu ; CDN public des images d'objets (`<icon_asset_name>.webp`) | **Haute** pour les images de l'onglet Recettes (v0.3.15) ; téléchargées une fois, gardées sur le PC, jamais republiées |
| bitjita.com/map et mapdata.bitjita.com (envoyés par Julien, 2026-11-26) | Carte du monde : tuiles de terrain brutes, densité des ressources par palier, entités (colonies, donjons…), passabilité, routes | **Haute** pour la v0.4.18 (modèle à suivre) : `technique/analyse-carte-bitjita-2026-11-26.md` |
| bitcraft.wiki.gg (wiki communautaire) | Portraits des voyageurs (Rumbagh, Heimlich, The Twins, Brico, Alesi, Ramparte) | Haute pour l'onglet Voyageurs (v0.4.15) ; images téléchargées une fois, gardées sur le PC, jamais republiées |
