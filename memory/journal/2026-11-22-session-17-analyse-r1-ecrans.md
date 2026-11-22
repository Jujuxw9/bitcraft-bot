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

## Vérification de la mémoire (agent Sonnet, lecture seule)
- Script : 0 erreur (D-041, Q-001 corrigés ; `echanges/2026-11-15-prompt-init-repo.md` fait 304 lignes, archive laissée telle quelle).
- Corrigé (notes datées, sens inchangé) : D-004, D-018, D-022, D-038, D-040, D-042, D-044, Q-002, Q-022 ; titre de D-045 dans l'index ; essais v0.3.14, v0.3.15, v0.3.18 marqués « archive » ; en-têtes de `architecture.md` (état v0.4.1, sections 1 à 16 historiques) et `architecture-serveur.md`.
- **À trancher par Julien** :
  1. Pas de Release GitHub : seulement dans CLAUDE.md, aucune décision ; D-010 dit encore « publiées en GitHub Releases ». Faire une décision D-051 ? « Jamais » ou « pour le moment » ?
  2. D-018 (position lue à l'écran) : la passer « Remplacée par D-048 » ?
  3. D-038 (onglet « Bêta : exploration ») : jamais codé. Abandonné, reporté ou à faire après la récolte ?
  4. Q-008 (déconnexion et cadence), Q-014 (ennemis), Q-015 (fenêtres du jeu) : toujours ouvertes ; Q-015 devient importante pour la récolte auto.
- Règles B9 à B12 ajoutées à l'agent mémoire.

## Suite de la session (réponses de Julien)
- Réponses : panneau au survol NON, touche d'interaction NON, cône = caméra OUI, captures OK, vue plus haute ?, Release GitHub JAMAIS, D-018 remplacée OUI, onglet Bêta ?, fenêtres du jeu à détecter OUI.
- **D-051** (jamais de Release GitHub ; D-010 remplacée en partie), **D-018 → remplacée par D-048**, **D-052** (visée vérifiée + fenêtres détectées avant toute action). Q-015 et Q-025 en partie résolues. D-038 laissée telle quelle (Julien ne sait pas).
- **Réglages du jeu lus** (registre, lecture seule) : `technique/reglages-du-jeu.md`. 4 réglages conseillés à changer (Q-026) : collision de la caméra, caméra adoucie, coffres ouverts seuls, herbe. Le registre contient aussi le jeton et l'e-mail : jamais recopiés.
- **v0.4.2** : vérification des réglages au démarrage (liste blanche). 303 tests, banc hors écran. .exe local.
