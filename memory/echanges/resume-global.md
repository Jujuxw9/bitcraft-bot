# Résumé global (à jour au 2026-11-16)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Dernier .exe validé dans `executable/` (D-020). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit gérés (D-025). Captures en local (D-026).
5. État : **v0.1.0 codée** (session 05) : jeu affiché en direct, arrêt Num 5, durée max, journaux. Aucune action sur le jeu. 152 captures analysées.
6. Constats clés : contour vert au survol (même de loin) ; clic droit = aller + récolter ; panneau cible lisible ; arbre = écorce + tronc (Q-013) ; endurance vide en ~9 min, +15/min.
7. Commandes validées : tout à la souris, ZQSD pour tourner la caméra si besoin, W pour la recentrer (D-024). Piste données : Bitjita + exports Hexel (Q-004).
8. À faire par Julien : test [T-20261116-01](../tests/T-20261116-01-v01-affichage.md) avec `artifacts/v0.1.0/BitcraftBot.exe`, puis validation (D-020).
9. Prochaine version : v0.2 = lecture du HUD (coordonnées, endurance, vie, faim, panneau cible). Récolte : écorce seulement (D-027), endurance à 100 % avant relance (D-028).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
