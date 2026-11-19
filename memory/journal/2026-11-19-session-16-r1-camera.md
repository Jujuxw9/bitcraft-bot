# Session 16 — 2026-11-19 — v0.4.0 : R1, le bot apprend la caméra

**Agent** : Claude Code

## Objectif
Étape R1 du plan de la récolte automatique (D-049) : apprendre la formule de la caméra en regardant Julien couper, sans aucune action.

## Réalisé
- Écoute de la souris en lecture seule, sur son propre fil (clics droits, molette, bouton du milieu).
- État de la caméra d'après W / Q / D / molette ; apprentissage des paires « clic droit → récolte confirmée par le serveur ».
- Formule de la caméra (plate ou perspective), faux indices écartés, précision mesurée honnêtement (chaque clic prévu sans lui).
- Carte « Caméra » dans Paramètres ; clics gardés dans `camera-clics.json`.
- 277 tests (14 nouveaux), banc hors écran OK. .exe local `artifacts/release/BitcraftBot-v0.4.0.exe`, sans Release GitHub.
- Détail : `technique/hybride/architecture-serveur.md`, section 34. Essai : `echanges/2026-11-19-essai-v040.md`.

## Écarts notés
- L'outil Bash de Claude ne reçoit pas les événements de souris (bac à sable) ; PowerShell oui.
- Windows ignore une molette de 0 cran : pas de test de bout en bout de l'écoute sans toucher l'écran de Julien (il utilisait son PC). Testé : installation, décodage d'un clic, réception de vrais mouvements (sonde).
- Pas d'essai en jeu (Julien sur Discord, jeu fermé).
- Pas de Python sur le PC : éditions par l'outil d'édition.

## Décisions prises
- Aucune nouvelle. Choix techniques : clics acceptés aussi quand la caméra est « inconnue » (pas de W vu depuis le démarrage), comme indices ; la formule écarte les faux.

## Questions ouvertes créées
- Q-024 : bouton du milieu, retour de W, molette.

## Prochaines étapes
- Essai de Julien (R1). Si précision < 25 px : R2 (dessiner les arbres du serveur sur l'image du jeu, v0.4.1).
