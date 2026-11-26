# T-06 (02:00) — v0.4.26 : onglet Recettes retiré

Règles communes : `nuit-2026-11-27-regles.md`. Lis la section T-05 du bilan avant de commencer.

## Pourquoi
Les recettes ont maintenant leur propre site (session « site des recettes », D-077, dossier `website-recipes/` : **n'y touche pas**). Julien (27/11) : « il faut supprimer l'onglet Recettes du bot, il ne sert plus à rien ». **Très important (Julien) : tout ce qui sert ailleurs est gardé.** Seul l'onglet et ce qui ne sert qu'à lui partent.

## À faire
1. **Carte des dépendances d'abord** (à écrire dans ton journal) : chercher tout ce qui touche aux recettes : `MainWindow.Recipes.cs`, `RecipeView.cs`, `RecipeBook`, planificateur (`RecipePlanner`, matières premières `RawMaterials`…), XAML de l'onglet, réglages, tests (`RecipePlannerTests.cs`, `RawMaterialsTests.cs`…). Pour chaque élément : « sert seulement à l'onglet » ou « sert ailleurs : où ».
   Déjà connus comme **servant ailleurs, à garder** : tiers et images des objets (onglet Personnage : sac, ceinture, banques), `RecipeBook.CraftActions` (barre de fabrication du tableau de bord, D-078), noms des objets (`ItemName`), données du jeu chargées (`donnees-jeu/`), tâches de voyageur (onglet Voyageurs), journal des tâches.
2. Retirer l'onglet (bouton du menu, page XAML, code), le planificateur et ses tests **seulement s'ils ne servent qu'à l'onglet**. Si une classe sert aux deux, garder la partie utile, retirer le reste.
3. Vérifier que le bot démarre et que les onglets restants marchent (essai sans le jeu : onglets Personnage avec images et tiers, tableau de bord). Regarder aussi `MainWindow.Layout.cs` / `MainWindow.View.cs` (liste des onglets, onglet ouvert au démarrage gardé dans les réglages : si c'était « Recettes », ouvrir le tableau de bord).
4. Mémoire : décision (retrait de l'onglet, ce qui est gardé et pourquoi), `architecture-suite.md`, `memory/technique/architecture.md` si l'onglet y est décrit.

## Fini quand
- Plus d'onglet Recettes ; images, tiers, barre de fabrication et voyageurs toujours là ; tests verts (tests des parties gardées conservés) ; v0.4.26 publiée ; essai sans le jeu OK.

## Interdit
- Toucher à `website-recipes/` ou `memory/site-recettes/`. Retirer des données du jeu chargées au démarrage si quelque chose d'autre les lit.

## Si la tâche d'avant a raté
- T-05 ne touche pas aux recettes : travailler normalement, à partir de la dernière version publiée.
