# Résumé global (à jour au 2026-11-23, session 18, v0.4.3)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : récolter (arbres d'abord, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Chaque version : .exe local dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`), **jamais de Release ni de tag GitHub** (D-051) ; anciennes dans `artifacts/old` (D-040). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : pas de touche d'arrêt (D-047) ; bouton Arrêter, souris bougée, pause 3 s quand Julien tape, durée max ; **aucune action hors de l'écran « en jeu » ni avec une fenêtre du jeu ouverte** (D-052). Repas auto Maj + E (D-042).
5. État : **v0.4.3** : en jeu, direction de la caméra lue sur le cône de la mini-carte, fenêtres du jeu vues (titre lu) ; écrans accueil / connexion / chargement / jeu reconnus ; réglages du jeu vérifiés au démarrage ; R1 note la direction avec chaque clic. Détail : `technique/hybride/architecture-serveur.md` sections 34 à 37.
6. Visée fiable (D-052) : direction de la caméra (cône), contour vert au survol, confirmation par le serveur, aucune fenêtre ouverte. L'essai R1 du 22/11 a montré que W met la caméra derrière le personnage (direction variable). Analyse : `technique/hybride/analyse-visee-2026-11-22.md`.
7. Constats clés : bot connecté **avant** le jeu ; une seule lecture de texte (OCR) à la fois ; l'outil Bash de Claude ne reçoit pas la souris (tester via PowerShell). Après W : 1 s (D-053). Réglages du jeu : `technique/reglages-du-jeu.md` (le registre contient aussi le jeton : ne jamais le lire).
8. **Prochaine étape** : essai v0.4.3 (`echanges/2026-11-23-essai-v043.md`) ; puis v0.4.4 : réapprendre la formule de la caméra avec la direction (R1 ter), puis R2 (arbres du serveur dessinés sur l'image).
9. Questions en attente : Q-026 (3 réglages du jeu restants), Q-025 (vue plus haute), D-038 (onglet Bêta : Julien ne sait pas). Mémoire : `tools/agent-memoire/` (script + agent Sonnet).
10. Plan des versions : la récolte automatique = v0.4.x, la Banque = v0.5.
