# Résumé global (à jour au 2026-11-22, session 17, v0.4.2)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : récolter (arbres d'abord, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Chaque version : .exe local dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`), **jamais de Release ni de tag GitHub** (D-051) ; anciennes dans `artifacts/old` (D-040). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : pas de touche d'arrêt (D-047) ; bouton Arrêter, souris bougée, pause 3 s quand Julien tape, durée max ; **aucune action hors de l'écran « en jeu »**. Repas auto Maj + E (D-042).
5. État : **v0.4.2** : réglages du jeu vérifiés au démarrage (lecture seule, liste blanche) ; le bot dit ce qu'il voit (bureau, autre programme, jeu réduit, accueil, connexion, chargement, en jeu) ; apprentissage de la caméra (R1) en lecture seule. Détail : `technique/hybride/architecture-serveur.md` sections 34 à 36.
6. **Essai R1 (22/11) : KO** : W met la caméra derrière le personnage (direction variable). Méthode retenue (D-052) : direction lue sur le cône de la mini-carte (toujours orientée nord), contour vert au survol, confirmation par le serveur, fenêtres du jeu détectées avant toute action. Analyse : `technique/hybride/analyse-visee-2026-11-22.md`.
7. Constats clés : bot connecté **avant** le jeu ; décoder des images WebP en parallèle bloque tout ; l'outil Bash de Claude ne reçoit pas la souris (tester via PowerShell). D-050 : W revient en 3 s. Réglages du jeu : `technique/reglages-du-jeu.md` (le registre contient aussi le jeton : ne jamais le lire).
8. **Prochaine étape** : essai v0.4.2 (`echanges/2026-11-22-essai-v041.md`), réponse à Q-026 (4 réglages du jeu) ; puis v0.4.3 : direction de la caméra (cône) et fenêtres ouvertes, sans action.
9. Questions en attente : Q-026, Q-025 (captures accueil / connexion, vue plus haute), D-038 (onglet Bêta : Julien ne sait pas). Mémoire : `tools/agent-memoire/` (script + agent Sonnet).
10. Plan des versions : la récolte automatique = v0.4.x, la Banque = v0.5.
