# Résumé global (à jour au 2026-11-16, session 08)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Dernier .exe validé dans `executable/` (D-020). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit (D-025). Captures en local (D-026).
5. État : **v0.3.4** (`artifacts/v0.3.4/`) : position lue 98-100 % (jour et nuit), jauge de métier fiable (Forestry, Foraging, Mining), données BitCraft_GameData vérifiées (D-033), capture du jeu avec **Num 0** (validée), images de diagnostic, faim confirmée sur 4 lectures, montée de niveau gérée (Q-018).
6. Constats clés : survol = contour vert seulement ; choix d'arbre = plus grand contour, clic droit, lire le nom, annuler si jeune (D-036) ; panneau d'action en bas pendant la coupe ; barre active = point blanc à droite des flèches ; Maker's Tree interdit.
7. Jeu mis à jour le 16/11 à 17:51 (build 26707176) sans note. Réglages de Julien : heure en 24 h, carte sans animation. Patch notes : API Steam (`technique/sources.md`).
8. À faire par Julien : répondre à **Q-020** (exploration bêta : rayon, durée, ennemi, captures d'eau) ; refaire A, B, C du lot 6 (les 5 captures de 20:10 ne les montrent pas) ; dire s'il faut pousser les **6 commits locaux** en attente (209ec8a à a5ea9a1 et suivants).
9. Plan : prochaine version = onglet **« Bêta : exploration »** (Q-020 : herbe seulement, survol avant clic pour refuser tout contour vert, éviter l'eau, minicarte, W), puis v0.4 coupe d'un vieil arbre (D-036). Mouvements avec un but (D-037). Repas : D-032 (Shift + E à vérifier).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
