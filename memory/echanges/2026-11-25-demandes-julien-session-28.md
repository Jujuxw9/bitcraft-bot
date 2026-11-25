# Demandes de Julien et plan d'action (session 28, 2026-11-25, Opus 5.5)

Statut : **plan validé par Julien (D-064)**, lot 1 en cours.

## 1. Bilan des journaux (fait en début de session)

Journaux lus : v0.4.9 (24/11 18:51), v0.4.11 (24/11 19:48 et 19:56), v0.4.12 (24/11 20:39), v0.4.13 (24/11 21:34 et 25/11 18:38, session de 55 min).

- Marche : connexion avant le jeu (refus à 18:38:16, connexion à 18:39:02, jeu à 18:40:01, pas d'éjection) ; réponse « @gpt » en Région à 19:18 avec faute corrigée « *Keldan », `{region}` remplacé, pas d'accolade ; le bot ne prend pas son propre message pour celui de Julien ; lignes de démarrage (amis, plafonds, 28 thèmes) ; repas toutes les 30 min ; lecture serveur 74 min sans coupure.
- Échecs : **capture à 1,8 image/s** le 25/11 (15 en v0.4.9 et v0.4.12) ; **W tenu = ~50 lignes « W : cap du cône »** en 9 s (19:43, et 24/11 18:53) ; réponses à côté de la question (« where is riverwood ? », « get to work ») ; canal lu « DESOIQI » avant « /y » ; fenêtre « CANCEL PROJECT » vue comme écran inconnu ; repas compté dans les objets d'une fabrication.
- À vérifier : Local, LFG, Trade jamais vus ; plafonds jamais atteints ; pause quand Julien écrit jamais vue (Julien a-t-il envoyé un message ?) ; hôte du serveur variable (`bitcraft-release` le 24/11, `bitcraft-early-access` le 25/11, les deux marchent) ; formule de visée toujours pas fiable.

## 2. Demandes de Julien (25/11), reformulées

### A. Fenêtre du bot
1. **Barres animées en douceur** : barres de vie, endurance, faim, métiers, barre d'action (recharges) : passage progressif (~0,3 s), jamais instantané.
2. **PERSONNAGE** : retirer le texte « clic sur le titre d'une colonne pour trier » ; BANQUES : retirer la colonne « Où » ; flèches de tri visibles sur **toutes** les colonnes (Palier masqué, Type à moitié, Quantité masqué aujourd'hui).
3. **Curseur main** seulement sur ce qui se clique (boutons, interrupteurs, quantités, titres de colonne triables). Aujourd'hui il apparaît dans les encadrés (ex. encadré Banques).
4. **VOYAGEURS** : bien, Julien doit relancer pour juger (rien à faire, à confirmer).
5. **CARTE** : filtre par **palier (tier)** des ressources, pour trouver les ressources d'un palier donné autour de soi.
6. **RECETTES** : on y revient plus tard ; à terme l'onglet quitte le bot pour un site web (voir C).
7. **PARAMÈTRES** : beaucoup moins de vide ; interrupteurs rouge/vert (comme sur BOT) à la place des cases à cocher (Sécurité, Affichage…), **sauf le journal** (cases gardées) ; taille et place de chaque partie **selon l'importance** : Se connecter / Se déconnecter et le journal en premier, Caméra (W) ensuite, Compte (e-mail, jeton) réduit car il ne change plus.
8. **Overlay** : collé **en bas de l'écran, au centre**, par-dessus la barre d'action (comme la barre des tâches Windows) ; **touche pour l'afficher ou le masquer**, choisie dans Paramètres.

### B. Nouvelles fonctions
9. **Capacités (abilities) automatiques** : lire la barre d'action (déjà fait sur le serveur), un interrupteur par capacité dans BOT ; quand Julien fait une action compatible (ex. couper du bois → Hewing Strikes), le bot appuie sur la touche de la capacité. Tenir compte de l'endurance consommée, de la recharge, de la durée d'effet. Source : `ability_custom_desc.json` (coût d'endurance, recharge, effets) et `buff_desc.json` (durée, effet) + wiki du jeu.
10. **Archive du chat en local** : tout le chat texte (Local, Région, LFG, Trade, messages privés) enregistré à chaque connexion : qui, quand, quel canal, quel texte. Petite base de données locale, dossier ignoré par Git. Servira à faire réfléchir le chatbot plus tard.

### C. Projet à part (autre session)
11. **Site web des recettes** sur le serveur Node de Julien : page HTML publique, recherche d'un objet, quantité voulue, arbre de tous les sous-objets par palier, comment les obtenir, temps, quantités exactes. Même logique que l'onglet RECETTES, en plus joli. Prompt préparé à donner à une nouvelle session.

## 3. Plan proposé

| Lot | Où | Version | Contenu |
|---|---|---|---|
| 1 | ce chat | v0.4.14 | Points 1, 2, 3, 5, 7, 8 + corrections des journaux (capture lente, W tenu) |
| 2 | ce chat | v0.4.15 | Point 10 : archive du chat (base locale, onglet de consultation) |
| 3 | ce chat | v0.4.16 | Point 9 : capacités automatiques (recherche wiki d'abord) |
| 4 | autre session | — | Point 11 : site des recettes (prompt préparé ici) ; puis retrait de l'onglet RECETTES du bot |

## 4. Questions posées à Julien (réponses à reporter ici)

Réponses de Julien (25/11 soir) :
1. Serveur : **Unraid** (petit PC chez lui), pas Node. Code du site dans le même dépôt GitHub, dossier à part. Hébergement à réfléchir dans la session du site.
2. Overlay : **pas de touche**, seulement l’interrupteur dans le bot.
3. Endurance : réserve de **30 %**.
4. Capacités : **seulement pendant que Julien fait l’action** ; récolte automatique plus tard. Interrupteur activé/désactivé comme le repas automatique.
5. Archive du chat : OK (SQLite + tableau dans CHAT).
6. Voyageurs : rien à faire, il doit relancer.
7. Ordre : lot 1, test ; lot 2, test ; lot 3. Une version par lot.

Décision : [D-064](../decisions/D-064-plan-lots-v0414-v0416-site-recettes.md).
