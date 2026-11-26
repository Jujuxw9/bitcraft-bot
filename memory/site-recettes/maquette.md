# Maquette du site des recettes (étape 3)

Démarrée le 2026-11-26. Méthode de Julien : besoin → plan (contraintes techniques séparées) → recherches → réalisation → tests → GitHub.

## 1. Besoin (tiré des demandes de Julien, D-077)
| # | Besoin | Vient de |
|---|---|---|
| B1 | Chercher un objet en tapant quelques lettres, fautes tolérées, résultat pendant la frappe | proposition |
| B2 | Voir la recette la plus facile d'un objet, liste et quantités | cas 3 |
| B3 | Arbre de fabrication : tier, machine (bâtiment et tier), métier, matière transformée, objet obtenu | cas 4 |
| B4 | Arbre zoomable et dézoomable, quantité lisible sous chaque objet, conversion en stacks | D-077 |
| B5 | Cocher un objet (grise sa branche) ou dire combien on en a déjà ; « Still needed » recalculé | cas 4 |
| B6 | Où récupérer une matière première (ressource, outil, niveau, images) + lien vers la carte de bitjita | cas 5, réponse du 26/11 |
| B7 | Liste des supplies du claim | cas 2 |
| B8 | Outils par métier et par tier (ex. hache T3) | cas 1 |
| B9 | Marche sur PC et sur téléphone ; 100 % anglais ; style BitCraft | cadrage |

Périmètre de la maquette : ~20 objets cibles et tout ce qu'il faut pour les fabriquer. Pas encore de Docker, pas de mise en ligne.

## 2. Plan et contraintes techniques
| Partie | Contrainte | Solution retenue |
|---|---|---|
| Données | Tables publiques `BitCraft_GameData`, figées sur un commit | Script Python : télécharge, trie, écrit un seul fichier de données pour la page |
| Variantes | Un même objet existe en plusieurs raretés (5 « Emarium Axe ») | Objets regroupés par nom et par genre (objet ou cargo) |
| Recette la plus facile | Recettes en boucle (« Recraft », paquets) ; la hache T3 demande une hache T2 | Coût = nombre de matières premières, calculé jusqu'à stabilité : une boucle ne fait jamais baisser le coût, donc les « Recraft » sont écartés d'office. Égalité : machine de plus bas tier |
| Matière première | Un objet récoltable peut parfois aussi se fabriquer | Récoltable = matière première ; sans recette ni récolte = « autre source » |
| Quantités | Une recette peut donner plusieurs objets à la fois | Nombre de fabrications arrondi au-dessus |
| Stacks | Case d'inventaire du joueur : volume 6 000 (code serveur, `player_create.rs`) ; une case cargo : 1 cargo | Stack = 6 000 ÷ volume (planche : 600 → 10). **À vérifier en jeu** |
| Bâtiment | Le jeu donne un type et un tier | Nom réel du bâtiment de ce tier (`building_desc`, ex. « Sturdy Smithing Station ») |
| Images | Pas de source officielle ; le bot a déjà 2 346 images | Images du bot réutilisées, les manquantes prises sur brico.app ; dossier non suivi par Git |
| Arbre | Grand, doit rester lisible | Arbre dessiné de haut en bas, zoom (molette, boutons), déplacement (glisser), branches repliables ; vue liste en retrait sur téléphone |
| Coches | Pas de compte | Gardées dans le navigateur |
| Calculs | Doivent être justes | Code de calcul séparé, testé automatiquement (Node.js et Python) |

## 3. Objets cibles
Hache T3 (Emarium Axe), Emarium Pickaxe, Luminite Machete, Sturdy Timber, Sturdy Brick Slab, Sturdy Cloth Tarp, Emarium Frames, Infused Experimental Compounds, Sturdy Plank, Emarium Ingot, Sturdy Rope, Sturdy Leather, Sturdy Wood Trunk (matière première), Simple Timber, Rough Brick Slab, Fine Timber, Pyrelite Saw, Emarium Hoe, Sturdy Cloth, Fine Plank.

## 4. État (2026-11-26 soir)
- **Maquette faite**, testée par Claude : 33 tests automatiques passent ; essai réel dans le navigateur (PC et taille téléphone). Fonctionnement : [architecture.md](architecture.md).
- Elle couvre **tout le jeu** (6 122 objets, 2 153 fabricables), pas seulement 20 objets : même effort, plus utile. Les 20 objets cibles sont mis en avant sur l'accueil.
- Corrections trouvées pendant les essais : objets « Output » du jeu, paquets en boucle, butin rare (arme de pirate) choisi à tort, troncs donnés par l'arbre abattu, achats chez Rumbagh, totaux comptés deux fois, chemin d'image dangereux.
- **Pas testé** : navigateur Brave (essai fait dans le navigateur intégré de Claude), vrai téléphone, stacks en jeu.
- **À vérifier par Julien** : taille d'un stack (planche : 10 par case ?) ; recettes proposées pour ses objets habituels.
