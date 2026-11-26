# Prompt de la session « site des recettes » (préparé le 2026-11-26, session 29)

Julien colle le bloc ci-dessous au début d'un **nouveau chat**, quand il décide de commencer. Cette session ne fait **que** le site des recettes.

---

> Tu es la session « site des recettes » du projet bitcraft-bot (dossier `C:\Users\throj\Desktop\bitcraft-bot`, dépôt GitHub `bitcraft-bot` de Julien). Ton seul sujet : concevoir, construire, tester et mettre en ligne un site web des recettes du jeu BitCraft Online. Tu ne touches pas au bot (dossiers `src/`, `tests/`, versions du bot).
>
> **Avant toute chose**
> 1. Lis `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, puis ce fichier en entier : `memory/echanges/2026-11-26-prompt-site-recettes.md`. Fais `git pull --rebase` (ignore « [SYNC PC-PERSO] » et les commits « [PERSO JULIEN] » : autre projet).
> 2. Lis D-064 (`memory/decisions/`) et `memory/echanges/2026-11-26-methode-site-recettes.md`.
> 3. Lis le dépôt du site **Dogo eSport** que Julien te donnera (site web qu'il héberge déjà) : regarde comment il est construit, hébergé, mis à jour, et ce qu'on peut reprendre (hébergement Unraid, conteneur, nom de domaine, accès depuis l'extérieur, style). Range le lien dans `memory/technique/sources.md` et `memory/technique/liens-utilises.md`.
>
> **Ta méthode, dans l'ordre (ne saute pas d'étape)**
> 1. **Cadrage, sans code.** Pose les questions à Julien **une à la fois**, attends chaque réponse et note tout dans `memory/site-recettes/cadrage.md`. Les questions de départ sont listées plus bas.
> 2. **Proposition.** Écris une proposition courte : parcours de l'utilisateur, écrans, fonctions, données, hébergement, tests. Julien la valide (décision `D-NNN`) avant que tu codes.
> 3. **Maquette** cliquable avec de vraies données sur environ 20 objets. Julien la teste et donne ses retours.
> 4. **Site complet**, puis **mise en ligne** sur Unraid, avec la procédure écrite pas à pas pour Julien.
> À chaque étape : teste toi-même avant de livrer (tests automatiques + essai réel dans le navigateur), et rappelle en fin de message les questions sans réponse.
>
> **Ce qu'on sait déjà**
> - **Pour qui** : Julien, ses amis (et leurs amis proches) et sa guilde. **Pas un site public** pour tous les joueurs. Donc il faut un moyen simple de limiter l'accès (à définir avec Julien : lien privé, mot de passe commun, autre). « À vérifier ».
> - **But** : un site « ultra fluide et ultra simple à utiliser », qui contient toutes les recettes et répond à la question « comment je fabrique cet objet ? ».
> - **Hébergement** : le serveur **Unraid** de Julien (petit PC chez lui, à côté de la box). Mode d'hébergement : « À vérifier » (le site Dogo eSport donnera sans doute la réponse).
> - **Code** : même dépôt, **dossier à part** (par exemple `site-recettes/`, nom à confirmer). Mémoire du site : `memory/site-recettes/` (à créer, avec un `README.md` index).
> - **Données disponibles** : le bot a déjà les données du jeu (fichiers `*_desc.json` dans `artifacts/release/donnees-jeu/`, dont `crafting_recipe_desc.json`, `extraction_recipe_desc.json`, objets, bâtiments, biomes ; et les icônes dans `donnees-jeu/icones`, environ 2 339 images). Le code du bot qui lit les recettes : `src/BitcraftBot.GameData/` (`RecipeBook`, `RecipePlanner`, `RawMaterials`) ; tu peux le **lire** pour comprendre, pas le modifier. D'où viennent ces fichiers et comment les mettre à jour après une mise à jour du jeu : « À vérifier », à documenter.
> - **Copie du code serveur officiel** : `references/BitCraftPublic/` (lecture seule) : utile pour comprendre la structure des recettes.
> - L'onglet RECETTES du bot sera retiré une fois le site en ligne (D-064).
>
> **Méthode proposée par Claude (à faire valider au cadrage)**
> - **Site statique** : toutes les recettes chargées dans le navigateur, recherche instantanée, rien de lourd à faire tourner sur Unraid (un petit serveur web suffit).
> - **Un script** transforme les données du jeu en fichier(s) pour le site, relancé à chaque mise à jour du jeu.
> - **Fonctions pressenties** : recherche tolérante aux fautes (français et anglais) ; page d'objet avec l'arbre de fabrication ; total des matières premières pour une quantité choisie ; où trouver chaque matière ; métier, palier (tier) et bâtiment requis ; « utilisé dans » (recettes qui ont besoin de l'objet) ; déduction de ce qu'on a déjà ; images des objets ; marche sur téléphone et sur PC.
>
> **Questions de départ pour Julien (une à la fois)**
> 1. Le lien du dépôt Dogo eSport (et ce qu'il veut en reprendre : style, hébergement, autre).
> 2. 3 à 5 cas concrets : « je cherche X, je veux savoir … ».
> 3. Langue(s) du site : français, anglais, les deux ?
> 4. Plutôt téléphone, PC, ou les deux ?
> 5. Comment les amis et la guilde y accèdent (adresse, accès depuis l'extérieur de chez Julien, protection).
> 6. Des sites de recettes qu'il aime ou n'aime pas (bitjita, autres) et pourquoi.
> 7. Faut-il des comptes (par exemple pour noter son stock), ou un site sans compte ?
>
> **Règles du projet (non négociables)**
> - Français partout sauf les lignes de code (commentaires, textes du site, commits, mémoire). Julien n'est pas technique et ne lit pas le code : explique le comportement, jamais les lignes. Phrases courtes, concret.
> - Au moindre doute, pose une question au lieu de supposer. Ce qui n'est pas vérifié est écrit « À vérifier ».
> - **Jamais de session ni d'agent lancé sans l'accord de Julien** (quota sur 5 h).
> - Aucune donnée privée de Julien (jeton du jeu, e-mail, mots de passe, adresse de sa box) dans Git, un journal ou une réponse. Le site ne se connecte **jamais** au serveur du jeu avec le compte de Julien.
> - Ne jamais fermer les programmes de Julien ; `git status` avant `git add` (une autre session peut travailler dans le dépôt : ne commite que tes fichiers).
> - Pas de Release ni de tag GitHub. Commit et push à chaque étape validée.
> - En fin de session : journal dans `memory/journal/`, `resume-global.md` (10 lignes max), index des dossiers modifiés, `tools/agent-memoire/verifier-memoire.ps1` à 0 erreur.

---
