# Architecture du site des recettes (maquette, 2026-11-26)

Explication du fonctionnement, sans lignes de code (D-023). Code : dossier `website-recipes/` du dépôt.

## Les trois morceaux
1. **Le fabricant de données** (`tools/build_data.py`, `tools/recipes_core.py`, Python) :
   - télécharge les tables publiques du jeu (`BitCraftToolBox/BitCraft_GameData`, branche `cereal/cs`) dans `build/raw/<commit>/` ;
   - les transforme en **un seul fichier** `build/data.json` (≈ 3,2 Mo, ≈ 520 Ko compressé) : objets, recettes gardées, recette la plus facile de chaque objet, autres recettes, sources de récolte, « used in », supplies ;
   - récupère les images : d'abord celles déjà téléchargées par le bot, sinon brico.app, sinon bitjita. Rangées dans `build/icons/` (≈ 1 600 images, 53 Mo). Les images introuvables sont notées pour ne pas les redemander.
   - `build/` n'est **pas** suivi par Git (données et images tierces).
2. **Le serveur web** (`serve.py`, Python, bibliothèque standard seulement) : sert la page, le fichier de données (compressé) et les images. Refuse tout fichier hors de ces dossiers. En-têtes de sécurité, pas d'indexation par les moteurs de recherche. Sur le PC : `http://127.0.0.1:8790`.
3. **La page** (`public/`, HTML + JavaScript + CSS, sans bibliothèque extérieure) : charge le fichier de données une fois, puis tout se passe dans le navigateur (recherche instantanée, calculs). Textes en anglais (D-077).

## Comment est choisie la recette la plus facile (D-077)
- Chaque objet reçoit un **coût** = nombre de matières premières pour en fabriquer un. Le calcul est refait jusqu'à ce que plus rien ne bouge ; une boucle de recettes ne peut jamais baisser un coût.
- Coût d'une matière récoltée = 1 ÷ sa meilleure chance d'obtention ; ×50 si la seule source est rare (ruines, épaves, coffres, quêtes…). Objet acheté à un voyageur (« Trade Good », ex. Metalworking Flux de Rumbagh) ou gibier : 1. Objet sans recette ni récolte (butin, événement) : 1 000.
- Recettes écartées d'office : recyclage (Salvage), amélioration (Recraft), mise en paquet (Package / Unpack).
- Les objets « … Output » du jeu (ex. « Ferralith Ingot Output ») s'ouvrent en leur résultat le plus probable (95 % : un lingot).
- À égalité : la machine de plus bas tier, puis le moins d'ingrédients.
- Les variantes de rareté d'un objet (5 « Emarium Axe ») sont regroupées en un seul objet.

## Ce que calcule la page
- **Arbre** : chaque case = un objet, la quantité, les stacks, la machine, le métier, le nombre de fabrications. Une case cochée grise toute sa branche. Un nombre « have » réduit la branche. Zoom (molette, boutons), déplacement (glisser), « Fit », branches repliables.
- **Still needed** et **Craft order** : totaux **mis en commun** sur tout l'arbre (un même objet demandé par deux branches est fabriqué en une fois, sans double arrondi).
- **Stacks** : 6 000 ÷ volume de l'objet (case d'inventaire du joueur, code serveur `player_create.rs`) ; 1 cargo par case. « À vérifier » en jeu.
- **Where to get it** : ressources qui donnent l'objet (outil, niveau du métier, chance par action, ou « when fully harvested » pour un arbre abattu), puis un lien vers la carte de bitjita.
- Coches, nombres, quantité, recette choisie, vue : gardés **dans le navigateur** (pas de compte).

## Tests
- `node --test website-recipes/tests/test_calc.mjs` : 18 tests (arbre, coches, « have », totaux mis en commun, ordre de fabrication, autre recette, boucles, stacks, recherche, cas réels de Julien).
- `py -3 -B -m unittest discover -s website-recipes/tests -p "test_*.py"` : 15 tests (règles de D-077 sur des tables inventées, cas réels).

## Pas encore fait (étape 4)
- Conteneur Docker sur Unraid, mise à jour quotidienne automatique des données, déploiement, sous-domaine `bitcraft-riverwood`, Nginx Proxy Manager.
