# D-062 — v0.4.12 : recettes simples sans paquets, plan d'actions, voyageurs en trois listes

- **Statut** : Validée
- **Date** : 2026-11-24
- **Validée par** : Julien (message du 2026-11-24, « fais la liste, prépare un plan d'action, fais les mises à jour »)
- **Liens** : D-043 (onglets par usage), D-046 (interface BitCraft)

## Décision
1. **Recettes** : pour un objet, seule la façon **la plus simple** est montrée d'abord (récolte, puis peu d'ingrédients, métiers bas, sans connaissance à débloquer) ; les autres sont dans « Autres façons ». **Tout ce qui passe par un paquet** (« Package », « Unpack ») est écarté.
2. **Arborescence et plan** : pour une quantité choisie, l'arbre des ingrédients (récolte ou fabrication, jusqu'aux ressources de base) et la liste des actions dans l'ordre, avec une case à cocher (non gardée à la fermeture du bot).
3. **Voyageurs** : trois listes : à rendre sans farmer (tout dans le sac, ou à aller chercher en banque / ceinture), les 10 plus avancées (en % d'objets déjà en stock), les plus faciles à farmer (récolte seulement, puis 1 fabrication, puis plus ; source inconnue en dernier). Avec les images des objets et un portrait par voyageur (initiale dorée si l'image n'existe pas).
4. **Personnage** : sac, ceinture et banques en trois listes, colonnes triables. **Bonus actifs** : sur l'onglet BOT. **Interrupteurs** vert (OUI) / rouge (NON) pour le repas auto et le chat « @gpt » (carte « Options »). **Paramètres** : 7 cartes ramenées à 4.

## Limites connues
- Le serveur n'a aucune source pour certains objets (ex. « Dried Basic Flowers » : aucune récolte, fabrication ni conversion dans les données publiques) : l'effort est « source inconnue », pas une erreur.
- Les conversions d'objets (`item_conversion_recipe_desc`) et les butins de ressources (`on_destroy_yield`) ne sont pas encore lus : **À vérifier**, à ajouter si des objets restent sans source.
