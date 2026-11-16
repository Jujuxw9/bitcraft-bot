# Résumé global (à jour au 2026-11-16)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Dernier .exe validé dans `executable/` (D-020). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit gérés (D-025). Captures en local (D-026).
5. État : **v0.1.0 validée** (dans `executable/`). **v0.2.2 codée** (session 07) : lecture du HUD, onglets Journal / Système / Tests, surveillance CPU/RAM, banc d'essai des mouvements humains (désactivé par défaut). Limite : le 2 des coordonnées mal lu par l'OCR (correctif prévu, Q-016).
6. Constats clés : contour vert au survol (même de loin) ; clic droit = aller + récolter ; panneau cible lisible ; arbre = écorce + tronc (Q-013) ; endurance vide en ~9 min, +15/min.
7. Commandes validées : tout à la souris, ZQSD pour tourner la caméra si besoin, W pour la recentrer (D-024). Piste données : Bitjita + exports Hexel (Q-004).
8. À faire par Julien (prochaine fois) : tests T-20261116-02 (lecture, dont nuit) et T-20261116-03 (mouvements) avec `artifacts/v0.2.2/BitcraftBot.exe` ; lot 5 de captures ; réponse à D-033.
9. Plan : v0.3 = lecture fiable (modèles de chiffres Q-016, notifications Q-018) + repérer les vieux arbres (D-034). Lot 5 de captures demandé. Repas : D-032. Récolte : écorce seulement (D-027), endurance à 100 % avant relance (D-028).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
