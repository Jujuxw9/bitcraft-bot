# Résumé global (à jour au 2026-11-18, session 10, v0.3.10)

1. Projet : bot autonome pour BitCraft Online (lecture du serveur + écran, actions clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Versions dans `artifacts/release` et `artifacts/old`, script `tools/publier-version.ps1` (D-040). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit (D-025). Captures en local (D-026).
5. État : **v0.3.10** (`artifacts/release/`, D-040 ; anciennes versions dans `artifacts/old/`) : lecture du HUD (v0.3.4) + onglet **« Serveur (H1) »** : jeton par code e-mail (chiffré), lecture seule de R18 (`idc-18`) : position, vie, endurance, faim, ≈ 700 ressources. **Lecture vérifiée en réel** (essais v0.3.7 et v0.3.8). Bot connecté avec le jeu ouvert = jeu éjecté : refusé depuis la v0.3.9. v0.3.10 : fenêtre agrandie, journal complet collé en bas.
6. Constats clés : survol = contour vert seulement ; choix d'arbre = plus grand contour, clic droit, lire le nom, annuler si jeune (D-036) ; panneau d'action en bas pendant la coupe ; barre active = point blanc à droite des flèches ; Maker's Tree interdit.
7. **Voie hybride prioritaire (D-039)** : lire le serveur en lecture seule (jeton de Julien), agir au clavier/souris. Brique `src/BitcraftBot.Server`, plan dans `technique/hybride/`. Vision = voie B (en pause).
8. À faire par Julien : utiliser `artifacts/release/BitcraftBot.exe` (jeu fermé → « Se connecter » → lancer le jeu). Vérifier que l'arbre coupé apparaît dans la liste (filtre arbres). Liens : `technique/liens-utilises.md`. Vidéo « lot 6 » : plus tard.
9. Ensuite : **H2** (coordonnées trouvées : E = arrondi(x ÷ 3), N = arrondi(z ÷ 3) ; reste la carte des arbres), H3 (aller à un arbre), H4 (boucle bois → Banque). Règle : bot connecté avant le jeu, jamais après ; pas de reconnexion automatique.
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
