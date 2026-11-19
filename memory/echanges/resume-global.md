# Résumé global (à jour au 2026-11-19, session 13, v0.3.18)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Versions dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`) et `artifacts/old` (D-040). Raccourci bureau « BitCraft Bot » = dernière version (`tools/lancer-bot.ps1`). Tag `vX.Y.Z` = Release GitHub. Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : **Num 5 toujours actif** quand BitCraft est actif (D-041, D-045), durée max, arrêt si la souris bouge. Repas automatique Maj + E **actif à chaque démarrage** (D-042). Captures en local (D-026).
5. État : **v0.3.18**. **Contrôleur d'actions commun** (D-045) : toutes les touches et clics passent par les mêmes sécurités. **Thème BitCraft**, peu de texte, fenêtre adaptative (D-046). Déconnexion du serveur garantie à la fermeture. Lecture du serveur en lecture seule (R18, `idc-18`) : position, vie, endurance, faim, ressources, sac et banques, bonus, métiers (formats et % vérifiés), action en cours. Historique : `technique/hybride/architecture-serveur.md` (sections 17 à 31).
6. Constats clés : bot connecté **avant** le jeu, jamais après (sinon jeu éjecté) ; positions ±1 case ; choix d'arbre (D-036) ; Maker's Tree interdit.
7. **Voie hybride (D-039)** et **serveur d'abord (D-044)** : tout prendre sur le serveur, l'écran seulement pour ce qui manque. Inventaire : `technique/hybride/donnees-serveur.md`.
8. **Passation** : `echanges/2026-11-19-passation-session-14.md` (Num 5 à supprimer, niveaux et tâches dans le journal, lecture écran de vie/endurance/faim/niveaux à retirer, optimisations). Essai v0.3.18 à faire par Julien. Pas de Release pour le moment : push seulement.
9. Ensuite : **H3** (aller au meilleur arbre, avec le contrôleur d'actions), puis H4 (boucle bois → Banque).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
