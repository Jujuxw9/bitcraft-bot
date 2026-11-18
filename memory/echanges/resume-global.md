# Résumé global (à jour au 2026-11-17, session 09)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Dernier .exe validé dans `executable/` (D-020). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit (D-025). Captures en local (D-026).
5. État : **v0.3.4** (`artifacts/v0.3.4/`) : position lue 98-100 % (jour et nuit), jauge de métier fiable (Forestry, Foraging, Mining), données BitCraft_GameData vérifiées (D-033), capture du jeu avec **Num 0** (validée), images de diagnostic, faim confirmée sur 4 lectures, montée de niveau gérée (Q-018).
6. Constats clés : survol = contour vert seulement ; choix d'arbre = plus grand contour, clic droit, lire le nom, annuler si jeune (D-036) ; panneau d'action en bas pendant la coupe ; barre active = point blanc à droite des flèches ; Maker's Tree interdit.
7. **Voie hybride prioritaire (D-039)** : lire le serveur en lecture seule (jeton de Julien), agir au clavier/souris. Brique `src/BitcraftBot.Server`, plan dans `technique/hybride/`. Vision = voie B (en pause).
8. À faire par Julien : répondre aux questions H1 (compte Steam ou e-mail, région). Vidéo « lot 6 » et captures : plus tard.
9. Prochain livrable : **H1 — Lecture serveur** (onglet Serveur : connexion par code e-mail, position, vie, endurance, faim, arbres proches). Point critique : lancer le bot avant le jeu (connexion la plus récente gagne). Ensuite : bêta exploration (D-038), v0.4 coupe (D-036).
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
