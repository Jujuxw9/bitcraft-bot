# D-079 — v0.4.23 : corrections de la carte, ressource recherchée, ceinture réparée, overlay avec CPU et RAM

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (retours sur la v0.4.22, 26/11 au soir, avec 3 captures)
- **Liens** : D-078, D-074, [architecture-suite.md](../technique/architecture-suite.md) section 27, `echanges/2026-11-26-demande-deplacement-carte.md`

## Contexte
Essai de la v0.4.22 (10 min) : zone de 30 cases acceptée par le serveur, RAM stable (570 à 600 Mo), barre de fabrication qui semble aller au bout (petites fabrications à revoir). Retours de Julien : boutons + et − inversés ; le cercle 30 sort du cadre quand on zoome ; hexagones un peu étirés vers le haut ; zoom voulu de 10 à 100 cases ; zones déjà visitées (visibles sur la carte du jeu) absentes ; ceinture vide ; overlay à détailler ; le « meilleur » ne sert à rien : il veut choisir la ressource qu'il cherche.

## Décision
1. **Zoom** : « + » rapproche, « − » éloigne ; 7 crans de vue : 10, 15, 20, 30, 45, 65, 100 cases.
2. **Cadre** : tout est coupé au bord de la carte (cercle, axes).
3. **Proportions** : les cases du jeu sont des hexagones réguliers ; une ligne (N) mesure √3/2 d'une colonne (E). Mesuré sur la capture de la carte du jeu (morceaux de 72 points de large pour 62,5 de haut). La limite de la zone lue devient un ovale légèrement aplati.
4. **Zones découvertes** : le bot lit une fois les morceaux de terrain déjà découverts par le personnage (table des zones découvertes) jusqu'à 130 cases autour de lui, 24 par abonnement, et les garde dans la mémoire de la carte. Ils restent ensuite sans nouvelle demande.
5. **Ressource recherchée** : à la place du « meilleur arbre ». Julien tape le nom (anglais, comme dans le jeu), la choisit dans la liste (« nom · Tn »), le choix est gardé ; la plus proche de lui (zone lue ou mémoire, pas épuisée) est entourée d'or et décrite (« Plus proche »). Le cercle « entamé » (orange) et la « cible » (rose, la cible lue à l'écran) restent.
6. **Ceinture** : réparée (les vignettes utilisaient le modèle des lignes de tableau, qui n'affiche rien sans colonnes).
7. **Overlay** : deuxième ligne « Bot : CPU x,x %, RAM n Mo · Jeu : … », CPU arrondi à 0,5 %, RAM à 5 Mo.

## Consignes de Julien notées au passage
- Si le journal montre de la **pêche ou du minage sans les capacités** (touches de capacité ou Break the Roots), le signaler à Julien.
- Riverwood (`/c1`) : la liaison en direct s'essaie **ensemble**, plus tard.
- **« Se déplacer ici »** depuis la carte du bot : **prochaine étape, très importante**, dès que la carte est stable.

## Conséquences
- Tests 635/635. Pas d'essai serveur (jeu ouvert) : à vérifier par Julien.
