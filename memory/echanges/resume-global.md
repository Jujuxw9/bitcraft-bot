# Résumé global (à jour au 2026-11-18, session 10)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : couper des arbres (bois, D-021), déposer à la Banque de Snowrest quand l'inventaire est plein, en boucle (D-002, D-016).
3. Stack : C# / .NET 10, WPF, .exe unique (D-009, D-010). Dernier .exe validé dans `executable/` (D-020). Claude code tout (D-023).
4. Langue : français partout, sauf les lignes de code (D-019). Sécurité : `Num 5`, 15 min (D-022). Jour et nuit (D-025). Captures en local (D-026).
5. État : **v0.3.5 (H1)** (`artifacts/v0.3.5/`) : lecture du HUD (v0.3.4) + onglet **« Serveur (H1) »** : jeton par code e-mail (chiffré), lecture seule de R18 (`bitcraft-live-18`) : position, vie, endurance, faim, arbres proches. Testé par Claude sans jeton ; essai réel à faire.
6. Constats clés : survol = contour vert seulement ; choix d'arbre = plus grand contour, clic droit, lire le nom, annuler si jeune (D-036) ; panneau d'action en bas pendant la coupe ; barre active = point blanc à droite des flèches ; Maker's Tree interdit.
7. **Voie hybride prioritaire (D-039)** : lire le serveur en lecture seule (jeton de Julien), agir au clavier/souris. Brique `src/BitcraftBot.Server`, plan dans `technique/hybride/`. Vision = voie B (en pause).
8. À faire par Julien : **essai H1** (`echanges/2026-11-18-essai-h1.md`) : jeton, connexion bot puis jeu, test de déconnexion, renvoyer journal + instantané. Compte Steam avec e-mail lié, région R18. Vidéo « lot 6 » et captures : plus tard.
9. Ensuite : **H2** (conversion des coordonnées serveur / écran, carte des arbres), H3 (aller à un arbre), H4 (boucle bois → Banque). Point critique : bot connecté avant le jeu ; pas de reconnexion automatique.
10. Plan des versions v0.1 → v0.6 (MVP) : dans la [feuille de route](2026-11-16-feuille-de-route.md).
