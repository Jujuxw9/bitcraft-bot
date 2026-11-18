# Résumé global (à jour au 2026-11-18, session 10, v0.3.12)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Versions dans `artifacts/release` et `artifacts/old`, script `tools/publier-version.ps1` (D-040). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit (D-025). Captures en local (D-026).
5. État : **v0.3.12 (H2)** (`artifacts/release/`, D-040 ; anciennes versions dans `artifacts/old/`) : lecture du HUD (v0.3.4) + onglet **« Serveur (H1) »** : jeton par code e-mail (chiffré), lecture seule de R18 (`idc-18`) : position, vie, endurance, faim, ≈ 700 ressources. **Lecture vérifiée en réel** (essais v0.3.7 et v0.3.8). Bot connecté avec le jeu ouvert = jeu éjecté : refusé depuis la v0.3.9. v0.3.10 : fenêtre agrandie, journal complet collé en bas. v0.3.11 : **carte des arbres** (santé, âge, distance, direction, N / E ±1), essai réel OK. v0.3.12 : onglet « Carte (H2) » séparé, filtres par famille, cible à l'écran stable.
6. Constats clés : survol = contour vert seulement ; choix d'arbre = plus grand contour, clic droit, lire le nom, annuler si jeune (D-036) ; panneau d'action en bas pendant la coupe ; barre active = point blanc à droite des flèches ; Maker's Tree interdit.
7. **Voie hybride prioritaire (D-039)** : lire le serveur en lecture seule (jeton de Julien), agir au clavier/souris. Brique `src/BitcraftBot.Server`, plan dans `technique/hybride/`. Vision = voie B (en pause).
8. À faire par Julien : **essai de la v0.3.12** (`echanges/2026-11-18-essai-h2.md`, fin du fichier) : filtres par famille, cible « c'est bien elle » stable. Essai long (30 min) quand possible. Vidéo « lot 6 » : plus tard.
9. Ensuite : corrections H2 après l'essai, puis **H3** (aller au meilleur arbre), H4 (boucle bois → Banque). Règle : bot connecté avant le jeu, jamais après ; pas de reconnexion automatique.
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
