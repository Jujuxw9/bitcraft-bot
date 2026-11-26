# Prompt de la session « site des recettes » (préparé le 2026-11-26, session 29)

Julien colle le bloc ci-dessous au début d'un **nouveau chat**, quand il décide de commencer. Cette session ne fait **que** le site des recettes.

---

> Tu es la session « site des recettes » du projet bitcraft-bot (dossier `C:\Users\throj\Desktop\bitcraft-bot`, dépôt GitHub `bitcraft-bot` de Julien). Ton seul sujet : concevoir, construire, tester et mettre en ligne un site web des recettes du jeu BitCraft Online. Tu ne touches pas au bot (dossiers `src/`, `tests/`, versions du bot).
>
> **Avant toute chose**
> 1. Lis `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, puis ce fichier en entier : `memory/echanges/2026-11-26-prompt-site-recettes.md`. Fais `git pull --rebase` (ignore « [SYNC PC-PERSO] » et les commits « [PERSO JULIEN] » : autre projet).
> 2. Lis D-064 (`memory/decisions/`) et `memory/echanges/2026-11-26-methode-site-recettes.md`.
> 3. Lis le dépôt **privé** `https://github.com/Jujuxw9/DOGGO-ESPORT` (site que Julien héberge déjà chez lui, accès avec `gh`) : surtout `rbrs-doggo/MISE-EN-LIGNE.txt`, `rbrs-doggo/docker/Dockerfile`, `.github/workflows/`, `rbrs-doggo/outils/deployer.py`, `rbrs-doggo/securite.py` et `rbrs-doggo/memoire/facon-de-travailler.md`. Range le lien dans `memory/technique/sources.md` et `memory/technique/liens-utilises.md`.
>    **Attention : le dépôt `bitcraft-bot` est PUBLIC.** N'y recopie jamais d'adresse IP, de nom de domaine, d'adresse du serveur Unraid, de mot de passe ni rien de privé tiré de DOGGO-ESPORT. Écris seulement « voir `MISE-EN-LIGNE.txt` du dépôt DOGGO-ESPORT ». Ne modifie rien dans DOGGO-ESPORT.
>
> **Ce que Claude a déjà retenu de DOGGO-ESPORT (26/11, à confirmer avec Julien)**
> - **Hébergement** : conteneur Docker sur Unraid ; l'image (langage et dépendances seulement) est fabriquée par GitHub et publiée ; le code reste dans le dossier `appdata` du serveur. Déploiement depuis le PC de Julien par le partage réseau du serveur, avec un script qui attend que le serveur confirme la nouvelle version. **Demander l'accord de Julien avant chaque déploiement.**
> - **Accès depuis l'extérieur** : un sous-domaine chez OVH → box (ports 80 et 443 seulement) → **Nginx Proxy Manager** sur Unraid (HTTPS, certificat Let's Encrypt) → le site. Julien a refusé Cloudflare et veut rester chez OVH. Le même chemin peut servir au site des recettes, avec un nouveau sous-domaine.
> - **Protection** : mot de passe administrateur (créé seulement depuis le réseau local) et mot de passe commun des amis ; mots de passe en empreinte, cookie de session sécurisé, blocage après 5 essais ratés, en-têtes de sécurité, adresse publique déclarée. Bon modèle pour « amis et guilde seulement ».
> - **Méthode qui marche avec Julien** : tout depuis la page web (pas de saisie à la main, pas de ligne de commande), pas de bouton inutile, peu de texte (l'aide derrière un bouton « i »), tutos pas à pas pour OVH, la box et Unraid, et à chaque livraison : ce qui est déployé et ce qui n'a pas été testé. Navigateur de Julien : Brave.
> - Python est disponible sur le PC par `py -3` (la commande `python` ouvre le Microsoft Store).
> - Choix à faire avec Julien : reprendre la même base (même langage et même protection que DOGGO-ESPORT, plus simple à maintenir pour lui) ou un site purement statique derrière la même protection.
>
> **Ta méthode, dans l'ordre (ne saute pas d'étape)**
> 1. **Cadrage, sans code.** Pose les questions à Julien **une à la fois**, attends chaque réponse et note tout dans `memory/site-recettes/cadrage.md`. Les questions de départ sont listées plus bas.
> 2. **Proposition.** Écris une proposition courte : parcours de l'utilisateur, écrans, fonctions, données, hébergement, tests. Julien la valide (décision `D-NNN`) avant que tu codes.
> 3. **Maquette** cliquable avec de vraies données sur environ 20 objets. Julien la teste et donne ses retours.
> 4. **Site complet**, puis **mise en ligne** sur Unraid, avec la procédure écrite pas à pas pour Julien.
> À chaque étape : teste toi-même avant de livrer (tests automatiques + essai réel dans le navigateur), et rappelle en fin de message les questions sans réponse.
>
> **Ce qu'on sait déjà**
> - **Pour qui** : Julien, ses amis (et leurs amis proches) et sa guilde. **Pas un site public** pour tous les joueurs. Accès limité : piste, le même système que DOGGO-ESPORT (mot de passe commun des amis), à confirmer avec Julien.
> - **But** : un site « ultra fluide et ultra simple à utiliser », qui contient toutes les recettes et répond à la question « comment je fabrique cet objet ? ».
> - **Hébergement** : le serveur **Unraid** de Julien (petit PC chez lui, à côté de la box), comme DOGGO-ESPORT (voir plus haut).
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
> 1. Ce qu'il veut reprendre de DOGGO-ESPORT : hébergement, protection, style graphique, autre ?
> 2. 3 à 5 cas concrets : « je cherche X, je veux savoir … ».
> 3. Langue(s) du site : français, anglais, les deux ?
> 4. Plutôt téléphone, PC, ou les deux ?
> 5. Le nom du sous-domaine pour le site, et un seul mot de passe commun pour les amis et la guilde, ou un par groupe ?
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
