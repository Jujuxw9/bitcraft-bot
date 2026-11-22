# Session 17 — 2026-11-22 — Analyse de l'essai R1, ce que voit le bot (v0.4.1), agent mémoire

**Agent** : Claude Code

## Objectif
Analyser l'essai R1 de Julien, noter ses réponses (Q-024), reconnaître les écrans (bureau, autre programme, accueil, connexion, chargement, jeu), analyser la fiabilité de la récolte automatique, vérifier la mémoire et créer un agent mémoire.

## Réalisé
- **Analyse R1** (`technique/hybride/analyse-visee-2026-11-22.md`) : la formule de la caméra ne marche pas. « Précision 18 px » trompeuse (4 clics gardés sur 17), écarts réels 148 à 3 375 px. Causes : W ne fixe pas la direction de la caméra dans le monde ; vue très rapprochée, clics sur le feuillage ; arbres qui se cachent. Trouvé : mini-carte orientée nord avec un cône qui montre la direction de la caméra ; contour vert autour de l'objet survolé. Méthode fiable proposée : viser, vérifier, cliquer, vérifier.
- **D-050** (réponses de Julien à Q-024) : bouton du milieu tourne la vue, W revient en douceur (3 s), W remet le zoom.
- **v0.4.1** : ce que voit le bot (fil à part, toujours actif), actions bloquées hors du jeu, 3 s après W, précision « pas fiable ». 300 tests, banc hors écran. .exe local, sans Release GitHub.
- **Agent mémoire** : `tools/agent-memoire/` (prompt pour Sonnet, règles, script de vérification). Script lancé : D-041 (titre) et Q-001 (statut) corrigés. Vérification de sens par un agent Sonnet (lecture seule).
- **Q-025** créée (survol, touche d'interaction, cône, écrans d'accueil et de connexion, zoom).

## Écarts notés
- Les mots-clés des écrans d'accueil et de connexion sont supposés (aucune capture) : la v0.4.1 garde une capture de chaque écran pour corriger.
- Le 22/11 à 20:51, le serveur a refusé la liaison (global et région), puis l'a acceptée 70 s plus tard : cause inconnue.
- Les captures avec chemin à barres « / » font échouer le décodeur d'images de Windows (tests) : utiliser des chemins Windows complets.

## Décisions prises
- D-050.

## Questions ouvertes créées
- Q-025.

## Prochaines étapes
- Essai v0.4.1 (`echanges/2026-11-22-essai-v041.md`), réponses à Q-025.
- R1 bis : lire la direction de la caméra sur la mini-carte (v0.4.2, aucune action).
