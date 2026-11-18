# Architecture — brique Server et voie hybride (H1)

Suite de [../architecture.md](../architecture.md) (sections 1 à 16 : vision, HUD, données du jeu). Découpé le 2026-11-18 (fichier trop long).

## 17. Voie hybride (D-039, 2026-11-17) : nouvelle brique Server

- Sixième brique : **Server** (`src/BitcraftBot.Server`). Rôle : lire en direct l'état du jeu sur le serveur officiel, **en lecture seule**, avec le jeton de Julien.
- Elle remplacera peu à peu la **Perception** comme source principale (position, arbres, vie, endurance, faim). La Perception reste pour la sécurité et le contrôle croisé.
- La brique n'envoie **aucune action** au serveur. Les actions restent dans **Input** (souris, clavier).
- Le jeton est chiffré avec la protection de Windows (lié à la session de Julien), jamais écrit dans le journal ni dans Git.
- État : **v0.3.10**, lecture vérifiée en réel (sections 18 à 23).

## 18. v0.3.5 — H1 : lecture du serveur (2026-11-18)

Nouvel onglet **« Serveur (H1) »**. Le bot lit le serveur officiel en direct, sans rien envoyer d'autre que des demandes de lecture.

```
  Julien : e-mail ──► service de connexion BitCraft ──► code par e-mail ──► jeton
                                                                             │ chiffré par Windows
                                                                             ▼  (%LOCALAPPDATA%\BitcraftBot\jeton.bin)
  Brique Server :  schéma public ─► liaison chiffrée avec bitcraft-live-18 ─► « qui suis-je ? »
                   ─► abonnement 1 : mon personnage (numéro d'entité)
                   ─► abonnement 2 : position, vie, endurance, faim, connecté ou non,
                                     + ressources dans un carré de 80 × 80 cases autour de moi
                   ─► le serveur envoie ensuite chaque changement ; copie locale à jour
```

| Élément | Rôle |
|---|---|
| Connexion au compte | Demande un code pour l'e-mail, échange le code contre le jeton. Le jeton n'est jamais dans le journal. |
| Coffre du jeton | Chiffrement Windows lié au compte de Julien. Bouton « Oublier le jeton ». |
| Liaison | WebSocket chiffré, protocole JSON de SpacetimeDB. **Ne sait pas** envoyer d'action (un test automatique le vérifie). |
| Schéma | Donne le nom de chaque colonne : les lignes arrivent en simples listes de valeurs. |
| Copie locale | Tables reçues, mises à jour à chaque changement (suppression puis ajout). |
| Instantané | Résumé du personnage + arbres triés par distance, noms et paliers tirés de BitCraft_GameData. |

Règles de sécurité de la liaison :
- **Ordre** : se connecter avant de lancer le jeu. Si le jeu tourne déjà, le bot demande confirmation (risque de déconnexion du personnage à la fermeture du bot).
- **Pas de reconnexion automatique** si la liaison tombe : elle ferait du bot la connexion principale.
- Le carré des ressources est redemandé quand le personnage s'est éloigné de 20 cases (au plus une fois toutes les 5 s).
- Si le serveur refuse la requête des ressources, le bot continue sans elles (journal : « Abonnement refusé »).

Comparaison écran / serveur : toutes les 15 s, le journal note les deux positions (pour trouver la conversion en H2).

## 19. v0.3.6 — découverte de la région du personnage (2026-11-18)

Avant de lire la région, si le jeu est fermé, le bot ouvre une liaison courte avec le module **global** (`bitcraft-live-global`) et demande : le compte a-t-il un personnage ? dans quelle région ? à quelle adresse ? Puis il ferme cette liaison et lit la bonne région. Si le jeu tourne, il saute cette étape (risque de déconnexion) et lit la région saisie. Si une région ferme la liaison avant le moindre message, le bot l'explique (personnage absent ou connexion du compte trop ancienne).

## 20. v0.3.7 — modules du monde 1.0 (2026-11-18)

Les régions du monde 1.0 s'appellent `idc-N` (R18 → `idc-18`) et le module global `idc-global`. Les versions 0.3.5 et 0.3.6 visaient `bitcraft-live-*`, un ancien monde : le serveur refusait le compte. Seuls ces deux noms changent.

## 21. v0.3.8 — journaux de liaison détaillés (2026-11-18)

Pour déboguer la connexion sans être devant le PC, le journal (catégorie « Serveur ») note maintenant :
- le jeton **sans le secret** : compte, date d'émission, âge, fin de validité ;
- chaque liaison (`[liaison idc-global]`, `[liaison idc-18]`) : adresse, durée de la poignée de main, code HTTP, sous-protocole accepté, en-têtes du serveur (sans ceux qui contiennent un jeton) ;
- en cas de refus : code HTTP, code d'erreur, cause ;
- les 5 premiers messages reçus : type, taille, extrait (jeton masqué) ; les suivants : type et taille (niveau « Détails ») ;
- chaque abonnement : numéro, raison, requêtes SQL ;
- la fin de liaison : durée, nombre de messages, octets reçus, statut de fermeture.

## 22. v0.3.9 — connexion refusée si le jeu est lancé (2026-11-18)

- Essais réels v0.3.7 et v0.3.8 : la lecture marche ; connecter le bot **avec le jeu ouvert éjecte le jeu** (« Logged in elsewhere »).
- Le bouton « Se connecter » refuse donc toute connexion si le processus BitCraft tourne, sans dérogation. La découverte par le module global se fait toujours (jeu fermé).
- « Se déconnecter » ne pose plus de question : le bot est toujours connecté avant le jeu, sa fermeture ne touche pas le jeu.
- Le journal compare toutes les 15 s la position écran et la position serveur ÷ 3, avec l'écart.

## 23. v0.3.10 — confort (2026-11-18)

- Fenêtre **toujours agrandie** au démarrage, sur l'écran où elle était.
- Onglet Journal : les 4 niveaux (erreurs, alertes, infos, détails) cochés par défaut ; la liste reste **collée à la dernière ligne** (à chaque ajout, après un filtre, à l'ouverture de l'onglet).
- Versions rangées par `tools/publier-version.ps1` dans `artifacts/release` (dernière) et `artifacts/old` (D-040).

## 24. v0.3.11 — H2 : carte des arbres (2026-11-18)

- **Coordonnées** : chaque position du serveur est traduite en coordonnées du jeu, E = arrondi(x ÷ 3), N = arrondi(z ÷ 3). Les distances sont données dans ces mêmes unités (« cases du jeu »).
- **Santé des arbres** : nouvelle demande de lecture `resource_health_state` (une ligne par ressource, créée à son apparition). Si le serveur la refuse, le bot réessaie sans la santé, puis sans les ressources.
- **Carte** (onglet Serveur, carte 4) : nord en haut, est à droite, vous au centre, cercles à 5 et 10 cases. Arbres mûrs en vert foncé, jeunes en vert clair, morts en brun, entamés entourés d'orange. Repères : meilleur arbre (jaune), cible à l'écran (rose), sélection (blanc). Un clic sur un point le sélectionne dans le tableau.
- **Tableau** : repère (★ meilleur, ◎ cible), nom, âge (jeune, mûr, mort, planté…), santé actuelle / max (%), distance, direction (N, NE… SO, O, NO), position N / E, palier. Mis à jour sur place : la sélection et le défilement restent.
- **Meilleur arbre** : l'arbre mûr le plus proche, jamais le Maker's Tree (D-036). C'est celui que H3 visera.
- **Cible à l'écran** : la cible lue sur le HUD (nom + santé) est retrouvée parmi les arbres du serveur ; même santé = sûr, même nom seulement = probable. Écrit dans le journal à chaque changement. Sert à vérifier H2 en jeu.
- **Zone lue** : centre, rayon (≈ 13 cases), heure de la dernière relecture ; le journal explique chaque relecture (distance parcourue).
- Tests : 200 OK (23 nouveaux : coordonnées réelles, direction, âge, santé, requêtes). Table de santé présente et publique dans le vrai schéma de `idc-18`.

## 25. v0.3.12 — onglet Carte, familles, cible stable (2026-11-18)

- Demandes de Julien après l'essai v0.3.11 : un onglet à part pour la carte, des cases à cocher par type de ressource.
- **Onglet « Serveur (H1) »** : compte, connexion, personnage, instantané. **Onglet « Carte (H2) »** : bandeau (votre position ±1, vie, endurance, satiété, région), cases par famille, carte agrandie (560 px), tableau avec la colonne « Famille ».
- **Familles** : les ~80 catégories du jeu regroupées en 12 familles (arbres, rochers et pierre, minerais, fleurs, plantes à fibres, baies et fruits, champignons, céréales et légumes, sable / argile / terre, pêche, obstacles, autres). Chaque case montre le nombre lu ; chaque famille a sa couleur, la même sur la carte. Boutons « Tout » et « Arbres seulement ».
- **Cible à l'écran** : ressources du même nom à moins de 8 cases seulement ; le serveur peut avoir jusqu'à 3 % d'avance sur l'écran ; « sûr » seulement si une seule ressource colle ; sinon on garde la précédente. Le journal note chaque changement de cible ou de certitude.
- Tests : 219 OK (19 nouveaux : familles, cas réels de la cible).
