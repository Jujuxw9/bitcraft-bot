# D-042 — Repas automatique avec Maj + E

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien (« si je suis à moins de 30 % de nourriture, tu manges automatiquement, et si je n'ai plus de boost de nourriture il faut aussi manger. Mange 1 item par 1 item, en faisant une pause entre les 2, et en vérifiant de combien est l'item par rapport à mon manque »)
- **Liens** : remplace D-032 (seuil de 50 % et clic sur la case 5) ; D-030, D-028, D-041

## Décision
Le bot appuie sur **Maj + E** (« Quick Eat » : mange le premier aliment du sac) dans deux cas :
1. **Faim sous 30 %** de la faim maximale. Il mange alors un aliment à la fois, **tant que l'aliment ne dépasse pas le manque** (ex. manque 40, poisson grillé 25 : il mange ; manque 20 : il s'arrête).
2. **Plus de bonus de nourriture** (« Food Regen ») : il mange **un** aliment, même si la faim est haute.

Entre deux aliments : attendre que le serveur confirme (faim en hausse ou bonus revenu), puis une pause de 8 à 14 s.

## Données utilisées (lecture seule)
- Serveur : `satiation_state` (faim), `character_stats_state` (faim maximale, case 19), `active_buff_state` (bonus actifs), sac (`inventory_state`).
- Données du jeu : `food_desc` (faim rendue par aliment), `buff_desc` et `buff_type_desc` (les 11 niveaux de « Food Regen »).

## Sécurités
- ~~Désactivé par défaut~~ → **actif à chaque démarrage** depuis la v0.3.18 (demande de Julien : « manger automatiquement doit être activé de base »). La case « Manger (Maj + E) » est dans l'onglet Bot ; décochée ou coupée par Num 5, elle le reste jusqu'à la fermeture du bot.
- **Pas de nourriture dans le sac** : alerte dans le journal (une fois), et « Pas de nourriture » dans les alertes du tableau de bord (v0.3.18).
- Depuis la v0.3.18, Maj + E passe par le contrôleur d'actions commun (D-045).
- Seulement si BitCraft est la fenêtre active et visible, le personnage connecté dans le jeu, et si Julien n'a pas tapé au clavier depuis 3 s (chat).
- Si le premier aliment du sac est une potion, un thé ou un élixir, le bot ne mange pas et le signale.
- Au plus 8 aliments en 10 min. Deux échecs de suite (rien ne change) : pause de 5 min.
- **Num 5** décoche le repas automatique.

## À vérifier en jeu
- « Premier aliment » de Maj + E = premier aliment du sac, dans l'ordre des cases ?
- Peut-on manger à faim pleine (cas 2) ?
- Format réel des bonus actifs dans le journal du serveur.

## Conséquences
- C'est la première action du bot sur le jeu en dehors du banc d'essai (D-035).
- La barre d'action (case 5) n'est plus nécessaire pour manger.
