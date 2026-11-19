# Résumé global (à jour au 2026-11-19, session 16, v0.4.0)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : récolter (arbres d'abord, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Chaque version : .exe local dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`), **jamais de Release GitHub** ; anciennes dans `artifacts/old` (D-040). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : pas de touche d'arrêt (D-047) ; bouton Arrêter, souris bougée, pause 3 s quand Julien tape, durée max. Repas auto Maj + E actif au démarrage (D-042).
5. État : **v0.4.0 = R1** : le bot apprend la caméra en regardant Julien couper (écoute de la souris en lecture seule, clic droit + récolte confirmée par le serveur → formule de la caméra, carte « Caméra » dans Paramètres, `camera-clics.json`). Aucune action nouvelle. Détail : `technique/hybride/architecture-serveur.md` section 34.
6. Constats clés : bot connecté **avant** le jeu, jamais après (sinon jeu éjecté) ; décoder des images WebP en parallèle bloque tout ; l'outil Bash de Claude ne reçoit pas la souris (tester les écoutes via PowerShell).
7. **Voie hybride (D-039)** et **serveur d'abord (D-044)**. Inventaire : `technique/hybride/donnees-serveur.md`.
8. **Prochaine étape : essai R1 de Julien** (`echanges/2026-11-19-essai-v040.md` : 10 à 15 arbres coupés à la main, réussi si précision < 25 px). Puis R2 (dessiner les arbres du serveur sur l'image du jeu, v0.4.1). Plan : `echanges/2026-11-19-plan-recolte-auto.md` (D-049).
9. Question ouverte : Q-024 (bouton du milieu, retour de W, molette). v0.3.20 jamais essayée en réel : surveiller l'image du jeu et les icônes.
10. Plan des versions : la récolte automatique = v0.4.x (R1 à R7), la Banque = v0.5.
