# D-077 — Site des recettes : proposition validée

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (session « site des recettes »)
- **Liens** : D-064, [cadrage](../site-recettes/cadrage.md), [proposition](../site-recettes/proposition.md), [recherches](../site-recettes/recherches.md)

## Contexte
Cadrage du site des recettes fait le 26/11 (5 cas concrets de Julien). Claude a écrit une proposition ; Julien la valide sans la lire en détail.

## Décision
1. **La proposition** (`site-recettes/proposition.md`) est validée : recherche instantanée, page d'objet avec arbre de fabrication, cases à cocher et « j'en ai déjà » (toute la branche grisée), « Still needed », « Where to get it », « Used in », pages Claim supplies et Tools ; coches gardées dans le navigateur ; pas de compte, pas de mot de passe pour l'instant.
2. **Recette la plus facile** : écarter les recettes qui améliorent ou recyclent un objet de même famille, puis prendre celle qui demande le moins de matières premières ; à égalité, la machine de plus bas tier. Les autres recettes restent dans un menu.
3. **Arbre** (ajout de Julien) : **zoom et dézoom**, la quantité bien lisible sous chaque objet, et la conversion en **stacks** (stack d'inventaire pour les objets, stack de cargo pour les cargos). Taille d'un stack : « À vérifier » dans les données (volume de l'objet et volume d'une case).
4. **Textes du site 100 % anglais** (exception à D-019 pour le site seulement ; mémoire, commentaires et commits restent en français).
5. **Sous-domaine** : `bitcraft-riverwood` (Riverwood = la guilde). Le nom de domaine complet reste hors du dépôt public.
6. **Dossier du code** : `website-recipes/` dans le dépôt `bitcraft-bot`. Mémoire : `memory/site-recettes/`.
7. **« HT3 »** = la hache T3 (Emarium Axe), d'après Julien.
8. Style propre à BitCraft. Images de la communauté (brico.app), gardées sur le serveur Unraid.

## Conséquences
- Étape suivante : maquette cliquable sur ~20 objets (hache T3, supplies, trunk compris), testée par Claude puis par Julien.
- Le script de déploiement ne contiendra aucune adresse privée : elles iront dans un fichier local ignoré par Git.
