# T-13 (09:00) — v0.4.31 : petits défauts

Règles communes : `nuit-2026-11-27-regles.md`. Lis le bilan (surtout T-11 : captures de la cible).

## À faire (trois sujets indépendants : finir chacun avant le suivant)
1. **Cible lue à l'écran** : depuis au moins la v0.4.22, le journal dit « Perception : cible aucune » en permanence, même pendant une coupe (journaux v0.4.22 à v0.4.24). Cette lecture (nom + santé de la cible en haut de l'écran, OCR, `BitcraftBot.Perception/Hud`) servait à l'onglet CARTE (« Cible écran ») et au journal. Trouver pourquoi : zone de lecture déplacée ? interface du jeu changée ? Utiliser les captures de T-11 (`images/analyse/20261127_essai-deplacement_*`) et celles des 22-25/11 (`images/analyse/`), les tests existants sur images. Corriger avec un test sur une vraie capture. Si aucune capture récente ne montre de cible : écrire précisément ce qu'il faut à Julien (une capture « Num 0 » en coupant un arbre) et ne rien casser.
2. **Barre de fabrication des petites fabrications** : Julien n'est pas sûr qu'elle aille au bout pour les petites tâches (D-078 : progression lue dans `progressive_action_state`). Vérifier avec les journaux (fabrications courtes : « Fin : fabrication, 24 s, 19 étapes ») et le code (`MainWindow.Dashboard.cs`, `CraftDone`, maintien 2,5 s) ; corriger si la barre n'atteint pas 100 % avant de disparaître. Test.
3. **Pêche ou minage sans capacités** (consigne de Julien, D-079) : quand le journal des tâches voit une **pêche** ou un **minage** (métiers Fishing, Mining), et que les capacités qui vont avec (Heavy Swings 8, Break the Roots 0, Reel In 5, Let It Run 6 ; voir `AbilityPlanner`) ne sont **jamais parties** pendant la tâche, écrire en fin de tâche une ligne « [Conseil] » dans le journal : « Minage de 3 min sans Heavy Swings : pensez à l'allumer (Capacités auto) ». La session principale la lira dans les journaux de Julien. Test.

## Fini quand
- v0.4.31 publiée, tests verts, essai sans le jeu OK, bilan avec le résultat de chaque sujet (corrigé / cause trouvée mais pas corrigée / besoin de Julien).
