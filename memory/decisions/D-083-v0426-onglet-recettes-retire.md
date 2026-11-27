# D-083 — v0.4.26 : onglet Recettes retiré, ce qui sert ailleurs est gardé

- **Statut** : Validée (tâche de nuit T-06, d'après la demande de Julien du 27/11)
- **Date** : 2026-11-27
- **Validée par** : Julien (« il faut supprimer l'onglet Recettes du bot, il ne sert plus à rien » ; fiche `taches-planifiees/T-06-v0426-sans-recettes.md`, D-081)
- **Liens** : D-077 (site des recettes), D-078 (barre de fabrication), D-066 (matières premières), [architecture-suite.md](../technique/architecture-suite.md) section 30

## Contexte
Les recettes ont leur propre site (D-077, `website-recipes/`). L'onglet RECETTES du bot ne sert plus. Julien : tout ce qui sert ailleurs doit rester.

## Décision
1. **Retirés** : la page RECETTES (recherche, boutons Rechercher / Effacer / Retour, quantité, liste, fiche en logigramme) et le dessin de la fiche (`RecipeView`), qui ne servaient qu'à l'onglet.
2. **Gardés**, car utilisés ailleurs :
   - le **livre des recettes** (`RecipeBook`), toujours chargé au démarrage : noms et tiers des objets (onglet Personnage : sac, ceinture, banques), nombre d'actions des fabrications (barre de fabrication du tableau de bord, D-078), tâches de voyageur ;
   - le **planificateur** (`RecipePlanner`) et les **matières premières** (`RawMaterials`) : effort et matières premières de l'onglet Voyageurs ;
   - les **images des objets** (`ItemIcons`, téléchargement unique au démarrage) : Personnage et Voyageurs ;
   - toutes les **données du jeu** téléchargées (`donnees-jeu/`, dont `crafting_recipe_desc`, `building_type_desc`, `item_conversion_recipe_desc`) : lues par le livre ;
   - leurs tests (`RecipePlannerTests`, `RawMaterialsTests`, `V0314Tests`…).
3. Quelques petites fonctions du livre (recherche par nom, résumé d'une recette) ne servent plus qu'aux tests : gardées (classe partagée, retrait sans gain réel).
4. L'onglet ouvert au démarrage n'est pas gardé dans les réglages : rien à changer (le bot s'ouvre sur BOT).

## Conséquences
- Tests 653/653 (4 nouveaux dans `V0426Tests`). Essai sans le jeu : livre chargé (9 322 objets), inventaires reçus, aucune erreur.
