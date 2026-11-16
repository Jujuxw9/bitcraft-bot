# Résumé global (à jour au 2026-11-16, session 08)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Dernier .exe validé dans `executable/` (D-020). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit (D-025). Captures en local (D-026).
5. État : **v0.3.0 codée** (`artifacts/v0.3.0/`) : coordonnées lues par modèles de chiffres (60/60 en direct, ~1 ms), données du jeu BitCraft_GameData téléchargées et vérifiées par le bot (D-033 validée). v0.2.2 testée par Julien : mouvements OK.
6. Constats clés : survol = contour vert seulement, pas le nom (Q-019) ; pendant la coupe, panneau d'action en bas (« Chop », coups, 1,59 s) ; la coupe s'arrête à 4/100 d'endurance ; Maker's Tree à ne jamais couper.
7. Le jeu a été mis à jour le 16/11 à 17:51 (build 26707176) sans note. Patch notes : API Steam (voir `technique/sources.md`).
8. À faire par Julien : test T-20261116-04 (v0.3.0, dont de nuit) ; ressenti sur les mouvements ; Q-019 ; captures toujours en **écran complet** (Impr. écran).
9. Plan : suite v0.3 = panneau d'action du bas, notifications (Q-018), repérage des vieux arbres (D-034, Q-019). Repas : D-032. Écorce seulement (D-027), endurance pleine avant relance (D-028).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
