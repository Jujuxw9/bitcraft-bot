# Analyse : la visée des arbres, après l'essai R1 (2026-11-22)

Rédigé en session 17, à la demande de Julien : « je veux que ce soit fiable, pas efficace ; pas de loupés, pas d'erreurs ».

## 1. Ce que dit l'essai R1 (v0.4.0, 22/11, 20:51 → 21:07)

- 23 récoltes à la main, 17 clics appris, 2 clics refusés (personnage en marche), 2 clics sans récolte, une vingtaine de clics refusés (caméra tournée : bouton du milieu, molette). Le tri des clics a bien marché.
- **Mais la formule n'a pas marché.** La carte affichait « précision 18 px » : c'était trompeur. Seuls **4 clics sur 17 étaient gardés**. Avec 4 clics, la formule passe presque exactement par eux : la précision ne veut rien dire.
- Le vrai test, « formule d'avant : écart », a donné **148 à 3 375 px** : la formule ne prévoyait jamais le clic suivant.
- Corrigé en v0.4.1 : « PAS FIABLE » tant que moins de 8 clics sont gardés ou que plus d'un tiers est écarté. Les 17 vrais clics donnent bien « pas fiable » (test automatique).

## 2. Pourquoi : trois causes

1. **W ne remet pas la caméra dans une direction fixe du monde.** Des arbres au nord (à 21 cases) et au sud (à 7 cases) apparaissent tous **en haut** de l'écran, donc devant la caméra. C'est impossible avec une caméra qui regarde toujours dans la même direction. W remet sans doute la caméra **derrière le personnage** (À vérifier, Q-025). La direction change donc à chaque fois.
2. **La vue est très rapprochée.** Après W, l'arbre remplit l'écran et on voit l'horizon (capture du 22/11 à 21:02:46). Julien clique sur le feuillage, **tout en haut de l'écran** (16 clics sur 17 dans le tiers haut). Un clic sur le feuillage peut être 5 à 10 m au-dessus du pied de l'arbre : sa place à l'écran ne dit presque rien de la position au sol.
3. **Les arbres se cachent entre eux.** En vue rapprochée, un arbre proche cache ceux de derrière. Un clic peut toucher un autre arbre que celui visé.

## 3. Ce qu'on a trouvé d'utile sur les captures

- **La mini-carte reste orientée nord** (les formes du terrain ne tournent pas d'une capture à l'autre).
- **Un cône clair** part du personnage sur la mini-carte : il montre où regarde la caméra. Il tourne d'une capture à l'autre (vu sur 4 captures, directions différentes). Le bot peut donc **lire la direction de la caméra à l'écran**, à chaque image.
- **Le jeu entoure en vert l'objet survolé** par la souris (capture du 22/11 à 21:02:54 : contour vert autour du tronc). Le bot peut vérifier « ma souris est sur un arbre » **avant de cliquer**.
- **Le serveur dit quel arbre est frappé** (déjà utilisé en R1) : vérification **après** le clic, sans erreur possible.

## 4. Méthode fiable proposée : viser, vérifier, cliquer, vérifier

Principe : aucune étape ne fait confiance à la précédente. Chaque étape se vérifie, et en cas de doute le bot **ne clique pas**.

| Étape | Ce que fait le bot | Vérification | Si raté |
|---|---|---|---|
| 0. Prêt ? | Écran « en jeu » (v0.4.1), aucune fenêtre du jeu ouverte, endurance pleine (D-028), personnage arrêté | Écran + serveur | Attendre |
| 1. Choisir | Le vieil arbre le plus proche (serveur, D-034), à moins de ~8 cases, sans ennemi autour (R7) | Serveur | Arbre suivant |
| 2. Orienter | W, attendre 1 s (D-053), lire la direction de la caméra sur la mini-carte | Écran : cône lu 2 fois de suite pareil | Ne pas viser |
| 3. Prévoir | Formule : position de l'arbre au sol + direction de la caméra → pixel du **pied du tronc** | Formule « fiable » seulement | Ne pas viser |
| 4. Survoler | Souris au point prévu (geste humain), sans cliquer | Contour vert à l'écran autour du curseur | Petite recherche autour (quelques points), sinon arbre suivant |
| 5. Cliquer | Clic droit | — | — |
| 6. Confirmer | Le serveur dit-il que le personnage frappe **cet** arbre dans les 10 s ? | Serveur (identité exacte) | Annuler (clic gauche à côté du personnage, D-036), noter le raté |
| 7. Couper | Attendre la fin (arbre abattu, endurance vide) | Serveur | — |

Garde-fous en plus : 3 ratés de suite → arrêt et message ; au plus 2 essais par arbre ; jamais de clic si l'écran n'est pas « en jeu » ; toutes les actions passent par le contrôleur d'actions (D-045).

## 5. Ce que ça change pour l'apprentissage (R1)

- Les clics de Julien restent des **indices** (D-049), mais il faut noter **la direction de la caméra** au moment de chaque clic (lue sur la mini-carte). Sans elle, ils ne servent à rien : les 17 clics actuels sont à jeter (`camera-clics.json` → « Oublier les clics » quand la nouvelle version sera là).
- La formule devient : « tourner la position de l'arbre selon la direction de la caméra », puis la même formule qu'aujourd'hui (le zoom et l'inclinaison sont fixes après W).
- Pour apprendre le **pied** de l'arbre et pas le feuillage, la meilleure source sera le bot lui-même (R3) : il survole, le contour vert apparaît ou pas, et le serveur confirme. Ses propres essais vérifiés valent mieux que les clics de Julien.

## 6. Prochaines étapes (ordre proposé)

Mise à jour du 2026-11-22 après les réponses de Julien (D-052) : le cône est confirmé, pas de touche d'interaction, le panneau du haut ne change pas au survol, fenêtres du jeu à détecter : oui.

1. **Essai de la v0.4.2** : fait le 22/11 (accueil, chargement, en jeu reconnus ; connexion corrigée en v0.4.3 ; herbe baissée, Q-026).
2. **R1 bis — direction de la caméra et fenêtres ouvertes** (v0.4.3 faite le 23/11, à essayer par Julien) : afficher « caméra vers le nord-est (45°) » (cône de la mini-carte) et « fenêtre ouverte : inventaire » ; essai de Julien en tournant la caméra et en ouvrant des fenêtres.
3. **R1 ter — réapprendre la formule avec la direction** (v0.4.4, aucune action) : mêmes clics de Julien, direction notée à chaque clic.
4. **R2 — dessiner les arbres du serveur sur l'image du jeu** (aucune action) : la preuve visuelle.
5. **R3 — survoler sans cliquer** + détection du contour vert.
6. **R4 — un clic, une coupe, vérifiée par le serveur.** Puis R5 (boucle), R6 (roches), R7 (ennemis).

## 7. Améliorations et corrections possibles (hors visée)

- **Écrans d'accueil et de connexion** : mots-clés supposés (v0.4.1) ; à corriger avec les captures de Julien.
- **Fenêtres du jeu ouvertes** (inventaire, carte) : les détecter avant toute action : **validé (D-052)**, prévu en v0.4.3. Les positions des fenêtres sont rangées par le jeu (`WindowOffset`, voir `../reglages-du-jeu.md`).
- **Réglages du jeu** : 4 réglages gênent la visée (Q-026), vérifiés au démarrage depuis la v0.4.2.
- **Liaison serveur** : le 22/11 à 20:51, le module global **et** la région idc-18 ont refusé la liaison dès l'ouverture ; 70 s plus tard (20:53), tout a marché (global accepté, région sur `bitcraft-release`). Cause inconnue (À vérifier). Pas de reconnexion automatique (règle du projet) : si ça se reproduit, le bot pourrait proposer « Réessayer dans 1 min ».
- **Overlay** : afficher sur le jeu ce que le bot voit et ce qu'il va faire (aide pour les essais).
- **Rangement de la mémoire** : agent mémoire (`tools/agent-memoire/`) à lancer de temps en temps.

## 8. Conclusions après la session du 23/11 (19 coupes, 17 clics appris avec la direction)

- **Le cône de la mini-carte ne bouge pas après W** (observation de Julien, confirmée par le journal) : W à 19:57:06 avec un cône à 224°, puis 232° pendant 8 coupes ; W à 20:03:14 avec 211°, puis 213°. Le cône tourne bien avec le bouton du milieu.
- **Les clics ne collent pas avec le cap lu après W** : avec un cap « nord » (18:58 à 19:01), des arbres **derrière** le personnage (au sud-ouest) sont cliqués en haut de l'écran ; même chose vers 232°. Donc, après W, le cône donne l'ancienne direction, pas la vraie.
- **Hypothèse** (À vérifier) : W met la caméra derrière le personnage. La pointe de la goutte suit le personnage. Après W, la vraie direction serait donc celle de la **goutte**, et non celle du cône. La v0.4.4 garde une capture 1,5 s après W pour le vérifier.
- **Conséquence pour la visée** : ne pas se fier au cône après W. Pistes : (a) ne plus utiliser W et tourner la caméra au bouton du milieu (le cône suit) ; (b) après W, un petit mouvement du bouton du milieu pour forcer la mise à jour du cône ; (c) lire la pointe de la goutte. À trancher avec Julien après les captures.
- Données encore trop peu nombreuses et bruitées pour la formule (v0.4.4 R1 ter reportée) : un clic sur le feuillage peut toucher un autre arbre que celui visé.
