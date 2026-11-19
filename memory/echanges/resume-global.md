# Résumé global (à jour au 2026-11-19, session 15, v0.3.20)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : récolter (arbres d'abord, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Chaque version : .exe local dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`), **jamais de Release GitHub** ; anciennes dans `artifacts/old` (D-040). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : pas de touche d'arrêt (D-047) ; bouton Arrêter, souris bougée, pause 3 s quand Julien tape, durée max. Repas auto Maj + E actif au démarrage (D-042).
5. État : **v0.3.20**. Serveur : tout (position, vie, endurance, faim, sac, bonus, métiers, action, ressources). Journal : niveaux et tâches (vérifié en réel v0.3.19). Écran : **cible sélectionnée seulement** (D-048). v0.3.20 répare l'image du jeu et les icônes (fils bloqués par le décodage des images). Historique : `technique/hybride/architecture-serveur.md` (sections 17 à 33).
6. Constats clés : bot connecté **avant** le jeu, jamais après (sinon jeu éjecté) ; décoder des images WebP en parallèle bloque tout (une par une).
7. **Voie hybride (D-039)** et **serveur d'abord (D-044)**. Inventaire : `technique/hybride/donnees-serveur.md`.
8. **Prochaine étape : récolte automatique**, plan en 7 étapes R1 à R7 : `echanges/2026-11-19-plan-recolte-auto.md`. R1 = apprendre la caméra en regardant Julien couper (aucune action). Questions : Q-023.
9. À faire par Julien : essai v0.3.20 (image du jeu, icônes), répondre à Q-023.
10. Plan des versions : feuille de route (`2026-11-16-feuille-de-route.md`) ; la récolte automatique devient v0.4.x, la Banque v0.5.
