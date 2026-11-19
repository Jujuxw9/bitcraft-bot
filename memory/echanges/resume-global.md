# Résumé global (à jour au 2026-11-19, session 14, v0.3.19)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Versions dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`) et `artifacts/old` (D-040). Raccourci bureau « BitCraft Bot » = dernière version. Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : **plus de touche Num 5** (D-047) ; bouton Arrêter, souris bougée, pause 3 s quand Julien tape, durée max. Repas automatique Maj + E actif à chaque démarrage (D-042). Captures en local (D-026).
5. État : **v0.3.19 dans le code, .exe pas encore fabriqué** (pas de Release ; fabrication locale à faire avec l'accord de Julien). Journal : passages de niveau et tâches (début, fin : durée, coups, XP, objets) lus sur le serveur. L'écran ne lit plus que position, zone, région, cible. Moins de CPU (onglet affiché seulement, capture ralentie). Historique : `technique/hybride/architecture-serveur.md` (sections 17 à 32).
6. Constats clés : bot connecté **avant** le jeu, jamais après (sinon jeu éjecté) ; positions ±1 case ; choix d'arbre (D-036) ; Maker's Tree interdit.
7. **Voie hybride (D-039)** et **serveur d'abord (D-044)** : tout prendre sur le serveur, l'écran seulement pour ce qui manque. Inventaire : `technique/hybride/donnees-serveur.md`.
8. À faire par Julien : confirmer les remplaçants de Num 5 (D-047), essais v0.3.18 et v0.3.19 (`echanges/2026-11-19-essai-v0319.md`).
9. Ensuite : **H3** (aller au meilleur arbre, avec le contrôleur d'actions), puis H4 (boucle bois → Banque).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
