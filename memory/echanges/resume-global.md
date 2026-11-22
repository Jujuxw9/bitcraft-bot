# Résumé global (à jour au 2026-11-22, session 17, v0.4.1)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : récolter (arbres d'abord, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Chaque version : .exe local dans `artifacts/release` (`BitcraftBot-vX.Y.Z.exe`), **jamais de Release GitHub** ; anciennes dans `artifacts/old` (D-040). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : pas de touche d'arrêt (D-047) ; bouton Arrêter, souris bougée, pause 3 s quand Julien tape, durée max ; **aucune action hors de l'écran « en jeu »** (v0.4.1). Repas auto Maj + E (D-042).
5. État : **v0.4.1** : le bot dit ce qu'il voit (bureau, autre programme, jeu réduit, accueil, connexion, chargement, en jeu) ; apprentissage de la caméra (R1) en lecture seule. Détail : `technique/hybride/architecture-serveur.md` sections 34 et 35.
6. **Essai R1 (22/11) : KO.** W ne fixe pas la direction de la caméra dans le monde ; la mini-carte (orientée nord) montre la direction par un cône ; le jeu entoure en vert l'objet survolé. Méthode fiable « viser, vérifier, cliquer, vérifier » : `technique/hybride/analyse-visee-2026-11-22.md`.
7. Constats clés : bot connecté **avant** le jeu ; décoder des images WebP en parallèle bloque tout ; l'outil Bash de Claude ne reçoit pas la souris (tester via PowerShell). D-050 : W revient en 3 s, bouton du milieu = rotation.
8. **Prochaine étape** : essai v0.4.1 (`echanges/2026-11-22-essai-v041.md`) et réponses à Q-025 ; puis R1 bis (lire la direction de la caméra sur la mini-carte, v0.4.2), R1 ter, R2 à R7.
9. Mémoire : vérifier avec `tools/agent-memoire/verifier-memoire.ps1` ; agent mémoire Sonnet (`tools/agent-memoire/PROMPT.md`).
10. Plan des versions : la récolte automatique = v0.4.x, la Banque = v0.5.
