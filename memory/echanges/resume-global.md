# Résumé global (à jour au 2026-11-16)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Dernier .exe validé dans `executable/` (D-020). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit gérés (D-025). Captures en local (D-026).
5. État : squelette .NET qui compile, aucune logique de bot. Lots 1 et 2 de captures analysés (96 images, session 04).
6. Constats clés : contour vert au survol ; clic droit = aller + récolter ; panneau cible lisible (nom, santé) ; message « inventaire plein » repéré (Q-001).
7. Commandes validées : tout à la souris, ZQSD pour tourner la caméra si besoin, W pour la recentrer (D-024). Piste données : Bitjita + exports Hexel (Q-004).
8. Captures restantes (facultatives) : M3 à M9 → [analyse des captures](../technique/analyse-captures-2026-11-16.md), section 12.
9. Prochaine version : v0.1 = capture du jeu affichée dans la fenêtre de supervision + arrêt d'urgence (aucune action sur le jeu).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
