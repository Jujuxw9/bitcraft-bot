# Cadrage du site des recettes (étape 1, sans code)

Démarré le 2026-11-26 (session « site des recettes »). Questions posées une à la fois ; réponses de Julien notées telles quelles.

## Déjà acquis (prompt et D-064)
- Pour qui : Julien, ses amis (et leurs amis proches), sa guilde. Pas un site public.
- But : « ultra fluide, ultra simple », répondre à « comment je fabrique cet objet ? ».
- Hébergement : serveur Unraid de Julien, même chemin que DOGGO-ESPORT (voir `MISE-EN-LIGNE.txt` de ce dépôt privé).
- Code : même dépôt, dossier à part.

## Points à vérifier (relevés par Claude)
- Images des objets : tranché par Julien le 2026-11-26 (voir plus bas).
- Origine des fichiers `*_desc.json` : trouvée (voir « Données du jeu »).

## Questions et réponses
1. Ce que Julien veut reprendre de DOGGO-ESPORT ? — **Réponse (2026-11-26)** :
   - Se servir du dépôt DOGGO-ESPORT et de sa mémoire comme **modèle** : même esprit (base Python + base web ; le site récupère des données, les trie et les met en page). « Faire la même chose avec BitCraft. »
   - **Une seule version du site** : pas de version administrateur, pas de mot de passe administrateur.
   - **Pas de mot de passe pour l'instant** ; la sécurité sera vue plus tard.
   - **Style graphique propre à BitCraft** (pas celui de DOGGO-ESPORT).
   - Le réglage réseau (OVH, box, Nginx Proxy Manager) sera fait **ensemble**, pas à pas.
   - Réponses données en même temps aux autres questions : voir 4, 5 et 7.
2. 3 à 5 cas concrets ? — **Réponse (2026-11-26)** :
   - **Cas 1 — un outil** : « je cherche un outil (ex. HT3) et je veux savoir comment le fabriquer ».
   - **Cas 2 — les supplies de la guilde** : « je veux faire des supplies pour ma guilde : comment, et quoi y mettre » (la liste des supplies).
   - **Cas 3 — n'importe quel objet** : je tape un objet fabricable et j'obtiens sa **recette la plus facile**, avec la liste et les quantités.
   - **Cas 4 — ce qu'il me reste** : le nombre d'objets qui me manquent avant de fabriquer. Pour chaque objet : son **tier**, ce qu'il faut fabriquer, **la machine** (bâtiment) à utiliser, la matière transformée, l'objet obtenu, et s'il faut encore le transformer.
     - **Arbre « généalogique »** de fabrication, gardé sous les yeux.
     - Sur l'arbre : **cocher** un objet qu'on a (ou qu'on a transformé), ou **taper le nombre** qu'on a déjà. Un objet coché est grisé et validé, **avec toute sa branche en dessous**.
   - **Cas 5 — où récupérer** : « je tape *trunk* et je veux savoir où on récupère les trunks », avec les images.
   - Couverture : **tous** les objets fabricables du jeu.
3. Langue(s) du site ? — **Réponse** : **100 % anglais** (on joue en anglais). Note de Claude : la règle D-019 (français partout sauf le code) vaut pour le projet ; ici, Julien demande des textes du site en anglais. Mémoire, commentaires et commits restent en français.
4. Téléphone ou PC ? — **Réponse** : les deux. Presque tous les utilisateurs seront sur PC, mais il faut une version téléphone qui marche.
5. Sous-domaine et mot de passe ? — **Réponse** : pas de mot de passe pour l'instant (Julien accepte que toute personne qui connaît l'adresse voie le site). Sous-domaine : basé sur « BitCraft » et le nom de la guilde, **Riverwood** ; nom exact à choisir. Julien dit « comme tu l'as écrit » : Claude n'avait pas encore écrit Riverwood (incohérence signalée, sans conséquence).
6. Sites de référence ? — **Réponse** : Claude cherche lui-même (bitjita, la « database », Google).
7. Comptes ? — **Réponse** : pas de compte.

## Images des objets (réponse de Julien, 2026-11-26)
- On garde les images de brico.app pour l'instant et on les affiche sur le site.
- Claude cherche une autre source (dépôt de BitCraft, base de données du jeu, autre). « Il y a un vrai travail à faire là-dessus. » À faire pendant la proposition (étape 2).

## Données du jeu (constat de Claude, 2026-11-26)
- Le bot télécharge les tables `*_desc.json` depuis le dépôt public GitHub `BitCraftToolBox/BitCraft_GameData` (branche `cereal/cs`, dossier `static/`), mis à jour chaque jour par ses auteurs (D-033). Ce n'est pas le serveur du jeu : aucun compte, aucun jeton.
- Le site peut donc faire comme DOGGO-ESPORT : récupérer ces données tout seul, les trier, les mettre en page. Fréquence de mise à jour : à proposer.

## Point d'attention (Claude)
- Sans mot de passe, toute personne qui connaît l'adresse verra le site. Avec un sous-domaine OVH, l'adresse peut être trouvée (les certificats HTTPS sont publics). Risque faible (rien de privé sur le site), mais à garder en tête quand on verra la sécurité.
