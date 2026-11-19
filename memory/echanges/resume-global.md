# Résumé global (à jour au 2026-11-19, session 12, v0.3.17)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Versions dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`) et `artifacts/old`, script `tools/publier-version.ps1` (D-040). Tag `vX.Y.Z` poussé = Release GitHub. Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5` seulement quand BitCraft est actif (D-022, D-041), 15 min. Repas automatique Maj + E (D-042, désactivé par défaut, vérifié en réel). Jour et nuit (D-025). Captures en local (D-026).
5. État : **v0.3.17**. Lecture du serveur en lecture seule (R18, `idc-18`, jeton par code e-mail, vérifiée en réel) : position, vie, endurance, faim, ressources autour, sac et banques, bonus, expérience des métiers, action en cours, ressource frappée. Écran en contrôle (cible, menus). Onglets (D-043) : **Bot** (tableau de bord, image, repas auto, sécurité), Personnage, Carte, Recettes (avec images), Paramètres (+ journal). Historique des versions : `technique/hybride/architecture-serveur.md` (sections 17 à 30).
6. Constats clés : bot connecté **avant** le jeu, jamais après (sinon jeu éjecté) ; positions ±1 case ; formats réels des poches et des bonus vérifiés ; choix d'arbre (D-036) ; Maker's Tree interdit.
7. **Voie hybride (D-039)** et **serveur d'abord (D-044)** : tout prendre sur le serveur, l'écran seulement pour ce qui manque. Inventaire : `technique/hybride/donnees-serveur.md`.
8. À faire par Julien : essai de la v0.3.17 (`echanges/2026-11-19-essai-v0317.md`) : tableau de bord, métier et % comparés à la jauge du jeu. Vidéo « lot 6 » : plus tard.
9. Ensuite : contrôleur d'actions commun (toutes les touches et clics avec les mêmes sécurités), puis **H3** (aller au meilleur arbre), H4 (boucle bois → Banque). H3 en attente : Julien veut d'abord retravailler le bot.
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
