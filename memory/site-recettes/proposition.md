# Proposition du site des recettes (étape 2) — validée (D-077)

Écrite le 2026-11-26 d'après le [cadrage](cadrage.md) et les [recherches](recherches.md). Validée par Julien le 2026-11-26 : [D-077](../decisions/D-077-site-recettes-proposition.md), avec les ajouts ci-dessous.

**Ajouts de Julien** : arbre avec zoom et dézoom ; quantité bien lisible sous chaque objet ; conversion en stacks (inventaire ou cargo).

## 1. Parcours de l'utilisateur
1. Il ouvre le site : une **grande barre de recherche** au centre, rien d'autre.
2. Il tape quelques lettres (« emar axe », « trunk », « timber ») : la liste se remplit **pendant la frappe**, avec image, nom, tier. Fautes tolérées.
3. Il clique un objet : la **page de l'objet** s'ouvre.

## 2. Écrans
- **Accueil** : la recherche, plus 2 raccourcis : « Claim supplies » et « Tools ».
- **Page d'un objet** (le cœur du site) :
  - en haut : image, nom, tier, rareté, catégorie ; champ **quantité** voulue ;
  - **la recette la plus facile**, choisie d'office (les autres recettes dans un menu) ;
  - **l'arbre de fabrication** : chaque case montre l'image, le nom, le tier, la quantité, **la machine** (bâtiment et son tier) et le métier. Sur PC, l'arbre se lit de haut en bas, branches repliables. Sur téléphone, il devient une liste en retrait (même contenu) ;
  - sur chaque case : **une case à cocher** (« je l'ai ») et **un champ « j'en ai déjà »**. Cocher grise la case **et toute sa branche** ; un nombre déjà possédé réduit ce qu'il faut en dessous ;
  - à côté (ou en dessous sur téléphone) : **« Still needed »**, la liste des matières premières restantes, recalculée à chaque coche ;
  - **« Where to get it »** pour une matière première : quelle ressource récolter (ex. tel arbre pour les trunks), l'outil, son niveau, le métier et le niveau requis, avec les images ;
  - **« Used in »** : les recettes qui ont besoin de cet objet.
- **Claim supplies** : les 64 objets qui donnent des supplies au claim, avec le nombre de supplies de chacun et un lien vers leur recette. Tri par « supplies par matière première » pour trouver le plus rentable.
- **Tools** : les outils par métier et par tier (Axe, Pickaxe, Hoe…), un clic donne la recette.

Les coches et les nombres sont gardés **dans le navigateur** de chacun (pas de compte, rien sur le serveur). Bouton « Reset » pour tout effacer.

## 3. Règle de la « recette la plus facile » (à valider)
Le jeu a souvent plusieurs recettes pour un objet. Exemple vu sur un autre site : une machette T5 calculée avec une recette qui demande 2 haches et 60 morceaux d'outils, pour 591 267 objets au total. Proposition :
1. écarter les recettes qui consomment un objet de même famille (amélioration d'un outil, recyclage) ;
2. puis prendre celle qui demande le **moins de matières premières au total** ;
3. à égalité, celle de **plus bas tier** de machine.
L'utilisateur peut toujours choisir une autre recette dans le menu.

## 4. Données
- Source : le dépôt public `BitCraftToolBox/BitCraft_GameData` (mis à jour chaque jour), comme le bot. Aucune connexion au serveur du jeu, aucun compte.
- Tables utilisées : objets, cargos, recettes de fabrication, recettes de récolte, ressources, bâtiments, métiers, outils, conversions, **supplies** (`building_repairs_desc`, pas encore téléchargée par le bot).
- **Mise à jour automatique** : le serveur regarde une fois par jour si le dépôt a changé ; si oui, il recalcule le fichier du site, le vérifie, puis le remplace. En cas d'erreur, l'ancienne version reste.
- **Images** : téléchargées une fois depuis la source communautaire (brico.app ou bitjita), gardées sur le serveur Unraid et servies par notre site (rapide, ne dépend pas d'eux). Images des bâtiments aussi. Aucune source officielle publique (voir recherches).
- **Où trouver** : la ressource à récolter, l'outil et le niveau sont dans les données. **Le biome ou l'endroit sur la carte n'y sont pas** : « À vérifier » (piste : un lien vers la carte de bitjita).

## 5. Hébergement (modèle DOGGO-ESPORT)
- Un **conteneur Docker sur Unraid** : petite image Python. Le serveur Python sert la page, le fichier de données, les images, et fait la mise à jour quotidienne.
- Le code reste dans `appdata` du serveur ; déploiement depuis le PC par le partage réseau, avec un script qui attend la confirmation. **Accord de Julien avant chaque déploiement.**
- Accès de l'extérieur : un sous-domaine OVH → box (80 et 443) → Nginx Proxy Manager → le site. Procédure écrite pas à pas, faite **ensemble** (voir `MISE-EN-LIGNE.txt` de DOGGO-ESPORT).
- Pas de mot de passe pour l'instant (Julien). En-têtes de sécurité et « pas d'indexation par les moteurs de recherche » quand même.
- Code dans le dossier `website-recipes/` du dépôt (D-077). Sous-domaine : `bitcraft-riverwood`.

## 6. Style
- Textes **100 % anglais**. Style BitCraft : fond sombre, tons bois et parchemin, accents dorés, couleurs de tier comme dans le jeu. Pas de paragraphe d'explication : l'aide derrière un bouton « i ».

## 7. Tests
- Tests automatiques : calcul des quantités (arbre, coches, « j'en ai déjà »), choix de la recette la plus facile, les 5 cas de Julien, chargement des données.
- Essai réel dans le navigateur, sur PC et en taille téléphone, avant chaque livraison.
- À chaque livraison : ce qui a été déployé et ce qui n'a pas été testé.

## 8. Suite
1. Julien valide cette proposition → décision D-NNN.
2. **Maquette** cliquable sur ~20 objets (dont une hache T3, des supplies, un trunk), lancée sur le PC.
3. Site complet, puis mise en ligne sur Unraid.
